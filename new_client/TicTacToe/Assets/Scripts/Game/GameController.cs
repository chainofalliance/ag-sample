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
    }

    private readonly Messages.Field[,] board = new Messages.Field[3, 3];
    private readonly List<PlayerData> playerData = new List<PlayerData>();

    private readonly GameView view;
    private readonly AccountManager accountManager;

    private AllianceGamesClient allianceGamesClient;
    private CancellationTokenSource cts;
    private UniTaskCompletionSource<int> turn = null;

    public GameController(
        GameView view,
        AccountManager accountManager,
        Action OnEndGame
    )
    {
        this.view = view;
        this.accountManager = accountManager;
        

        view.OnClickField += idx =>
        {
            if (turn != null
                && !turn.Task.Status.IsCompleted()
                && board[idx % 3, idx / 3] == Messages.Field.Empty)
            {
                turn.TrySetResult(idx);
            }
        };

        view.OnClickBack += async () =>
        {
            await GameOver(true);
            OnEndGame?.Invoke();
        };
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
                new WebSocketTransport(config.Logger),
                config,
                ct: cts.Token
            );

            if (allianceGamesClient == null)
            {
                Debug.Log("Could not create client");
                return;
            }

            RegisterHandlers();

            var response = await Request<Messages.PlayerDataResponse>(
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
                playerData.Add(new PlayerData()
                {
                    Address = player.PubKey.Parse(),
                    Symbol = player.Symbol
                });
            }

            Debug.Log($"Send ready");
            await allianceGamesClient.Send((int)Messages.Header.Ready, Buffer.Empty(), cts.Token);
        }
        catch (OperationCanceledException) { }
    }

    private void RegisterHandlers()
    {
        allianceGamesClient.RegisterMessageHandler((int)Messages.Header.Sync, data =>
        {
            var sync = new Messages.Sync(data);
            view.SetBoard(sync.Fields.ToList());
            if (sync.Turn == Messages.Field.X)
                Debug.Log("Your turn.");
            else
                Debug.Log("Opponents turn.");
        });

        allianceGamesClient.RegisterMessageHandler((int)Messages.Header.GameOver, async winner =>
        {
            Debug.Log($"{winner} has won!");
            await GameOver(false);
        });

        allianceGamesClient.RegisterMessageHandler((int)Messages.Header.MoveRequest, async data =>
        {
            Debug.Log("Received move request.");
            turn = new UniTaskCompletionSource<int>();
            var idx = await turn.Task;
            turn = null;
            Debug.Log($"Send move response {idx}.");

            var response = new Messages.MoveResponse(idx).Encode();
            await allianceGamesClient.Send((int)Messages.Header.MoveResponse, response, cts.Token);
        });
    }

    private async UniTask<T> Request<T>(
        IMessage message,
        CancellationToken ct
    ) where T : class, IMessage, new()
    {
        var response = await allianceGamesClient.RequestUnverified((uint)message.Header, message.Encode(), ct);
        if (response == null)
            return null;

        var responseMessage = new T();
        responseMessage.Decode(response.Value);
        return responseMessage;
    }

    private async UniTask GameOver(bool forfeit)
    {
        if (allianceGamesClient != null)
        {
            if (forfeit)
            {
                await allianceGamesClient.Send((int)Messages.Header.Forfeit, Buffer.Empty(), cts.Token);
            }

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
}
