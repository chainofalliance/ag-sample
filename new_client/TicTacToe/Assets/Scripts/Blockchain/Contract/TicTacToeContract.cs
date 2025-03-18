using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Reown.AppKit.Unity;
using UnityEngine;

public class TicTacToeContract
{
    public const string RPC_URL = "https://data-seed-prebsc-2-s1.binance.org:8545/";
    private const string CONTRACT_ADDRESS = "0x0A881312dBb60C0111c090d3eE64567CcDecACeD";

    private readonly IAccount account;
    private readonly string abi;

    public TicTacToeContract(IAccount account)
    {
        this.account = account;
        abi = Resources.Load<TextAsset>("TicTacToeAbi").text;
    }

    public async Task<int> GetPoints(string address)
    {
        var evm = AppKit.Evm;
        return await evm.ReadContractAsync<int>(CONTRACT_ADDRESS, abi, "getPoints", new object[]
        {
            address
        });
    }

    public async Task<string> Claim(EvmTypes.EventWithProof eventWithProof, string encodedData)
    {
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

        var txHash = await account.SendTransaction(CONTRACT_ADDRESS, abi, "Claim", arguments);
        var receipt = await WaitForTransactionConfirmation(txHash);

        return receipt.TransactionHash;
    }

    private async UniTask<TransactionReceipt> WaitForTransactionConfirmation(string transactionHash)
    {
        Debug.Log($"Waiting for transaction confirmation: {transactionHash}");

        var web3 = new Web3(RPC_URL);
        while (true)
        {
            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            if (receipt != null)
            {
                Debug.Log($"Transaction Confirmed! Block: {receipt.BlockNumber.Value}");
                return receipt;
            }

            Debug.Log("Waiting for transaction to be mined...");
            await UniTask.Delay(TimeSpan.FromSeconds(1));
        }
    }
}