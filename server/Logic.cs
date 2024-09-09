using AllianceGames.Sample.TicTacToe.Grpc;
using AllianceGamesSdk;
using AllianceGamesSdk.Common;
using Grpc.Core;
using Serilog;
using System.Net.WebSockets;
using System.Threading.Channels;

internal class Logic
{
    public enum Field
    {
        Empty,
        X,
        O
    }

    public IReadOnlyDictionary<string, Player> Players => players;
    public CancellationToken CancellationToken => cts.Token;
    private CancellationTokenSource cts = new CancellationTokenSource();
    private readonly Dictionary<string, Player> players = new();

    private Channel<Response> channel = Channel.CreateUnbounded<Response>();

    private readonly Field[,] board = new Field[3, 3];
    private Field turn = Field.Empty;

    private Action<object?>? onEnd = null;

    public async Task Forfeit(Chromia.Buffer address)
    {
        Log.Information($"Player {address.Parse()} forfeited the game");

        var winnerField = players.First(p => p.Key != address.Parse()).Value.Symbol;
        await GameOver(winnerField);
    }

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

        var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, CancellationToken);

        Log.Information($"Open ServerRequest channel for {playerId}");
        var player = new Player(
            playerId,
            requestStream,
            responseStream,
            channel.Writer,
            ++turn
        );
        players.Add(playerId, player);

        await player.Process(cts.Token);

        players.Remove(playerId);
    }

    public async Task Run(Action<object?> onEnd)
    {
        this.onEnd = onEnd;

        try
        {
            Log.Information($"Waiting for both players to connect");
            // wait until two players are connected
            await TaskUtil.WaitUntil(() => players.Count == 2);

            Log.Information($"Initializing board");
            InitializeBoard();

            Log.Information($"Giving players a sec...");
            await Task.Delay(1000, CancellationToken);

            // play game
            Field? winner = null;
            var currentPlayerId = players.Keys.First();
            Log.Information($"Start with player {currentPlayerId}");
            while (winner == null)
            {
                var validMove = false;
                while (!validMove)
                { 
                    foreach (var (id, player) in players)
                    {
                        Log.Information($"Send request to player {id} with your turn == {id == currentPlayerId}");
                        await player.Requests.WriteAsync(new Request
                        {
                            NewTurn = new NewTurnRequest()
                            {
                                Squares = { board.Cast<Field>().Select(f => (int)f) },
                                YouTurn = id == currentPlayerId
                            }
                        }, CancellationToken);
                    }

                    var currentPlayer = players[currentPlayerId];
                    Log.Information($"Waiting for sending his turn");
                    var response = await channel.Reader.ReadAsync(CancellationToken);
                    var move = response.MakeMove.Square;
                    if (move < 0 || move >= 9 || board[move / 3, move % 3] != Field.Empty)
                    {
                        Log.Information($"Invalid move on square {move}, try again");
                        continue;
                    }
                    board[move / 3, move % 3] = currentPlayer.Symbol;
                    validMove = true;

                    Log.Information($"Setting square [{move / 3}, {move % 3}] to {currentPlayer.Symbol}");
                    currentPlayerId = players.First(p => p.Key != currentPlayerId).Key;
                    winner = GetWinner();
                }
            }

            await GameOver(winner);
        }
        catch (WebSocketException) { }
        catch (OperationCanceledException) { }
    }

    private async Task GameOver(Field? winner)
    {
        var winnerPlayer = players.Values.First(p => p.Symbol == winner);
        Log.Information($"Game is over, winner is {winnerPlayer?.Address}");
        foreach (var (address, player) in players)
        {
            await player.Requests.WriteAsync(new Request
            {
                GameOver = new GameOverRequest()
                {
                    Squares = { board.Cast<Field>().Select(f => (int)f) },
                    Winner = winnerPlayer?.Address,
                    Points = winnerPlayer?.Address == player.Address ? 100 : 50
                }
            }, CancellationToken);
        }

        object[] blockchainReward;
        if (winnerPlayer == null)
        {
            blockchainReward = players
                .Select(p => new object[] { p.Value.PubKey, 50 })
                .ToArray();
        }
        else
        {
            blockchainReward = new object[]
            {
                    new object[]{ winnerPlayer!.PubKey, 100 },
                    new object[]{ players.First(p => p.Key != winnerPlayer!.Address).Value.PubKey, 50 }
            };

        }

        onEnd?.Invoke(blockchainReward);
        onEnd = null;
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
