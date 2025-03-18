using Chromia;
using Nethereum.Util;
using Nethereum.Web3;
using Reown.AppKit.Unity;
using System;
using UnityEngine;

public class AccountManager
{
    public event Action<string> OnAddressConnected;

    public string AddressWithoutPrefix => Address.StartsWith("0x") ? Address.Substring(2) : Address;
    public string Address => Account.Address;
    public string Balance => Web3.Convert.FromWei(Account.Balance, UnitConversion.EthUnit.Ether).ToString("N5");
    public IAccount Account { get; private set; }
    public SignatureProvider SignatureProvider { get; set; }
    public TicTacToeContract TicTacToeContract { get; private set; }

    public bool IsMyAddress(string address)
    {
        var incoming = address.StartsWith("0x") ? address.Substring(2) : address;
        return incoming.ToLower() == AddressWithoutPrefix.ToLower();
    }

    public AccountManager()
    {
        AppKit.AccountConnected += OnAccountConnected;

        SignatureProvider = SignatureProvider.Create();
    }

    public void LocalLogin()
    {
        Account = new LocalAccount();
        FinishLogin();
    }

    private async void OnAccountConnected(object sender, Connector.AccountConnectedEventArgs eventArgs)
    {
        Debug.Log("AccountManager: New Account Connected!");
        Account = new ReownAccount(await eventArgs.GetAccount());
        FinishLogin();
    }

    private void FinishLogin()
    {
        TicTacToeContract = new TicTacToeContract(Account);
        OnAddressConnected?.Invoke(Address);
    }
}
