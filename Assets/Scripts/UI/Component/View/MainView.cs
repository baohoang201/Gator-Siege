using Data;
using Gameplay.Controller;
using Gameplay.Controller.Level;
using Gameplay.Manager;
using TMPro;
using UI.Event;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Component.View {
    public class MainView : MonoBehaviour {
        [SerializeField] TMP_Text levelText;
        [SerializeField] TMP_Text waveText;
        [SerializeField] TMP_Text coinText;

        [SerializeField] View playView;
        [SerializeField] View buildView;

        private void Start() {
            UserModel userModel = DataManager.Instance.GetUserModel();
            if (userModel != null) {
                levelText.text = $"LEVEL {userModel.GetCurrentLevelIndex() + 1}";
            }
        }

        private void UpdateWave(int currentWave, int totalWaves) {
            waveText.text = $"Wave: {currentWave}/{totalWaves}";
        }

        private void UpdateCoin(int amount) {
            coinText.text = amount.ToString();
        }

        private void ChangeView(WavePhase phase, int currentPhaseIndex) {
            switch (phase) {
                case WavePhase.Build:
                    StartCoroutine(playView.Hide(0.3f, () => { StartCoroutine(buildView.Show(0.3f)); }));
                    break;
                case WavePhase.Combat:
                    StartCoroutine(buildView.Hide(0.3f, () => { StartCoroutine(playView.Show(0.3f)); }));
                    break;
            }
        }

        private void OnEnable() {
            UIEvent.OnStartWaveEvent += UpdateWave;
            GameplayController.OnCoinChanged += UpdateCoin;
            LevelGenerator.OnPhaseChanged += ChangeView;
        }

        private void OnDisable() {
            UIEvent.OnStartWaveEvent -= UpdateWave;
            GameplayController.OnCoinChanged -= UpdateCoin;
            LevelGenerator.OnPhaseChanged -= ChangeView;
        }
    }
}