using Chromia;
using Chromia.Transport;
using Buffer = Chromia.Buffer;

public static class BlockchainFactory
{
    public static Blockchain Get()
    {
#if PROD_ENV
        return new Blockchain("http://bc.ttt.com/");
#else
        return new Blockchain("http://host.docker.internal:7740/", 2);
#endif
    }
}

public class Blockchain
{
    public ITransport Transport { get; set; }

    private ChromiaClient client;

    private readonly string nodeUrl;
    private readonly int chainId;

    public Blockchain(string nodeUrl, int chainId)
    {
        this.nodeUrl = nodeUrl;
        this.chainId = chainId;

    }

    public async Task Login()
    {
        client = await ChromiaClient.Create(nodeUrl, chainId);
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