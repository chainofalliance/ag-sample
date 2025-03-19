using UnityEngine.UIElements;
using Code.Helpers.UniTaskHelpers;

namespace TTT.Components
{
    [UxmlElement]
    partial class ModalError : VisualElement
    {
        public IAsyncEnumerableWithEvent<bool> OnDialogAction => onDialogAction;
        protected readonly AsyncEnumerableWithEvent<bool> onDialogAction = new();

        private Label labelInfo;
        private Button buttonCancel;

        public ModalError()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            labelInfo = this.Q<Label>("LableInfo");
            buttonCancel = this.Q<Button>("ButtonCancel");
            buttonCancel.clicked += () => onDialogAction.Write(true);
        }

        public void SetInfo(string info)
        {
            labelInfo.text = info;
        }

        public void SetVisible(bool visible)
        {
            parent.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}