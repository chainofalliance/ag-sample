using AllianceGamesSdk.Client;
using AllianceGamesSdk.Common;
using AllianceGamesSdk.Transport.WebSocket;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Buffer = Chromia.Buffer;
using IMessage = Messages.IMessage;

public class GameController
{
    public struct PlayerData
    {
        public string Address;
        public int Points;
        public Messages.Field Symbol;
    }

    private readonly Messages.Field[,] board = new Messages.Field[3, 3];
    private readonly List<PlayerData> playerData = new List<PlayerData>();

    private readonly GameView view;
    private readonly Blockchain blockchain;
    private readonly ITaskRunner taskRunner;

    private AllianceGamesClient agClient;

    private CancellationTokenSource cts;
    private UniTaskCompletionSource<int> turn = null;

    public GameController(
        GameView view,
        Blockchain blockchain,
        ITaskRunner taskRunner,
        Action OnEndGame
    )
    {
        this.view = view;
        this.blockchain = blockchain;
        this.taskRunner = taskRunner;

        view.OnClickField += idx =>
        {
            if (turn != null
                && !turn.Task.Status.IsCompleted()
                && board[idx % 3, idx / 3] == Messages.Field.Empty)
            {
                turn.TrySetResult(idx);
            }
        };
        view.OnClickBack += () =>
        {
            Forfeit().Forget();
            if (cts != null && !cts.IsCancellationRequested)
                cts.Cancel();
            OnEndGame?.Invoke();
        };
    }

    public void SetVisible(
        bool visible
    )
    {
        view.SetVisible(visible);
    }

    public async UniTask StartGame(
        Uri nodeUri,
        string matchId
    )
    {
        cts?.Dispose();
        cts = new CancellationTokenSource();

        view.Reset();
        try
        {
            view.SetInfo($"Creating connection to {nodeUri}...");
            var config = new ClientConfig(
                matchId,
                nodeUri,
                blockchain.SignatureProvider,
                taskRunner
            );
            agClient = await AllianceGamesClient.Create(
#if UNITY_WEBGL && !UNITY_EDITOR
                new UnityWebSocket(),
#else
                new WebSocketTransport(config.Logger),
#endif
                config,
                ct: cts.Token
            );
            RegisterHandlers();
            await UniTask.Delay(2000);

            await agClient.Send((int)Messages.Header.Ready, Buffer.Empty(), cts.Token);

            view.SetInfo("Sending request to get player data...");
            var response = await Request<Messages.PlayerDataResponse>(
                new Messages.PlayerDataRequest(),
                cts.Token
            );
            if (response == null)
            {
                view.SetInfo("Could not retrieve player data");
                return;
            }

            foreach (var player in response.Players)
            {
                view.SetInfo($"Adding player {player.PubKey.Parse()}...");
                playerData.Add(new PlayerData()
                {
                    Address = player.PubKey.Parse(),
                    Points = player.Points,
                    Symbol = player.Symbol
                });
            }

            view.SetInfo($"Finalize startup...");
            var pubKey = blockchain.SignatureProvider.PubKey.Parse();
            view.Initialize(
                playerData.Find(p => p.Address == pubKey),
                playerData.Find(p => p.Address != pubKey)
            );
        }
        catch (OperationCanceledException) { }
    }

    public async UniTask Forfeit()
    {
        if (!cts.IsCancellationRequested)
            await agClient.Send((int)Messages.Header.Forfeit, Buffer.Empty(), cts.Token);
    }

    private void RegisterHandlers()
    {
        agClient.RegisterMessageHandler((int)Messages.Header.Sync, data =>
        {
            var sync = new Messages.Sync(data);
            view.SetBoard(sync.Fields.ToList());
            if (sync.Turn == Messages.Field.X)
                view.SetInfo("Your turn.");
            else
                view.SetInfo("Opponents turn.");
        });

        agClient.RegisterMessageHandler((int)Messages.Header.GameOver, winner =>
        {
            view.SetInfo($"{winner} has won!");
            cts.Cancel();
        });

        agClient.RegisterMessageHandler((int)Messages.Header.MoveRequest, async data =>
        {
            view.SetInfo("Received move request.");
            turn = new UniTaskCompletionSource<int>();
            var idx = await turn.Task;
            turn = null;
            view.SetInfo($"Send move response {idx}.");

            var response = new Messages.MoveResponse(idx).Encode();
            await agClient.Send((int)Messages.Header.MoveResponse, response, cts.Token);
        });
    }

    private async UniTask<T> Request<T>(
        IMessage message,
        CancellationToken ct
    ) where T : class, IMessage, new()
    {
        var response = await agClient.RequestUnverified((int)message.Header, message.Encode(), ct);
        if (response == null)
            return null;

        var responseMessage = new T();
        responseMessage.Decode(response.Value);
        return responseMessage;
    }
}
