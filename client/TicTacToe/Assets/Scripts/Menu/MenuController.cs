using AllianceGamesSdk.Matchmaking;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using System;

using Buffer = Chromia.Buffer;
using System.Numerics;

public class MenuController
{
    public event Action<Queries.EifEventData[]> OnClaim;

    private readonly string DUID = null;
    private readonly string DISPLAY_NAME = "TicTacToe";

    private readonly string AI_QUEUE_NAME = "1Vs1";
    private readonly string PVP_QUEUE_NAME = "pvp";

    private readonly MenuView view;
    private readonly IMatchmakingService matchmakingService;
    private readonly BlockchainConnectionManager connectionManager;
    private readonly AccountManager accountManager;
    private readonly Action<Uri, string, Buffer> OnStartGame;
    private string duid;
    private Queries.EifEventData[] unclaimedRewards;
    private CancellationTokenSource timerCts;
    private CancellationTokenSource updateCts;

    public MenuController(
        MenuView view,
        BlockchainConnectionManager connectionManager,
        AccountManager accountManager,
        Action<Uri, string, Buffer> OnStartGame
    )
    {
        this.view = view;
        this.connectionManager = connectionManager;
        this.accountManager = accountManager;
        this.OnStartGame = OnStartGame;
        this.duid = DUID;

        matchmakingService = MatchmakingServiceFactory.Get(connectionManager.AlliancesGamesClient);

        view.OnPlayPve += OpenPvEMatchmaking;
        view.OnPlayPvp += OpenPvPMatchmaking;
        view.OnClaim += Claim;
        view.OnClickViewAllSessions += () =>
        {
            Application.OpenURL($"{Config.EXPLORER_URL}address/{accountManager.AddressWithoutPrefix}/sessions");
        };
        view.OnClickAddressLink += () =>
        {
            Application.OpenURL($"https://testnet.bscscan.com/address/{accountManager.Address}");
        };

        AutoPlayerInfoUpdate();
    }

    public void SetVisible(bool visible)
    {
        timerCts?.CancelAndDispose();
        timerCts = null;

        view.SetVisible(visible);
    }

    public void OpenPvEMatchmaking()
    {
        OnPlay(AI_QUEUE_NAME);
    }

    public void OpenPvPMatchmaking()
    {
        OnPlay(PVP_QUEUE_NAME);
    }

    public async UniTask UpdatePlayerInfo()
    {
        var address = accountManager.Address;
        await accountManager.Account.SyncBalance();
        var pointsEvm = await accountManager.TicTacToeContract.GetPoints(address);
        var tttUpdate = await Queries.GetPlayerUpdate(connectionManager.TicTacToeClient, Buffer.From(address));
        unclaimedRewards = await accountManager.GetUnclaimedEifEvents();

        duid ??= await MatchmakingService.GetDuid(connectionManager.AlliancesGamesClient, DISPLAY_NAME, CancellationToken.None);
        var playersInQueue = await matchmakingService.GetAmountTicketsInQueue(new()
        {
            Duid = duid,
            QueueName = PVP_QUEUE_NAME
        }, CancellationToken.None);

        var balanceString = accountManager.Account.Balance == BigInteger.Zero ? "0 (Get TBNB from faucet)" : accountManager.Balance;
        view.SetPlayersInQueue(playersInQueue);
        view.SetPlayerUpdate(tttUpdate, pointsEvm, balanceString, unclaimedRewards.Length > 0);
        view.SetAddress(address);
    }

    private void Claim()
    {
        if (unclaimedRewards.Length == 0)
        {
            Debug.Log("No unclaimed rewards");
            return;
        }

        OnClaim?.Invoke(unclaimedRewards);
    }

    private async void OnPlay(string queueName)
    {
        var cts = new CancellationTokenSource();

        RunMatchmaking(cts, matchmakingService, duid).Forget();

        try
        {
            view.SetMatchmakingStatus("Creating matchmaking ticket...");
            var ticketId = await CreateMatchmakingTicket(matchmakingService, duid, queueName, cts.Token);
            if (string.IsNullOrEmpty(ticketId))
            {
                Debug.Log("Failed to get ticket ID");
                view.ShowError("Matchmaking ticket closed", "Failed to create matchmaking ticket. Please try again.");
                view.CloseWaitingForMatch();
                timerCts?.CancelAndDispose();
                timerCts = null;
                return;
            }

            view.SetMatchmakingStatus("Waiting for a match...");
            var result = await WaitForMatch(matchmakingService, ticketId, cts.Token);
            cts?.CancelAndDispose();

            if (result == null)
            {
                view.CloseWaitingForMatch();
                timerCts?.CancelAndDispose();
                timerCts = null;
                return;
            }

            var (sessionId, coordinatorUrl, coordinatorPubkey) = result.Value;

            var uriBuilder = new UriBuilder(coordinatorUrl);
            if (uriBuilder.Host == "host.docker.internal")
            {
                uriBuilder.Host = "localhost";
            }

            Debug.Log($"Connecting to {uriBuilder.Uri}...");
            view.SetMatchmakingStatus("Connecting to the game...");
            view.DisableLeaveButton();
            OnStartGame?.Invoke(uriBuilder.Uri, sessionId, coordinatorPubkey);
        }
        catch (OperationCanceledException)
        { }
        catch (Exception e)
        {
            Debug.LogError($"Error while in matchmaking: {e.Message}");
        }
    }

