using Chromia;
using Reown.AppKit.Unity;
using Reown.Sign.Models;
using System;

public class AccountManager
{
    public event Action<string> OnAddressConnected;

    public string AddressWithoutPrefix => Address.StartsWith("0x") ? Address.Substring(2) : Address;

    public string Address;
    public Account? Account { get; set; } = null;
    public SignatureProvider SignatureProvider { get; set; }

    public AccountManager()
    {
        AppKit.AccountConnected += OnAccountConnected;
    }

    public void LocalLoginIn()
    {
        SignatureProvider = SignatureProvider.Create();
        Address = "0xf39fd6e51aad88f6f4ce6ab8827279cfffb92266";
        OnAddressConnected?.Invoke(Address);
    }

    private async void OnAccountConnected(object sender, Connector.AccountConnectedEventArgs eventArgs)
    {
        UnityEngine.Debug.Log("AccountManager: New Account Connected!");
        Account = await eventArgs.GetAccount();
        SignatureProvider = SignatureProvider.Create();

        OnAddressConnected?.Invoke(Account?.Address);
        Address = Account?.Address;
    }
}
