using Chromia;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

using Buffer = Chromia.Buffer;

public enum Outcome
{
    WIN,
    LOOSE,
    DRAW
}

public class Queries
{
    public class PlayerUpdate
    {
        [JsonProperty("info")]
        public PlayerInfoResponse Info;

        [JsonProperty("history")]
        public List<PlayerHistoryResponse> History;
    }

    public class PlayerInfoResponse
    {
        [JsonProperty("points")]
        public int Points;

        [JsonProperty("win_count")]
        public int WinCount;

        [JsonProperty("loose_count")]
        public int LooseCount;

        [JsonProperty("draw_count")]
        public int DrawCount;
    }

    public class PlayerHistoryResponse
    {
        [JsonProperty("session_id")]
        public string SessionId;

        [JsonProperty("opponent")]
        public Buffer Opponent;

        [JsonProperty("outcome")]
        public Outcome Outcome;

        [JsonProperty("points")]
        public int Points;
    }

    public class EifEventData
    {
        [JsonProperty("session_id")]
        public string SessionId;

        [JsonProperty("event_hash")]
        public Buffer EventHash;

        [JsonProperty("serial")]
        public int Serial;

        [JsonProperty("encoded_data")]
        public string EncodedData;

        [JsonProperty("hash")]
        public Buffer Hash;

        public override string ToString()
        {
            return $"Session ID: {SessionId}\nEvent Hash: {EventHash}\nSerial: {Serial}\nEncoded Data: {EncodedData}";
        }
    }

    public static async Task<PlayerUpdate> GetPlayerUpdate(ChromiaClient client, Buffer pubKey)
    {
        return await client.Query<PlayerUpdate>(
            "ttt.IPlayer.get_update",
            ("pubkey", Buffer.From(pubKey))
        );
    }

    public static async Task<EifEventData[]> GetUnclaimedEifEvents(ChromiaClient client, Buffer pubKey)
    {
        return await client.Query<EifEventData[]>(
            "ag.ISession.get_unclaimed_eif_events",
            ("address", Buffer.From(pubKey))
        );
    }

    public static async Task<EifEventData?> GetEifEventBySession(ChromiaClient client, string sessionId)
    {
        return await client.Query<EifEventData?>(
            "ag.ISession.get_eif_event_by_session_id",
            ("session_id", sessionId)
        );
    }

    public static async Task<ChromiaTypes.MerkleProof> GetEventMerkleProof(ChromiaClient client, Buffer eventHash)
    {
        return await client.Query<ChromiaTypes.MerkleProof>(
            "get_event_merkle_proof",
            ("eventHash", eventHash.Parse())
        );
    }
}
