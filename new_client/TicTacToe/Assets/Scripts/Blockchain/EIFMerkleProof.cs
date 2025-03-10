using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using Chromia;

using Buffer = Chromia.Buffer;

public static class EIFMerkleProofRaw
{
    public struct MerkleProof
    {
        [JsonProperty("blockHeader")]
        public Buffer BlockHeader;

        [JsonProperty("blockWitness")]
        public BlockWitness[] BlockWitness;

        [JsonProperty("eventData")]
        public Buffer EventData;

        [JsonProperty("eventProof")]
        public EventProof EventProof;

        [JsonProperty("extraMerkleProof")]
        public ExtraMerkleProof ExtraMerkleProof;
    }

    public struct BlockWitness
    {
        [JsonProperty("pubkey")]
        public Buffer Pubkey;

        [JsonProperty("sig")]
        public Buffer Sig;
    }

    public struct EventProof
    {
        [JsonProperty("leaf")]
        public Buffer Leaf;

        [JsonProperty("merkleProofs")]
        public Buffer[] MerkleProofs;

        [JsonProperty("position")]
        public int Position;
    }

    public struct ExtraMerkleProof
    {
        [JsonProperty("extraMerkleProofs")]
        public Buffer[] ExtraMerkleProofs;

        [JsonProperty("extraRoot")]
        public Buffer ExtraRoot;

        [JsonProperty("hashedLeaf")]
        public Buffer HashedLeaf;

        [JsonProperty("leaf")]
        public Buffer Leaf;

        [JsonProperty("position")]
        public int Position;
    }

    public static async Task<MerkleProof> GetEventMerkleProof(ChromiaClient client, Buffer eventHash)
    {
        return await client.Query<MerkleProof>(
            "get_event_merkle_proof",
            ("eventHash", eventHash.Parse())
        );
    }
}

public static class EIFMerkleProof
{
    public static EventWithProof Construct(EIFMerkleProofRaw.MerkleProof merkleProof)
    {
        var blockHeader = CryptoUtils.ToBytesLike(merkleProof.BlockHeader);
        var (sigs, signers) = CryptoUtils.GetWeb3BlockWitness(merkleProof.BlockWitness);
        var eventData = CryptoUtils.ToBytesLike(merkleProof.EventData);
        var proof = CryptoUtils.GetWeb3Proof(merkleProof.EventProof);
        var extraProof = CryptoUtils.GetWeb3ExtraProof(merkleProof.ExtraMerkleProof);

        return new EventWithProof(eventData, proof, blockHeader, sigs, signers, extraProof);
    }

    public struct EventWithProof
    {
        public string _Event;
        public EventProof EventProof;
        public string BlockHeader;
        public string[] Sigs;
        public string[] Signers;
        public ExtraMerkleProof ExtraProof;

        public EventWithProof(
            string _event, EventProof eventProof, string blockHeader,
            string[] sigs, string[] signers, ExtraMerkleProof extraProof)
        {
            _Event = _event;
            EventProof = eventProof;
            BlockHeader = blockHeader;
            Sigs = sigs;
            Signers = signers;
            ExtraProof = extraProof;
        }
    }

    public struct EventProof
    {
        public string Leaf;
        public int Position;
        public string[] MerkleProofs;

        public EventProof(string leaf, int position, string[] merkleproofs)
        {
            Leaf = leaf;
            Position = position;
            MerkleProofs = merkleproofs;
        }
    }

    public struct ExtraMerkleProof
    {
        public string Leaf;
        public string HashedLead;
        public int Position;
        public string ExtraRoot;
        public string[] MerkleProofs;

        public ExtraMerkleProof(string leaf, string hashedLead, int position, string extraRoot, string[] merkleProofs)
        {
            Leaf = leaf;
            HashedLead = hashedLead;
            Position = position;
            ExtraRoot = extraRoot;
            MerkleProofs = merkleProofs;
        }
    }
}

public static class CryptoUtils
{
    public static (string[] sigs, string[] signers) GetWeb3BlockWitness(EIFMerkleProofRaw.BlockWitness[] blockWitness)
    {
        var witness = blockWitness.ToArray();


        var sigs = witness.Select(w => ToBytesLike(w.Sig)).ToArray();
        var signers = witness.Select(w => ToBytesLike(w.Pubkey)).ToArray();

        return (sigs, signers);
    }

    public static EIFMerkleProof.EventProof GetWeb3Proof(EIFMerkleProofRaw.EventProof proof)
    {
        return new EIFMerkleProof.EventProof(
            ToBytesLike(proof.Leaf),
            proof.Position,
            proof.MerkleProofs.Select(w => ToBytesLike(w)).ToArray()
        );
    }

    public static EIFMerkleProof.ExtraMerkleProof GetWeb3ExtraProof(EIFMerkleProofRaw.ExtraMerkleProof extraProof)
    {
        return new EIFMerkleProof.ExtraMerkleProof(
            ToBytesLike(extraProof.Leaf),
            ToBytesLike(extraProof.HashedLeaf),
            extraProof.Position,
            ToBytesLike(extraProof.ExtraRoot),
            extraProof.ExtraMerkleProofs.Select(w => ToBytesLike(w)).ToArray()
        );
    }


    public static string ToBytesLike(Buffer data)
    {
        return $"0x{data.Parse()}";
    }
}