using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Numerics;
using Nethereum.Web3;
using Nethereum.Signer;
using Nethereum.Hex.HexTypes;
using Nethereum.Util;

public class LocalAccount : IAccount
{
    public string Address { get; private set; }
    private readonly Web3 web3;

    private const string PRIVKEY_PLAYER_PREFS_KEY = "local-privkey";

    public LocalAccount()
    {
        var privateKey = LoadOrCreatePrivateKey();
        var key = new EthECKey(privateKey);

        Address = key.GetPublicAddress();

        var ethAccount = new Nethereum.Web3.Accounts.Account(privateKey);
        web3 = new Web3(ethAccount, TicTacToeContract.RPC_URL);
    }

    public async UniTask<string> SendTransaction(
        string contractAddress,
        string abi,
        string methodName,
        object[] parameters
    )
    {
        var contract = web3.Eth.GetContract(abi, contractAddress);
        var function = contract.GetFunction(methodName);

        var estimatedGas = await function.EstimateGasAsync(Address, null, null, parameters);
        if (!await HasEnoughGas(estimatedGas.Value))
        {
            Debug.LogError("Insufficient funds for gas.");
            return null;
        }

        var transactionInput = function.CreateTransactionInput(Address, estimatedGas, new HexBigInteger(0), parameters);
        var transactionHash = await web3.Eth.Transactions.SendTransaction.SendRequestAsync(transactionInput);
        Debug.Log($"Transaction Sent. Hash: {transactionHash}");

        return transactionHash;
    }

    private async UniTask<bool> HasEnoughGas(BigInteger estimatedGas)
    {
        var balanceWei = await web3.Eth.GetBalance.SendRequestAsync(Address);
        var gasCost = estimatedGas * Web3.Convert.ToWei(1, UnitConversion.EthUnit.Gwei);

        Debug.Log($"Balance: {balanceWei.Value} Wei, Estimated Gas Cost: {gasCost} Wei");

        return balanceWei.Value >= gasCost;
    }

    private string LoadOrCreatePrivateKey()
    {
        if (PlayerPrefs.HasKey(PRIVKEY_PLAYER_PREFS_KEY))
        {
            return PlayerPrefs.GetString(PRIVKEY_PLAYER_PREFS_KEY);
        }

        var key = EthECKey.GenerateKey();
        string privateKey = key.GetPrivateKey();
        PlayerPrefs.SetString(PRIVKEY_PLAYER_PREFS_KEY, privateKey);
        PlayerPrefs.Save();
        return privateKey;
    }
}
