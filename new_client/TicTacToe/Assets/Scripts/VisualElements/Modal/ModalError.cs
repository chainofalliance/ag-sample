using UnityEngine.UIElements;
using Code.Helpers.UniTaskHelpers;

namespace TTT.Components
{
    [UxmlElement]
    partial class ModalError : VisualElement
    {
        public IAsyncEnumerableWithEvent<bool> OnDialogAction => onDialogAction;
        protected readonly AsyncEnumerableWithEvent<bool> onDialogAction = new();

        private Label labelTitle;
        private Label labelInfo;
        private Button buttonCancel;

        public ModalError()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            labelTitle = this.Q<Label>("LableTitle");
            labelInfo = this.Q<Label>("LableInfo");
            buttonCancel = this.Q<Button>("ButtonCancel");
            buttonCancel.clicked += () => onDialogAction.Write(true);
        }

        public void SetTitle(string title)
        {
            labelTitle.text = title;
        }

        public void SetInfo(string info)
        {
            labelInfo.text = info;
        }

        public void SetButton(bool visible, string text = null)
        {
            buttonCancel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (text != null)
            {
                buttonCancel.text = text;
            }
        }

        public void SetVisible(bool visible)
        {
            parent.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}