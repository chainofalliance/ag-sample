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

    private readonly VisualElement root;
    private readonly PlayerInfoElement playerInfoSelf;
    private readonly PlayerInfoElement playerInfoOpponent;
    private readonly CellElement[] cells;
    private readonly VisualElement modalContainer;
    private readonly ModalResult modalResult;
    private readonly Label labelSessionId;
    private readonly Button buttonViewInExplorer;

    private Dictionary<Messages.Field, PlayerInfoElement> players;
    private CancellationTokenSource turnTimerCts;

    public GameView(VisualElement root)
    {
        this.root = root;

        players = new();

        playerInfoSelf = root.Q("PlayerInfoMe").Q<PlayerInfoElement>();
        playerInfoOpponent = root.Q("PlayerInfoOpponent").Q<PlayerInfoElement>();

        cells = new CellElement[9];
        for (int i = 0; i < 9; i++)
        {
            var cell = root.Q($"Cell{i}");
            cells[i] = cell.Q<CellElement>();
            int index = i;

            cells[i].RegisterCallback<ClickEvent>((_) => OnClickField?.Invoke(index));
        }

        modalContainer = root.Q("ResultModalContainer");
        modalResult = modalContainer.Q<ModalResult>();

        labelSessionId = root.Q<Label>("LabelSessionIdValue");
        buttonViewInExplorer = root.Q<Button>("ButtonViewInExplorer");

        buttonViewInExplorer.clicked += () => OnClickViewInExplorer?.Invoke();

        //var backButton = root.Q<Button>("BackButton");
        //backButton.clicked += () => OnClickBack?.Invoke();
    }

    public void SetVisible(bool visible)
    {
        root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void Populate(
        string sessionId,
        PlayerData player1,
        PlayerData player2
    )
    {
        labelSessionId.text = Util.FormatAddress(sessionId);

        players.Add(player1.Symbol, playerInfoSelf);
        playerInfoSelf.Populate(player1.Symbol, player1.Address);

        players.Add(player2.Symbol, playerInfoOpponent);
        playerInfoOpponent.Populate(player2.Symbol, player2.Address, true);
    }

    public void Reset()
    {
        for (int i = 0; i < 9; i++)
        {
            cells[i].SetSymbol(Messages.Field.Empty);
        }

        playerInfoSelf.EndTurn();
        playerInfoOpponent.EndTurn();

        players.Clear();
        turnTimerCts?.CancelAndDispose();
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

    public async UniTask<ModalAction> OpenGameResult(
        string sessionId, string winner, List<PlayerData> player,
        BlockchainConnectionManager connectionManager, CancellationToken ct)
    {
        modalContainer.style.display = DisplayStyle.Flex;
        modalResult.Resolve(sessionId, winner, player, connectionManager);

        try
        {
            var response = await modalResult.OnDialogAction.Task(ct);
            return response;
        }
        catch (OperationCanceledException) { }
        return default;
    }

    public void CloseGameResult()
    {
        modalContainer.style.display = DisplayStyle.None;
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
