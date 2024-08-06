using Chromia;
using Cysharp.Threading.Tasks;

using Buffer = Chromia.Buffer;

public static class BlockchainFactory
{
    public static Blockchain Get()
    {
#if PROD_ENV
        return new Blockchain("http://bc.ttt.com/");
#else
        return new Blockchain("http://localhost:7740/");
#endif
    }
}

public class Blockchain
{
    public SignatureProvider SignatureProvider { get; private set; }

    private ChromiaClient client;

    private readonly string nodeUrl;

    public Blockchain(string nodeUrl)
    {
        this.nodeUrl = nodeUrl;
    }

    public async UniTask Login(string privKey)
    {
        client = await ChromiaClient.Create(nodeUrl, 0);
        SignatureProvider = SignatureProvider.Create(Buffer.From(privKey));
    }

    public async UniTask<int> GetPoints(string pubKey = null)
    {
        return await client.Query<int>(
            "ttt.ILeaderboard.get_points",
            ("pubkey", pubKey != null ? Buffer.From(pubKey) : SignatureProvider.PubKey)
        );
    }
}