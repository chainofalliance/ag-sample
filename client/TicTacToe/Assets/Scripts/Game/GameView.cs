using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GameView
{
    public event Action<int> OnClickField;
    public event Action OnClickBack;

    private readonly VisualElement root;

    private readonly Button[] cells;

    private readonly Label player1Label;
    private readonly Label player2Label;
    private readonly Button backButton;


    private readonly Label infoLabel;

    public GameView(
        VisualElement root
    )
    {
        this.root = root;

        cells = new Button[9];
        for (int i = 0; i < 9; i++)
        {
            cells[i] = root.Q<Button>($"Cell{i}");
            int index = i;
            cells[i].clicked += () =>
            {
                OnClickField?.Invoke(index);
            };
        }

        Debug.Log("cells"  + cells.Length);

        player1Label = root.Q<Label>("Player1");
        player2Label = root.Q<Label>("Player2");
        infoLabel = root.Q<Label>("InfoLabel");

        backButton = root.Q<Button>("BackButton");
        backButton.clicked += () => OnClickBack?.Invoke();
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
        for (int i = 0; i < 9; i++)
        {
            cells[i].text = "";
            cells[i].SetEnabled(true);
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
            var symbol = (Messages.Field)symbolIdx;
            if(symbol != Messages.Field.Empty)
            {
                cells[idx].text =  symbol.ToString();
                cells[idx].SetEnabled(false);
            }
            idx++;
        }
    }
}
