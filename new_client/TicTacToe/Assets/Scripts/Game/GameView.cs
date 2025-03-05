using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class GameView
{
    public event Action<int> OnClickField;
    public event Action OnClickBack;

    private readonly VisualElement root;

    private readonly Button[] cells;

    public GameView(VisualElement root)
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

        var backButton = root.Q<Button>("BackButton");
        backButton.clicked += () => OnClickBack?.Invoke();
    }

    public void SetVisible(bool visible)
    {
        root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void Reset()
    {
        for (int i = 0; i < 9; i++)
        {
            cells[i].text = "";
            cells[i].SetEnabled(true);
        }
    }

    public void SetBoard(List<int> fields)
    {
        int idx = 0;
        foreach (var symbolIdx in fields)
        {
            var symbol = (Messages.Field)symbolIdx;
            if (symbol != Messages.Field.Empty)
            {
                cells[idx].text = symbol.ToString();
                cells[idx].SetEnabled(false);
            }
            idx++;
        }
    }
}
