using AllianceGamesSdk.Transport.Unity;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using AllianceGamesSdk.Client;
using System.Threading.Tasks;
using AllianceGamesSdk.Unity;
using Serilog.Sinks.Unity3D;
using System.Threading;
using System.Linq;
using UnityEngine;
using Serilog;
using System;

using Buffer = Chromia.Buffer;
using static Messages;

public class GameController
{
    public enum AfterGameAction
    {
        Menu,
        NextRoundPvE,
        NextRoundPvP,
    }

    public struct PlayerData
    {
        public string Address;
        public Messages.Field Symbol;
        public bool IsMe;
        public bool IsAI => Address == "0x0000000000000000000000000000000000000000";
    }

    public event Action OnClaim
    {
        add { view.OnClaim += value; }
        remove { view.OnClaim -= value; }
    }

    private readonly Messages.Field[,] board = new Messages.Field[3, 3];
    private readonly List<PlayerData> playerData = new List<PlayerData>();

    private readonly GameView view;
    private readonly AccountManager accountManager;
    private readonly BlockchainConnectionManager connectionManager;
    private readonly Action<AfterGameAction> onEndGame;

    private string sessionId;
    private AllianceGamesClient allianceGamesClient;
    private CancellationTokenSource cts;
    private UniTaskCompletionSource<int> turn = null;

    private CancellationTokenSource openGameResultCts = null;

    public GameController(
        GameView view,
        AccountManager accountManager,
        BlockchainConnectionManager connectionManager,
        Action<AfterGameAction> onEndGame
    )
    {
        this.view = view;
        this.accountManager = accountManager;
        this.connectionManager = connectionManager;
        this.onEndGame = onEndGame;

        view.OnClickField += idx =>
        {
            if (turn != null
                && !turn.Task.Status.IsCompleted()
                && board[idx % 3, idx / 3] == Messages.Field.Empty)
            {
                turn.TrySetResult(idx);
            }
        };

        view.OnClickBack += OpenCancelGame;
        view.OnClickViewInExplorer += () => OpenLinkToExplorer(sessionId);
    }

    public void SetVisible(bool visible)
    {
        view.SetVisible(visible);

        if (!visible)
        {
            openGameResultCts?.CancelAndDispose();
            openGameResultCts = null;
        }
    }

    public void OnSuccessfulClaim()
    {
        view.DisableClaimButton();
    }

    public async Task<bool> StartGame(Uri nodeUri, string matchId)
    {
        Debug.Log("StartGame");
        cts = new CancellationTokenSource();

        view.Reset();

        try
        {
            var logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Unity3D()
            .CreateLogger();

            var config = GetClientConfig(nodeUri, matchId, logger);

            allianceGamesClient = await AllianceGamesClient.Create(
                WebSocketTransportFactory.Get(config.Logger),
                config,
                ct: cts.Token
            );

            if (allianceGamesClient == null)
            {
                await OnError("Could not connect to the game server.");
                return false;
            }

            sessionId = matchId;

            RegisterHandlers();

            var response = await Request<PlayerDataResponse>(
                new Messages.PlayerDataRequest(),
                cts.Token
            );

            if (response == null)
            {
                await OnError("Could not retrieve player data");
                return false;
            }

            foreach (var player in response.Players)
            {
                Debug.Log($"Adding player {player.PubKey.Parse()}...");
                var address = $"0x{player.PubKey.Parse()}";
                playerData.Add(new PlayerData()
                {
                    Address = address,
                    Symbol = player.Symbol,
                    IsMe = accountManager.IsMyAddress(address),
                });
            }

            var myPlayerData = playerData.Find(p => p.IsMe);
            var opponentPlayerData = playerData.Find(p => !p.IsMe);

            view.Populate(
                matchId,
                myPlayerData,
                opponentPlayerData,
                myPlayerData.Symbol
            );

            Debug.Log($"Send ready");
            await allianceGamesClient.Send((int)Header.Ready, Buffer.Empty(), cts.Token);
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            await OnError($"Failed to start game: {e.Message}");
        }
        return true;
    }

