using System.Threading;
using System.Threading.Tasks;
using Reown.AppKit.Unity;
using UnityEngine;

public class LoginController
{
    private readonly LoginView view;
    private readonly AccountManager accountManager;

    public LoginController(LoginView view, AccountManager accountManager)
    {
        this.view = view;
        this.accountManager = accountManager;

        view.OnWalletLogin += OnWalletLogin;
        view.OnGuestLogin += OnGuestLogin;
        accountManager.OnLoginFailed += OnLoginFailed;

        AppKit.ModalController.OpenStateChanged += (sender, eventArgs) =>
        {
            if (!eventArgs.IsOpen)
            {
                view.CloseInfo();
            }
        };
    }

    public void SetVisible(bool visible)
    {
        view.SetVisible(visible);
        view.CloseInfo();
    }

    private void OnWalletLogin()
    {
        AppKit.OpenModal();
        view.OpenInfo("Waiting for wallet connection...");
    }

    private void OnGuestLogin()
    {
        accountManager.LocalLogin();
        view.OpenInfo("Logging in as guest...");
    }

    private async void OnLoginFailed(string error)
    {
        await view.OpenError($"Failed to login: {error}", CancellationToken.None);
    }
}
