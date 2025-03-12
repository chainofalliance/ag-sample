using UnityEngine.UIElements;
using System;

public class MenuView
{
    public event Action OnPlayPve;

    private readonly VisualElement root;
    private readonly Button playPveButton;
    private readonly Label labelAddress;
    private readonly Label labelPoints;
    private readonly Label labelTotalMatches;
    private readonly Label labelWonMatches;
    private readonly Label labelDrawMatches;
    private readonly Label labelLostMatches;

    public MenuView(VisualElement root)
    {
        this.root = root;

        labelAddress = root.Q<Label>("LabelAddressValue");
        labelPoints = root.Q<Label>("LabelPointsValue");
        labelTotalMatches = root.Q<Label>("LabelTotalMatchesValue");
        labelWonMatches = root.Q<Label>("LabelWonMatchesValue");
        labelDrawMatches = root.Q<Label>("LabelDrawMatchesValue");
        labelLostMatches = root.Q<Label>("LabelLoseMatchesValue");

        playPveButton = root.Q<Button>("ButtonPlayPve");
        playPveButton.clicked += () => OnPlayPve?.Invoke();
    }

    public void SetVisible(bool visible)
    {
        root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void SetAddress(string address)
    {
        labelAddress.text = address;
    }

    public void SetPoints(string points)
    {
        labelPoints.text = points;
    }

    public void SetStats(int total, int won, int draw, int lost)
    {
        labelTotalMatches.text = total.ToString();
        labelWonMatches.text = won.ToString();
        labelDrawMatches.text = draw.ToString();
        labelLostMatches.text = lost.ToString();
    }
}
