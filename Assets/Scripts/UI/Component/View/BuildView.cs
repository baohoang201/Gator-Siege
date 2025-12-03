using Gameplay.Controller.Level;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Component.View {
    public class BuildView : View {
        [SerializeField] Image timeFillImage;
        [SerializeField] Button startEarlyBtn;
        [SerializeField] LevelGenerator levelGenerator;

        private void UpdateTime(float time, float maxTime) {
            timeFillImage.fillAmount = time / maxTime;
        }

        private void StartEarly() {
            levelGenerator.StartNextWaveEarly();
        }

        private void OnEnable() {
            LevelGenerator.OnBuildTimeChanged += UpdateTime;
            startEarlyBtn.onClick.AddListener(StartEarly);
        }

        private void OnDisable() {
            LevelGenerator.OnBuildTimeChanged -= UpdateTime;
            startEarlyBtn.onClick.RemoveListener(StartEarly);
        }
    }
}