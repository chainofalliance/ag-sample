using System;
using System.Collections.Generic;
using System.Linq;

using Buffer = Chromia.Buffer;
using static TicTacToeContract;
using Newtonsoft.Json;

public static class WagmiArgConverter
{
    public static string ToHex(byte[] bytes)
    {
        return bytes != null ? "0x" + BitConverter.ToString(bytes).Replace("-", "").ToLower() : null;
    }

    public static object[] ConvertToViemTupleArgs(ClaimData[] claims)
    {
        var formattedClaims = claims.Select(data =>
        {
            return new object[]
            {
            ToHex(data.EventWithProof._Event),
            new object[]
            {
                ToHex(data.EventWithProof.EventProof.Leaf),
                data.EventWithProof.EventProof.Position,
                (data.EventWithProof.EventProof.MerkleProofs).Select(ToHex).ToArray()
            },
            ToHex(data.EventWithProof.BlockHeader),
            (data.EventWithProof.Sigs).Select(ToHex).ToArray(),
            (data.EventWithProof.Signers).ToArray(),
            new object[]
            {
                ToHex(data.EventWithProof.ExtraProof.Leaf),
                ToHex(data.EventWithProof.ExtraProof.HashedLeaf),
                data.EventWithProof.ExtraProof.Position,
                ToHex(data.EventWithProof.ExtraProof.ExtraRoot),
                (data.EventWithProof.ExtraProof.ExtraMerkleProofs).Select(ToHex).ToArray()
            },
            ToHex(Buffer.From(data.EncodedData).Bytes)
            };
        }).ToArray();

        return new object[] { formattedClaims };
    }

    public static string ToFormattedString(object[] wagmiArgs)
    {
        var json = JsonConvert.SerializeObject(wagmiArgs, Formatting.Indented);
        return $"📦 Wagmi ABI Arguments:\n{json}";
    }
}
