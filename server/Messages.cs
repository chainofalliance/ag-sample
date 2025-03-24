using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Chromia;
using Chromia.Encoding;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Buffer = Chromia.Buffer;

public class Messages
{
    private static readonly JsonSerializerSettings settings = new JsonSerializerSettings()
    {
        Converters = new List<JsonConverter> { new BigIntegerConverter(), new BufferConverter() },
        ContractResolver = new PostchainPropertyContractResolver()
    };

    public static Buffer Encode(IMessage message)
    {
        return ChromiaClient.EncodeToGtv(message);
    }

    public static T Decode<T>(Buffer data) where T : IMessage
    {
        var gtv = ChromiaClient.DecodeFromGtv(data);
        var str = JsonConvert.SerializeObject(gtv, settings);
        return JsonConvert.DeserializeObject<T>(str, settings);
    }

    public enum Field
    {
        Empty,
        X,
        O
    }

    public enum Header : uint
    {
        Ready,
        Sync,
        PlayerDataRequest,
        PlayerDataResponse,
        Forfeit,
        MoveRequest,
        MoveResponse,
        GameOver
    }

    public interface IMessage
    {
        Header Header { get; }
    }

    [PostchainSerializable]
    public class Sync : IMessage
    {
        public Header Header => Header.Sync;

        [PostchainProperty("turn")]
        public Field Turn { get; private set; }

        [PostchainProperty("fields")]
        public int[] Fields { get; private set; }

        public Sync(Field turn, Field[,] board)
        {
            Turn = turn;
            Fields = board?.Cast<int>()?.ToArray();
        }
    }

    [PostchainSerializable]
    public class GameOver : IMessage
    {
        public Header Header => Header.GameOver;

        [PostchainProperty("winner")]
        public byte[]? Winner { get; private set; }

        [PostchainProperty("is_forfeit")]
        public bool IsForfeit { get; private set; }

        public GameOver(byte[]? winner, bool isForfeit)
        {
            Winner = winner;
            IsForfeit = isForfeit;
        }
    }

    [PostchainSerializable]
    public class PlayerDataRequest : IMessage
    {
        public Header Header => Header.PlayerDataRequest;
    }

    [PostchainSerializable]
    public class PlayerDataResponse : IMessage
    {
        [PostchainSerializable]
        public class Player : IMessage
        {
            public Header Header => Header.PlayerDataResponse;

            [PostchainProperty("pubkey")]
            public Buffer PubKey { get; private set; }

            [PostchainProperty("symbol")]
            public Field Symbol { get; private set; }

            public Player(Buffer pubKey, Field symbol)
            {
                PubKey = pubKey;
                Symbol = symbol;
            }
        }

        public Header Header => Header.PlayerDataResponse;

        [PostchainProperty("players")]
        public Player[] Players { get; private set; }

        public PlayerDataResponse(Player[] players)
        {
            Players = players;
        }
    }

    [PostchainSerializable]
    public class MoveRequest : IMessage
    {
        public Header Header => Header.MoveRequest;
    }

    [PostchainSerializable]
    public class MoveResponse : IMessage
    {
        public Header Header => Header.MoveResponse;

        [PostchainProperty("move")]
        public int Move { get; private set; }

        public MoveResponse(int move)
        {
            Move = move;
        }
    }

    internal class PostchainPropertyContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            var customAttr = member.GetCustomAttribute<PostchainPropertyAttribute>();
            if (customAttr != null)
            {
                property.PropertyName = customAttr.Name;
                property.Writable = true;
            }

            return property;
        }
    }
}
