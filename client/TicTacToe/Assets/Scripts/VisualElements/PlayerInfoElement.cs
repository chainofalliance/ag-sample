using UnityEngine.UIElements;

namespace TTT.Components
{
    [UxmlElement]
    partial class PlayerInfoElement : VisualElement
    {
        const string SYMBOL = "symbol-";
        const string TURN = "your-turn";

        private readonly ClassTracker symbolClassTracker;
        private readonly ClassTracker turnClassTracker;
        private Label labelPlayerAddress;
        private Label labelPlayerAddressValue;
        private Label labelTurnTimer;
        private Messages.Field symbol;

        public PlayerInfoElement()
        {
            symbolClassTracker = new ClassTracker(this);
            turnClassTracker = new ClassTracker(this);

            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            labelPlayerAddress = this.Q<Label>("LabelPlayerAddress");
            labelPlayerAddressValue = this.Q<Label>("LabelPlayerAddressValue");
            labelTurnTimer = this.Q<Label>("LabelTurnTimer");
        }

        public void Populate(Messages.Field symbol, string address, bool isOpponent = false)
        {
            symbolClassTracker.Set($"{SYMBOL}{symbol.ToString().ToLower()}");
            labelPlayerAddressValue.text = Util.FormatAddress(address);
            labelTurnTimer.text = "00:30";

            if (isOpponent)
            {
                labelPlayerAddress.text = "Opponent";
            }

            this.symbol = symbol;
        }

        public void StartTurn()
        {
            turnClassTracker.Set(TURN);
        }

        public void SetTimer(long timeLeft)
        {
            var secs = timeLeft % 60;
            var mins = timeLeft / 60;
            labelTurnTimer.text = $"{mins:00}:{secs:00}";
        }

        public void EndTurn()
        {
            turnClassTracker.Set("");
            labelTurnTimer.text = "00:30";
        }

        public void Reset()
        {
            symbolClassTracker.Set("");
            turnClassTracker.Set("");
            labelTurnTimer.text = "00:30";

        }
    }
}