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
    public const string DEFAULT_ADMIN_PRIVKEY = "854D8402085EC5F737B1BE63FFD980981EED2A0DA5FAC6B4468CB1F176BA0321";

    public SignatureProvider SignatureProvider { get; set; }
    public ITransport Transport { get; set; }

    private ChromiaClient client;

    private readonly string nodeUrl;
    private readonly int chainId;

    public Blockchain(string nodeUrl, int chainId)
    {
        this.nodeUrl = nodeUrl;
        this.chainId = chainId;

    }

    public async Task Login(string privKey)
    {
        client = await ChromiaClient.Create(nodeUrl, chainId);
        SignatureProvider = SignatureProvider.Create(Buffer.From(privKey));
    }

    public async Task<int> GetPoints(string pubKey = null)
    {
        return await client.Query<int>(
            "ttt.ILeaderboard.get_points",
            ("pubkey", pubKey != null ? Buffer.From(pubKey) : SignatureProvider.PubKey)
        );
    }
}