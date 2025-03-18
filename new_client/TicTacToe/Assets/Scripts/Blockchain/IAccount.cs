using System.Numerics;
using Cysharp.Threading.Tasks;

public interface IAccount
{
    string Address { get; }
    BigInteger Balance { get; }

    UniTask SyncBalance();

    UniTask<string> SendTransaction(
        string contractAddress,
        string abi,
        string methodName,
        object[] parameters
    );
}

