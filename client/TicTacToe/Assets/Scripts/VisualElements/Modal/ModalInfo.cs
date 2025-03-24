using UnityEngine.UIElements;
using Code.Helpers.UniTaskHelpers;
using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace TTT.Components
{
    [UxmlElement]
    partial class ModalInfo : VisualElement
    {
        public const string TITLE_ERROR = "Something went wrong";

        protected readonly AsyncEnumerableWithEvent<bool> onDialogAction = new();

        private Label labelTitle;
        private Label labelInfo;
        private Button buttonCancel;
        private Action onInfoClick;

        public ModalInfo()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            labelTitle = this.Q<Label>("LabelTitle");
            labelInfo = this.Q<Label>("LabelInfo");
            buttonCancel = this.Q<Button>("ButtonCancel");
            buttonCancel.clicked += () => onDialogAction.Write(true);
        }

        public async UniTask Show(string title, string info, bool button = true, string buttonText = null, CancellationToken ct = default)
        {
            parent.style.display = DisplayStyle.Flex;

            labelTitle.text = title;
            labelInfo.text = info;
            buttonCancel.style.display = button ? DisplayStyle.Flex : DisplayStyle.None;
            if (buttonText != null)
            {
                buttonCancel.text = buttonText;
            }
            else
            {
                buttonCancel.text = "Ok";
            }

            if (button)
            {
                await onDialogAction.Task(ct);
                Close();
            }
        }

        public void ShowInfo(string title, string info)
        {
            Show(title, info, false).Forget();
        }

        public async UniTask ShowError(string info, Exception exception = null, CancellationToken ct = default)
        {
            info = $"{info}\n\n{exception?.Message}";
            Debug.LogError($"{info}\n{exception?.StackTrace}");
            await Show(TITLE_ERROR, info, ct: ct);
        }

        public async UniTask CloseAfter(float seconds, CancellationToken ct = default)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: ct);
            Close();
        }

        public void Close()
        {
            onInfoClick = null;
            parent.style.display = DisplayStyle.None;
        }

        public void SetInfoClickCallback(Action callback)
        {
            onInfoClick = callback;
            labelInfo.RegisterCallback<ClickEvent>(_ => onInfoClick?.Invoke());
        }
    }
}