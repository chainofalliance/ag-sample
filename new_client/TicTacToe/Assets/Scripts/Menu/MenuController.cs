using AllianceGamesSdk.Matchmaking;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using System;

public class MenuController
{
    private readonly string DUID;
    private readonly string DISPLAY_NAME = "TicTacToe";
    private readonly string QUEUE_NAME = "1Vs1";

    private readonly MenuView view;
    private readonly BlockchainConnectionManager connectionManager;
    private readonly AccountManager accountManager;
    private readonly Action<Uri, string> OnStartGame;
    private CancellationTokenSource cts;

    public MenuController(
        MenuView view,
        BlockchainConnectionManager connectionManager,
        AccountManager accountManager,
        Action<Uri, string> OnStartGame
    )
    {
        this.view = view;
        this.connectionManager = connectionManager;
        this.accountManager = accountManager;

        view.OnPlayPve += OnPlay;
    }

    public void SetVisible(bool visible)
    {
        view.SetVisible(visible);
    }

    private async void OnPlay()
    {
        cts?.Dispose();
        cts = new CancellationTokenSource();

        var matchmakingService = MatchmakingServiceFactory.Get(
            connectionManager.AlliancesGamesClient, accountManager.SignatureProvider);

        var duid = DUID;
        duid ??= await MatchmakingService.GetDuid(connectionManager.AlliancesGamesClient, DISPLAY_NAME, cts.Token);

        Debug.Log("Clearing pending tickets...");
        await matchmakingService.CancelAllMatchmakingTicketsForPlayer(new()
        {
            Creator = accountManager.SignatureProvider.PubKey,
            Duid = duid
        }, cts.Token);

        Debug.Log("Creating ticket...");
        var response = await matchmakingService.CreateMatchmakingTicket(new()
        {
            Creator = accountManager.SignatureProvider.PubKey,
            Duid = duid,
            QueueName = QUEUE_NAME
        }, cts.Token);

        if (response.Status == Chromia.TransactionReceipt.ResponseStatus.Rejected)
        {
            Debug.Log("Creating ticket transaction got rejected " + response.RejectReason);
            return;
        }

        var ticketId = await matchmakingService.GetMatchmakingTicket(new()
        {
            Creator = accountManager.SignatureProvider.PubKey,
            Duid = duid,
            QueueName = QUEUE_NAME
        }, cts.Token);

        Debug.Log($"Waiting for match with ticket ID {ticketId}...");
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

            Debug.Log($"Waiting for match with ticket ID {ticketId}...\nStatus: {ticket.Status}");
            await UniTask.Delay(1000);
        }

        Debug.Log($"Match found! Getting server details for match ID {sessionId}...");
        var connectionDetails = await matchmakingService.GetConnectionDetails(new()
        {
            SessionId = sessionId
        }, cts.Token);

        var node = new UriBuilder(connectionDetails);
        if (node.Host == "host.docker.internal")
        {
            node.Host = "localhost";
        }

        Debug.Log($"Connecting to {node}...");
        OnStartGame?.Invoke(node.Uri, sessionId);
    }
}
