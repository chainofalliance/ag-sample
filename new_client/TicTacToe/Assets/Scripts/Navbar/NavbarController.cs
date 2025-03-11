using Reown.AppKit.Unity;
using Reown.Sign.Models;

public class NavbarController
{
    private readonly NavbarView view;

    public NavbarController(NavbarView view)
    {
        this.view = view;

        view.OnWalletDisconnect += OnWalletDisconnect;

        AppKit.AccountConnected += async (sender, eventArgs) => {
            Account account = await eventArgs.GetAccount();
            view.SetAddress(account.Address);
        };
    }

    public void SetVisible(bool visible)
    {
        view.SetVisible(visible);
    }

    private void OnWalletDisconnect()
    {
        AppKit.OpenModal();
    }
}
