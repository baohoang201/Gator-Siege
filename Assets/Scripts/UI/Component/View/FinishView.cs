using Data;
using Gameplay.Event;
using Gameplay.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

namespace UI.Component.View {
    public class FinishView : Dialog {
        [SerializeField] TMP_Text gemRewardText;
        [SerializeField] Button homeBtn;
        [SerializeField] Button replayBtn;
        [SerializeField] Button nextBtn;

        public void SetGem(string gemText) {
            if (gemRewardText != null ) {
                gemRewardText.text = gemText;
            }
        }

        private void ReturnHome() {
            Hide();
            GameEvent.RaiseChangeScene(Gameplay.Manager.SceneType.Home);
        }

        private void Replay() {
            Hide();
            GameEvent.RaiseChangeScene(Gameplay.Manager.SceneType.Play);
        }

        private void Next() {
            Hide();
            UserModel userModel = DataManager.Instance.GetUserModel();
            if (userModel.TrySetNextLevel()) {
                GameEvent.RaiseChangeScene(Gameplay.Manager.SceneType.Play);
            }
            else {
                GameEvent.RaiseChangeScene(Gameplay.Manager.SceneType.Home);
            }
        }

        private void OnEnable() {
            homeBtn.onClick.AddListener(ReturnHome);
            replayBtn.onClick.AddListener(Replay);
            nextBtn?.onClick.AddListener(Next);
        }

        private void OnDisable() {
            homeBtn.onClick.RemoveListener(ReturnHome);
            replayBtn.onClick.RemoveListener(Replay);
            nextBtn?.onClick.RemoveListener(Next);
        }
    }
}