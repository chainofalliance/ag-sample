using Reown.AppKit.Unity;

public class NavbarController
{
    private readonly NavbarView view;

    public NavbarController(NavbarView view, AccountManager accountManager)
    {
        this.view = view;

        view.OnWalletDisconnect += OnWalletDisconnect;

        accountManager.OnAddressConnected += (address) =>
        {
            view.SetAddress(address);
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
