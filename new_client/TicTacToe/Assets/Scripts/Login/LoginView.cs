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
    private readonly ModalError modalError;

    public LoginView(VisualElement root)
    {
        this.root = root;

        walletLoginButton = root.Q<Button>("ButtonConnectWallet");
        guestLoginButton = root.Q<Button>("ButtonLoginGuest");
        modalError = root.panel.visualTree.Q("ModalError").Q<ModalError>();

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
        modalError.SetTitle("Something went wrong");
        modalError.SetInfo(error);
        modalError.SetVisible(true);
        await modalError.OnDialogAction.Task(ct);
        modalError.SetVisible(false);
    }

    public void OpenInfo(string info)
    {
        modalError.SetTitle(info);
        modalError.SetInfo("");
        modalError.SetVisible(true);
        modalError.SetButton(false);
    }

    public void CloseInfo()
    {
        modalError.SetVisible(false);
        modalError.SetButton(true);
    }
}
