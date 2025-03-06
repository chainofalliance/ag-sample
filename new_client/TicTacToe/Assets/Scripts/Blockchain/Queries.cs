using Chromia;
using Newtonsoft.Json;
using System.Threading.Tasks;
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

    public static async Task<PlayerInfoResponse> GetPlayerInfo(ChromiaClient client, Buffer pubKey)
    {
        return await client.Query<PlayerInfoResponse>(
            "ttt.IPlayer.get_info",
            ("pubkey", Buffer.From(pubKey))
        );
    }
}
