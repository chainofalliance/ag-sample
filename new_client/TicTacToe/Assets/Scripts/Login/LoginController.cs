using Reown.AppKit.Unity;
using UnityEngine;

public class LoginController
{
    private readonly LoginView view;

    public LoginController(LoginView view)
    {
        this.view = view;

        view.OnWalletLogin += OnWalletLogin;
        view.OnGuestLogin += OnGuestLogin;
    }

    public void SetVisible(bool visible)
    {
        view.SetVisible(visible);
    }

    private void OnWalletLogin()
    {
        AppKit.OpenModal();
    }

    private void OnGuestLogin()
    {
        Debug.Log("OnGuestLogin");
    }
}
