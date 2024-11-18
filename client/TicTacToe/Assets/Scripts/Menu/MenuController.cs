using Cysharp.Threading.Tasks;
using System;
using System.Threading;

public class MenuController
{
    private readonly MenuView view;
    private readonly Blockchain blockchain;
    private readonly IMatchmakingService matchmakingService;
    private readonly Action<Uri, string, string> onStartGame;


    private CancellationTokenSource cts;

    public MenuController(
        MenuView view,
        Blockchain blockchain,
        IMatchmakingService matchmakingService,
        Action<Uri, string, string> onStartGame
    )
    {
        this.view = view;
        this.blockchain = blockchain;
        this.matchmakingService = matchmakingService;
        this.onStartGame = onStartGame;

        view.OnLogin += OnLogin;
        view.OnPlay += OnPlay;
        view.OnCancel += OnCancel;
    }

    public void SetVisible(
        bool visible
    )
    {
        view.SetVisible(visible);
    }

    public async UniTask SyncPoints()
    {
        var points = await blockchain.GetPoints();
        view.SetLogin(blockchain.SignatureProvider.PubKey, points);
    }

    private async void OnLogin(string privKey)
    {
        view.SetInfo("Logging in...");
        await blockchain.Login(privKey);
        view.SetInfo("Syncing points...");
        await SyncPoints();
    }

    private async void OnPlay()
    {
        cts?.Dispose();
        cts = new CancellationTokenSource();

        view.SetInfo("Clearing pending tickets...");
        await matchmakingService.CancelAllMatchmakingTicketsForPlayer(new()
        {
            Address = blockchain.SignatureProvider.PubKey
        }, cts.Token);

        view.SetInfo("Creating ticket...");
        var response = await matchmakingService.CreateMatchmakingTicket(new()
        {
            Address = blockchain.SignatureProvider.PubKey,
        }, cts.Token);
        var ticketId = response.TicketId;
        view.SetInfo($"Waiting for match with ticket ID {ticketId}...");
        string matchId = null;
        while (!cts.Token.IsCancellationRequested)
        {
            var ticket = await matchmakingService.GetMatchmakingTicket(new()
            {
                Address = blockchain.SignatureProvider.PubKey,
                TicketId = ticketId
            }, cts.Token);

            if (ticket.MatchId != null)
            {
                matchId = ticket.MatchId;
                break;
            }

            view.SetInfo($"Waiting for match with ticket ID {ticketId}...\nStatus: {ticket.Status}");
            await UniTask.Delay(1000);
        }

        view.SetInfo($"Match found! Getting server details for match ID {matchId}...");
        var match = await matchmakingService.GetMatch(new()
        {
            Address = blockchain.SignatureProvider.PubKey,
            MatchId = matchId
        }, cts.Token);

        var node = new UriBuilder(match.ServerDetails);
        // fix docker network mapping
        if (node.Host == "host.docker.internal")
        {
            node.Host = "localhost";
        }

        var opponent = match.OpponentId;
        view.SetInfo($"Playing against {opponent}, connecting to {node}...");
        onStartGame?.Invoke(node.Uri, matchId, opponent);
    }

    private void OnCancel()
    {
        cts.Cancel();
    }
}
