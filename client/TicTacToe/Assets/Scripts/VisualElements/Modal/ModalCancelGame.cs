using UnityEngine.UIElements;
using Code.Helpers.UniTaskHelpers;

namespace TTT.Components
{
    [UxmlElement]
    partial class ModalCancelGame : VisualElement
    {
        public IAsyncEnumerableWithEvent<bool> OnDialogAction => onDialogAction;
        protected readonly AsyncEnumerableWithEvent<bool> onDialogAction = new();

        private Button buttonCancel;
        private Button buttonLeave;

        public ModalCancelGame()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            buttonCancel = this.Q<Button>("ButtonCancel");
            buttonLeave = this.Q<Button>("ButtonLeave");
            buttonCancel.clicked += () => onDialogAction.Write(false);
            buttonLeave.clicked += () => onDialogAction.Write(true);
        }

        public void SetVisible(bool visible)
        {
            parent.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}