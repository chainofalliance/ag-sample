using UnityEngine.UIElements;
using Code.Helpers.UniTaskHelpers;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace TTT.Components
{
    [UxmlElement]
    partial class ModalMatchmaking: VisualElement
    {
        public IAsyncEnumerableWithEvent<bool> OnDialogAction => onDialogAction;
        protected readonly AsyncEnumerableWithEvent<bool> onDialogAction = new();

        private Label labelTitle;
        private Label labelTimer;
        private Button buttonCancel;

        public ModalMatchmaking()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            labelTimer = this.Q<Label>("LabelTimerValue");
            buttonCancel = this.Q<Button>("ButtonLeave");
            buttonCancel.clicked += () => onDialogAction.Write(false);
        }

        public void SetVisible(bool visible)
        {
            parent.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void StartTimer(CancellationToken ct)
        {
            timerTask(ct).Forget();

            async UniTaskVoid timerTask(CancellationToken cts)
            {
                var count = 0;
                while (!cts.IsCancellationRequested)
                {
                    labelTimer.text = $"{count} sec";

                    await UniTask.Delay(TimeSpan.FromMilliseconds(1000), cancellationToken: ct);
                    count++;
                }
            }
        }
    }
}