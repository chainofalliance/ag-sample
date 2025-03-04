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
#if DEVNET
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
            Brid = "8F9530C6A159EE2108953462AE74A5F8D6C26EBC84F07DF763E4315D973D5205",
            NodeUrls = new[]
            {
                "https://node8.devnet1.chromia.dev:7740/",
                "https://node9.devnet1.chromia.dev:7740/",
                "https://node10.devnet1.chromia.dev:7740/",
                "https://node11.devnet1.chromia.dev:7740/"
            }
        };
#endif
    }

    public static BlockchainConfig AllianceGames()
    {
#if DEVNET
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
            Brid = "8F9530C6A159EE2108953462AE74A5F8D6C26EBC84F07DF763E4315D973D5205",
            NodeUrls = new[]
            {
                "https://node8.devnet1.chromia.dev:7740/",
                "https://node9.devnet1.chromia.dev:7740/",
                "https://node10.devnet1.chromia.dev:7740/",
                "https://node11.devnet1.chromia.dev:7740/"
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
