using System.Collections.Generic;
using UnityEngine.UIElements;
using Code.Helpers.UniTaskHelpers;
using System;
using static GameController;
using Buffer = Chromia.Buffer;
using Cysharp.Threading.Tasks;
using System.Threading;
using Reown.AppKit.Unity;
using static Queries;

namespace TTT.Components
{
    public enum ModalAction
    {
        CLOSE,
        NEXT_ROUND
    }

    [UxmlElement]
    partial class ModalResult : VisualElement
    {
        public event Action OnClaim;
        public IAsyncEnumerableWithEvent<ModalAction> OnDialogAction => onDialogAction;
        protected readonly AsyncEnumerableWithEvent<ModalAction> onDialogAction = new();

        private Label labelTitle;
        private Label labelPointsGainedValue;
        private Label labelSessionValue;

        private ModalResultPlayerInfo homeInfo;
        private ModalResultPlayerInfo awayInfo;

        private Button claimButton;
        private Button homeButton;
        private Button nextRoundButton;
        private Button viewInExplorerButton;

        private Action openLinkAction = null;


        public ModalResult()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            labelTitle = this.Q<Label>("LableTitle");
            labelPointsGainedValue = this.Q<Label>("LabelPointsGainedValue");
            labelSessionValue = this.Q<Label>("LabelSessionValue");

            var homeElem = this.Q<VisualElement>("HomeInfo");
            var awayElem = this.Q<VisualElement>("AwayInfo");

            homeInfo = homeElem.Q<ModalResultPlayerInfo>();
            awayInfo = awayElem.Q<ModalResultPlayerInfo>();

            claimButton = this.Q<Button>("ButtonClaim");
            homeButton = this.Q<Button>("ButtonHome");
            nextRoundButton = this.Q<Button>("ButtonNextRound");
            viewInExplorerButton = this.Q<Button>("ButtonViewInExplorer");

            claimButton.clicked += () => OnClaim?.Invoke();
            homeButton.clicked += () => onDialogAction.Write(ModalAction.CLOSE);
            nextRoundButton.clicked += () => onDialogAction.Write(ModalAction.NEXT_ROUND);
        }

        private void Reset()
        {
            UnityEngine.Debug.Log("Reset");
            homeInfo.Reset();
            awayInfo.Reset();
            viewInExplorerButton.clicked -= openLinkAction;
        }

        public void SetVisible(bool visible)
        {
            parent.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetClaimState(bool canClaim)
        {
            claimButton.text = canClaim ? "Claim points" : "Waiting for proof...";
            claimButton.SetEnabled(canClaim);
            claimButton.MarkDirtyRepaint();
        }

        public void DisableClaimButton()
        {
            claimButton.text = "Claimed points!";
            claimButton.SetEnabled(false);
            claimButton.MarkDirtyRepaint();
        }

        public void Resolve(
            string sessionId,
            bool? amIWinner,
            List<PlayerData> player,
            bool forfeit
        )
        {
            try
            {
                Reset();

                openLinkAction = () => OpenLinkToExplorer(sessionId);
                viewInExplorerButton.clicked += openLinkAction;

                foreach (var p in player)
                {
                    UnityEngine.Debug.Log($"Player: {p.Address} {p.IsMe} {p.Symbol} {p.IsAI}");
                }

                var home = player.Find(e => e.IsMe);
                var away = player.Find(e => !e.IsMe);

                if (forfeit)
                {
                    labelTitle.text = "Game Canceled";
                }
                else if (amIWinner == null)
                {
                    labelTitle.text = "Draw";
                }
                else
                {
                    labelTitle.text = amIWinner.Value ? "You Win" : "You Lose";
                }

                UnityEngine.Debug.Log($"Home: {home.Address} {amIWinner.HasValue && amIWinner.Value}");

                homeInfo.Populate(home, amIWinner.HasValue && amIWinner.Value);
                awayInfo.Populate(away, amIWinner.HasValue && !amIWinner.Value);

                if (!amIWinner.HasValue)
                {
                    labelPointsGainedValue.text = "50";
                }
                else if (amIWinner.Value)
                {
                    labelPointsGainedValue.text = "100";
                }
                else
                {
                    labelPointsGainedValue.text = "25";
                }
                labelSessionValue.text = Util.FormatAddress(sessionId);
            }
            catch
            {
                onDialogAction.Write(ModalAction.CLOSE);
            }
        }
    }
}