using Chromia;
using Newtonsoft.Json;
using System.Threading.Tasks;
using static EIFMerkleProofRaw;
using Buffer = Chromia.Buffer;

public class Queries
{
    public struct PlayerInfoResponse
    {
        [JsonProperty("points")]
        public int Points;

        [JsonProperty("win_count")]
        public int WinCount;

        [JsonProperty("loose_count")]
        public int LooseCount;

        [JsonProperty("draw_count")]
        public int DrawCount;


        public override string ToString()
        {
            return $"Points: {Points}\nWins: {WinCount}\nLooses: {LooseCount}\nDraws: {DrawCount}";
        }
    }

    public struct EifEventData
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

    public static async Task<PlayerInfoResponse> GetPlayerInfo(ChromiaClient client, Buffer pubKey)
    {
        return await client.Query<PlayerInfoResponse>(
            "ttt.IPlayer.get_info",
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

    public static async Task<MerkleProof> GetEventMerkleProof(ChromiaClient client, Buffer eventHash)
    {
        return await client.Query<MerkleProof>(
            "get_event_merkle_proof",
            ("eventHash", eventHash.Parse())
        );
    }
}
