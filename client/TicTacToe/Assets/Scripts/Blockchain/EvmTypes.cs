using Nethereum.ABI.FunctionEncoding.Attributes;
using Newtonsoft.Json;
using Reown.Core.Common.Utils;
using System.Collections.Generic;
using System.Numerics;

public class EvmTypes
{
    [Preserve]
    public struct EventWithProof
    {
        public byte[] _Event;
        public EventProof EventProof;
        public byte[] BlockHeader;
        public List<byte[]> Sigs;
        public List<string> Signers;
        public ExtraMerkleProof ExtraProof;

        [JsonConstructor]
        public EventWithProof(
            byte[] _event, EventProof eventProof, byte[] blockHeader,
            List<byte[]> sigs, List<string> signers, ExtraMerkleProof extraProof)
        {
            _Event = _event;
            EventProof = eventProof;
            BlockHeader = blockHeader;
            Sigs = sigs;
            Signers = signers;
            ExtraProof = extraProof;
        }
    }

    [Preserve]
    [Struct("ClaimData")]
    public class ClaimData
    {
        [Parameter("bytes", "eventData", 1)]
        public byte[] EventData { get; set; }

        [Parameter("tuple", "eventProof", 2)]
        public EventProof EventProof { get; set; }

        [Parameter("bytes", "blockHeader", 3)]
        public byte[] BlockHeader { get; set; }

        [Parameter("bytes[]", "signatures", 4)]
        public List<byte[]> Signatures { get; set; }

        [Parameter("address[]", "signers", 5)]
        public List<string> Signers { get; set; }

        [Parameter("tuple", "extraProof", 6)]
        public ExtraMerkleProof ExtraProof { get; set; }

        [Parameter("bytes", "encodedData", 7)]
        public byte[] EncodedData { get; set; }

        [JsonConstructor]
        public ClaimData() { }
    }

    [Preserve]
    [Struct("Proof")]
    public class EventProof
    {
        [Parameter("bytes32", "leaf", 1)]
        public byte[] Leaf { get; set; }

        [Parameter("uint256", "position", 2)]
        public BigInteger Position { get; set; }

        [Parameter("bytes32[]", "merkleProofs", 3)]
        public List<byte[]> MerkleProofs { get; set; }

        [JsonConstructor]
        public EventProof(byte[] leaf, BigInteger position, List<byte[]> merkleproofs)
        {
            Leaf = leaf;
            Position = position;
            MerkleProofs = merkleproofs;
        }
    }

    [Preserve]
    [Struct("ExtraProofData")]
    public class ExtraMerkleProof
    {
        [Parameter("bytes", "leaf", 1)]
        public byte[] Leaf { get; set; }

        [Parameter("bytes32", "hashedLeaf", 2)]
        public byte[] HashedLeaf { get; set; }

        [Parameter("uint256", "position", 3)]
        public BigInteger Position { get; set; }

        [Parameter("bytes32", "extraRoot", 4)]
        public byte[] ExtraRoot { get; set; }

        [Parameter("bytes32[]", "extraMerkleProofs", 5)]
        public List<byte[]> ExtraMerkleProofs { get; set; }

        [JsonConstructor]
        public ExtraMerkleProof(byte[] leaf, byte[] hashedLead, BigInteger position, byte[] extraRoot, List<byte[]> extraMerkleProofs)
        {
            Leaf = leaf;
            HashedLeaf = hashedLead;
            Position = position;
            ExtraRoot = extraRoot;
            ExtraMerkleProofs = extraMerkleProofs;
        }
    }
}
