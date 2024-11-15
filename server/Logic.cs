using AllianceGamesSdk.Server;
using Chromia;
using Newtonsoft.Json;
using Serilog;
using System.Net.WebSockets;

using Buffer = Chromia.Buffer;

internal class Logic
{
    public enum Field
    {
        Empty,
        X,
        O
    }

    public event Action<string?>? OnGameEnd;

    public CancellationToken CancellationToken => cts.Token;
    private readonly CancellationTokenSource cts = new();
    private readonly TaskCompletionSource connectCs = new();
    private readonly AllianceGamesServer server;
    private readonly List<Buffer> players = [];

    private readonly Field[,] board = new Field[3, 3];

    public Logic(AllianceGamesServer server)
    {
        this.server = server;

        RegisterHandlers();
    }

    public async Task Forfeit(Buffer address)
    {
        Log.Information($"Player {address.Parse()} forfeited the game");

        var winnerField = GetField(players.First(p => p != address.Parse()));
        await GameOver(winnerField);
    }

    public async Task Run()
    {
        try
        {
            server.OnClientConnect += OnClientConnect;
            Log.Information($"Waiting for both players to connect");
            await connectCs.Task;

            Log.Information($"Initializing board");
            InitializeBoard();

            // play game
            Field? winner = null;
            var currentPlayerId = players[0];
            Log.Information($"Start with player {currentPlayerId}");
            while (winner == null)
            {
                var validMove = false;
                while (!validMove)
                {
                    Log.Information($"Send request to player {currentPlayerId}");

                    var response = await server.Request(
                        (int)Messages.Header.MoveRequest,
                        currentPlayerId,
                        new(),
                        CancellationToken,
                        30000
                    );

                    var move = response == null ? RandomMove()
                        : (int)(long)ChromiaClient.DecodeFromGtv(response.Value);

                    if (move < 0 || move >= 9 || board[move / 3, move % 3] != Field.Empty)
                    {
                        Log.Information($"Invalid move on square {move}, try again");
                        continue;
                    }

                    var symbol = GetField(currentPlayerId);
                    board[move / 3, move % 3] = symbol;
                    validMove = true;

                    Log.Information($"Setting square [{move / 3}, {move % 3}] to {symbol}");
                    foreach (var player in players)
                    {
                        await server.Send(
                            (int)Messages.Header.MoveResponse,
                            player,
                            ChromiaClient.EncodeToGtv(new object[] { move, (int)symbol }),
                            CancellationToken
                        );
                    }
                    currentPlayerId = players.First(p => p != currentPlayerId);
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
        Buffer? winnerPlayer = winner != null ? players[(int)winner.Value - 1] : null;
        Log.Information($"Game is over, winner is {winnerPlayer}");
        foreach (var player in players)
        {
            await server.Send((int)Messages.Header.GameOver, player, new(), default);
        }

        Reward[] blockchainReward;
        if (winnerPlayer == null)
        {
            blockchainReward = players
                .Select(p => new Reward(p, 50))
                .ToArray();
        }
        else
        {
            blockchainReward = [
                new Reward(winnerPlayer.Value, 100),
                new Reward(players.First(p => p != winnerPlayer.Value), 50)
            ];

        }

        OnGameEnd?.Invoke(JsonConvert.SerializeObject(blockchainReward, Formatting.Indented));
    }

    private void OnClientConnect(Buffer pubKey)
    {
        players.Add(pubKey);
        if (players.Count == 2)
        {
            server.OnClientConnect -= OnClientConnect;
            connectCs.SetResult();
        }
    }

    private Field GetField(Buffer player) => (Field)players.IndexOf(player) + 1;

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

    private int RandomMove()
    {
        var validMoves = new List<int>();
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (board[i, j] == Field.Empty)
                {
                    validMoves.Add(i * 3 + j);
                }
            }
        }
        return validMoves[server.Random.Next(validMoves.Count)];
    }

    private void RegisterHandlers()
    {
        server.RegisterMessageHandler((int)Messages.Header.Forfeit, async (address, data) =>
        {
            if (!players.Contains(address))
            {
                Log.Error($"Player {address} is not in the game");
                return;
            }

            await Forfeit(address);
        });
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

    private class Reward(Buffer pubKey, int points)
    {
        [JsonProperty("pubkey")]
        public Buffer PubKey { get; } = pubKey;
        [JsonProperty("points")]
        public int Points { get; } = points;
    }
}
