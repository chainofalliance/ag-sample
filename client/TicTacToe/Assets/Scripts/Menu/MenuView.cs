using System;
using UnityEngine;
using UnityEngine.UIElements;

public class MenuView
{
    public event Action<string> OnLogin;
    public event Action OnPlay;
    public event Action OnCancel;

    private readonly VisualElement root;

    private readonly Label accountLabel;
    private readonly TextInputBaseField<string> privKeyInput;
    private readonly Button loginButton;

    private readonly Button playButton;
    private readonly Button cancelButton;
    private readonly Label infoLabel;

    public MenuView(
        VisualElement root
    )
    {
        this.root = root;

        accountLabel = root.Q<Label>("Account");
        privKeyInput = root.Q<TextInputBaseField<string>>("PrivKey");
        loginButton = root.Q<Button>("Login");
        loginButton.clicked += () => OnLogin?.Invoke(privKeyInput.value);

        playButton = root.Q<Button>("Play");
        playButton.clicked += () =>
        {
            cancelButton.SetEnabled(true);
            playButton.SetEnabled(false);
            OnPlay?.Invoke();
        };
        cancelButton = root.Q<Button>("Cancel");
        cancelButton.clicked += () =>
        {
            cancelButton.SetEnabled(false);
            playButton.SetEnabled(true);
            OnCancel?.Invoke();
        };
        
        infoLabel = root.Q<Label>("Info");

        playButton.SetEnabled(false);
        cancelButton.SetEnabled(false);
    }

    public void SetVisible(
        bool visible
    )
    {
        root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void SetInfo(
        string info = ""
    )
    {
        Debug.Log(info);
        infoLabel.text = info;
    }

    public void SetLogin(
        string account,
        int points
    )
    {
        accountLabel.text = $"Logged in as: {account}\nPoints: {points}";
        privKeyInput.value = "";
        privKeyInput.SetEnabled(false);
        loginButton.SetEnabled(false);
        playButton.SetEnabled(true);
    }
}