    private void RegisterHandlers()
    {
        allianceGamesClient.RegisterMessageHandler((int)Messages.Header.Sync, async data =>
        {
            try
            {
                var sync = Decode<Sync>(data);
                view.SetBoard(sync.Fields.ToList());

                if (sync.Turn == Messages.Field.X)
                {
                    view.StartTurn(Field.X);
                    view.EndTurn(Field.O);

                }
                else
                {
                    view.StartTurn(Field.O);
                    view.EndTurn(Field.X);
                }
            }
            catch (Exception e)
            {
                await OnError($"Failed to sync game state: {e.Message}");
            }
        });

        allianceGamesClient.RegisterMessageHandler((int)Header.GameOver, async data =>
        {
            try
            {
                var gameOver = Decode<GameOver>(data);
                Debug.Log($"{gameOver.Winner} has won {(gameOver.IsForfeit ? "by forfeit" : "")}!");
                OpenGameResult(gameOver);
                await GameOver();
            }
            catch (Exception e)
            {
                await OnError($"Failed to handle game over: {e.Message}");
            }
        });

        allianceGamesClient.RegisterMessageHandler((int)Messages.Header.MoveRequest, async data =>
        {
            try
            {
                Debug.Log("Received move request.");
                turn = new UniTaskCompletionSource<int>();
                var idx = await turn.Task;
                turn = null;
                Debug.Log($"Send move response {idx}.");

                var response = Encode(new MoveResponse(idx));
                await allianceGamesClient.Send((int)Messages.Header.MoveResponse, response, cts.Token);
            }
            catch (Exception e)
            {
                await OnError($"Failed to handle move request: {e.Message}");
            }
        });
    }

    private async UniTask<T> Request<T>(
        IMessage message,
        CancellationToken ct
    ) where T : class, IMessage
    {
        var response = await allianceGamesClient.RequestUnverified((uint)message.Header, Encode(message), ct);
        if (response == null)
            return null;

        return Decode<T>(response.Value);
    }

    private async UniTask GameOver()
    {
        view.Reset();

        if (allianceGamesClient != null)
        {
            await allianceGamesClient.Stop(cts?.Token ?? CancellationToken.None);
            allianceGamesClient = null;
        }

        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }
    }

    private IClientConfig GetClientConfig(
        Uri nodeUri,
        string matchId,
        Serilog.ILogger logger
    )
    {
#if LOCAL
    return new ClientTestConfig(
        matchId,
        "",
        nodeUri,
        accountManager.SignatureProvider,
        new UniTaskRunner(),
        new UnityHttpClient(),
        logger: logger
    );

#else
        return new ClientConfig(
            matchId,
            nodeUri,
            accountManager.SignatureProvider,
            new UniTaskRunner(),
            new UnityHttpClient(),
            logger: logger
        );
#endif
    }

    public static void OpenLinkToExplorer(string sessionId)
    {
        Application.OpenURL($"{Config.EXPLORER_URL}sessions/{sessionId}");
    }

    private async void OpenGameResult(GameOver gameOver)
    {
        openGameResultCts = new CancellationTokenSource();

        var winner = gameOver.Winner != null ? Buffer.From(gameOver.Winner).Parse() : null;
        var amIWinner = string.IsNullOrEmpty(winner) ? (bool?)null : accountManager.IsMyAddress(winner);
        Debug.Log($"Winner: {winner} {accountManager.Address} {amIWinner}");

        CheckClaimState(openGameResultCts.Token).Forget();
        var res = await view.OpenGameResult(sessionId, amIWinner, playerData, gameOver.IsForfeit, openGameResultCts.Token);

        view.CloseGameResult();

        openGameResultCts?.CancelAndDispose();
        openGameResultCts = null;

        if (res == TTT.Components.ModalAction.CLOSE)
        {
            onEndGame?.Invoke(AfterGameAction.Menu);
        }
        else if (res == TTT.Components.ModalAction.NEXT_ROUND)
        {
            if (playerData.Any(p => p.IsAI))
            {
                onEndGame?.Invoke(AfterGameAction.NextRoundPvE);
            }
            else
            {
                onEndGame?.Invoke(AfterGameAction.NextRoundPvP);
            }
        }
    }

    private async UniTaskVoid CheckClaimState(CancellationToken ct)
    {
        view.UpdateClaimState(false);
        while (!ct.IsCancellationRequested)
        {
            var result = await Queries.GetEifEventBySession(connectionManager.AlliancesGamesClient, sessionId);
            if (result != null)
            {
                view.UpdateClaimState(true);
                break;
            }
            await UniTask.Delay(500, cancellationToken: ct);
        }
    }

    public async void OpenCancelGame()
    {
        if (cts == null)
            return;

        var res = await view.OpenCancelGame(cts.Token);
        if (res)
        {
            await allianceGamesClient.Send((int)Messages.Header.Forfeit, Buffer.Empty(), cts.Token);
        }
    }

    private async UniTask OnError(string info)
    {
        Debug.LogError(info);
        await GameOver();
        await view.OpenError(info, CancellationToken.None);
        onEndGame?.Invoke(AfterGameAction.Menu);
    }
}
