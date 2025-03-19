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
    public struct PlayerData
    {
        public string Address;
        public Messages.Field Symbol;
        public bool IsMe;
    }

    private readonly Messages.Field[,] board = new Messages.Field[3, 3];
    private readonly List<PlayerData> playerData = new List<PlayerData>();

    private readonly GameView view;
    private readonly AccountManager accountManager;
    private readonly BlockchainConnectionManager connectionManager;
    private readonly Action onEndGame;

    private string sessionId;
    private AllianceGamesClient allianceGamesClient;
    private CancellationTokenSource cts;
    private UniTaskCompletionSource<int> turn = null;

    private CancellationTokenSource openGameResultCts = null;

    public GameController(
        GameView view,
        AccountManager accountManager,
        BlockchainConnectionManager connectionManager,
        Action onEndGame
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
    }

    public async Task StartGame(Uri nodeUri, string matchId)
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
                Debug.Log("Could not create client");
                return;
            }

            sessionId = matchId;

            RegisterHandlers();

            var response = await Request<PlayerDataResponse>(
                new Messages.PlayerDataRequest(),
                cts.Token
            );

            if (response == null)
            {
                Debug.Log("Could not retrieve player data");
                return;
            }

            foreach (var player in response.Players)
            {
                Debug.Log($"Adding player {player.PubKey.Parse()}...");
                var address = $"0x{player.PubKey.Parse()}";
                playerData.Add(new PlayerData()
                {
                    Address = address,
                    Symbol = player.Symbol,
                    IsMe = accountManager.Address == address,
                });
            }

            view.Populate(
                matchId,
                playerData.Find(p => accountManager.IsMyAddress(p.Address)),
                playerData.Find(p => !accountManager.IsMyAddress(p.Address))
            );

            Debug.Log($"Send ready");
            await allianceGamesClient.Send((int)Messages.Header.Ready, Buffer.Empty(), cts.Token);
        }
        catch (OperationCanceledException) { }
    }

    private void RegisterHandlers()
    {
        allianceGamesClient.RegisterMessageHandler((int)Messages.Header.Sync, data =>
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
        });

        allianceGamesClient.RegisterMessageHandler((int)Messages.Header.GameOver, async data =>
        {
            var gameOver = Decode<GameOver>(data);
            Debug.Log($"{gameOver.Winner} has won {(gameOver.IsForfeit ? "by forfeit" : "")}!");
            OpenGameResult(gameOver);
            await GameOver();
        });

        allianceGamesClient.RegisterMessageHandler((int)Messages.Header.MoveRequest, async data =>
        {
            Debug.Log("Received move request.");
            turn = new UniTaskCompletionSource<int>();
            var idx = await turn.Task;
            turn = null;
            Debug.Log($"Send move response {idx}.");

            var response = Encode(new MoveResponse(idx));
            await allianceGamesClient.Send((int)Messages.Header.MoveResponse, response, cts.Token);
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

    private async void OpenGameResult(Messages.GameOver gameOver)
    {
        openGameResultCts?.CancelAndDispose();
        openGameResultCts = new CancellationTokenSource();

        var winner = gameOver.Winner?.Parse();
        var amIWinner = string.IsNullOrEmpty(winner) ? (bool?)null : accountManager.IsMyAddress(winner);
        var res = await view.OpenGameResult(sessionId, amIWinner, playerData, gameOver.IsForfeit, connectionManager, openGameResultCts.Token);

        if (res == TTT.Components.ModalAction.CLOSE || res == TTT.Components.ModalAction.NEXT_ROUND)
        {
            view.CloseGameResult();
            onEndGame?.Invoke();
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
}
