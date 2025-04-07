using Reown.Core.Common.Logging;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using Reown.AppKit.Unity;
using Reown.Sign.Unity;
using UnityEngine;
using System;
using System.Collections.Generic;
using TTT.Components;
using Buffer = Chromia.Buffer;

public class Bootstrap : MonoBehaviour
{
    [SerializeField]
    private UIDocument mainDocument;

    private VisualElement root;
    private LoginController loginController;
    private MenuController menuController;
    private GameController gameController;
    private NavbarController navbarController;

    private ModalInfo modalInfo;

    private BlockchainConnectionManager connectionManager;
    private AccountManager accountManager;

    public static Chain ChainBNBTestnet = new Chain(
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
        modalInfo = mainDocument.rootVisualElement.Q("ModalInfo").Q<ModalInfo>();
        var loginElement = mainDocument.rootVisualElement.Q<VisualElement>("LoginScreen");
        var menuElement = mainDocument.rootVisualElement.Q<VisualElement>("MenuScreen");
        var gameElement = mainDocument.rootVisualElement.Q<VisualElement>("GameScreen");
        var sideNavbarElement = mainDocument.rootVisualElement.Q<VisualElement>("ContainerWithSideBar");

        await AppKitInit();
        connectionManager = new BlockchainConnectionManager();
        await connectionManager.Connect();

        accountManager = new AccountManager(connectionManager);

        var navbarView = new NavbarView(sideNavbarElement);
        navbarController = new NavbarController(navbarView, accountManager);
        navbarController.OnDisconnect += () =>
        {
            OnChangeScreen(Screen.LOGIN);
        };

        var loginView = new LoginView(loginElement);
        loginController = new LoginController(loginView, accountManager);

        var menuView = new MenuView(menuElement);
        menuController = new MenuController(menuView, connectionManager, accountManager, OnStartGame);
        menuController.OnClaim += (unclaimedRewards) => OnClaim(unclaimedRewards);

        var gameView = new GameView(gameElement);
        gameController = new GameController(gameView, accountManager, connectionManager, OnEndGame);
        navbarController.OnHome += () =>
        {
            gameController.OpenCancelGame();
        };
        gameController.OnClaim += () => OnClaim(null);

        accountManager.OnAddressConnected += async (_) =>
        {
            await menuController.UpdatePlayerInfo();
            OnChangeScreen(Screen.MENU);
        };

        AppKit.AccountDisconnected += (sender, eventArgs) =>
        {
            OnChangeScreen(Screen.LOGIN);
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
            ),
            supportedChains = new[]
            {
                ChainBNBTestnet
            }
        };

        await AppKit.InitializeAsync(
            appKitConfig
        );
    }

    private async void OnClaim(Queries.EifEventData[] unclaimedRewards)
    {
        try
        {
            const string title = "Claiming rewards...";
            modalInfo.ShowInfo(title, "Fetching unclaimed rewards...");
            unclaimedRewards ??= await accountManager.GetUnclaimedEifEvents();

            if (unclaimedRewards.Length == 0)
            {
                await modalInfo.ShowError("No unclaimed rewards.");
                return;
            }

            modalInfo.ShowInfo(title, "Fetching proof events for rewards...");
            var claimData = new List<TicTacToeContract.ClaimData>();
            foreach (var e in unclaimedRewards)
            {
                var rawMerkleProof = await Queries.GetEventMerkleProof(connectionManager.AlliancesGamesClient, e.EventHash);
                if (!rawMerkleProof.HasValue || rawMerkleProof.Value.EventData.IsEmpty)
                {
                    await modalInfo.ShowError($"Failed to get merkle proof for event: {e.EventHash}");
                    modalInfo.ShowInfo(title, "Fetching proof events for rewards...");
                    continue;
                }

                var merkleProof = EIFUtils.Construct(rawMerkleProof.Value);
                claimData.Add(new TicTacToeContract.ClaimData
                {
                    EventWithProof = merkleProof,
                    EncodedData = e.EncodedData
                });
            }

            if (claimData.Count == 0)
            {
                await modalInfo.ShowError("No unclaimed rewards.");
                return;
            }

            modalInfo.ShowInfo(title, "Waiting for transaction confirmation...");
            var result = await accountManager.TicTacToeContract.ClaimBatch(claimData.ToArray());

            modalInfo.ShowInfo(title, "Syncing player data...");
            await menuController.UpdatePlayerInfo();

            modalInfo.SetInfoClickCallback(() =>
            {
                Application.OpenURL($"https://testnet.bscscan.com/tx/{result}");
            });
            await modalInfo.Show("Claiming Success!", $"{claimData.Count} reward{(claimData.Count == 1 ? "" : "s")} claimed at transaction hash <color=cyan><u>{result}</u></color>");
            gameController.OnSuccessfulClaim();
        }
        catch (Exception e)
        {
            await modalInfo.ShowError($"Failed to claim rewards", e);
        }
    }

    private async void OnStartGame(Uri nodeUri, string matchId, Buffer coordinatorPubkey)
    {
        var res = await gameController.StartGame(nodeUri, matchId, coordinatorPubkey);
        if (res)
        {
            OnChangeScreen(Screen.GAME);
        }
    }

    private void OnEndGame(GameController.AfterGameAction afterGameAction)
    {
        OnChangeScreen(Screen.MENU);
        if (afterGameAction == GameController.AfterGameAction.NextRoundPvE)
        {
            menuController.OpenPvEMatchmaking();
        }
        else if (afterGameAction == GameController.AfterGameAction.NextRoundPvP)
        {
            menuController.OpenPvPMatchmaking();
        }
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
                navbarController.SetVisible(false);
                loginController.SetVisible(true);
                break;
            case Screen.MENU:
                navbarController.SetVisible(true);
                menuController.SetVisible(true);
                break;
            case Screen.GAME:
                navbarController.SetVisible(true);
                gameController.SetVisible(true);
                break;
        }
    }
}