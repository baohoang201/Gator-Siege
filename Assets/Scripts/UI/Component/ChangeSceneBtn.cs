using Gameplay.Event;
using Gameplay.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Component
{
    [RequireComponent(typeof(Button))]
    public class ChangeSceneBtn : MonoBehaviour
    {
        [SerializeField] SceneType sceneType;

        private Button btn;

        private void Awake() {
            btn = GetComponent<Button>();
        }

        private void ChangeScene()
        {
            if (GameManager.Instance.IsPaused()) return;
            GameEvent.RaiseChangeScene(sceneType);
        }

        private void OnEnable()
        {
            btn.onClick.AddListener(ChangeScene);
        }

        private void OnDisable()
        {
            btn.onClick.RemoveListener(ChangeScene);
        }
    }
}