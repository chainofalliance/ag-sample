using System;
using System.Text;
using UnityEngine.Networking;
using Newtonsoft.Json;

using Models;
using System.Threading;
using System.Threading.Tasks;

public static class MatchmakingServiceFactory
{
    public static MatchmakingService Get()
    {
#if PROD_ENV
        return new MatchmakingService(new("https://mm.ttt.com/"));
#else
        return new MatchmakingService(new("http://localhost:8090/"));
#endif
    }
}

public class MatchmakingService
{
    private readonly Uri baseUri;

    public MatchmakingService(Uri baseUri)
    {
        this.baseUri = baseUri;
    }

    public Task<CreateMatchmakingTicketResult> OnboardingTicket(
        OnboardingTicketRequest request,
        CancellationToken ct
    )
    {
        return HandleRequest<CreateMatchmakingTicketResult>(request, ct);
    }

    public Task<CreateMatchmakingTicketResult> CreateMatchmakingTicket(
        CreateMatchmakingTicketRequest request,
        CancellationToken ct
    )
    {
        return HandleRequest<CreateMatchmakingTicketResult>(request, ct);
    }

    public Task<GetMatchmakingTicketResult> GetMatchmakingTicket(
        GetMatchmakingTicketRequest request,
        CancellationToken ct
    )
    {
        return HandleRequest<GetMatchmakingTicketResult>(request, ct);
    }

    public Task<GetMatchResult> GetMatch(
        GetMatchRequest request,
        CancellationToken ct
    )
    {
        return HandleRequest<GetMatchResult>(request, ct);
    }

    public Task<CancelMatchmakingTicketResult> CancelMatchmakingTicket(
        CancelMatchmakingTicketRequest request,
        CancellationToken ct
    )
    {
        return HandleRequest<CancelMatchmakingTicketResult>(request, ct);
    }

    public Task<CancelAllMatchmakingTicketsForPlayerResult> CancelAllMatchmakingTicketsForPlayer(
        CancelAllMatchmakingTicketsForPlayerRequest request,
        CancellationToken ct
    )
    {
        return HandleRequest<CancelAllMatchmakingTicketsForPlayerResult>(request, ct);
    }


    private async Task<T> HandleRequest<T>(
        BaseRequest requestBody,
        CancellationToken ct
    ) where T : BaseResult, new()
    {
        using var request = JsonWebRequest(requestBody);
        try
        {
            var response = await SendRequest(request, ct);
            return JsonConvert.DeserializeObject<T>(response.downloadHandler.text);
        }
        catch
        {
            return BaseResult.ErrorResult<T>(request.error);
        }
    }

    private async Task<UnityWebRequest> SendRequest(UnityWebRequest request, CancellationToken ct)
    {
        _ = request.SendWebRequest();

        while (!request.isDone)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Yield();
        }

        return request;
    }

    private UnityWebRequest JsonWebRequest(BaseRequest requestBody)
    {
        var uri = new Uri(baseUri, requestBody.Path);
        var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requestBody));
        var request = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbPOST)
        {
            uploadHandler = new UploadHandlerRaw(bytes),
            downloadHandler = new DownloadHandlerBuffer()
        };
        request.SetRequestHeader("accept", "application/json");
        request.SetRequestHeader("Content-Type", "application/json");
        return request;
    }
}

namespace Models
{
    public class OnboardingTicketRequest : BaseRequest
    {
        public override string Path { get; protected set; } = "onboarding";
        [JsonProperty("partyId")]
        public string PartyId;
    }

    public class CreateMatchmakingTicketRequest : BaseRequest
    {
        public override string Path { get; protected set; } = "create-ticket";
    }

    public class CreateMatchmakingTicketResult : BaseResult
    {
        [JsonProperty("ticketId")]
        public string TicketId;
    }

    public class GetMatchmakingTicketRequest : BaseRequest
    {
        public override string Path { get; protected set; } = "ticket-status";
        [JsonProperty("ticketId")]
        public string TicketId;
    }

    public class GetMatchmakingTicketResult : BaseResult
    {
        [JsonProperty("matchId")]
        public string MatchId;
        [JsonProperty("ticketId")]
        public string TicketId;
        [JsonProperty("createdAt")]
        public long CreatedAtTimestamp;
        public DateTime CreatedAt => DateTimeOffset.FromUnixTimeMilliseconds(CreatedAtTimestamp).DateTime;
        [JsonProperty("status")]
        public TicketState Status;
    }

    public class GetMatchRequest : BaseRequest
    {
        public override string Path { get; protected set; } = "match-details";
        [JsonProperty("matchId")]
        public string MatchId;
    }

    public class GetMatchResult : BaseResult
    {
        [JsonProperty("id")]
        public string MatchId;
        [JsonProperty("createdAt")]
        public long MatchedAtTimestamp;
        public DateTime MatchedAt => DateTimeOffset.FromUnixTimeMilliseconds(MatchedAtTimestamp).DateTime;
        [JsonProperty("serverDetails")]
        public string ServerDetails;
        [JsonProperty("opponent")]
        public string OpponentId;
    }

    public class CancelMatchmakingTicketRequest : BaseRequest
    {
        public override string Path { get; protected set; } = "cancel-ticket";
        [JsonProperty("ticketId")]
        public string TicketId;
    }

    public class CancelMatchmakingTicketResult : BaseResult
    {

    }

    public class CancelAllMatchmakingTicketsForPlayerRequest : BaseRequest
    {
        public override string Path { get; protected set; } = "cancel-all-tickets";
    }

    public class CancelAllMatchmakingTicketsForPlayerResult : BaseResult
    {

    }

    public abstract class BaseRequest
    {
        public abstract string Path { get; protected set; }

        [JsonProperty("address")]
        public string Address;
    }

    public abstract class BaseResult
    {
        [JsonProperty("error")]
        public bool Error = false;
        [JsonProperty("errorMessage")]
        public string ErrorMessage = null;

        public static T ErrorResult<T>(string message) where T : BaseResult, new()
        {
            return new()
            {
                Error = true,
                ErrorMessage = message
            };
        }
    }

    public enum TicketState
    {
        Open,
        WaitingForServer,
        Matched,
        Closed,
        Cancelled
    }
}