    private async UniTask<string> CreateMatchmakingTicket(
        IMatchmakingService matchmakingService,
        string duid,
        string queueName,
        CancellationToken ct
    )
    {
        Debug.Log("Clearing pending tickets...");
        await matchmakingService.CancelAllMatchmakingTicketsForPlayer(new()
        {
            Identifier = Buffer.From(accountManager.Address),
            Duid = duid
        }, ct);

        Debug.Log("Creating ticket...");
        var response = await matchmakingService.CreateMatchmakingTicket(new()
        {
            Identifier = Buffer.From(accountManager.Address),
            NetworkSigner = accountManager.SignatureProvider.PubKey,
            Duid = duid,
            QueueName = queueName
        }, ct);

        if (response.Status != Chromia.TransactionReceipt.ResponseStatus.Confirmed)
        {
            Debug.Log("Creating ticket transaction got rejected " + response.RejectReason);
            return null;
        }

        return await matchmakingService.GetMatchmakingTicket(new()
        {
            Identifier = Buffer.From(accountManager.Address),
            Duid = duid,
            QueueName = queueName
        }, ct);
    }

    private async UniTask<(string sessionId, string coordinatorUrl, Buffer coordinatorPubkey)?> WaitForMatch(
        IMatchmakingService matchmakingService,
        string ticketId,
        CancellationToken ct
    )
    {
        string sessionId = null;
        while (!ct.IsCancellationRequested)
        {
            var ticket = await matchmakingService.GetMatchmakingTicketStatus(new()
            {
                TicketId = ticketId
            }, ct);

            if (!string.IsNullOrEmpty(ticket.SessionId))
            {
                sessionId = ticket.SessionId;
                break;
            }
            else if (ticket.Status == MatchmakingTicketState.Closed)
            {
                view.ShowError("Matchmaking ticket closed", "The matchmaking ticket has been closed. Please try again.");
                return null;
            }

            await UniTask.Delay(1000, cancellationToken: ct);
        }

        Debug.Log($"Match found! Getting server details for match ID {sessionId}...");
        var response = await matchmakingService.GetConnectionDetails(new()
        {
            SessionId = sessionId
        }, ct);

        return (sessionId, response.CoordinatorUrl, response.CoordinatorPubkey);
    }

    private async UniTaskVoid RunMatchmaking(
        CancellationTokenSource cts,
        IMatchmakingService matchmakingService,
        string duid
    )
    {
        view.OpenMatchmaking();

        Timer().Forget();

        if (!await view.OpenWaitingForMatch(cts.Token))
        {
            try
            {
                cts?.CancelAndDispose();
                timerCts?.CancelAndDispose();
                timerCts = null;
            }
            catch (ObjectDisposedException)
            { }

            view.CloseWaitingForMatch();

            await matchmakingService.CancelAllMatchmakingTicketsForPlayer(new()
            {
                Identifier = Buffer.From(accountManager.Address),
                Duid = duid
            }, CancellationToken.None);
        }
    }

    private async UniTaskVoid Timer()
    {
        timerCts?.CancelAndDispose();
        timerCts = new();
        var count = 0;
        while (!timerCts.IsCancellationRequested)
        {
            await UniTask.Delay(1000, cancellationToken: timerCts.Token);
            view.UpdateMatchmakingTimer(++count);
        }
    }

    private void AutoPlayerInfoUpdate()
    {
        updateCts?.CancelAndDispose();
        updateCts = new();

        UpdateInfo(updateCts.Token).Forget();

        async UniTaskVoid UpdateInfo(CancellationToken ct)
        {
            while (true)
            {
                if (view.IsVisible())
                {
                    await UpdatePlayerInfo();
                }

                await UniTask.Delay(TimeSpan.FromMilliseconds(1000), cancellationToken: ct);
            }
        }
    }
}
