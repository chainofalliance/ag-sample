using AllianceGamesSdk.Common;
using AllianceGamesSdk.Common.Transport;
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
    public static bool MOCK = false;

    [SerializeField]
    private UIDocument mainDocument;

    private VisualElement root;

    private VisualElement menu;
    private VisualElement game;

    private Blockchain blockchain = new();
    private Blockchain agBlockchain = new();
    private ITaskRunner taskRunner;
    private IHttpClient httpClient;
    private MenuController menuController;
    private GameController gameController;

    private void Start()
    {
        root = mainDocument.rootVisualElement;

        menu = mainDocument.rootVisualElement.Q<VisualElement>("Menu");
        game = mainDocument.rootVisualElement.Q<VisualElement>("Game");

        taskRunner = new UniTaskRunner();
        httpClient = new UnityHttpClient(blockchain);
#if ENABLE_IL2CPP
        blockchain.AotTypeEnforce();
#endif
        var menuView = new MenuView(menu);
        menuController = new MenuController(
            menuView,
            blockchain,
            agBlockchain,
            AgMatchmaking.MatchmakingServiceFactory.Get(agBlockchain),
            OnStartGame
        );

        var gameView = new GameView(game);
        gameController = new GameController(
            gameView,
            blockchain,
            taskRunner,
            httpClient,
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

    class UnityHttpClient : IHttpClient
    {
        private readonly Blockchain blockchain;

        public UnityHttpClient(Blockchain blockchain)
        {
            this.blockchain = blockchain;
        }

        public async Task<string> Get(Uri uri, CancellationToken ct)
        {
            return await blockchain.Transport.Get(uri, ct);
        }
    }
}
