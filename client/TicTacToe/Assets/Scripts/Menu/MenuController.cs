using System.Net.Http;
using System;
using System.Threading;
using System.Threading.Tasks;

public class MenuController
{
    private readonly MenuView view;
    private readonly Blockchain blockchain;
    private readonly MatchmakingService matchmakingService;
    private readonly Action<Uri, string, string> onStartGame;

    private CancellationTokenSource cts;

    public MenuController(
        MenuView view,
        Blockchain blockchain,
        MatchmakingService matchmakingService,
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

    private async void OnLogin(string privKey)
    {
        await blockchain.Login(privKey);
        
        var points = await blockchain.GetPoints();
        view.SetLogin(blockchain.SignatureProvider.PubKey, points);
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
        while(!cts.Token.IsCancellationRequested)
        {
            var ticket = await matchmakingService.GetMatchmakingTicket(new()
            {
                Address = blockchain.SignatureProvider.PubKey,
                TicketId = ticketId
            }, cts.Token);

            if(ticket.MatchId != null)
            {
                matchId = ticket.MatchId;
                break;
            }

            view.SetInfo($"Waiting for match with ticket ID {ticketId}...\nStatus: {ticket.Status}");
            await Task.Delay(1000);
        }

        view.SetInfo($"Match found! Getting server details for match ID {matchId}...");
        var match = await matchmakingService.GetMatch(new()
        {
            Address = blockchain.SignatureProvider.PubKey,
            MatchId = matchId
        }, cts.Token);

        var node = new UriBuilder(match.ServerDetails);
        var opponent = match.OpponentId;
        view.SetInfo($"Playing against {opponent}, connecting to {node}...");
        var port = await GetGamePort(node.Uri, matchId, cts.Token);

        node.Port = port;
        view.SetInfo($"Received game port {port}. Connecting to {node}...");

        onStartGame?.Invoke(node.Uri, matchId, opponent);
    }

    private async Task<ushort> GetGamePort(
        Uri serverUrl,
        string matchId,
        CancellationToken ct
    )
    {
        const int RETRIES = 5;
        var route = new Uri(serverUrl, $"/dapp/session/get-port/{matchId}");
        var client = new HttpClient();

        var tries = 0;
        while (!ct.IsCancellationRequested && tries < RETRIES)
        {
            try
            {
                return ushort.Parse(await client.GetStringAsync(route));
            }
            catch (Exception)
            {
                tries++;
                await Task.Delay(1000, cancellationToken: ct);
            }
        }
        return 0;
    }

    private void OnCancel()
    {
        cts.Cancel();
    }
}
