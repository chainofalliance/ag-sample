using AllianceGamesSdk.Common;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
    private ITaskRunner taskRunner;
    private MenuController menuController;
    private GameController gameController;

    private void Start()
    {
        root = mainDocument.rootVisualElement;

        menu = mainDocument.rootVisualElement.Q<VisualElement>("Menu");
        game = mainDocument.rootVisualElement.Q<VisualElement>("Game");

        taskRunner = new UniTaskRunner();
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
            taskRunner,
            OnEndGame
        );

        menuController.SetVisible(true);
        gameController.SetVisible(false);
    }

    private async void OnStartGame(
        Uri nodeUri,
        string matchId
    )
    {
        menuController.SetVisible(false);
        gameController.SetVisible(true);
        await gameController.StartGame(
            nodeUri,
            matchId
        );
    }

    private async void OnEndGame()
    {
        menuController.SetVisible(true);
        gameController.SetVisible(false);
        await menuController.SyncPoints();
    }

    class UniTaskRunner : ITaskRunner
    {
        public async Task Delay(int millisecondsDelay, CancellationToken cancellationToken)
        {
            await UniTask.Delay(millisecondsDelay, cancellationToken: cancellationToken);
        }

        public async IAsyncEnumerable<T> Yield<T>(
            System.Threading.Channels.ChannelReader<T> reader,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                yield return await reader.ReadAsync(cancellationToken).AsUniTask();
            }
        }
    }
}
