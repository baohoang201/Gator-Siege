using Data;
using Gameplay.Event;
using Gameplay.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Component.Level
{
    public class UILevel : MonoBehaviour
    {
        [SerializeField] TMP_Text _levelText;
        [SerializeField] GameObject _lock;
        [SerializeField] GameObject _complete;

        private Button _btn;
        private int _levelIndex;
        private bool _isUnlocked = false;

        private void Awake()
        {
            _btn = GetComponent<Button>();
        }

        public void SetData(int levelIndex, bool isUnlocked, bool isCompleted)
        {
            _levelIndex = levelIndex;
            _isUnlocked = isUnlocked;

            _levelText.text = (levelIndex + 1).ToString();

            _lock.SetActive(!isUnlocked);
            _complete.SetActive(isCompleted);
        }

        private void SelectLevel()
        {
            if (!_isUnlocked) return;

            UserModel userModel = DataManager.Instance.GetUserModel();
            if (userModel != null) {
                userModel.SetCurrentLevelIndex(_levelIndex);
                GameEvent.RaiseChangeScene(SceneType.Play);
            }
        }

        private void OnEnable()
        {
            _btn.onClick.AddListener(SelectLevel);
        }

        private void OnDisable()
        {
            _btn.onClick.RemoveListener(SelectLevel);
        }
    }
}