using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GameView
{
    public event Action<int> OnClickField;
    public event Action OnClickBack;

    private readonly VisualElement root;

    private readonly List<VisualElement> board;

    private readonly Label player1Label;
    private readonly Label player2Label;
    private readonly Label infoLabel;
    private readonly Button cancelButton;

    public GameView(
        VisualElement root
    )
    {
        this.root = root;

        board = root.Q("Board").Query<VisualElement>("Field").ToList();
        player1Label = root.Q<Label>("Player1");
        player2Label = root.Q<Label>("Player2");
        infoLabel = root.Q<Label>("Info");
        cancelButton = root.Q<Button>("Cancel");
        cancelButton.clicked += () => OnClickBack?.Invoke();
    }

    public void SetVisible(
        bool visible
    )
    {
        root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private string PlayerDataToString(
        GameController.PlayerData playerData
    )
    {
        var address = $"{playerData.Address[..4]}..{playerData.Address[^4..]}";
        return $"<b>{playerData.Symbol}</b> {address} Points: {playerData.Points})";
    }

    public void Reset()
    {
        player1Label.text = "Player1";
        player2Label.text = "Player2";
        infoLabel.text = "";
        foreach (var field in board)
        {
            field.AddToClassList("Empty");
            field.RemoveFromClassList("X");
            field.RemoveFromClassList("O");
            field.UnregisterCallback<ClickEvent>(OnClick);
            field.RegisterCallback<ClickEvent>(OnClick);
        }
    }

    public void Initialize(
        GameController.PlayerData player1,
        GameController.PlayerData player2
    )
    {
        player1Label.text = PlayerDataToString(player1);
        player2Label.text = PlayerDataToString(player2);
    }

    private void OnClick(
        ClickEvent clickEvent
    )
    {
        var field = clickEvent.target as VisualElement;
        var index = board.IndexOf(field);
        OnClickField?.Invoke(index);
    }

    public void SetInfo(
        string info
    )
    {
        Debug.Log(info);
        infoLabel.text = info;
    }

    public void SetBoard(List<int> fields)
    {
        int idx = 0;
        foreach (var symbolIdx in fields)
        {
            var symbol = (GameController.Field)symbolIdx;
            var field = board[idx];
            field.RemoveFromClassList("Empty");
            field.RemoveFromClassList("X");
            field.RemoveFromClassList("O");
            field.AddToClassList(symbol.ToString());
            idx++;
        }
    }
}
