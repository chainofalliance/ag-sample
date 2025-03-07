using Reown.Core.Common.Logging;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using Reown.AppKit.Unity;
using Reown.Sign.Unity;
using UnityEngine;
using System;

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

            var account = await eventArgs.GetAccount();
            var res = await Queries.GetPlayerInfo(connectionManager.TicTacToeClient, Chromia.Buffer.From(account.Address));
            Debug.Log(res.ToString());
        };

        OnChangeScreen(Screen.LOGIN);

        //// TODO
        //var res = await Queries.GetPlayerInfo(connectionManager.TicTacToeClient, accountManager.SignatureProvider.PubKey);
        //Debug.Log(res.ToString());
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
