using Data;
using Gameplay.Event;
using Gameplay.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Component {
    public class FinishDialog : Dialog {
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text bestScoreText;
        [SerializeField] Button homeBtn;
        [SerializeField] Button replayBtn;

        protected override void Awake() {
            base.Awake();

            homeBtn.onClick.AddListener(ReturnHome);
            replayBtn.onClick.AddListener(Replay);
        }

        private void ReturnHome() {
            GameEvent.RaiseChangeScene(SceneType.Home);
        }

        private void Replay() {
            GameEvent.RaiseChangeScene(SceneType.Play);
        }

        private void FinishHandle() {
            //scoreText.text = userModel.UserData.Score.ToString();
            //bestScoreText.text = userModel.UserData.BestScore.ToString();

            Show();
        }

        public override void Show() {
            base.Show();
        }

        private void OnDestroy() {
            homeBtn.onClick.RemoveListener(ReturnHome);
            replayBtn.onClick.RemoveListener(Replay);
        }
    }
}
