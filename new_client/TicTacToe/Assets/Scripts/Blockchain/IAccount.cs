using System.Numerics;
using Cysharp.Threading.Tasks;
using Nethereum.Hex.HexTypes;

public interface IAccount
{
    string Address { get; }
    BigInteger Balance { get; }

    UniTask SyncBalance();

    UniTask<string> SendTransaction(
        string contractAddress,
        string abi,
        string methodName,
        HexBigInteger gasLimit,
        object[] parameters
    );
}

