using Chromia;
using System.Linq;
using Buffer = Chromia.Buffer;

public class Messages
{
    public enum Field
    {
        Empty,
        X,
        O
    }

    public enum Header
    {
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
        Buffer Encode();
        void Decode(Buffer data);
    }

    public class Sync : IMessage
    {
        public Header Header => Header.Sync;
        public Field Turn { get; private set; }
        public int[] Fields { get; private set; }

        public Sync()
        { }

        public Sync(Field turn, Field[,] board)
        {
            Turn = turn;
            Fields = board.Cast<int>().ToArray();
        }

        public Sync(Buffer data)
        {
            Decode(data);
        }

        public Buffer Encode()
        {
            var obj = new object[] { Turn, Fields };
            return ChromiaClient.EncodeToGtv(obj);
        }

        public void Decode(Buffer data)
        {
            var obj = ChromiaClient.DecodeFromGtv(data) as object[];
            Turn = (Field)obj[0];
            Fields = ((long[])obj[1]).Select(i => (int)i).ToArray();
        }
    }

    public class PlayerDataRequest : IMessage
    {
        public Header Header => Header.PlayerDataRequest;
        public Buffer Encode()
        {
            return Buffer.Empty();
        }
        public void Decode(Buffer data) { }
    }

    public class PlayerDataResponse : IMessage
    {
        public class Player : IMessage
        {
            public Header Header => Header.PlayerDataResponse;
            public Buffer PubKey { get; private set; }
            public int Points { get; private set; }
            public Field Symbol { get; private set; }

            public Player(Buffer pubKey, int points, Field symbol)
            {
                PubKey = pubKey;
                Points = points;
                Symbol = symbol;
            }

            public Player(Buffer data)
            {
                Decode(data);
            }

            public Buffer Encode()
            {
                var obj = new object[] { PubKey, Points, Symbol };
                return ChromiaClient.EncodeToGtv(obj);
            }

            public void Decode(Buffer data)
            {
                var obj = ChromiaClient.DecodeFromGtv(data) as object[];
                PubKey = (Buffer)obj[0];
                Points = (int)(long)obj[1];
                Symbol = (Field)(long)obj[2];
            }
        }

        public Header Header => Header.PlayerDataResponse;
        public Player[] Players { get; private set; }
        public PlayerDataResponse()
        { }

        public PlayerDataResponse(Player[] players)
        {
            Players = players;
        }

        public PlayerDataResponse(Buffer data)
        {
            Decode(data);
        }

        public Buffer Encode()
        {
            var obj = Players.Select(p => p.Encode()).ToArray();
            return ChromiaClient.EncodeToGtv(obj);
        }
        public void Decode(Buffer data)
        {
            var obj = ChromiaClient.DecodeFromGtv(data) as object[];
            Players = obj.Select(o => new Player((Buffer)o)).ToArray();
        }
    }


    public class MoveRequest : IMessage
    {
        public Header Header => Header.MoveRequest;
        public Buffer Encode()
        {
            return Buffer.Empty();
        }
        public void Decode(Buffer data) { }
    }

    public class MoveResponse : IMessage
    {
        public Header Header => Header.MoveResponse;
        public int Move { get; private set; }

        public MoveResponse()
        { }

        public MoveResponse(int move)
        {
            Move = move;
        }

        public MoveResponse(Buffer data)
        {
            Decode(data);
        }

        public Buffer Encode()
        {
            var obj = new object[] { Move };
            return ChromiaClient.EncodeToGtv(obj);
        }
        public void Decode(Buffer data)
        {
            var obj = ChromiaClient.DecodeFromGtv(data) as object[];
            Move = (int)(long)obj[0];
        }
    }
}
