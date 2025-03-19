using System;
using Reown.AppKit.Unity;

public class NavbarController
{
    public event Action OnHome
    {
        add => view.OnHome += value;
        remove => view.OnHome -= value;
    }
    public event Action OnDisconnect;

    private readonly NavbarView view;
    private readonly AccountManager accountManager;

    public NavbarController(NavbarView view, AccountManager accountManager)
    {
        this.view = view;
        this.accountManager = accountManager;

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
        if (accountManager.Account is LocalAccount)
        {
            OnDisconnect?.Invoke();
        }
        else
        {
            AppKit.OpenModal();
        }
    }
}
