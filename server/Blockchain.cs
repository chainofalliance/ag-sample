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
            Brid = "020922A1C2E0D30E5463ED93BCA46217CD8E777DFE49E65DBDBA26905EAAE5FE",
            NodeUrls = new[]
            {
                "https://node1.testnet.chromia.com:7740/",
                "https://node2.testnet.chromia.com:7740/",
                "https://node3.testnet.chromia.com:7740/"
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
