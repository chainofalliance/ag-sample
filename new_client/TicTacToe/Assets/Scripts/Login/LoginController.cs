using Reown.AppKit.Unity;

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
        accountManager.LocalLoginIn();
    }
}
