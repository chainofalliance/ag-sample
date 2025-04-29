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
            Brid = "4FC7F780620D35B0BAE620DA69DC1476AA676AE7F11A640C65D88127EFFAA08B",
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
            Brid = "63F766110ED31818038A323D849ECBA64E85ABA5104E6D7F24014CEF2F0756A5",
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
