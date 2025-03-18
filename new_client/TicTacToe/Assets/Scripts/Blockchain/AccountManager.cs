using Chromia;
using Reown.AppKit.Unity;
using System;
using UnityEngine;

public class AccountManager
{
    public event Action<string> OnAddressConnected;

    public string AddressWithoutPrefix => Address.StartsWith("0x") ? Address.Substring(2) : Address;

    public string Address => Account.Address;
    public IAccount Account { get; private set; }
    public SignatureProvider SignatureProvider { get; set; }
    public TicTacToeContract TicTacToeContract { get; private set; }

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
