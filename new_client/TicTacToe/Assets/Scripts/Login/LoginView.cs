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

    public LoginView(VisualElement root)
    {
        this.root = root;

        walletLoginButton = root.Q<Button>("ButtonConnectWallet");
        guestLoginButton = root.Q<Button>("ButtonLoginGuest");
        modalInfo = root.panel.visualTree.Q("ModalInfo").Q<ModalInfo>();

        walletLoginButton.clicked += () => OnWalletLogin?.Invoke();
        guestLoginButton.clicked += () => OnGuestLogin?.Invoke();
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
        guestLoginButton.SetEnabled(true);
    }

    public void DisableButtons()
    {
        walletLoginButton.SetEnabled(false);
        guestLoginButton.SetEnabled(false);
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
