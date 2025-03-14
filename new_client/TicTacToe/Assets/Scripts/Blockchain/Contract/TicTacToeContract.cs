using System.Threading.Tasks;
using Reown.AppKit.Unity;
using UnityEngine;

public class TicTacToeContract
{
    public static string contractAddress = "0xe7f1725E7734CE288F8367e1Bb143E90bb3F0512";
    public static string abi = null;

    public static void LoadAbi()
    {
        if (abi == null)
        {
            TextAsset abiTextAsset = Resources.Load<TextAsset>("TicTacToe");
            abi = abiTextAsset.text;
        }
    }

    public static async Task<int> GetPoints(string address)
    {
        LoadAbi();

        var evm = AppKit.Evm;
        return await evm.ReadContractAsync<int>(contractAddress, abi, "getPoints", new object[]
        {
            address
        });
    }

    public static async Task<string> Claim(EvmTypes.EventWithProof eventWithProof, string encodedData)
    {
        LoadAbi();

        var arguments = new object[]
        {
            eventWithProof._Event,
            eventWithProof.EventProof,
            eventWithProof.BlockHeader,
            eventWithProof.Sigs,
            eventWithProof.Signers,
            eventWithProof.ExtraProof,
            Nethereum.Hex.HexConvertors.Extensions.HexByteConvertorExtensions.HexToByteArray(encodedData)
        };

        var gasAmount = await AppKit.Evm.EstimateGasAsync(contractAddress, abi, "Claim", arguments: arguments);
        Debug.Log("Gas Amount: " +  gasAmount);

        return await AppKit.Evm.WriteContractAsync(contractAddress, abi, "Claim", gasAmount, arguments);
    }
}