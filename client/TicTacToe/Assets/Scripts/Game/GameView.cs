using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;
using System.Threading;
using TTT.Components;
using System;
using static GameController;

public class GameView
{
    public event Action OnClickViewInExplorer;
    public event Action<int> OnClickField;
    public event Action OnClickBack;
    public event Action OnClaim;

    private readonly VisualElement root;
    private readonly PlayerInfoElement myPlayerInfo;
    private readonly PlayerInfoElement opponentPlayerInfo;
    private readonly CellElement[] cells;
    private readonly ModalCancelGame modalCancel;
    private readonly ModalResult modalResult;
    private readonly ModalInfo modalInfo;
    private readonly Label labelSessionId;
    private readonly Button buttonViewInExplorer;

    private Dictionary<Messages.Field, PlayerInfoElement> players;
    private CancellationTokenSource turnTimerCts;

    public GameView(VisualElement root)
    {
        this.root = root;

        players = new();

        myPlayerInfo = root.Q("PlayerInfoMe").Q<PlayerInfoElement>();
        opponentPlayerInfo = root.Q("PlayerInfoOpponent").Q<PlayerInfoElement>();

        cells = new CellElement[9];
        for (int i = 0; i < 9; i++)
        {
            var cell = root.Q($"Cell{i}");
            cells[i] = cell.Q<CellElement>();
            int index = i;

            cells[i].SetMyHoverSymbol(Messages.Field.X);
            cells[i].RegisterCallback<ClickEvent>((_) => OnClickField?.Invoke(index));
        }

        modalCancel = root.panel.visualTree.Q("ModalCancelGame").Q<ModalCancelGame>();
        modalResult = root.panel.visualTree.Q("ModalGameResult").Q<ModalResult>();
        modalInfo = root.panel.visualTree.Q("ModalInfo").Q<ModalInfo>();

        labelSessionId = root.Q<Label>("LabelSessionIdValue");
        buttonViewInExplorer = root.Q<Button>("ButtonViewInExplorer");

        buttonViewInExplorer.clicked += () => OnClickViewInExplorer?.Invoke();
        modalResult.OnClaim += () => OnClaim?.Invoke();

        var backButton = root.Q<Button>("ButtonExit");
        backButton.clicked += () => OnClickBack?.Invoke();
    }

    public void SetVisible(bool visible)
    {
        root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void Populate(
        string sessionId,
        PlayerData me,
        PlayerData opponent,
        Messages.Field mySymbol
    )
    {
        labelSessionId.text = Util.FormatAddress(sessionId);

        players.Add(me.Symbol, myPlayerInfo);
        myPlayerInfo.Populate(me.Symbol, me.Address);

        foreach (var cell in cells)
        {
            cell.SetMyHoverSymbol(mySymbol);
        }

        players.Add(opponent.Symbol, opponentPlayerInfo);
        opponentPlayerInfo.Populate(opponent.Symbol, opponent.Address, true);
    }

    public void Reset()
    {
        for (int i = 0; i < 9; i++)
        {
            cells[i].SetSymbol(Messages.Field.Empty);
        }

        myPlayerInfo.Reset();
        opponentPlayerInfo.Reset();

        players.Clear();
        turnTimerCts?.CancelAndDispose();
        turnTimerCts = null;
    }

    public void SetBoard(List<int> fields)
    {
        int idx = 0;
        foreach (var symbolIdx in fields)
        {
            var symbol = (Messages.Field)symbolIdx;
            if (symbol != Messages.Field.Empty)
            {
                cells[idx].SetSymbol(symbol);
            }
            idx++;
        }
    }

    public void StartTurn(Messages.Field turn)
    {
        var currentTurn = players[turn];
        currentTurn.StartTurn();
        StartTurnTimer(currentTurn);
    }

    public void EndTurn(Messages.Field turn)
    {
        players[turn].EndTurn();
    }

    public async UniTask<bool> OpenCancelGame(CancellationToken ct)
    {
        modalCancel.SetVisible(true);
        var response = await modalCancel.OnDialogAction.Task(ct);
        modalCancel.SetVisible(false);
        return response;
    }

    public async UniTask OpenError(string info, CancellationToken ct)
    {
        await modalInfo.ShowError(info);
    }

    public async UniTask<ModalAction> OpenGameResult(
        string sessionId,
        bool? amIWinner,
        List<PlayerData> player,
        bool forfeit,
        CancellationToken ct
    )
    {
        modalResult.SetVisible(true);
        modalResult.Resolve(sessionId, amIWinner, player, forfeit);

        try
        {
            var response = await modalResult.OnDialogAction.Task(ct);
            return response;
        }
        catch (OperationCanceledException) { }
        return default;
    }

    public void UpdateClaimState(bool canClaim)
    {
        modalResult.SetClaimState(canClaim);
    }

    public void DisableClaimButton()
    {
        modalResult.DisableClaimButton();
    }

    public void CloseGameResult()
    {
        modalResult.SetVisible(false);
    }

    private void StartTurnTimer(PlayerInfoElement elem)
    {
        var turnDuration = 30;
        var endTime = DateTime.UtcNow.AddSeconds(turnDuration);

        turnTimerCts?.CancelAndDispose();
        turnTimerCts = new();

        timerTask(turnTimerCts.Token).Forget();

        async UniTaskVoid timerTask(CancellationToken ct)
        {
            while (DateTime.UtcNow <= endTime)
            {
                var remainingTime = (int)Math.Ceiling((endTime - DateTime.UtcNow).TotalSeconds);

                elem.SetTimer(remainingTime);

                await UniTask.Delay(TimeSpan.FromMilliseconds(500), cancellationToken: ct);
            }
        }
    }
}
