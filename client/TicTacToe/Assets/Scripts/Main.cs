using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class Main : MonoBehaviour
{
    public static bool MOCK = true;

    [SerializeField]
    private UIDocument mainDocument;

    private VisualElement root;

    private VisualElement menu;
    private VisualElement game;

    private Blockchain blockchain;
    private MenuController menuController;
    private GameController gameController;

#if UNITY_WEBGL && !UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeSynchronizationContext()
    {
        SynchronizationContext.SetSynchronizationContext(null);
    }
#endif

    private void Start()
    {
        root = mainDocument.rootVisualElement;

        menu = mainDocument.rootVisualElement.Q<VisualElement>("Menu");
        game = mainDocument.rootVisualElement.Q<VisualElement>("Game");
        
        blockchain = BlockchainFactory.Get();
#if ENABLE_IL2CPP
        blockchain.AotTypeEnforce();
#endif
        var menuView = new MenuView(menu);
        menuController = new MenuController(
            menuView,
            blockchain,
            MatchmakingServiceFactory.Get(MOCK),
            OnStartGame
        );

        var gameView = new GameView(game);
        gameController = new GameController(
            gameView,
            blockchain,
            OnEndGame
        );

        menuController.SetVisible(true);
        gameController.SetVisible(false);
    }

    private async void OnStartGame(
        Uri nodeUri,
        string matchId,
        string opponent
    )
    {
        menuController.SetVisible(false);
        gameController.SetVisible(true);
        await gameController.StartGame(
            nodeUri,
            matchId,
            opponent
        );
    }

    private async void OnEndGame()
    {
        menuController.SetVisible(true);
        gameController.SetVisible(false);
        await menuController.SyncPoints();
    }
}
