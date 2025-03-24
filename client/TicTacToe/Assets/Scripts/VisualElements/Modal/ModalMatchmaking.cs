using UnityEngine.UIElements;
using Code.Helpers.UniTaskHelpers;

namespace TTT.Components
{
    [UxmlElement]
    partial class ModalMatchmaking : VisualElement
    {
        public IAsyncEnumerableWithEvent<bool> OnDialogAction => onDialogAction;
        protected readonly AsyncEnumerableWithEvent<bool> onDialogAction = new();

        private Label labelStatus;
        private Label labelTimer;
        private Button buttonCancel;

        public ModalMatchmaking()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            labelStatus = this.Q<Label>("LableStatus");
            labelTimer = this.Q<Label>("LabelTimerValue");
            buttonCancel = this.Q<Button>("ButtonLeave");
            buttonCancel.clicked += () => onDialogAction.Write(false);
        }

        public void SetStatus(string status)
        {
            labelStatus.text = status;
        }

        public void SetVisible(bool visible)
        {
            parent.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void UpdateTimer(int seconds)
        {
            labelTimer.text = $"{seconds} sec";
        }

        public void EnableLeaveButton()
        {
            buttonCancel.SetEnabled(true);
        }

        public void DisableLeaveButton()
        {
            buttonCancel.SetEnabled(false);
        }
    }
}