using AllianceGamesSdk.Client;
using System.Threading;
using System;

public class GameController
{
    private readonly GameView view;
    private readonly AccountManager accountManager;

    private AllianceGamesClient allianceGamesClient;
    private CancellationTokenSource cts;

    public GameController(
        GameView view,
        AccountManager accountManager,
        Action<Uri, string> OnStartGame
    )
    {
        this.view = view;
        this.accountManager = accountManager;
        OnStartGame += StartGame;
    }

    public void SetVisible(bool visible)
    {
        view.SetVisible(visible);
    }

    private async void StartGame(Uri nodeUri, string matchId)
    {
        cts = new CancellationTokenSource();
    }
}
