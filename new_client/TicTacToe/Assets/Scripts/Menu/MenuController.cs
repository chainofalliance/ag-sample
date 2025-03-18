using AllianceGamesSdk.Matchmaking;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using System;

using Buffer = Chromia.Buffer;

public class MenuController
{
    private readonly string DUID = null;
    private readonly string DISPLAY_NAME = "TicTacToe";

    // TODO: second queue for pve
    private readonly string QUEUE_NAME = "1Vs1";

    private readonly MenuView view;
    private readonly BlockchainConnectionManager connectionManager;
    private readonly AccountManager accountManager;
    private readonly Action<Uri, string> OnStartGame;
    private CancellationTokenSource updateCts;

    public MenuController(
        MenuView view,
        BlockchainConnectionManager connectionManager,
        AccountManager accountManager,
        Action<Uri, string> OnStartGame
    )
    {
        this.view = view;
        this.connectionManager = connectionManager;
        this.accountManager = accountManager;
        this.OnStartGame = OnStartGame;

        view.OnPlayPve += OnPlay;

        view.OnClickViewAllSessions += () =>
        {
            Application.OpenURL($"https://alliance-games-explorer.vercel.app/address/{accountManager.AddressWithoutPrefix}/sessions");
        };

        accountManager.OnAddressConnected += OnUpdatePlayerInfo;

        AutoPlayerInfoUpdate();
    }

    public void SetVisible(bool visible)
    {
        view.SetVisible(visible);
    }

    private async void OnUpdatePlayerInfo(string address)
    {
        var pointsEvm = await accountManager.TicTacToeContract.GetPoints(address);
        var res = await Queries.GetPlayerUpdate(connectionManager.TicTacToeClient, Buffer.From(address));
        view.SetPlayerUpdate(res, pointsEvm);
        view.SetAddress(address);
    }

    private async void OnPlay()
    {
        var cts = new CancellationTokenSource();

        var matchmakingService = MatchmakingServiceFactory.Get(
            connectionManager.AlliancesGamesClient, accountManager.SignatureProvider);
        var duid = DUID;
        duid ??= await MatchmakingService.GetDuid(connectionManager.AlliancesGamesClient, DISPLAY_NAME, cts.Token);

        RunMatchmaking(cts, matchmakingService, duid).Forget();

        try
        {
            var ticketId = await CreateMatchmakingTicket(matchmakingService, duid, cts.Token);
            if (string.IsNullOrEmpty(ticketId))
            {
                Debug.Log("Failed to get ticket ID");
                return;
            }

            var (sessionId, node) = await WaitForMatch(matchmakingService, ticketId, cts.Token);

            cts?.CancelAndDispose();
            view.CloseWaitingForMatch();

            var uriBuilder = new UriBuilder(node);
            if (uriBuilder.Host == "host.docker.internal")
            {
                uriBuilder.Host = "localhost";
            }

            Debug.Log($"Connecting to {uriBuilder.Uri}...");
            OnStartGame?.Invoke(uriBuilder.Uri, sessionId);
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
            QueueName = QUEUE_NAME
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
            QueueName = QUEUE_NAME
        }, ct);
    }

    private async UniTask<(string sessionId, string node)> WaitForMatch(
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

            Debug.Log($"Waiting for match with ticket ID {ticketId}...\nStatus: {ticket.Status}");
            await UniTask.Delay(1000, cancellationToken: ct);
        }

        Debug.Log($"Match found! Getting server details for match ID {sessionId}...");
        var node = await matchmakingService.GetConnectionDetails(new()
        {
            SessionId = sessionId
        }, ct);

        return (sessionId, node);
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

        async UniTask<string> Timer()
        {
            var count = 0;
            while (!cts.IsCancellationRequested)
            {
                await UniTask.Delay(1000, cancellationToken: cts.Token);
                view.UpdateMatchmakingTimer(++count);
            }
            return null;
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
                    OnUpdatePlayerInfo(accountManager.Address);
                }

                await UniTask.Delay(TimeSpan.FromMilliseconds(1000), cancellationToken: ct);
            }
        }
    }
}
