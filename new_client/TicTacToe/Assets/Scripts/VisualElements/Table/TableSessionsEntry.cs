using UnityEngine;
using UnityEngine.UIElements;

using static Queries;

namespace TTT.Components
{
    [UxmlElement]
    partial class TableSessionsEntry: VisualElement
    {
        private Label labelSessionId;
        private Label labelOpponent;
        private Label labelOutcome;
        private Label labelPoints;
        private Button buttonViewSession;

        private string sessionId;

        public TableSessionsEntry() 
        {
            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            labelSessionId = this.Q<Label>("LabelSessionIdValue");
            labelOpponent = this.Q<Label>("LabelOpponentValue");
            labelOutcome = this.Q<Label>("LabelOutcomeValue");
            labelPoints = this.Q<Label>("LabelPointsValue");
            buttonViewSession = this.Q<Button>("ButtonViewSession");

            buttonViewSession.clicked += () =>
            {
                Application.OpenURL($"{Config.EXPLORER_URL}sessions/{sessionId}");
            };
        }

        public void Populate(PlayerHistoryResponse history)
        {
            sessionId = history.SessionId;

            labelSessionId.text = Util.FormatAddress(history.SessionId);
            labelOpponent.text = Util.FormatAddress($"0x{history.Opponent.Parse()}");
            labelPoints.text = history.Points.ToString();
            handleOutcome(history.Outcome);
        }

        public void SetVisible(bool visible)
        {
            style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void handleOutcome(Outcome outcome)
        {
            if (outcome == Outcome.WIN)
            {
                labelOutcome.text = "Win";
                labelOutcome.style.color = new StyleColor(Color.green);
            } 
            else if(outcome == Outcome.LOOSE)
            {
                labelOutcome.text = "Lose";
                labelOutcome.style.color = new StyleColor(Color.red);
            } else
            {
                labelOutcome.text = "Draw";
                labelOutcome.style.color = new StyleColor(Color.white);
            }
        }
    }
}