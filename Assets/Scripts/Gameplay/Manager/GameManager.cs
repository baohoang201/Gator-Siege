using UnityEngine;

namespace Gameplay.Manager {
    public enum GameState {
        Playing, Effect, Pause, Finish
    }

    public class GameManager : MonoBehaviour {
        public static GameManager Instance { get; private set; }

        private GameState _gameState = GameState.Playing;
        public GameState gameState => _gameState;

        private void Awake() {
            if (Instance == null) {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else {
                Destroy(gameObject);
            }
        }

        public void SetGameState(GameState gameState) {
            _gameState = gameState;
        }

        public bool IsPaused() {
            return _gameState != GameState.Playing;
        }

        public bool IsGameState(GameState state) {
            return _gameState == state;
        }
    }
}