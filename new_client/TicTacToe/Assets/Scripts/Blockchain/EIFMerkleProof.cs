using Chromia;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

using static EIFMerkleProofRaw;
using Buffer = Chromia.Buffer;
using System.Linq;

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
    public static EventWithProof Construct(MerkleProof merkleProof)
    {
        var blockHeader = CryptoUtils.ToBytesLike(merkleProof.BlockHeader);
        var (sigs, signers) = CryptoUtils.GetWeb3BlockWitness(merkleProof.BlockWitness);
        var eventData = CryptoUtils.ToBytesLike(merkleProof.EventData);


        return default;
    }

    

    public struct EventWithProof
    {
        public string _Event;
        public EventProof EventProof;
        public string BlockHeader;
        public string[] Sigs;
        public string[] Signers;
        public ExtraProof ExtraProof;
    }

    public struct EventProof
    {
        public string Leaf;
        public int Position;
        public string[] MerkleProofs;
    }

    public struct ExtraProof
    {
        public string Leaf;
        public string HashedLead;
        public int Position;
        public string ExtraRoot;
        public string[] MerkleProofs;
    }
}

public static class CryptoUtils
{
    public static (string[] sigs, string[] signers) GetWeb3BlockWitness(BlockWitness[] blockWitness)
    {
        var witness = blockWitness.ToArray();


        var sigs = witness.Select(w => ToBytesLike(w.Sig)).ToArray();
        var signers = witness.Select(w => ToBytesLike(w.Pubkey)).ToArray();

        return (sigs, signers);
    }

    public static EventProof GetWeb3Proof(EventProof proof)
    {
        var merkleProofs = proof.MerkleProofs.Select(w => ToBytesLike(w)).ToArray();
        return default;
    }


    public static string ToBytesLike(Buffer data)
    {
        return $"0x{data.Parse()}";
    }
}