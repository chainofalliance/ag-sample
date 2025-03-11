using UnityEngine.UIElements;
using System;

public class LoginView
{
    public event Action OnWalletLogin;
    public event Action OnGuestLogin;

    private readonly VisualElement root;
    private readonly Button walletLoginButton;
    private readonly Button guestLoginButton;

    public LoginView(VisualElement root)
    {
        this.root = root;

        walletLoginButton = root.Q<Button>("ButtonConnectWallet");
        guestLoginButton = root.Q<Button>("ButtonLoginGuest");

        walletLoginButton.clicked += () => OnWalletLogin?.Invoke();
        guestLoginButton.clicked += () => OnGuestLogin?.Invoke();
    }

    public void SetVisible(bool visible)
    {
        root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
