using Reown.Core.Common.Logging;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using Reown.AppKit.Unity;
using Reown.Sign.Unity;
using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    [SerializeField]
    private UIDocument mainDocument;

    private VisualElement root;
    private LoginController loginController;
    private MenuController menuController;

    private async void Start()
    {
        root = mainDocument.rootVisualElement;
        var loginElement = mainDocument.rootVisualElement.Q<VisualElement>("LoginScreen");
        var menuElement = mainDocument.rootVisualElement.Q<VisualElement>("MenuScreen");

        await AppKitInit();

        AppKit.AccountDisconnected += (sender, eventArgs) => {
            OnChangeScreen(Screen.LOGIN);
        };

        AppKit.AccountConnected += (sender, eventArgs) => {
            OnChangeScreen(Screen.MENU);
        };

        var blockchainConnectionManager = new BlockchainConnectionManager();
        //await blockchainConnectionManager.Connect();
        var accountManager = new AccountManager();

        var loginView = new LoginView(loginElement);
        loginController = new LoginController(loginView);

        var menuView = new MenuView(menuElement);
        menuController = new MenuController(menuView);

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
        };

        Debug.Log("[AppKit Init] Initializing AppKit...");

        await AppKit.InitializeAsync(
            appKitConfig
        );
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

        switch (screen)
        {
            case Screen.LOGIN:
                loginController.SetVisible(true);
                break;
            case Screen.MENU:
                menuController.SetVisible(true);
                break;
            case Screen.GAME:
                break;
        }
    }
}
