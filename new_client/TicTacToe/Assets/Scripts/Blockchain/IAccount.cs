using Cysharp.Threading.Tasks;

public interface IAccount
{
    string Address { get; }

    UniTask<string> SendTransaction(
        string contractAddress,
        string abi,
        string methodName,
        object[] parameters
    );
}

