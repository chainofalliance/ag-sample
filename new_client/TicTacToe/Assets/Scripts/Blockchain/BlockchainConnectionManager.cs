using System.Threading.Tasks;
using Chromia.Transport;
using System.Linq;
using Chromia;

public struct BlockchainConfig
{
    public string[] NodeUrls;
    public string Brid;
    public int ChainId;

    public static BlockchainConfig TicTacToe()
    {
#if AG_DEVNET
    return new()
    {
        ChainId = 2,
        NodeUrls = new[]
        {
            "http://localhost:7740/"
        }
    };
#else
        return new()
        {
            Brid = "1DE8D3B60899F944CE845239242F01F874ABD384272A9A2439CBFF5AE648C64F",
            NodeUrls = new[]
            {
                "https://node1.testnet.chromia.com:7740/",
                "https://node2.testnet.chromia.com:7740/",
                "https://node3.testnet.chromia.com:7740/"
            }
        };
#endif
    }

    public static BlockchainConfig AllianceGames()
    {
#if AG_DEVNET
    return new()
    {
        ChainId = 1,
        NodeUrls = new[]
        {
            "http://localhost:7740/"
        }
    };
#else
        return new()
        {
            Brid = "58537AE680F4BB0CD7D49A399A655D9823223B3B8DAE27155F8C922A86D28E46",
            NodeUrls = new[]
            {
                "https://node1.testnet.chromia.com:7740/",
                "https://node2.testnet.chromia.com:7740/",
                "https://node3.testnet.chromia.com:7740/"
            }
        };
#endif
    }
}

public class BlockchainConnectionManager
{
    public ITransport Transport { get; set; }
    public ChromiaClient AlliancesGamesClient;
    public ChromiaClient TicTacToeClient;

    public async Task Connect()
    {
        Transport = new AllianceGamesSdk.Unity.UnityTransport();
        ChromiaClient.SetTransport(Transport);
        AlliancesGamesClient = await InternalConnect(BlockchainConfig.AllianceGames());
        TicTacToeClient = await InternalConnect(BlockchainConfig.TicTacToe());
    }

    private async Task<ChromiaClient> InternalConnect(BlockchainConfig config)
    {
        if (!string.IsNullOrEmpty(config.Brid))
        {
            return await ChromiaClient.Create(config.NodeUrls.ToList(), Buffer.From(config.Brid));
        }
        else
        {
            return await ChromiaClient.Create(config.NodeUrls.ToList(), config.ChainId);
        }
    }
}
