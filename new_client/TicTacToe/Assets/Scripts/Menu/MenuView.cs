using UnityEngine.UIElements;
using TTT.Components;
using System;
using static Queries;
using Cysharp.Threading.Tasks;
using static GameController;
using System.Collections.Generic;
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
    private readonly Label labelPoints;
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
        labelPoints = root.Q<Label>("LabelPointsValue");
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

    public void SetPlayerUpdate(PlayerUpdate update)
    {
        var info = update.Info;

        if (info != null)
        {
            labelPoints.text = info.Points.ToString();
            labelTotalMatches.text = (info.WinCount + info.LooseCount + info.DrawCount).ToString();
            labelWonMatches.text = info.WinCount.ToString();
            labelDrawMatches.text = info.DrawCount.ToString();
            labelLostMatches.text = info.LooseCount.ToString();
        } else
        {
            labelPoints.text = "0";
            labelPoints.text = "0";
            labelTotalMatches.text = "0";
            labelWonMatches.text = "0";
            labelDrawMatches.text = "0";
            labelLostMatches.text = "0";
        }

        tableSessions.Populate(update.History);
    }

    public async UniTask<bool> OpenWaitingForMatch(CancellationToken ct)
    {
        try
        {
            modalMatchmaking.SetVisible(true);
            modalMatchmaking.StartTimer(ct);
            var response = await modalMatchmaking.OnDialogAction.Task(ct);

            return response;
        }
        catch (OperationCanceledException) { }
        return default;
    }

    public void CloseWaitingForMatch()
    {
        modalMatchmaking.SetVisible(false);
    }
}
