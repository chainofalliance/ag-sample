using Nethereum.Contracts.Standards.ERC20.TokenList;
using UnityEngine.UIElements;

namespace TTT.Components
{
    [UxmlElement]
    partial class TableSessionsEntry: VisualElement
    {
        private Label labelSessionId;
        private Label labelOpponent;
        private Label labelOutcome;
        private Label labelPoints;

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
        }

        public void Populate()
        {

        }

        public void SetVisible(bool visible)
        {
            style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}