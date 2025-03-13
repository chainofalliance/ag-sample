using UnityEngine.UIElements;
using System;
using static Queries;

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

        //ContainerUnclaimedPoints
        //LabelUnclaimedPointsValue
        //ButtonClaim

        playPveButton = root.Q<Button>("ButtonPlayPve");
        playPveButton.clicked += () => OnPlayPve?.Invoke();
    }

    public void SetVisible(bool visible)
    {
        root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void SetAddress(string address)
    {
        labelAddress.text = Util.FormatAddress(address);
    }

    public void SetPlayerInfo(PlayerInfoResponse info)
    {
        labelPoints.text = info.Points.ToString();
        labelTotalMatches.text = (info.WinCount + info.LooseCount + info.DrawCount).ToString();
        labelWonMatches.text = info.WinCount.ToString();
        labelDrawMatches.text = info.DrawCount.ToString();
        labelLostMatches.text = info.LooseCount.ToString();
    }
}
