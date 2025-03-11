using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Collections.Generic;
using System.Numerics;

public class EvmTypes
{
    public struct EventWithProof
    {
        public byte[] _Event;
        public EventProof EventProof;
        public byte[] BlockHeader;
        public List<byte[]> Sigs;
        public List<string> Signers;
        public ExtraMerkleProof ExtraProof;

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

    [Struct("Proof")]
    public class EventProof
    {
        [Parameter("bytes32", "leaf", 1)]
        public byte[] Leaf { get; set; }

        [Parameter("uint256", "position", 2)]
        public BigInteger Position { get; set; }

        [Parameter("bytes32[]", "merkleProofs", 3)]
        public List<byte[]> MerkleProofs { get; set; }

        public EventProof(byte[] leaf, BigInteger position, List<byte[]> merkleproofs)
        {
            Leaf = leaf;
            Position = position;
            MerkleProofs = merkleproofs;
        }
    }

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
