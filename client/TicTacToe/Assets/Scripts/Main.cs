using AllianceGamesSdk.Common;
using AllianceGamesSdk.Common.Transport;
using AllianceGamesSdk.Matchmaking;
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
    private MenuController menuController;
    private GameController gameController;

    private void Start()
    {
        root = mainDocument.rootVisualElement;
        menu = mainDocument.rootVisualElement.Q<VisualElement>("LoginScreen");
        game = mainDocument.rootVisualElement.Q<VisualElement>("Game");

#if ENABLE_IL2CPP
        blockchain.AotTypeEnforce();
#endif
        var menuView = new MenuView(menu);
        menuController = new MenuController(
            menuView,
            blockchain,
            agBlockchain,
            MatchmakingServiceFactory.Get(agBlockchain.Client, agBlockchain.SignatureProvider),
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
}
