using Reown.Core.Common.Logging;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using Reown.AppKit.Unity;
using Reown.Sign.Unity;
using UnityEngine;
using System;
using Reown.Core.Crypto;

public class Bootstrap : MonoBehaviour
{
    [SerializeField]
    private UIDocument mainDocument;

    private VisualElement root;
    private LoginController loginController;
    private MenuController menuController;
    private GameController gameController;

    private BlockchainConnectionManager connectionManager;
    private AccountManager accountManager;

    private Chain bnbTestnet = new Chain(
        ChainConstants.Namespaces.Evm,
        chainReference: "97",
        name: "BNB Smart Chain Testnet",
        nativeCurrency: new Currency("Testnet Binance Coin", "tBNB", 18),
        rpcUrl: "https://data-seed-prebsc-1-s1.binance.org:8545",
        blockExplorer: new BlockExplorer("BnbTestnet", "https://testnet.bscscan.com"),
        isTestnet: true,
        imageUrl: $"https://api.web3modal.com/public/getAssetImage/692ed6ba-e569-459a-556a-776476829e00",
        viemName: null
    );

    private async void Start()
    {
        root = mainDocument.rootVisualElement;
        var loginElement = mainDocument.rootVisualElement.Q<VisualElement>("LoginScreen");
        var menuElement = mainDocument.rootVisualElement.Q<VisualElement>("MenuScreen");
        var gameElement = mainDocument.rootVisualElement.Q<VisualElement>("GameScreen");

        await AppKitInit();

        connectionManager = new BlockchainConnectionManager();
        await connectionManager.Connect();
        accountManager = new AccountManager();

        var loginView = new LoginView(loginElement);
        loginController = new LoginController(loginView);

        var menuView = new MenuView(menuElement);
        menuController = new MenuController(menuView, connectionManager, accountManager, OnStartGame);

        var gameView = new GameView(gameElement);
        gameController = new GameController(gameView, accountManager, OnEndGame);

        AppKit.AccountDisconnected += (sender, eventArgs) => {
            OnChangeScreen(Screen.LOGIN);
        };

        AppKit.AccountConnected += async (sender, eventArgs) => {
            OnChangeScreen(Screen.MENU);

            await AppKit.NetworkController.ChangeActiveChainAsync(ChainConstants.Chains.Ethereum);

            //var account = await eventArgs.GetAccount();
            //var res = await Queries.GetPlayerInfo(connectionManager.TicTacToeClient, Chromia.Buffer.From(account.Address));
            //Debug.Log(res.ToString());

            //var events = await Queries.GetUnclaimedEifEvents(connectionManager.AlliancesGamesClient, Chromia.Buffer.From(account.Address));
            //Debug.Log("Amount events found: " + events.Length);

            //foreach (var e in events)
            //{
            //    Debug.Log(e.ToString());
            //    var rawMerkleProof = await Queries.GetEventMerkleProof(connectionManager.AlliancesGamesClient, e.EventHash);
            //    var merkleProof = EIFUtils.Construct(rawMerkleProof);

            //    await TicTacToeContract.Claim(merkleProof, e.EncodedData);
            //}

            //var myPoints = await TicTacToeContract.GetPoints(account.Address);
            //Debug.Log("MyPoints: " + myPoints);
        };

        OnChangeScreen(Screen.LOGIN);
    }

    private async Task AppKitInit()
    {
        ReownLogger.Instance = new UnityLogger();
        var appKitConfig = new AppKitConfig
        {
            projectId = "7457ffbc1346fad3a828e49743fba2e0",
            metadata = new Metadata(
                "TicTacToe",
                "TicTacToe Powered By Alliance Games",
                "https://alliancegames.xyz/",
                "https://raw.githubusercontent.com/reown-com/reown-dotnet/main/media/appkit-icon.png"
            )
            //,
            //supportedChains = new[]
            //{
            //    bnbTestnet
            //}
        };

        Debug.Log("[AppKit Init] Initializing AppKit...");

        await AppKit.InitializeAsync(
            appKitConfig
        );
    }

    private async void OnStartGame(Uri nodeUri, string matchId)
    {
        await gameController.StartGame(nodeUri, matchId);
        OnChangeScreen(Screen.GAME);
    }

    private void OnEndGame()
    {
        OnChangeScreen(Screen.MENU);
    }

    public enum Screen
    {
        LOGIN,
        MENU,
        GAME
    }
    private void OnChangeScreen(Screen screen)
    {
        loginController.SetVisible(false);
        menuController.SetVisible(false);
        gameController.SetVisible(false);

        switch (screen)
        {
            case Screen.LOGIN:
                loginController.SetVisible(true);
                break;
            case Screen.MENU:
                menuController.SetVisible(true);
                break;
            case Screen.GAME:
                gameController.SetVisible(true);
                break;
        }
    }
}
