using UnityEngine.UIElements;
using System;

public class NavbarView
{
    public event Action OnWalletDisconnect;

    private readonly VisualElement root;

    private Label addressLabel;

    public NavbarView(VisualElement root)
    {
        this.root = root;

        addressLabel = root.Q<Label>("LabelAddress");

        var walletDisconnectButton = root.Q<Button>("ButtonDisconnect");
        walletDisconnectButton.clicked += () => OnWalletDisconnect?.Invoke();
    }

    public void SetVisible(bool visible)
    {
        root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void SetAddress(string address)
    {
        addressLabel.text = Util.FormatAddress(address);
    }
}
