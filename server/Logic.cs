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
    private readonly Blockchain blockchain;
    private readonly CancellationTokenSource cts = new();
    private readonly TaskCompletionSource connectCs = new();
    private readonly INodeConfig config;
    private List<Buffer> players => points.Keys.ToList();
    private readonly Dictionary<Buffer, int> points = new();
    private readonly bool isAi;
    private AllianceGamesServer? server = null;
    private int readyPlayers = 0;
    private Buffer currentPlayerId = Buffer.Empty();
    private TaskCompletionSource<Messages.MoveResponse>? moveTcs;

    private readonly Messages.Field[,] board = new Messages.Field[3, 3];

    public Logic(INodeConfig config, bool isAi)
    {
        this.config = config;
        blockchain = BlockchainFactory.Get();
        this.isAi = isAi;
    }

    public async Task Forfeit(Buffer address)
    {
        Log.Information($"Player {address.Parse()} forfeited the game");

        var winnerField = GetField(players.First(p => p != address.Parse()));
        await GameOver(winnerField);
    }

    public async Task Run()
    {
        server = await AllianceGamesServer.Create(
            new WebSocketTransport(config.Logger),
            config
        );

        RegisterHandlers();

        if (server == null)
        {
            config.Logger.Error("Failed to create server");
            return;
        }

        try
        {
            Log.Information($"Login to blockchain");
            await blockchain.Login();

            Log.Information($"Waiting for both players to connect");
            await connectCs.Task;

            Log.Information($"Initializing board");
            InitializeBoard();

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
                        new Messages.Sync(GetField(currentPlayerId), board).Encode(),
                        CancellationToken
                    );
                    Log.Information($"Send request to player {currentPlayerId.Parse()}");

                    int move;
                    if (isAi && currentPlayerId == aiAddress)
                    {
                        move = RandomMove();
                        await server.Delay("ai-move", 1000, CancellationToken);
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
                new Messages.Sync(GetField(currentPlayerId), board).Encode(),
                CancellationToken
            );

            await GameOver(winner);
        }
        catch (WebSocketException) { }
        catch (OperationCanceledException) { }
    }

    private async Task GameOver(Messages.Field? winner)
    {
        Buffer? winnerPlayer = winner == null ? null : players[(int)winner - 1];
        Log.Information($"Game is over, winner is {winnerPlayer?.Parse()}");
        await server!.Send(
            (int)Messages.Header.GameOver,
            winnerPlayer ?? Buffer.Empty(),
            default
        );

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

        await server.Stop(JsonConvert.SerializeObject(blockchainReward));
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
        return validMoves[server!.Random.Next(validMoves.Count)];
    }

    private Buffer NextPlayer(Buffer currentPlayer)
    {
        return players.First(p => p != currentPlayer);
    }

    private async Task<Messages.MoveResponse?> RequestMove(Buffer currentPlayerId, CancellationToken ct)
    {
        var delayCts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            try
            {
                ct.Register(() => delayCts.Cancel());
                Log.Information($"Starting timeout");
                await server!.Delay("move", 5000, delayCts.Token);
                Log.Information($"Timeout invoked");
                moveTcs?.TrySetCanceled();
            }
            catch (TaskCanceledException)
            {
                Log.Information($"TaskCanceledException Timeout");
            }
            catch (OperationCanceledException)
            {
                Log.Information($"OperationCanceledException Timeout");
            }
        });

        moveTcs = new TaskCompletionSource<Messages.MoveResponse>();

        Log.Information($"Send request start");
        await server!.Send(
            (int)Messages.Header.MoveRequest,
            currentPlayerId,
            Buffer.Empty(),
            ct
        );
        Log.Information($"Send request done");

        try
        {
            var res = await moveTcs.Task;
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
        finally
        {
            delayCts.Cancel();
        }
    }

    private void RegisterHandlers()
    {
        server!.RegisterMessageHandler((int)Messages.Header.Ready, async (address, _) =>
        {
            if (!server.Clients.Contains(address))
            {
                Log.Error($"Message.Ready:: Player {address.Parse()} is not in the game");
                return;
            }

            int playerPoints = 0;
            try
            {
                playerPoints = await blockchain.GetPoints(address.Parse());
            }
            catch (Exception e)
            {
                Log.Error(e, $"Failed to get points for player {address.Parse()}");
            }

            points.Add(address, playerPoints);

            if (isAi)
            {
                var aiPoints = await blockchain.GetPoints(AI_ADDRESS);
                points.Add(aiAddress, aiPoints);
                players.Add(aiAddress);
                Log.Information($"Add AI player with {aiPoints} points");
            }

            readyPlayers++;
            Log.Information($"Player {address.Parse()} is ready");
            if (readyPlayers == (isAi ? 1 : 2))
            {
                connectCs.SetResult();
            }
        });

        server.RegisterMessageHandler((int)Messages.Header.MoveResponse, (address, data) =>
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

            moveTcs.SetResult(new Messages.MoveResponse(data));
        });

        server.RegisterMessageHandler((int)Messages.Header.Forfeit, async (address, _) =>
        {
            if (!players.Contains(address))
            {
                Log.Error($"Player {address.Parse()} is not in the game");
                return;
            }

            await Forfeit(address);
        });

        server.RegisterRequestHandler((int)Messages.Header.PlayerDataRequest, async (address, data) =>
        {
            await connectCs.Task;

            if (!players.Contains(address))
            {
                Log.Error($"Player {address.Parse()} is not in the game");
                return Buffer.Empty();
            }

            return new Messages.PlayerDataResponse(players.Select(
                p => new Messages.PlayerDataResponse.Player(p, points[p], GetField(p))
            ).ToArray()).Encode();
        });
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

    private class Reward(Buffer pubKey, int points)
    {
        [JsonProperty("pubkey")]
        public string PubKey { get; } = pubKey.Parse();
        [JsonProperty("points")]
        public int Points { get; } = points;
    }
}
