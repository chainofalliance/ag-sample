using Cysharp.Threading.Tasks;
using System;
using System.Threading;

public class MenuController
{
    private readonly MenuView view;
    private readonly Blockchain blockchain;
    private readonly Blockchain agBlockchain;
    private readonly AgMatchmaking.IMatchmakingService matchmakingService;
    private readonly Action<Uri, string> onStartGame;


    private CancellationTokenSource cts;

    public MenuController(
        MenuView view,
        Blockchain blockchain,
        Blockchain agBlockchain,
        AgMatchmaking.IMatchmakingService agMatchmakingService,
        Action<Uri, string> onStartGame
    )
    {
        this.view = view;
        this.blockchain = blockchain;
        this.agBlockchain = agBlockchain;
        this.matchmakingService = agMatchmakingService;
        this.onStartGame = onStartGame;

        view.OnLogin += OnLogin;
        view.OnSync += async () => await SyncPoints();
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
        await blockchain.Login(BlockchainConfig.TTT(view.ConnectToDevnet), privKey);
        await agBlockchain.Login(BlockchainConfig.AG(view.ConnectToDevnet), privKey);
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
            Creator = blockchain.SignatureProvider.PubKey,
            Duid = AgMatchmaking.MatchmakingServiceFactory.DUID
        }, cts.Token);

        view.SetInfo("Creating ticket...");
        var response = await matchmakingService.CreateMatchmakingTicket(new()
        {
            Creator = blockchain.SignatureProvider.PubKey,
            Duid = AgMatchmaking.MatchmakingServiceFactory.DUID,
            QueueName = AgMatchmaking.MatchmakingServiceFactory.QUEUE
        }, cts.Token);


        if (response.Status == Chromia.TransactionReceipt.ResponseStatus.Rejected)
        {
            view.SetInfo("Creating ticket transaction got rejected " + response.RejectReason);
            return;
        }

        var ticketId = await matchmakingService.GetMatchmakingTicket(new()
        {
            Creator = blockchain.SignatureProvider.PubKey,
            Duid = AgMatchmaking.MatchmakingServiceFactory.DUID,
            QueueName = AgMatchmaking.MatchmakingServiceFactory.QUEUE
        }, cts.Token);

        view.SetInfo($"Waiting for match with ticket ID {ticketId}...");
        string sessionId = null;
        while (!cts.Token.IsCancellationRequested)
        {
            var ticket = await matchmakingService.GetMatchmakingTicketStatus(new()
            {
                TicketId = ticketId
            }, cts.Token);

            if (ticket.SessionId != "")
            {
                sessionId = ticket.SessionId;
                break;
            }

            view.SetInfo($"Waiting for match with ticket ID {ticketId}...\nStatus: {ticket.Status}");
            await UniTask.Delay(1000);
        }

        view.SetInfo($"Match found! Getting server details for match ID {sessionId}...");
        var connectionDetails = await matchmakingService.GetConnectionDetails(new()
        {
            SessionId = sessionId
        }, cts.Token);

        var node = new UriBuilder(connectionDetails);
        // fix docker network mapping
        if (node.Host == "host.docker.internal")
        {
            node.Host = "localhost";
        }

        view.SetInfo($"Connecting to {node}...");
        onStartGame?.Invoke(node.Uri, sessionId);
    }

    private void OnCancel()
    {
        cts.Cancel();
    }
}