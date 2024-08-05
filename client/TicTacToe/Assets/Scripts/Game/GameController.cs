using AllianceGamesSdk.Client;
using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System.Collections.Generic;

using TicTacToe = AllianceGames.Sample.TicTacToe.Grpc.TicTacToeService.TicTacToeServiceClient;
using RequestOneofCase = AllianceGames.Sample.TicTacToe.Grpc.Request.RequestOneofCase;
using System.Linq;

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
    private TaskCompletionSource<int> turn = null;

    public GameController(
        GameView view,
        Blockchain blockchain
    )
    {
        this.view = view;
        this.blockchain = blockchain;

        view.OnClickField += idx => 
        {
            if (turn != null 
                && !turn.Task.IsCompleted
                && board[idx % 3, idx / 3] == Field.Empty)
            {
                turn.SetResult(idx);
            }
        };
    }

    public void SetVisible(
        bool visible
    )
    {
        view.SetVisible(visible);
    }

    public async Task StartGame(
        Uri nodeUri,
        string matchId,
        string opponent
    )
    {
        cts?.Dispose();
        cts = new CancellationTokenSource();

        var client = await AllianceGamesClient.Create(
            nodeUri,
            matchId,
            blockchain.SignatureProvider,
            ct: cts.Token
        );

        service = client.CreateService<TicTacToe>();
        SetupRpcStream();

        var response = service.GetPlayerData(new Empty());
        await foreach (var msg in response.ResponseStream.ReadAllAsync())
        {
            playerData.Add(new PlayerData()
            {
                Address = msg.Address,
                Points = await blockchain.GetPoints(msg.Address),
                Symbol = msg.HasX ? Field.X : Field.O
            });
        }

        view.Initialize(
            playerData.Find(p => p.Address != opponent),
            playerData.Find(p => p.Address == opponent)
        );
    }

    private void SetupRpcStream()
    {
        var response = service.ServerRequests();
        _ = Task.Run(async () =>
        {
            await foreach (var msg in response.ResponseStream.ReadAllAsync())
            {
                switch (msg.RequestCase)
                {
                    case RequestOneofCase.NewTurn:
                        var request = msg.NewTurn;
                        view.SetBoard(request.Squares.ToList());
                        view.SetInfo(request.YouTurn ? "Your turn." : "Opponents turn.");

                        if (request.YouTurn)
                        {
                            turn = new TaskCompletionSource<int>();
                            var idx = await turn.Task;
                            await response.RequestStream.WriteAsync(new()
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
                        return;
                    default:
                        throw new ArgumentOutOfRangeException();
                };
            }
        });
    }
}
