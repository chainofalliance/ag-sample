using Chromia;
using Chromia.Encoding;
using Chromia.Transport;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Utilities;
using System.Linq;
using Buffer = Chromia.Buffer;

public struct BlockchainConfig
{
    public string[] NodeUrls;
    public string Brid;
    public int ChainId;

    public static BlockchainConfig TTT(bool devnet)
    {
        return !devnet ? new() { ChainId = 2, NodeUrls = new[] { "http://localhost:7740/" } } : new()
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

    public static BlockchainConfig AG(bool devnet)
    {
        return !devnet ? new() { ChainId = 1, NodeUrls = new[] { "http://localhost:7740/" } } : new()
        {
            Brid = "2840DCF725182C0D7731FE41A110A2FEC3A7B5CF944DF02596D423091364F62C",
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
    public SignatureProvider SignatureProvider { get; set; }
    public ITransport Transport { get; set; }

    public ChromiaClient Client;

    public async UniTask Login(BlockchainConfig config, string privKey)
    {
        Transport = new UnityTransport();
        UnityEngine.Debug.Log("Creating Chromia Client...");
        ChromiaClient.SetTransport(Transport);

        if (!string.IsNullOrEmpty(config.Brid))
        {
            Client = await ChromiaClient.Create(config.NodeUrls.ToList(), Buffer.From(config.Brid));
        }
        else
        {
            Client = await ChromiaClient.Create(config.NodeUrls.ToList(), config.ChainId);
        }


        UnityEngine.Debug.Log("Creating SignatureProvider...");
        SignatureProvider = SignatureProvider.Create(Buffer.From(privKey));
    }

    public async UniTask<int> GetPoints(string pubKey = null)
    {
        return await Client.Query<int>(
            "ttt.ILeaderboard.get_points",
            ("pubkey", pubKey != null ? Buffer.From(pubKey) : SignatureProvider.PubKey)
        );
    }

    public void AotTypeEnforce()
    {
        AotHelper.EnsureType<BufferConverter>();
        AotHelper.EnsureType<BigIntegerConverter>();
    }
}