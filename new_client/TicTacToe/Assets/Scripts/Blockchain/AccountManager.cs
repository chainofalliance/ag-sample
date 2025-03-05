using Chromia;
using Reown.AppKit.Unity;
using Reown.Sign.Models;

public class AccountManager
{
    public Account? Account { get; set; } = null;
    public SignatureProvider SignatureProvider { get; set; }

    public AccountManager()
    {
        AppKit.AccountConnected += OnAccountConnected;
        // For faster testing 
        SignatureProvider = SignatureProvider.Create();
    }

    private async void OnAccountConnected(object sender, Connector.AccountConnectedEventArgs eventArgs)
    {
        UnityEngine.Debug.Log("AccountManager: New Account Connected!");
        Account = await eventArgs.GetAccount();
        SignatureProvider = SignatureProvider.Create();
    }
}
