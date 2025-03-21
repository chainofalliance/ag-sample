using UnityEngine.UIElements;
using static GameController;

namespace TTT.Components
{
    [UxmlElement]
    partial class ModalResultPlayerInfo : VisualElement
    {
        const string SYMBOL = "symbol-";
        const string WINNER = "winner";

        private Label addressLabel;
        private ClassTracker winTracker;
        private ClassTracker symbolTracker;

        public ModalResultPlayerInfo()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttach);

            winTracker = new ClassTracker(this);
            symbolTracker = new ClassTracker(this);
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            addressLabel = this.Q<Label>("LabelAddress");
        }

        public void Populate(PlayerData data, bool isWinner)
        {
            addressLabel.text = Util.FormatAddress(data.Address);
            winTracker.Set(isWinner ? WINNER : "");
            symbolTracker.Set($"{SYMBOL}{data.Symbol.ToString().ToLower()}");
        }

        public void Reset()
        {
            winTracker.Set("");
            symbolTracker.Set("");
        }
    }
}