using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Nethereum.Hex.HexConvertors.Extensions;
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

        var estimatedGas = await account.EstimateGas(CONTRACT_ADDRESS, abi, "batchClaim", arguments);
        if (!await HasEnoughGas(estimatedGas))
        {
            throw new Exception("Insufficient funds for gas.");
        }

        var txHash = await account.SendTransaction(CONTRACT_ADDRESS, abi, "batchClaim", estimatedGas, arguments);
        if (string.IsNullOrEmpty(txHash))
        {
            throw new Exception("Failed to send transaction");
        }

        var success = await WaitForTransactionConfirmation(txHash);
        return success ? txHash : null;
    }

    public async Task<List<bool>> GetClaimStatus(Queries.EifEventData[] eventData)
    {
        return await account.ReadContract<List<bool>>(
            CONTRACT_ADDRESS,
            abi,
            "getClaimStatus",
            new object[]
            {
                eventData.Select(e => CryptoUtils.ToBytesLike(e.EventHash)).ToArray(),
                account.Address
            }
        );
    }

    private async UniTask<bool> WaitForTransactionConfirmation(string transactionHash)
    {
        Debug.Log($"Waiting for transaction confirmation: {transactionHash}");

        var tries = 0;
        var maxTries = 30;
        while (tries < maxTries)
        {
            var receipt = await account.GetTransactionReceipt(transactionHash);
            if (receipt != null && receipt.BlockNumber != null && receipt.BlockNumber.Value > 0)
            {
                Debug.Log($"Transaction Confirmed! Block: {receipt.BlockNumber.Value}");
                return true;
            }

            Debug.Log("Waiting for transaction to be mined...");
            await UniTask.Delay(TimeSpan.FromSeconds(1));
            tries++;
        }

        Debug.LogError($"Transaction failed to be mined after {maxTries} tries");
        return false;
    }

    private async UniTask<bool> HasEnoughGas(BigInteger estimatedGas)
    {
        await account.SyncBalance();

        return account.Balance >= estimatedGas * Web3.Convert.ToWei(1, UnitConversion.EthUnit.Gwei);
    }
}