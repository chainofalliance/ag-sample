using System.Collections.Generic;
using System.Linq;
using System;

using Buffer = Chromia.Buffer;

public static class EIFUtils
{
    public static EvmTypes.EventWithProof Construct(ChromiaTypes.MerkleProof merkleProof)
    {
        var blockHeader = CryptoUtils.ToBytesLike(merkleProof.BlockHeader);
        var (sigs, signers) = CryptoUtils.GetWeb3BlockWitness(merkleProof.BlockWitness);
        var eventData = CryptoUtils.ToBytesLike(merkleProof.EventData);
        var proof = CryptoUtils.GetWeb3Proof(merkleProof.EventProof);
        var extraProof = CryptoUtils.GetWeb3ExtraProof(merkleProof.ExtraMerkleProof);

        return new EvmTypes.EventWithProof(eventData, proof, blockHeader, sigs, signers, extraProof);
    }
}

public static class CryptoUtils
{
    public static (List<byte[]> sigs, List<string> signers) GetWeb3BlockWitness(ChromiaTypes.BlockWitness[] blockWitness)
    {
        var witness = blockWitness.Sort();

        var sigs = witness.Select(w => ToBytesLike(w.Sig)).ToList();
        var signers = witness.Select(w => $"0x{w.Pubkey.Parse()}").ToList();

        return (sigs, signers);
    }

    public static EvmTypes.EventProof GetWeb3Proof(ChromiaTypes.EventProof proof)
    {
        return new EvmTypes.EventProof(
            ToBytesLike(proof.Leaf),
            proof.Position,
            proof.MerkleProofs.Select(w => ToBytesLike(w)).ToList()
        );
    }

    public static EvmTypes.ExtraMerkleProof GetWeb3ExtraProof(ChromiaTypes.ExtraMerkleProof extraProof)
    {
        return new EvmTypes.ExtraMerkleProof(
            ToBytesLike(extraProof.Leaf),
            ToBytesLike(extraProof.HashedLeaf),
            extraProof.Position,
            ToBytesLike(extraProof.ExtraRoot),
            extraProof.ExtraMerkleProofs.Select(w => ToBytesLike(w)).ToList()
        );
    }

    public static byte[] ToBytesLike(Buffer data)
    {
        return data.Bytes;
    }

    public static IEnumerable<ChromiaTypes.BlockWitness> Sort(this IEnumerable<ChromiaTypes.BlockWitness> witnesses)
    {
        var list = witnesses.ToList();
        list.Sort((a, b) => Compare(a.Pubkey, b.Pubkey));
        return list;
    }

    public static int Compare(this Buffer a, Buffer b)
    {
        var bytesA = new ReadOnlySpan<byte>(a.Bytes);
        var bytesB = new ReadOnlySpan<byte>(b.Bytes);

        return bytesA.SequenceCompareTo(bytesB);
    }
}