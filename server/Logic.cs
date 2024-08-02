using AllianceGames.Sample.TicTacToe.Grpc;
using AllianceGamesSdk;
using AllianceGamesSdk.Common;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Serilog;
using System.Collections;
using System.Threading.Channels;

internal class Logic
{
    public enum Field
    {
        Empty,
        X,
        O
    }

    public CancellationToken CancellationToken => cts.Token;
    private readonly CancellationTokenSource cts = new();
    private readonly Dictionary<string, Player> players = new();

    private Channel<Response> channel = Channel.CreateUnbounded<Response>();

    private readonly Field[,] board = new Field[3, 3];
    private Field turn = Field.Empty;

    public async Task ServerRequests(
        IAsyncStreamReader<Response> requestStream,
        IServerStreamWriter<Request> responseStream,
        ServerCallContext context
    )
    {
        var playerId = context.GetAddress();
        if (players.ContainsKey(playerId))
        {
            Log.Error($"Player {playerId} has a ServerRequest channel open already");
            return;
        }

        var player = new Player(
            playerId,
            requestStream,
            responseStream,
            channel.Writer,
            ++turn
        );

        await Task.Run(() => player.Process(CancellationToken), CancellationToken);
    }

    public async Task Run(Action<object?> onEnd)
    {
        // wait until two players are connected
        await TaskUtil.WaitWhile(() => players.Count == 2);

        InitializeBoard();

        // play game
        Field? winner = null;
        var currentPlayerId = players.Keys.First();
        while (winner == null)
        {
            foreach (var (id, player) in players)
            {
                await player.Requests.WriteAsync(new Request
                {
                    NewTurn = new NewTurnRequest()
                    {
                        Squares = { board.Cast<Field>().Select(f => (int)f) },
                        YouTurn = id == currentPlayerId
                    }
                });
            }

            var currentPlayer = players[currentPlayerId];
            var response = await channel.Reader.ReadAsync(CancellationToken);
            // TODO we should validate the response
            var move = response.MakeMove.Square;
            board[move / 3, move % 3] = currentPlayer.Symbol;

            currentPlayerId = players.First(p => p.Key != currentPlayerId).Key;
            winner = GetWinner();
        }

        var winnerPlayer = players.Values.First(p => p.Symbol == winner);
        // send game over message
        foreach (var (_, player) in players)
        {
            await player.Requests.WriteAsync(new Request
            {
                // Game over 
            });
        }


        onEnd?.Invoke(null);
    }

    private void InitializeBoard()
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                board[i, j] = Field.Empty;
            }
        }
    }

    private Field? GetWinner()
    {
        for (int i = 0; i < 3; i++)
        {
            // Check rows
            if (board[i, 0] != Field.Empty && board[i, 0] == board[i, 1] && board[i, 1] == board[i, 2])
            {
                return board[i, 0];
            }

            // Check columns
            if (board[0, i] != Field.Empty && board[0, i] == board[1, i] && board[1, i] == board[2, i])
            {
                return board[0, i];
            }
        }

        // Check diagonals
        if (board[0, 0] != Field.Empty && board[0, 0] == board[1, 1] && board[1, 1] == board[2, 2])
        {
            return board[0, 0];
        }
        if (board[0, 2] != Field.Empty && board[0, 2] == board[1, 1] && board[1, 1] == board[2, 0])
        {
            return board[0, 2];
        }

        if (board.Cast<Field>().All(f => f != Field.Empty))
        {
            // Draw
            return Field.Empty;
        }

        // No winner
        return null;
    }
}
