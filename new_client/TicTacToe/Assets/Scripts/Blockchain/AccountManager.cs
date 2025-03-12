using Chromia;
using Reown.AppKit.Unity;
using Reown.Sign.Models;
using System;

public class AccountManager
{

    public event Action<string> OnAddressConnected;

    public Account? Account { get; set; } = null;
    public SignatureProvider SignatureProvider { get; set; }

    public AccountManager()
    {
        AppKit.AccountConnected += OnAccountConnected;
    }

    private async void OnAccountConnected(object sender, Connector.AccountConnectedEventArgs eventArgs)
    {
        UnityEngine.Debug.Log("AccountManager: New Account Connected!");
        Account = await eventArgs.GetAccount();
        SignatureProvider = SignatureProvider.Create();

        OnAddressConnected?.Invoke(Account?.Address);
    }
}
