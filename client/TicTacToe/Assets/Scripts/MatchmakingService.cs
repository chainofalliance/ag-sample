using System;
using System.Text;
using UnityEngine.Networking;
using Newtonsoft.Json;

using Models;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

public static class MatchmakingServiceFactory
{
    public static IMatchmakingService Get(bool mock = false)
    {
#if PROD_ENV
        return new MatchmakingService(new("https://mm.ttt.com/"));
#else
        if (mock)
            return new MockMatchmakingService();
        else
            return new MatchmakingService(new("http://localhost:9085/"));
#endif
    }
}

public interface IMatchmakingService
{
    UniTask<CreateMatchmakingTicketResult> CreateMatchmakingTicket(
        CreateMatchmakingTicketRequest request,
        CancellationToken ct
    );
    UniTask<GetMatchmakingTicketResult> GetMatchmakingTicket(
        GetMatchmakingTicketRequest request,
        CancellationToken ct
    );
    UniTask<GetMatchResult> GetMatch(
        GetMatchRequest request,
        CancellationToken ct
    );
    UniTask<CancelMatchmakingTicketResult> CancelMatchmakingTicket(
        CancelMatchmakingTicketRequest request,
        CancellationToken ct
    );

    UniTask<CancelAllMatchmakingTicketsForPlayerResult> CancelAllMatchmakingTicketsForPlayer(
        CancelAllMatchmakingTicketsForPlayerRequest request,
        CancellationToken ct
    );
}

public class MatchmakingService : IMatchmakingService
{
    private readonly Uri baseUri;

    public MatchmakingService(Uri baseUri)
    {
        this.baseUri = baseUri;
    }

    public UniTask<CreateMatchmakingTicketResult> CreateMatchmakingTicket(
        CreateMatchmakingTicketRequest request,
        CancellationToken ct
    )
    {
        return HandleRequest<CreateMatchmakingTicketResult>(request, ct);
    }

    public UniTask<GetMatchmakingTicketResult> GetMatchmakingTicket(
        GetMatchmakingTicketRequest request,
        CancellationToken ct
    )
    {
        return HandleRequest<GetMatchmakingTicketResult>(request, ct);
    }

    public UniTask<GetMatchResult> GetMatch(
        GetMatchRequest request,
        CancellationToken ct
    )
    {
        return HandleRequest<GetMatchResult>(request, ct);
    }

    public UniTask<CancelMatchmakingTicketResult> CancelMatchmakingTicket(
        CancelMatchmakingTicketRequest request,
        CancellationToken ct
    )
    {
        return HandleRequest<CancelMatchmakingTicketResult>(request, ct);
    }

    public UniTask<CancelAllMatchmakingTicketsForPlayerResult> CancelAllMatchmakingTicketsForPlayer(
        CancelAllMatchmakingTicketsForPlayerRequest request,
        CancellationToken ct
    )
    {
        return HandleRequest<CancelAllMatchmakingTicketsForPlayerResult>(request, ct);
    }


    private async UniTask<T> HandleRequest<T>(
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

    private async UniTask<UnityWebRequest> SendRequest(UnityWebRequest request, CancellationToken ct)
    {
        _ = request.SendWebRequest();

        while (!request.isDone)
        {
            ct.ThrowIfCancellationRequested();
            await UniTask.Yield();
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

public class MockMatchmakingService : IMatchmakingService
{
    public UniTask<CreateMatchmakingTicketResult> CreateMatchmakingTicket(
        CreateMatchmakingTicketRequest request,
        CancellationToken ct
    )
    {
        Debug.Log("Mock CreateMatchmakingTicket...");
        return UniTask.FromResult(new CreateMatchmakingTicketResult()
        {
            TicketId = "mock-ticket-id",
            Error = false,
        });
    }

    public UniTask<GetMatchmakingTicketResult> GetMatchmakingTicket(
        GetMatchmakingTicketRequest request,
        CancellationToken ct
    )
    {
        Debug.Log("Mock GetMatchmakingTicket...");
        return UniTask.FromResult(new GetMatchmakingTicketResult()
        {
            TicketId = "mock-ticket-id",
            CreatedAtTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            Status = TicketState.Matched,
            MatchId = "mock-match-id",
            Error = false,
        });
    }

    public UniTask<GetMatchResult> GetMatch(
        GetMatchRequest request,
        CancellationToken ct
    )
    {
        Debug.Log("Mock GetMatch...");
        return UniTask.FromResult(new GetMatchResult()
        {
            MatchId = "mock-match-id",
            MatchedAtTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            ServerDetails = "http://localhost:5172",
#if UNITY_EDITOR
            OpponentId = "02466d7fcae563e5cb09a0d1870bb580344804617879a14949cf22285f1bae3f27",
#else
            OpponentId = "034f355bdcb7cc0af728ef3cceb9615d90684bb5b2ca5f859ab0f0b704075871aa",
#endif
            Error = false,
        });
    }


    public UniTask<CancelMatchmakingTicketResult> CancelMatchmakingTicket(
        CancelMatchmakingTicketRequest request,
        CancellationToken ct
    )
    {
        Debug.Log("Mock CancelMatchmakingTicket...");
        return UniTask.FromResult(new CancelMatchmakingTicketResult()
        {
            Error = false
        });

    }

    public UniTask<CancelAllMatchmakingTicketsForPlayerResult> CancelAllMatchmakingTicketsForPlayer(
        CancelAllMatchmakingTicketsForPlayerRequest request,
        CancellationToken ct
    )
    {
        Debug.Log("Mock CancelAllMatchmakingTicketsForPlayer...");
        return UniTask.FromResult(new CancelAllMatchmakingTicketsForPlayerResult()
        {
            Error = false
        });
    }
}