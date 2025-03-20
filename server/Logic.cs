using AllianceGamesSdk.Server;
using AllianceGamesSdk.Transport.WebSocket;
using Newtonsoft.Json;
using Serilog;
using System.Net.WebSockets;

using Buffer = Chromia.Buffer;

internal class Logic
{
    private const string AI_ADDRESS = "0000000000000000000000000000000000000000000000000000000000000000";
    private readonly Buffer aiAddress = Buffer.From(AI_ADDRESS);

    public CancellationToken CancellationToken => cts.Token;
    private readonly AllianceGamesServer server;
    private readonly CancellationTokenSource cts = new();
    private readonly TaskCompletionSource initCs = new();
    private readonly TaskCompletionSource readyCs = new();
    private List<Buffer> players = new();
    private bool IsAi => server.Clients.Count == 1;
    private int readyPlayers = 0;
    private Buffer currentPlayerId = Buffer.Empty();
    private TaskCompletionSource<Messages.MoveResponse>? moveTcs;

    private readonly Messages.Field[,] board = new Messages.Field[3, 3];

    public Logic(INodeConfig config)
    {
        var server = AllianceGamesServer.Create(
            new WebSocketTransport(config.Logger),
            config
        );
        if (server == null)
        {
            throw new Exception("Failed to create server");
        }
        this.server = server;

        RegisterHandlers();
    }

    public async Task Forfeit(Buffer address)
    {
        Log.Information($"Player {address.Parse()} forfeited the game");

        var winnerField = GetField(players.First(p => p != address.Parse()));
        await GameOver(winnerField, true);
    }

    public async Task Run()
    {
        var result = await server.Start(CancellationToken);

        if (server == null)
        {
            Log.Error("Failed to create server");
            return;
        }

        try
        {
            Log.Information($"Initializing players");
            InitializePlayers();

            Log.Information($"Initializing board");
            InitializeBoard();

            Log.Information($"Waiting for both players to ready up");
            await readyCs.Task;

            // play game
            Messages.Field? winner = null;
            currentPlayerId = players[0];
            Log.Information($"Start with player {currentPlayerId.Parse()}");
            while (winner == null)
            {
                var validMove = false;
                while (!validMove)
                {
                    await server.Send(
                        (int)Messages.Header.Sync,
                        Messages.Encode(new Messages.Sync(GetField(currentPlayerId), board)),
                        CancellationToken
                    );
                    Log.Information($"Send request to player {currentPlayerId.Parse()}");

                    int move;
                    if (IsAi && currentPlayerId == aiAddress)
                    {
                        move = RandomMove();
                        var cts = new TaskCompletionSource();
                        server.SetTimeout(() => cts.TrySetResult(), 2500, CancellationToken);
                        await cts.Task;
                    }
                    else
                    {
                        var response = await RequestMove(currentPlayerId, CancellationToken);
                        move = response == null ? RandomMove() : response.Move;
                    }

                    if (move < 0 || move >= 9 || board[move / 3, move % 3] != Messages.Field.Empty)
                    {
                        Log.Information($"Invalid move on square {move}, try again");
                        continue;
                    }

                    var symbol = GetField(currentPlayerId);
                    board[move / 3, move % 3] = symbol;
                    validMove = true;
                    currentPlayerId = NextPlayer(currentPlayerId);

                    Log.Information($"Setting square [{move / 3}, {move % 3}] to {symbol}");
                    winner = GetWinner();
                }
            }

            await server.Send(
                (int)Messages.Header.Sync,
                Messages.Encode(new Messages.Sync(GetField(currentPlayerId), board)),
                CancellationToken
            );

            await GameOver(winner.Value, false);
        }
        catch (WebSocketException) { }
        catch (OperationCanceledException) { }
        finally
        {
            await Stop(null);
        }
    }

    private void InitializePlayers()
    {
        Log.Information($"Initialize players");
        foreach (var client in server.Clients)
        {
            Log.Information($"Add player {client.Parse()}");
            players.Add(client);
        }

        if (IsAi)
        {
            Log.Information($"Initialize AI player");
            players.Add(aiAddress);
            Log.Information($"Add AI player");
        }

        Log.Information($"Initialize players done");
        initCs.SetResult();
    }

    private async Task GameOver(Messages.Field winner, bool isForfeit)
    {
        Buffer? winnerPlayer = winner == Messages.Field.Empty ? null : players[(int)winner - 1];
        Log.Information($"Game is over, winner is {winnerPlayer?.Parse()}, forfeit: {isForfeit}");
        await server!.Send(
            (int)Messages.Header.GameOver,
            Messages.Encode(new Messages.GameOver(winnerPlayer?.Bytes, isForfeit)),
            CancellationToken
        );

        List<Reward> blockchainReward = new();
        if (winnerPlayer == null)
        {
            for (int i = 0; i < players.Count; i++)
            {
                blockchainReward.Add(
                    new Reward(players[i], server.SessionId, players[(i + 1) % 2], 50, Outcome.Draw)
                );
            }
        }
        else
        {
            var looser = players.First(p => p != winnerPlayer.Value);
            blockchainReward = [
                new Reward(winnerPlayer.Value, server.SessionId, looser, 100, Outcome.Win),
                new Reward(looser, server.SessionId, winnerPlayer.Value, 25, Outcome.Loose)
            ];
        }

        blockchainReward.RemoveAll(r => r.PubKey == AI_ADDRESS);

        await Stop(JsonConvert.SerializeObject(blockchainReward));
    }

