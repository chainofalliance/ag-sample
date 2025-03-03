using Chromia;
using Chromia.Transport;
using Buffer = Chromia.Buffer;

public struct BlockchainConfig
{
    public string[] NodeUrls;
    public string Brid;
    public int ChainId;

    public static BlockchainConfig TTT(bool devnet)
    {
        return !devnet ? new() { ChainId = 2, NodeUrls = new[] { "http://host.docker.internal:7740/" } } : new()
        {
            Brid = "8F9530C6A159EE2108953462AE74A5F8D6C26EBC84F07DF763E4315D973D5205",
            NodeUrls = new[]
            {
                "https://node8.devnet1.chromia.dev:7740/",
                "https://node9.devnet1.chromia.dev:7740/",
                "https://node10.devnet1.chromia.dev:7740/",
                "https://node11.devnet1.chromia.dev:7740/"
            }
        };
    }
}

public class Blockchain
{
    private ChromiaClient client;

    private Blockchain(ChromiaClient client)
    {
        this.client = client;
    }

    public static async Task<Blockchain> Create(BlockchainConfig config)
    {
        ChromiaClient client;
        if (!string.IsNullOrEmpty(config.Brid))
        {
            client = await ChromiaClient.Create(config.NodeUrls.ToList(), Buffer.From(config.Brid));
        }
        else
        {
            client = await ChromiaClient.Create(config.NodeUrls.ToList(), config.ChainId);
        }

        return new Blockchain(client);
    }

    public async Task<int> GetPoints(Buffer pubKey)
    {
        return await client.Query<int>(
            "ttt.ILeaderboard.get_points",
            ("pubkey", Buffer.From(pubKey))
        );
    }

    public async Task<int> GetPoints(string pubKey)
    {
        return await GetPoints(Buffer.From(pubKey));
    }
}