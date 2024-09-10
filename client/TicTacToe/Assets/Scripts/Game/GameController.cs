using AllianceGamesSdk.Client;
using AllianceGamesSdk.Common;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System.Collections.Generic;
using GrpcWebSocketBridge.Client;

using TicTacToeGrpc = AllianceGames.Sample.TicTacToe.Grpc;
using TicTacToe = AllianceGames.Sample.TicTacToe.Grpc.TicTacToeService.TicTacToeServiceClient;
using RequestOneofCase = AllianceGames.Sample.TicTacToe.Grpc.Request.RequestOneofCase;
using System.Linq;
using Grpc.Net.Client;

public class GameController
{
    public struct PlayerData
    {
        public string Address;
        public int Points;
        public Field Symbol;
    }

    public enum Field
    {
        Empty,
        X,
        O
    }

    private readonly Field[,] board = new Field[3, 3];
    private readonly List<PlayerData> playerData = new List<PlayerData>();

    private readonly GameView view;
    private readonly Blockchain blockchain;
    private TicTacToe service;

    private CancellationTokenSource cts;
    private UniTaskCompletionSource<int> turn = null;

    public GameController(
        GameView view,
        Blockchain blockchain,
        Action OnEndGame
    )
    {
        this.view = view;
        this.blockchain = blockchain;

        view.OnClickField += idx => 
        {
            if (turn != null 
                && !turn.Task.Status.IsCompleted()
                && board[idx % 3, idx / 3] == Field.Empty)
            {
                turn.TrySetResult(idx);
            }
        };
        view.OnClickBack += () =>
        {
            Forfeit().Forget();
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
            view.SetInfo($"Creating channel to {nodeUri}...");
            var options = new GrpcChannelOptions()
            {
                HttpHandler = new GrpcWebSocketBridgeHandler(true)
            };
            var client = await AllianceGamesClient.Create(
                nodeUri,
                matchId,
                blockchain.SignatureProvider,
                options: options,
                ct: cts.Token
            );

            view.SetInfo("Creating grpc service...");
            service = client.CreateService<TicTacToe>();
            await SetupRpcStream();

            view.SetInfo("Sending request to get player data...");
            var response = service.GetPlayerData(new Empty());
            view.SetInfo("Waiting to read player data response...");
            await foreach (var msg in response.ResponseStream.ReadAllAsync(cts.Token))
            {
                view.SetInfo($"Adding player {msg.Address}...");
                playerData.Add(new PlayerData()
                {
                    Address = msg.Address,
                    Points = await blockchain.GetPoints(msg.Address),
                    Symbol = msg.HasX ? Field.X : Field.O
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
        {
            await service.ForfeitAsync(new Empty(), cancellationToken: cts.Token);
            cts?.Cancel();
        }
    }

    private async UniTask SetupRpcStream()
    {
        view.SetInfo("Setting up rpc streams...");
        var rpcStream = await service.ServerRequests(cancellationToken: cts.Token).ConnectAsync(cancellationToken: cts.Token);

        RpcTask().Forget();

        async UniTask RpcTask()
        {
            try
            {
                await foreach (var msg in rpcStream.ResponseStream.ReadAllAsync(cts.Token))
                {
                    view.SetInfo($"Received {msg.RequestCase} message...");
                    switch (msg.RequestCase)
                    {
                        case RequestOneofCase.NewTurn:
                            view.SetInfo($"Test...");
                            var request = msg.NewTurn;
                            view.SetInfo(request.YouTurn ? "Your turn." : "Opponents turn.");
                            view.SetBoard(request.Squares.ToList());

                            if (request.YouTurn)
                            {
                                turn = new UniTaskCompletionSource<int>();
                                var idx = await turn.Task;
                                await rpcStream.RequestStream.WriteAsync(new()
                                {
                                    MakeMove = new()
                                    {
                                        Square = idx
                                    }
                                });
                                turn = null;
                            }
                            break;
                        case RequestOneofCase.GameOver:
                            view.SetInfo($"{msg.GameOver.Winner} has won!");
                            view.SetBoard(msg.GameOver.Squares.ToList());
                            cts.Cancel();
                            return;
                        default:
                            throw new ArgumentOutOfRangeException();
                    };
                }
            } 
            catch (OperationCanceledException) { }
        }
    }
}
