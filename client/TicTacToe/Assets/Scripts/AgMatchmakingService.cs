using Chromia;
using Cysharp.Threading.Tasks;
using Models;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;
using System.Threading;
using Buffer = Chromia.Buffer;

namespace AgMatchmaking
{
    public static class MatchmakingServiceFactory
    {
        public static string DUID = null;
        public static string QUEUE = "1Vs1";

        public static IMatchmakingService Get(Blockchain blockchain)
        {
            return new MatchmakingService(blockchain);
        }
    }

    public interface IMatchmakingService
    {
        UniTask<TransactionReceipt> CreateMatchmakingTicket(
            Models.CreateMatchmakingTicketRequest request,
            CancellationToken ct
        );

        UniTask<string> GetMatchmakingTicket(
            Models.GetMatchmakingTicketRequest request,
            CancellationToken ct
        );

        UniTask<Models.GetMatchmakingTicketStatusResult> GetMatchmakingTicketStatus(
            Models.GetMatchmakingTicketStatusRequest request,
            CancellationToken ct
        );

        UniTask<string> GetConnectionDetails(
            Models.GetConnectionDetailsRequest request,
            CancellationToken ct
        );

        UniTask<TransactionReceipt> CancelMatchmakingTicket(
            Models.CancelMatchmakingTicketRequest request,
            CancellationToken ct
        );

        UniTask<TransactionReceipt> CancelAllMatchmakingTicketsForPlayer(
            Models.CancelAllMatchmakingTicketRequests request,
            CancellationToken ct
        );

        UniTask<string> GetUid(
            CancellationToken ct
        );
    }

    public class MatchmakingService : IMatchmakingService
    {
        private readonly Blockchain blockchain;

        public MatchmakingService(Blockchain blockchain)
        {
            this.blockchain = blockchain;
        }

        public async UniTask<TransactionReceipt> CreateMatchmakingTicket(
            Models.CreateMatchmakingTicketRequest request,
            CancellationToken ct
        )
        {
            return await blockchain.Client.SendUniqueTransaction(
                new Operation("ag.IMatchmaking.create_ticket", request.ToGtv()), blockchain.SignatureProvider, ct).AsUniTask();
        }

        public async UniTask<string> GetMatchmakingTicket(
            Models.GetMatchmakingTicketRequest request,
            CancellationToken ct
        )
        {
            return await blockchain.Client.Query<string>(
                "ag.IMatchmaking.get_ticket_id",
                ("par", new object[] { request.Creator, request.Duid, request.QueueName })
            );
        }

        public async UniTask<Models.GetMatchmakingTicketStatusResult> GetMatchmakingTicketStatus(
            Models.GetMatchmakingTicketStatusRequest request,
            CancellationToken ct
        )
        {
            return await blockchain.Client.Query<Models.GetMatchmakingTicketStatusResult>(
                "ag.IMatchmaking.get_ticket_status",
                ("par", new object[] { request.TicketId })
            );
        }

        public async UniTask<string> GetConnectionDetails(
            Models.GetConnectionDetailsRequest request,
            CancellationToken ct
        )
        {
            return await blockchain.Client.Query<string>(
                "ag.ISession.get_connection_details",
                ("session_id", request.SessionId)
            );
        }

        public async UniTask<TransactionReceipt> CancelMatchmakingTicket(
            Models.CancelMatchmakingTicketRequest request,
            CancellationToken ct
        )
        {
            return await blockchain.Client.SendUniqueTransaction(
                new Operation("ag.IMatchmaking.cancel_ticket", request.ToGtv()), blockchain.SignatureProvider, ct).AsUniTask();
        }

        public async UniTask<TransactionReceipt> CancelAllMatchmakingTicketsForPlayer(
            Models.CancelAllMatchmakingTicketRequests request,
            CancellationToken ct
        )
        {
            return await blockchain.Client.SendUniqueTransaction(
                new Operation("ag.IMatchmaking.cancel_all_tickets", request.ToGtv()), blockchain.SignatureProvider, ct).AsUniTask();
        }

        public async UniTask<string> GetUid(CancellationToken ct)
        {
            return await blockchain.Client.Query<string>(
                "ag.IDappProvider.get_uid",
                ("dsiplay_name", "TicTacToe")
            );
        }
    }

    namespace Models
    {
        public class CreateMatchmakingTicketRequest
        {
            public Buffer Creator;
            public string Duid;
            public string QueueName;
            public string MatchData = "[]";

            public object ToGtv()
            {
                return new object[] { Creator, Duid, QueueName, MatchData };
            }
        }

        public class GetMatchmakingTicketRequest
        {
            public Buffer Creator;
            public string Duid;
            public string QueueName;
        }

        public class GetMatchmakingTicketStatusRequest
        {
            public string TicketId;
            public object ToGtv()
            {
                return new object[] { TicketId };
            }
        }

        public class GetMatchmakingTicketStatusResult
        {
            [JsonProperty("ticket_id")]
            public string TicketId;
            [JsonProperty("queue_name")]
            public string QueueName;
            [JsonProperty("created_at")]
            public long CreatedAtTimestamp;
            public DateTime CreatedAt => DateTimeOffset.FromUnixTimeMilliseconds(CreatedAtTimestamp).DateTime;
            [JsonProperty("status")]
            public TicketState Status;
            [JsonProperty("give_up_after_seconds")]
            public int GiveUpAfterSeconds;
            [JsonProperty("creator")]
            public Buffer Creator;
            [JsonProperty("session_id")]
            public string SessionId;
        }

        public class GetConnectionDetailsRequest
        {
            public string SessionId;
            public object ToGtv()
            {
                return new object[] { SessionId };
            }
        }


        public class CancelMatchmakingTicketRequest
        {
            public Buffer Creator;
            public string TicketId;

            public object ToGtv()
            {
                return new object[] { Creator, TicketId };
            }
        }

        public class CancelAllMatchmakingTicketRequests
        {
            public string Duid;
            public Buffer Creator;

            public object ToGtv()
            {
                return new object[] { Creator, Duid };
            }
        }

        public enum TicketStatus
        {
            [EnumMember(Value = "open")]
            open,
            [EnumMember(Value = "canceled")]
            canceled,
            [EnumMember(Value = "matched")]
            matched
        }
    }
}