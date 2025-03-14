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
using Reown.Core.Crypto;

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
        private Action claimAction = null;
        private CancellationTokenSource resultCts;


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

            homeButton.clicked += () => onDialogAction.Write(ModalAction.CLOSE);
            nextRoundButton.clicked += () => onDialogAction.Write(ModalAction.NEXT_ROUND);
        }

        private void Reset()
        {
            homeInfo.Reset();
            awayInfo.Reset();
            claimButton.text = "Claim points";
            claimButton.SetEnabled(false);
            viewInExplorerButton.clicked -= openLinkAction;
            claimButton.clicked -= claimAction;
        }

        public void SetVisible(bool visible)
        {
            style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Resolve(
            string sessionId,
            string winner,
            List<PlayerData> player,
            BlockchainConnectionManager connectionManager
        )
        {
            try
            {
                Reset();

                openLinkAction = () => OpenLinkToExplorer(sessionId);
                viewInExplorerButton.clicked += openLinkAction;

                var home = player.Find(e => e.IsMe);
                var away = player.Find(e => !e.IsMe);
                var homeWinner = Buffer.From(winner) == Buffer.From(home.Address);

                if (winner == null)
                {
                    labelTitle.text = "Draw";
                }
                else
                {
                    labelTitle.text = homeWinner ? "You Win" : "You Lose";
                }

                homeInfo.Populate(home, homeWinner);
                awayInfo.Populate(away, !homeWinner);

                labelPointsGainedValue.text = homeWinner ? "100" : "50";
                labelSessionValue.text = Util.FormatAddress(sessionId);


                // wait until proof is written
                resultCts?.CancelAndDispose();
                resultCts = new();

                EifEventData eventData = null;
                waitForProof(resultCts.Token).Forget();
                async UniTaskVoid waitForProof(CancellationToken ct)
                {
                    while (eventData == null)
                    {
                        await UniTask.Delay(TimeSpan.FromMilliseconds(1000), cancellationToken: ct);
                        eventData = await Queries.getEifEventBySession(connectionManager.AlliancesGamesClient, sessionId);
                    }

                    claimButton.SetEnabled(true);
                }

                claimAction = async () =>
                {
                    if(AppKit.NetworkController.ActiveChain.ChainId != Bootstrap.ChainBNBTestnet.ChainId)
                    {
                        await AppKit.NetworkController.ChangeActiveChainAsync(Bootstrap.ChainBNBTestnet);

                        UnityEngine.Debug.LogError("Error: Not the correct chain active!");
                        return;
                    }

                    if (eventData == null)
                    {
                        UnityEngine.Debug.LogError("Error: eventData is null, claim cannot proceed!");
                        return;
                    }
                    
                    claimButton.SetEnabled(false);
                    UnityEngine.Debug.Log(eventData.ToString());

                    var rawMerkleProof = await Queries.GetEventMerkleProof(connectionManager.AlliancesGamesClient, eventData.EventHash);
                    var merkleProof = EIFUtils.Construct(rawMerkleProof);

                    var claimRes = await TicTacToeContract.Claim(merkleProof, eventData.EncodedData);
                    UnityEngine.Debug.Log("Claim Result: " + claimRes);

                    claimButton.text = $"Claimed {labelPointsGainedValue.text} points";
                };

                claimButton.clicked += claimAction;
            }
            catch
            {
                onDialogAction.Write(ModalAction.CLOSE);
            }
        }
    }
}