    private Messages.Field GetField(Buffer? player) =>
        player == null ? Messages.Field.O : (Messages.Field)players.IndexOf(player.Value) + 1;

    private void InitializeBoard()
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                board[i, j] = Messages.Field.Empty;
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
                if (board[i, j] == Messages.Field.Empty)
                {
                    validMoves.Add(i * 3 + j);
                }
            }
        }
        return validMoves[server.Random.Next(validMoves.Count)];
    }

    private Buffer NextPlayer(Buffer currentPlayer)
    {
        return players.First(p => p != currentPlayer);
    }

    private async Task<Messages.MoveResponse?> RequestMove(Buffer currentPlayerId, CancellationToken ct)
    {
        moveTcs = new TaskCompletionSource<Messages.MoveResponse>();

        Log.Information($"Send request start");
        await server.Send(
            (int)Messages.Header.MoveRequest,
            currentPlayerId,
            Buffer.Empty(),
            ct
        );
        Log.Information($"Send request done");

        var timeout = server.SetTimeout(() => moveTcs?.TrySetCanceled(), 30000, ct);
        try
        {
            var res = await moveTcs.Task;
            timeout.Cancel();
            moveTcs = null;
            return res;
        }
        catch (TaskCanceledException)
        {
            Log.Information($"TaskCanceledException Response canceled");
            return null;
        }
        catch (OperationCanceledException)
        {
            Log.Information($"OperationCanceledException Response canceled");
            return null;
        }
        catch (Exception e)
        {
            Log.Error(e, $"Error while waiting for response");
            return null;
        }
    }

    private void RegisterHandlers()
    {
        server.RegisterMessageHandler((uint)Messages.Header.Ready, (address, _) =>
        {
            readyPlayers++;
            Log.Information($"Player {address.Parse()} is ready");
            if (readyPlayers == (IsAi ? 1 : 2))
            {
                readyCs.SetResult();
            }
        });

        server.RegisterMessageHandler((uint)Messages.Header.MoveResponse, (address, data) =>
        {
            if (address != currentPlayerId)
            {
                Log.Error($"Its not players {address.Parse()} turn");
                return;
            }
            else if (moveTcs == null)
            {
                Log.Error($"Not waiting for a move");
                return;
            }

            moveTcs.SetResult(Messages.Decode<Messages.MoveResponse>(data));
        });

        server.RegisterMessageHandler((uint)Messages.Header.Forfeit, async (address, _) =>
        {
            if (!players.Contains(address))
            {
                Log.Error($"Player {address.Parse()} is not in the game");
                return;
            }

            await Forfeit(address);
        });

        server.RegisterRequestHandler((uint)Messages.Header.PlayerDataRequest, async (address, data) =>
        {
            await initCs.Task;

            if (!players.Contains(address))
            {
                Log.Error($"Player {address.Parse()} is not in the game");
                return Buffer.Empty();
            }

            return Messages.Encode(new Messages.PlayerDataResponse(players.Select(
                p => new Messages.PlayerDataResponse.Player(p, GetField(p))
            ).ToArray()));
        });

        server.OnClientConnect += address => Log.Information($"Player {address.Parse()} connected");
        server.OnClientDisconnect += address => Log.Information($"Player {address.Parse()} disconnected");
    }

    private async Task Stop(string? reward)
    {
        if (server.IsRunning)
        {
            await server.Stop(reward);
        }

        try
        {
            cts.Cancel();
            cts.Dispose();
        }
        catch (Exception)
        { }
    }

    private Messages.Field? GetWinner()
    {
        for (int i = 0; i < 3; i++)
        {
            // Check rows
            if (board[i, 0] != Messages.Field.Empty && board[i, 0] == board[i, 1] && board[i, 1] == board[i, 2])
            {
                return board[i, 0];
            }

            // Check columns
            if (board[0, i] != Messages.Field.Empty && board[0, i] == board[1, i] && board[1, i] == board[2, i])
            {
                return board[0, i];
            }
        }

        // Check diagonals
        if (board[0, 0] != Messages.Field.Empty && board[0, 0] == board[1, 1] && board[1, 1] == board[2, 2])
        {
            return board[0, 0];
        }
        if (board[0, 2] != Messages.Field.Empty && board[0, 2] == board[1, 1] && board[1, 1] == board[2, 0])
        {
            return board[0, 2];
        }

        if (board.Cast<Messages.Field>().All(f => f != Messages.Field.Empty))
        {
            // Draw
            return Messages.Field.Empty;
        }

        // No winner
        return null;
    }

    enum Outcome
    {
        Win,
        Loose,
        Draw
    }

    private class Reward(Buffer pubKey, string SessionId, Buffer opponent, int points, Outcome outcome)
    {
        [JsonProperty("pubkey")]
        public string PubKey { get; } = pubKey.Parse();

        [JsonProperty("session_id")]
        public string SessionId { get; } = SessionId;

        [JsonProperty("opponent")]
        public string Opponent { get; } = opponent.Parse();

        [JsonProperty("points")]
        public int Points { get; } = points;

        [JsonProperty("outcome")]
        public Outcome Outcome { get; } = outcome;
    }
}
