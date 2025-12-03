using Gameplay.Manager;
using System;
using System.Collections;
using UI.Component.View;
using UnityEngine;

namespace Gameplay.Controller {
    public enum LoseReason { NoGroundLeft }

    public class GameplayController : MonoBehaviour {
        public static GameplayController Instance { get; private set; }

        [Header("Currency (Coin in-run)")]
        [SerializeField] private int _currentCoin = 0;

        [Header("Finish UI")]
        [SerializeField] private FinishView _winView;
        [SerializeField] private FinishView _loseView;
        [SerializeField] private float delayToFinish;

        // ==== Public events ====
        public static event Action<int> OnCoinChanged;     // coin thay đổi trong run

        // ==== Runtime flags ====
        private bool _finished = false;

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
            }
            else {
                Instance = this;
            }
        }

        private void Start() {
            Time.timeScale = 1f;
            OnCoinChanged?.Invoke(_currentCoin);
        }

        // ------- Coin runtime -------
        public void AddCoin(int amount) {
            _currentCoin += amount;
            OnCoinChanged?.Invoke(_currentCoin);
        }

        public int GetCurrentCoin() => _currentCoin;

        public bool SpendCoin(int amount) {
            if (_currentCoin >= amount) {
                _currentCoin -= amount;
                OnCoinChanged?.Invoke(_currentCoin);
                return true;
            }
            return false;
        }

        // ------- Lose/Win entry points -------
        private void TriggerLose() {
            if (_loseView != null) {
                SoundManager.Instance.PlaySoundFX(SoundType.Victory);
                _loseView.Show(); 
            }
        }

        /// <summary>
        /// Gọi khi hoàn tất LEVEL (hết waves). Cộng Gem rồi mở FinishView có gem.
        /// </summary>
        private void TriggerWin(int gem) {
            var user = DataManager.Instance?.GetUserModel();
            if (user != null && gem > 0) {
                user.AddGems(gem);
                user.CompleteLevel();
                user.SaveData();
            }

            if (_winView != null) {
                _winView.SetGem(gem.ToString());
                _winView.Show();
            }
        }

        public void Finish(bool isWin, int gem = 0) {
            if (_finished) return;

            _finished = true;
            StartCoroutine(DelayedFinish(isWin, gem));
        }

        private IEnumerator DelayedFinish(bool isWin, int gem = 0) {
            yield return new WaitForSeconds(delayToFinish);

            if (isWin) {
                TriggerWin(gem);
            }
            else {
                TriggerLose();
            }
        }
    }
}