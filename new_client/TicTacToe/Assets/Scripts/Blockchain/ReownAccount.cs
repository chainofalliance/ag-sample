using Cysharp.Threading.Tasks;
using Nethereum.Util;
using Nethereum.Web3;
using Reown.AppKit.Unity;
using Reown.Sign.Models;
using UnityEngine;

public class ReownAccount : IAccount
{
    public string Address => account.Address;

    private readonly Account account;

    public ReownAccount(Account account)
    {
        this.account = account;
    }

    public async UniTask<string> SendTransaction(
        string contractAddress,
        string abi,
        string methodName,
        object[] parameters
    )
    {
        if (!await HasEnoughGas(contractAddress, abi, methodName, parameters))
        {
            Debug.LogError("Insufficient funds for gas.");
            return null;
        }

        var transactionHash = await AppKit.Evm.WriteContractAsync(
            contractAddress,
            abi,
            methodName,
            parameters
        );
        Debug.Log($"Transaction Sent. Hash: {transactionHash}");
        return transactionHash;
    }

    private async UniTask<bool> HasEnoughGas(
        string contractAddress,
        string abi,
        string methodName,
        object[] parameters
    )
    {
        var balance = await AppKit.Evm.GetBalanceAsync(Address);
        var gasLimit = await AppKit.Evm.EstimateGasAsync(
            contractAddress,
            abi,
            methodName,
            0,
            parameters
        );
        return balance >= gasLimit * Web3.Convert.ToWei(1, UnitConversion.EthUnit.Gwei);
    }
}