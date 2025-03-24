using UnityEngine.UIElements;
using System;
using TTT.Components;
using System.Threading;
using Cysharp.Threading.Tasks;

public class LoginView
{
    public event Action OnWalletLogin;
    public event Action OnGuestLogin;

    private readonly VisualElement root;
    private readonly Button walletLoginButton;
    private readonly Button guestLoginButton;
    private readonly ModalInfo modalInfo;

    private bool isGuestLoginDisabled = true;

    public LoginView(VisualElement root)
    {
        this.root = root;

        walletLoginButton = root.Q<Button>("ButtonConnectWallet");
        guestLoginButton = root.Q<Button>("ButtonLoginGuest");
        modalInfo = root.panel.visualTree.Q("ModalInfo").Q<ModalInfo>();

        walletLoginButton.clicked += () => OnWalletLogin?.Invoke();
    }

    public void SetVisible(bool visible)
    {
        root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        if (visible)
        {
            EnableButtons();
        }
    }
    public void EnableButtons()
    {
        walletLoginButton.SetEnabled(true);
        if (!isGuestLoginDisabled)
        {
            guestLoginButton.SetEnabled(true);
        }
    }

    public void DisableButtons()
    {
        walletLoginButton.SetEnabled(false);
        if (!isGuestLoginDisabled)
        {
            guestLoginButton.SetEnabled(false);
        }
    }

    public void EnableGuestLogin()
    {
        isGuestLoginDisabled = false;
        guestLoginButton.style.display = DisplayStyle.Flex;
        guestLoginButton.clicked += () => OnGuestLogin?.Invoke();
    }

    public async UniTask OpenError(string error, CancellationToken ct)
    {
        await modalInfo.ShowError(error, ct: ct);
    }

    public void OpenInfo(string info)
    {
        modalInfo.ShowInfo("Login", info);
    }

    public void CloseInfo()
    {
        modalInfo.Close();
    }
}
