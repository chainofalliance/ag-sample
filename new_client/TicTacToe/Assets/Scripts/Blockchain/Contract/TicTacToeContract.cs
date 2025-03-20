using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Web3;
using Reown.AppKit.Unity;
using UnityEngine;

public class TicTacToeContract
{
    public struct ClaimData
    {
        public EvmTypes.EventWithProof EventWithProof;
        public string EncodedData;
    }

    public const string RPC_URL = "https://data-seed-prebsc-2-s1.binance.org:8545/";
    private const string CONTRACT_ADDRESS = "0x21F3031936918685c29E37aCba99Af05D5275a60";

    private readonly IAccount account;
    private readonly Web3 web3;
    private readonly string abi;

    public TicTacToeContract(IAccount account)
    {
        this.account = account;
        abi = Resources.Load<TextAsset>("TicTacToeAbi").text;
        web3 = new Web3(RPC_URL);
    }

    public async Task<int> GetPoints(string address)
    {
        var evm = AppKit.Evm;
        return await evm.ReadContractAsync<int>(CONTRACT_ADDRESS, abi, "getPoints", new object[]
        {
            address
        });
    }

    public async Task<string> ClaimBatch(ClaimData[] claimData)
    {
        var argumentsList = new List<object>();
        foreach (var data in claimData)
        {
            argumentsList.Add(new object[]
            {
                data.EventWithProof._Event,
                data.EventWithProof.EventProof,
                data.EventWithProof.BlockHeader,
                data.EventWithProof.Sigs,
                data.EventWithProof.Signers,
                data.EventWithProof.ExtraProof,
                data.EncodedData.HexToByteArray()
            });
        }

        var arguments = new object[] { argumentsList.ToArray() };

        var estimatedGas = await EstimateGas("batchClaim", arguments);
        if (!await HasEnoughGas(estimatedGas))
        {
            Debug.LogError("Insufficient funds for gas.");
            return null;
        }

        var txHash = await account.SendTransaction(CONTRACT_ADDRESS, abi, "batchClaim", estimatedGas, arguments);
        if (string.IsNullOrEmpty(txHash))
        {
            Debug.LogError("Failed to send transaction");
            return null;
        }

        var receipt = await WaitForTransactionConfirmation(txHash);

        return receipt.TransactionHash;
    }

    private async UniTask<TransactionReceipt> WaitForTransactionConfirmation(string transactionHash)
    {
        Debug.Log($"Waiting for transaction confirmation: {transactionHash}");

        var tries = 0;
        var maxTries = 30;
        while (tries < maxTries)
        {
            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            if (receipt != null)
            {
                Debug.Log($"Transaction Confirmed! Block: {receipt.BlockNumber.Value}");
                return receipt;
            }

            Debug.Log("Waiting for transaction to be mined...");
            await UniTask.Delay(TimeSpan.FromSeconds(1));
            tries++;
        }

        Debug.LogError($"Transaction failed to be mined after {maxTries} tries");
        return null;
    }

    private async UniTask<HexBigInteger> EstimateGas(string methodName, object[] parameters)
    {
        var contract = web3.Eth.GetContract(abi, CONTRACT_ADDRESS);
        var function = contract.GetFunction(methodName);
        return await function.EstimateGasAsync(account.Address, null, null, parameters);
    }

    private async UniTask<bool> HasEnoughGas(HexBigInteger estimatedGas)
    {
        await account.SyncBalance();

        return account.Balance >= estimatedGas * Web3.Convert.ToWei(1, UnitConversion.EthUnit.Gwei);
    }
}