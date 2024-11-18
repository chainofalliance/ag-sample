using AllianceGamesSdk.Client;
using AllianceGamesSdk.Common;
using AllianceGamesSdk.Common.Transport;
using AllianceGamesSdk.Common.Transport.WebSocket;
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
    private readonly IHttpClient httpClient;

    private AllianceGamesClient agClient;

    private CancellationTokenSource cts;
    private UniTaskCompletionSource<int> turn = null;

    public GameController(
        GameView view,
        Blockchain blockchain,
        ITaskRunner taskRunner,
        IHttpClient httpClient,
        Action OnEndGame
    )
    {
        this.view = view;
        this.blockchain = blockchain;
        this.taskRunner = taskRunner;
        this.httpClient = httpClient;

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
        string matchId,
        string opponent
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
                httpClient,
                taskRunner,
                resolveSessionPort: !Main.MOCK
            );
            agClient = await AllianceGamesClient.Create(
                new WebSocketTransport(config.Logger),
                config,
                ct: cts.Token
            );
            RegisterHandlers();

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
                    Address = player.PubKey,
                    Points = player.Points,
                    Symbol = player.Symbol
                });
            }

            view.SetInfo($"Finalize startup...");
            view.Initialize(
                playerData.Find(p => p.Address != opponent),
                playerData.Find(p => p.Address == opponent)
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

        agClient.RegisterRequestHandler((int)Messages.Header.MoveRequest, async data =>
        {
            turn = new UniTaskCompletionSource<int>();
            var idx = await turn.Task;
            turn = null;
            return new Messages.MoveResponse(idx).Encode();
        });
    }

    private async UniTask<T> Request<T>(
        IMessage message,
        CancellationToken ct
    ) where T : class, IMessage, new()
    {
        var response = await agClient.Request((int)message.Header, message.Encode(), ct);
        if (response == null)
            return null;

        var responseMessage = new T();
        responseMessage.Decode(response.Value);
        return responseMessage;
    }
}
