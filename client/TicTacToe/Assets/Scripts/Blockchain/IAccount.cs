using System.Numerics;
using Cysharp.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;

public interface IAccount
{
    string Address { get; }
    BigInteger Balance { get; }

    UniTask SyncBalance();

    UniTask<string> SendTransaction(
        string contractAddress,
        string abi,
        string methodName,
        BigInteger gasLimit,
        object[] parameters
    );

    UniTask<TransactionReceipt> GetTransactionReceipt(string transactionHash);
    UniTask<BigInteger> EstimateGas(
        string contractAddress,
        string abi,
        string methodName,
        object[] parameters
    );

    UniTask<T> ReadContract<T>(
        string contractAddress,
        string abi,
        string methodName,
        object[] parameters
    );
}

