using Chromia;
using Cysharp.Threading.Tasks;
using Nethereum.Util;
using Nethereum.Web3;
using Reown.AppKit.Unity;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Buffer = Chromia.Buffer;

public class AccountManager
{
    public event Action<string> OnAddressConnected;
    public event Action<string> OnLoginFailed;

    public string AddressWithoutPrefix => Address.StartsWith("0x") ? Address.Substring(2) : Address;
    public string Address => Account.Address;
    public string Balance => Web3.Convert.FromWei(Account.Balance, UnitConversion.EthUnit.Ether).ToString("N5");
    public IAccount Account { get; private set; }
    public SignatureProvider SignatureProvider { get; set; }
    public TicTacToeContract TicTacToeContract { get; private set; }

    public bool LoggingIn { get; private set; } = false;

    private BlockchainConnectionManager connectionManager;

    public bool IsMyAddress(string address)
    {
        var incoming = address.StartsWith("0x") ? address.Substring(2) : address;
        return incoming.ToLower() == AddressWithoutPrefix.ToLower();
    }

    public AccountManager(BlockchainConnectionManager connectionManager)
    {
        this.connectionManager = connectionManager;
        AppKit.AccountConnected += OnAccountConnected;
        SignatureProvider = SignatureProvider.Create();
    }

    public void LocalLogin()
    {
        try
        {
            Account = new LocalAccount();
            FinishLogin();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to local login: {e.Message}");
            OnLoginFailed?.Invoke(e.Message);
        }
    }

    public async UniTask<Queries.EifEventData[]> GetUnclaimedEifEvents()
    {
        var chrEvents = await Queries.GetUnclaimedEifEvents(connectionManager.AlliancesGamesClient, Buffer.From(Address));
        var evmEvents = await TicTacToeContract.GetClaimStatus(chrEvents);
        return chrEvents.Where((e, i) => !evmEvents[i]).ToArray();
    }

    private async void OnAccountConnected(object sender, Connector.AccountConnectedEventArgs eventArgs)
    {
        LoggingIn = true;
        try
        {
            Account = new ReownAccount(await eventArgs.GetAccount());
            FinishLogin();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to connect account: {e.Message}");
            OnLoginFailed?.Invoke(e.Message);
        }
    }

    private void FinishLogin()
    {
        TicTacToeContract = new TicTacToeContract(Account);
        OnAddressConnected?.Invoke(Address);
        LoggingIn = false;
    }
}
