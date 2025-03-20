using System.Numerics;
using Cysharp.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Newtonsoft.Json;
using Reown.AppKit.Unity;
using Reown.Sign.Models;
using UnityEngine;

public class ReownAccount : IAccount
{
    public string Address => account.Address;
    public BigInteger Balance { get; private set; }

    private readonly Account account;

    public ReownAccount(Account account)
    {
        this.account = account;
    }

    public async UniTask SyncBalance()
    {
        Balance = await AppKit.Evm.GetBalanceAsync(Address);
    }

    public async UniTask<string> SendTransaction(
        string contractAddress,
        string abi,
        string methodName,
        BigInteger gasLimit,
        object[] parameters
    )
    {
        var transactionHash = await AppKit.Evm.WriteContractAsync(
            contractAddress,
            abi,
            methodName,
            gasLimit,
            parameters
        );
        Debug.Log($"Transaction Sent. Hash: {transactionHash}");
        return transactionHash;
    }

    public async UniTask<TransactionReceipt> GetTransactionReceipt(string transactionHash)
    {
        return await AppKit.Evm.RpcRequestAsync<TransactionReceipt>("eth_getTransactionByHash", transactionHash);
    }

    public async UniTask<BigInteger> EstimateGas(string contractAddress, string abi, string methodName, object[] parameters)
    {
        return await AppKit.Evm.EstimateGasAsync(
            contractAddress,
            abi,
            methodName,
            0,
            parameters
        );
    }

}