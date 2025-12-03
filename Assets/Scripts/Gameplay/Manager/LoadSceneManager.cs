using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Gameplay.Event;

namespace Gameplay.Manager {
    public enum SceneType {
        Home = 0, Play = 1
    }

    public class LoadSceneManager : MonoBehaviour {
        public static LoadSceneManager Instance { get; private set; }

        [SerializeField] float transitionSceneTime = 1.0f;

        private Animator anim;
        private bool isLoadScene = false;

        private void Awake() {
            if (Instance == null) {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                anim = GetComponent<Animator>();
            }
            else {
                Destroy(gameObject);
            }
        }

        public void ChangeScene(SceneType sceneType) {
            if (isLoadScene) return;

            StartCoroutine(LoadScene((int)sceneType));
        }

        private void SceneLoadedHandle(Scene scene, LoadSceneMode loadSceneMode) {
            StartCoroutine(SceneLoaded());
        }

        private IEnumerator LoadScene(int sceneType) {
            isLoadScene = true;
            anim.SetTrigger("StartLoad");
            GameManager.Instance.SetGameState(GameState.Pause);

            yield return new WaitForSecondsRealtime(transitionSceneTime);

            SceneManager.LoadScene(sceneType);
        }

        private IEnumerator SceneLoaded() {
            anim.SetTrigger("EndLoad");
            GameEvent.RaiseSceneLoaded();

            yield return new WaitForSecondsRealtime(transitionSceneTime);

            GameManager.Instance.SetGameState(GameState.Playing);
            isLoadScene = false;
        }

        private void OnEnable() {
            SceneManager.sceneLoaded += SceneLoadedHandle;
            GameEvent.OnChangeSceneEvent += ChangeScene;
        }

        private void OnDisable() {
            SceneManager.sceneLoaded -= SceneLoadedHandle;
            GameEvent.OnChangeSceneEvent -= ChangeScene;
        }
    }
}
