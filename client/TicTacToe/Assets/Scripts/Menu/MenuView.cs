using System;
using UnityEngine;
using UnityEngine.UIElements;

public class MenuView
{
    public event Action<string> OnLogin;
    public event Action OnSync;
    public event Action OnPlay;
    public event Action OnCancel;

    private readonly VisualElement root;

    private readonly Label titleLabel;

    private readonly Label accountLabel;
    private readonly TextInputBaseField<string> privKeyInput;
    private readonly Button loginButton;

    private readonly Button syncButton;
    private readonly Button playButton;
    private readonly Button cancelButton;
    private readonly Label infoLabel;
    private readonly Toggle devnetToggle;

    public bool ConnectToDevnet =>
#if DEPLOYED
        true;
#else
        devnetToggle.value;
#endif

    // TODO add UI for local connection
    public bool ConnectToLocal => false;

    public MenuView(
        VisualElement root
    )
    {
        this.root = root;

        titleLabel = root.Q<Label>("Title");
        accountLabel = root.Q<Label>("Account");
        privKeyInput = root.Q<TextInputBaseField<string>>("PrivKey");
        loginButton = root.Q<Button>("Login");
        loginButton.clicked += () => OnLogin?.Invoke(privKeyInput.value);

        syncButton = root.Q<Button>("Sync");
        syncButton.clicked += () =>
        {
            OnSync?.Invoke();
        };
        syncButton.SetEnabled(false);
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

        infoLabel = root.Q<Label>("InfoLabel");
        devnetToggle = root.Q<Toggle>("DevnetToggle");
#if DEPLOYED
        devnetToggle.style.display = DisplayStyle.None;
#endif

        playButton.SetEnabled(false);
        cancelButton.SetEnabled(false);
    }

    public void SetVersion(string version)
    {
        titleLabel.text = $"TicTacToe v{version}";
    }

    public void SetPrivKey(
        string privKey
    )
    {
        privKeyInput.SetValueWithoutNotify(privKey);
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
        syncButton.SetEnabled(true);
    }
}
