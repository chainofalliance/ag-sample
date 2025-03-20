using System.Numerics;
using Cysharp.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.Util;
using Nethereum.Web3;
using Reown.AppKit.Unity;
using Reown.AppKit.Unity.WebGl.Wagmi;
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
        HexBigInteger gasLimit,
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
}