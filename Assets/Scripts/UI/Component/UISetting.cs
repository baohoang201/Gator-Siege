namespace UI.Component {
    using Data;
    using Gameplay.Event;
    using Gameplay.Manager;
    using UI.Event;
    using UnityEngine;
    using UnityEngine.UI;

    public class UISettings : Dialog {
        [SerializeField] Slider _musicSlider;
        [SerializeField] Slider _soundFxSlider;
        [SerializeField] Image _musicFillImage;
        [SerializeField] Image _soundFxFillImage;

        [SerializeField] Button _pauseBtn;
        [SerializeField] Button _homeBtn;
        [SerializeField] Button _continueBtn;
        [SerializeField] Button _replayBtn;

        private UserModel _userModel;

        private void Start() {
            _userModel = DataManager.Instance.GetUserModel();

            _musicSlider.value = _userModel.GetMusicValue();
            _soundFxSlider.value = _userModel.GetSoundFXValue();

            UIEvent.RaiseChangeMusic(_musicSlider.value);
            UIEvent.RaiseChangeSoundFX(_soundFxSlider.value);
        }

        private void ChangeMusic(float value) {
            _userModel.SetMusicValue(value);
            _musicFillImage.fillAmount = value;
            UIEvent.RaiseChangeMusic(value);
        }

        private void ChangeSoundFx(float value) {
            _userModel.SetSoundFXValue(value);
            _soundFxFillImage.fillAmount = value;
            UIEvent.RaiseChangeSoundFX(value);
        }

        private void ContinueGame() {
            GameManager.Instance.SetGameState(GameState.Playing);
            Hide();
        }

        private void GoToHome() {
            GameEvent.RaiseChangeScene(SceneType.Home);
            Hide();
        }

        private void PauseGame() {
            Show();
            GameManager.Instance.SetGameState(GameState.Pause);
        }

        private void ReplayGame() {
            GameEvent.RaiseChangeScene(SceneType.Play);
            Hide();
        }

        private void OnEnable() {
            _musicSlider.onValueChanged.AddListener(ChangeMusic);
            _soundFxSlider.onValueChanged.AddListener(ChangeSoundFx);
            _pauseBtn.onClick.AddListener(PauseGame);
            _homeBtn.onClick.AddListener(GoToHome);
            _continueBtn.onClick.AddListener(ContinueGame);
            _replayBtn.onClick.AddListener(ReplayGame);
        }

        private void OnDisable() {
            _musicSlider.onValueChanged.RemoveListener(ChangeMusic);
            _soundFxSlider.onValueChanged.RemoveListener(ChangeSoundFx);
            _pauseBtn.onClick.RemoveListener(PauseGame);
            _homeBtn.onClick.RemoveListener(GoToHome);
            _continueBtn.onClick.RemoveListener(ContinueGame);
            _replayBtn.onClick.RemoveListener(ReplayGame);
        }
    }
}