using Newtonsoft.Json;

using Buffer = Chromia.Buffer;

public class ChromiaTypes
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
}
