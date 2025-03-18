using UnityEngine.UIElements;
using TTT.Components;
using System;
using static Queries;
using Cysharp.Threading.Tasks;
using static GameController;
using System.Threading;

public class MenuView
{
    public event Action OnPlayPve;

    public event Action OnClickViewAllSessions
    {
        add
        {
            tableSessions.OnClickViewAllSessions += value;
        }
        remove
        {
            tableSessions.OnClickViewAllSessions -= value;
        }
    }

    private readonly VisualElement root;
    private readonly Button playPveButton;
    private readonly Label labelAddress;
    private readonly Label labelPointsChr;
    private readonly Label labelPointsEvm;
    private readonly Label labelTotalMatches;
    private readonly Label labelWonMatches;
    private readonly Label labelDrawMatches;
    private readonly Label labelLostMatches;
    private readonly TableSessions tableSessions;
    private readonly ModalMatchmaking modalMatchmaking;

    public MenuView(VisualElement root)
    {
        this.root = root;

        labelAddress = root.Q<Label>("LabelAddressValue");
        labelPointsChr = root.Q<Label>("LabelPointsValueChr");
        labelPointsEvm = root.Q<Label>("LabelPointsValueEvm");
        labelTotalMatches = root.Q<Label>("LabelTotalMatchesValue");
        labelWonMatches = root.Q<Label>("LabelWonMatchesValue");
        labelDrawMatches = root.Q<Label>("LabelDrawMatchesValue");
        labelLostMatches = root.Q<Label>("LabelLoseMatchesValue");

        var tableElem = root.Q<VisualElement>("SectionLastSessions");
        tableSessions = tableElem.Q<TableSessions>();

        modalMatchmaking = root.panel.visualTree.Q("ModalMatchmaking").Q<ModalMatchmaking>();
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

    public bool IsVisible()
    {
        return root.style.display == DisplayStyle.Flex;
    }

    public void SetAddress(string address)
    {
        labelAddress.text = Util.FormatAddress(address);
    }

    public void SetPlayerUpdate(PlayerUpdate update, int pointsEvm)
    {
        var info = update.Info;

        if (info != null)
        {
            labelPointsChr.text = info.Points.ToString();
            labelPointsEvm.text = pointsEvm.ToString();
            labelTotalMatches.text = (info.WinCount + info.LooseCount + info.DrawCount).ToString();
            labelWonMatches.text = info.WinCount.ToString();
            labelDrawMatches.text = info.DrawCount.ToString();
            labelLostMatches.text = info.LooseCount.ToString();
        }
        else
        {
            labelPointsChr.text = "0";
            labelPointsEvm.text = "0";
            labelTotalMatches.text = "0";
            labelWonMatches.text = "0";
            labelDrawMatches.text = "0";
            labelLostMatches.text = "0";
        }

        tableSessions.Populate(update.History);
    }

    public void OpenMatchmaking()
    {
        modalMatchmaking.SetVisible(true);
        UpdateMatchmakingTimer(0);
    }

    public void UpdateMatchmakingTimer(int seconds)
    {
        modalMatchmaking.UpdateTimer(seconds);
    }

    public async UniTask<bool> OpenWaitingForMatch(CancellationToken ct)
    {
        return await modalMatchmaking.OnDialogAction.Task(ct);
    }

    public void CloseWaitingForMatch()
    {
        modalMatchmaking.SetVisible(false);
    }
}
