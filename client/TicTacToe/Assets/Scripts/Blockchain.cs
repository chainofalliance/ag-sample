using Chromia;
using Chromia.Encoding;
using Chromia.Transport;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Utilities;
using System.Collections.Generic;
using System.Linq;
using Buffer = Chromia.Buffer;

public static class BlockchainFactory
{
    public static Blockchain Get()
    {
#if PROD_ENV
        return new Blockchain("8F9530C6A159EE2108953462AE74A5F8D6C26EBC84F07DF763E4315D973D5205",
            "https://node8.devnet1.chromia.dev:7740/",
            "https://node9.devnet1.chromia.dev:7740/",
            "https://node10.devnet1.chromia.dev:7740/",
            "https://node11.devnet1.chromia.dev:7740/"
        );
#else
        return new Blockchain(2, "http://localhost:7740/");
#endif
    }

    public static Blockchain GetAg()
    {
#if PROD_ENV
        return new Blockchain("2840DCF725182C0D7731FE41A110A2FEC3A7B5CF944DF02596D423091364F62C",
            "https://node8.devnet1.chromia.dev:7740/",
            "https://node9.devnet1.chromia.dev:7740/",
            "https://node10.devnet1.chromia.dev:7740/",
            "https://node11.devnet1.chromia.dev:7740/"
        );
#else
        return new Blockchain(1, "http://localhost:7740/");
#endif
    }
}

public class Blockchain
{
    public SignatureProvider SignatureProvider { get; set; }
    public ITransport Transport { get; set; }

    public ChromiaClient Client;

    private readonly List<string> nodeUrls;
    private readonly int chainId = 0;
    private readonly string brid = string.Empty;

    public Blockchain(int chainId, params string[] nodeUrls)
    {
        this.nodeUrls = nodeUrls.ToList();
        this.chainId = chainId;
    }

    public Blockchain(string brid, params string[] nodeUrls)
    {
        this.nodeUrls = nodeUrls.ToList();
        this.brid = brid;
    }

    public async UniTask Login(string privKey)
    {
        Transport = new UnityTransport();
        UnityEngine.Debug.Log("Creating Chromia Client...");
        ChromiaClient.SetTransport(Transport);
        if (brid != string.Empty)
        {
            Client = await ChromiaClient.Create(nodeUrls, Buffer.From(brid));
        }
        else
        {
            Client = await ChromiaClient.Create(nodeUrls, chainId);
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