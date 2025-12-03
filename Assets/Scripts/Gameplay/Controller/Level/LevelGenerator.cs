using Data;
using Gameplay.Controller.Enemy;
using Gameplay.Controller.Ground;
using Gameplay.Manager;
using Gameplay.SO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UI.Event;
using UnityEngine;

namespace Gameplay.Controller.Level {
    public enum WavePhase { Combat, Build, Done }

    public class LevelGenerator : MonoBehaviour {
        public static LevelGenerator Instance { get; private set; }

        [Header("Data")]
        [SerializeField] private LevelControllerSO _levelControllerSO;
        [SerializeField] private int _levelIndexOverride = -1; // =-1 dùng UserModel

        [Header("Ground Generation")]
        [SerializeField] private List<GroundEntry> _grounds; // thay vì List<GameObject> _groundPrefabs
        [SerializeField] private float _cellSize = 2f;
        [SerializeField] private float _groundY = 0f;

        [Tooltip("Giảm tỉ lệ xuất hiện của các loại sau (nhân vào weight)")]
        [SerializeField, Range(0f, 1f)] private float _bridgeMultiplier = 0.35f; // cầu hiếm hơn
        [SerializeField, Range(0f, 1f)] private float _treeBigMultiplier = 0.25f; // cây to hiếm hơn


        [Header("Enemy Spawn")]
        [SerializeField] private List<EnemySO> _enemyCatalog; // map EnemyType -> prefab/chỉ số
        [SerializeField] private float _spawnPaddingCells = 2f; // khoảng cách thêm so với biên đảo (đơn vị cell)
        [SerializeField] private float _spawnHeight = 0f;
        [SerializeField] private float _firstWaveDelay = 0f;

        [SerializeField] private UI.Component.View.WinWaveView _winWaveView;

        // runtime
        private LevelSO _levelSO;
        private readonly HashSet<Vector2Int> _cells = new HashSet<Vector2Int>();
        private readonly List<GameObject> _spawnedTiles = new List<GameObject>();
        private readonly List<GameObject> _aliveEnemies = new List<GameObject>();

        private int _currentWaveIndex = 0;
        private float _outerRadiusWorld = 5f; // tính sau khi sinh đảo
        private Dictionary<EnemyType, EnemySO> _enemyDict;

        public WavePhase Phase { get; private set; } = WavePhase.Build;

        public static System.Action<WavePhase, int> OnPhaseChanged;   // (phase, currentWaveIndex)
        public System.Action<int> OnWaveStarted;    // waveIndex
        public System.Action<int> OnWaveCleared;    // waveIndex
        public static System.Action<float, float> OnBuildTimeChanged; // seconds remaining
        public System.Action OnAllWavesDone;

        [SerializeField] private float _buildDuration = 20f;   // 20s build
        private Coroutine _buildTimerCo;
        private Coroutine _watchAliveCo;

        public static event Action OnLevelReady;

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            else {
                Instance = this;
            }
        }

        private void Start() {
            // Lấy level SO
            int idx = _levelIndexOverride;
            if (idx < 0) {
                var user = DataManager.Instance?.GetUserModel();
                idx = user != null ? user.GetCurrentLevelIndex() : 0;
            }

            _levelSO = _levelControllerSO?.GetLevel(idx);
            if (_levelSO == null) {
                Debug.LogError("[LevelGenerator] LevelSO is null. Kiểm tra LevelControllerSO / index.");
                return;
            }

            // Map EnemyType -> EnemySO
            _enemyDict = new Dictionary<EnemyType, EnemySO>();
            foreach (var e in _enemyCatalog) {
                if (e != null) _enemyDict[e.Type] = e;
            }

            // 1) Sinh đảo liền mạch, tâm trùng generator
            GenerateIsland(_levelSO.GroundCount);

            // 2) Tính bán kính spawn (cách đảo 1 khoảng)
            _outerRadiusWorld = ComputeSpawnRadiusWorld();

            // 3) Auto spawn wave đầu
            if (_levelSO.WaveDatas != null && _levelSO.WaveDatas.Count > 0) {
                StartCoroutine(SpawnWaveAfterDelay(_currentWaveIndex, _firstWaveDelay));
            }
        }

        // ------------------------- ISLAND GENERATION -------------------------

        private void GenerateIsland(int groundCount) {
            // dọn cũ
            _cells.Clear();
            foreach (var go in _spawnedTiles) if (go) Destroy(go);
            _spawnedTiles.Clear();

            var rng = new System.Random();
            Vector2Int cur = Vector2Int.zero;
            _cells.Add(cur);

            Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

            // random-walk tạo cụm liền kề
            while (_cells.Count < groundCount) {
                if (rng.NextDouble() < 0.3)
                    cur = _cells.ElementAt(rng.Next(_cells.Count));

                var dir = dirs[rng.Next(dirs.Length)];
                var next = cur + dir;

                if (!_cells.Contains(next))
                    _cells.Add(next);

                cur = next;
            }

            // --- cân tâm cụm ô ---
            Vector2 sum = Vector2.zero;
            foreach (var c in _cells) sum += new Vector2(c.x, c.y);
            Vector2 avg = sum / Mathf.Max(1, _cells.Count);
            avg = new Vector2(Mathf.Round(avg.x), Mathf.Round(avg.y));

            var allCells = _cells.ToList();

            // === B1: Random vị trí Land ===
            int landCount = Mathf.Clamp(_levelSO.LandCount, 0, allCells.Count);
            Shuffle(allCells);                     // Fisher–Yates
            var landCells = new HashSet<Vector2Int>();
            for (int i = 0; i < landCount; i++) landCells.Add(allCells[i]);

            // Chuẩn bị prefab
            var landPrefab = GetLandPrefab();      // Prefab dành cho Land (bỏ qua trọng số)
            if (landPrefab == null)
                Debug.LogWarning("[LevelGenerator] Không tìm thấy Land prefab. Sẽ dùng prefab thường và chỉ SetAsLand(true).");

            // === B2+B3: Instantiate ===
            foreach (var c in allCells) {
                bool isLand = landCells.Contains(c);

                GameObject prefab;
                if (isLand && landPrefab != null)
                    prefab = landPrefab;           // ô Land: bỏ qua trọng số
                else
                    prefab = PickNonLandPrefab();  // ô thường: chọn theo trọng số (không bao gồm Land)

                if (prefab == null) continue;

                // Cân tâm: pos = (cell - avg) * cellSize + transform.position
                Vector2 centered = new Vector2(c.x, c.y) - avg; // _avgCell đã tính ở phần cân tâm của bạn
                Vector3 pos = transform.position + new Vector3(centered.x * _cellSize, _groundY, centered.y * _cellSize);

                var go = Instantiate(prefab, pos, Quaternion.identity, transform);
                _spawnedTiles.Add(go);

                // Đặt cờ IsLand đúng theo lựa chọn vị trí
                var gc = go.GetComponentInChildren<GroundController>();
                if (gc != null) gc.SetAsLand(isLand);
            }

            OnLevelReady?.Invoke();
        }

        // Trộn danh sách cell (Fisher–Yates)
        private void Shuffle<T>(IList<T> list) {
            var rng = new System.Random();
            for (int i = list.Count - 1; i > 0; i--) {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // Lấy prefab dành cho Land (bỏ qua trọng số)
        private GameObject GetLandPrefab() {
            // Giả sử bạn có GroundType.Land trong GroundEntry
            var entry = _grounds.FirstOrDefault(g => g.Type == GroundType.Land && g.Prefab != null);
            return entry !=null ? entry.Prefab : null;
        }

        // Chọn prefab theo trọng số nhưng LOẠI trừ Land
        private GameObject PickNonLandPrefab() {
            if (_grounds == null || _grounds.Count == 0) return null;

            float total = 0f;
            // gom trọng số, bỏ Land
            foreach (var ge in _grounds) {
                if (ge == null || ge.Prefab == null) continue;
                if (ge.Type == GroundType.Land) continue; // loại land khỏi random

                float mul = 1f;
                switch (ge.Type) {
                    case GroundType.Bridge: mul = _bridgeMultiplier; break;
                    case GroundType.TreeBig: mul = _treeBigMultiplier; break;
                    default: mul = 1f; break;
                }
                total += Mathf.Max(0f, ge.Weight * mul);
            }
            if (total <= 0f) {
                // fallback: lấy entry đầu tiên KHÔNG phải Land
                var e = _grounds.FirstOrDefault(g => g.Prefab != null && g.Type != GroundType.Land);
                return e != null ? e.Prefab : null;
            }

            float r = UnityEngine.Random.value * total;
            float acc = 0f;
            foreach (var ge in _grounds) {
                if (ge == null || ge.Prefab == null) continue;
                if (ge.Type == GroundType.Land) continue;

                float mul = 1f;
                switch (ge.Type) {
                    case GroundType.Bridge: mul = _bridgeMultiplier; break;
                    case GroundType.TreeBig: mul = _treeBigMultiplier; break;
                    default: mul = 1f; break;
                }
                float w = Mathf.Max(0f, ge.Weight * mul);
                if (w <= 0f) continue;

                acc += w;
                if (r <= acc) return ge.Prefab;
            }
            return null;
        }


        private GameObject PickGroundPrefab(System.Random rng) {
            if (_grounds == null || _grounds.Count == 0) return null;

            // Tính tổng trọng số sau khi áp hệ số loại
            float total = 0f;
            float[] acc = new float[_grounds.Count];

            for (int i = 0; i < _grounds.Count; i++) {
                var ge = _grounds[i];
                if (ge.Prefab == null) { acc[i] = total; continue; }

                float mul = 1f;
                switch (ge.Type) {
                    case GroundType.Bridge: mul = _bridgeMultiplier; break;
                    case GroundType.TreeBig: mul = _treeBigMultiplier; break;
                    default: mul = 1f; break;
                }

                float w = Mathf.Max(0f, ge.Weight * mul);
                total += w;
                acc[i] = total;
            }

            if (total <= 0f) {
                // fallback: chọn entry đầu tiên có prefab
                for (int i = 0; i < _grounds.Count; i++)
                    if (_grounds[i].Prefab) return _grounds[i].Prefab;
                return null;
            }

            // bốc ngẫu nhiên theo tổng trọng số
            float r = (float)rng.NextDouble() * total;
            int idx = System.Array.FindIndex(acc, v => v > r);
            idx = Mathf.Clamp(idx, 0, _grounds.Count - 1);
            return _grounds[idx].Prefab;
        }

        private Vector3 CellToWorld(Vector2Int c) {
            // Tâm generator là (0,0) cell => đảo cân quanh transform.position
            return transform.position + new Vector3(c.x * _cellSize, _groundY, c.y * _cellSize);
        }

        private float ComputeSpawnRadiusWorld() {
            // Lấy bbox theo cell rồi + padding, chuyển sang world
            if (_cells.Count == 0) return 5f;

            int minX = _cells.Min(v => v.x);
            int maxX = _cells.Max(v => v.x);
            int minY = _cells.Min(v => v.y);
            int maxY = _cells.Max(v => v.y);

            float halfW = (maxX - minX + 1) * 0.5f;
            float halfH = (maxY - minY + 1) * 0.5f;
            float radiusCells = Mathf.Max(halfW, halfH) + _spawnPaddingCells;

            return radiusCells * _cellSize;
        }

        // ------------------------- WAVES / ENEMY SPAWN -------------------------

        private IEnumerator SpawnWaveAfterDelay(int waveIndex, float delay) {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            SpawnWave(waveIndex);
        }

        public void SpawnWave(int waveIndex) {
            if (_levelSO == null || _levelSO.WaveDatas == null || waveIndex < 0 || waveIndex >= _levelSO.WaveDatas.Count) {
                Debug.LogWarning("[LevelGenerator] Wave index không hợp lệ.");
                return;
            }

            var wave = _levelSO.WaveDatas[waveIndex];
            if (wave.EnemyTypes == null || wave.EnemyCount == null || wave.EnemyTypes.Count == 0) {
                Debug.LogWarning("[LevelGenerator] Wave trống.");
                return;
            }

            // Phase -> Combat
            Phase = WavePhase.Combat;
            OnPhaseChanged?.Invoke(Phase, _currentWaveIndex);

            int kinds = Mathf.Min(wave.EnemyTypes.Count, wave.EnemyCount.Count);
            for (int i = 0; i < kinds; i++) {
                EnemyType type = wave.EnemyTypes[i];
                int count = Mathf.Max(0, wave.EnemyCount[i]);

                if (!_enemyDict.TryGetValue(type, out var enemySo) || enemySo.EnemyPrefab == null) {
                    Debug.LogWarning($"[LevelGenerator] EnemySO / Prefab thiếu cho type {type}");
                    continue;
                }

                for (int k = 0; k < count; k++) {
                    Vector3 spawnPos = PickSpawnPosition();
                    var go = Instantiate(enemySo.EnemyPrefab, spawnPos, Quaternion.identity);
                    _aliveEnemies.Add(go);

                    // Đăng ký OnDied nếu có CrocodileAI
                    var ai = go.GetComponent<CrocodileAI>();
                    if (ai != null) ai.OnDied += HandleEnemyDied;
                }
            }

            _currentWaveIndex = waveIndex;
            UIEvent.RaiseStartWave(_currentWaveIndex + 1, _levelSO.WwaveDatasCount());
            OnWaveStarted?.Invoke(_currentWaveIndex);

            // Hủy watcher cũ nếu còn, rồi bật watcher mới
            if (_watchAliveCo != null) StopCoroutine(_watchAliveCo);
            _watchAliveCo = StartCoroutine(WatchAliveAndEnterBuildWhenClear());
        }

        private void HandleEnemyDied(CrocodileAI ai) {
            if (ai != null) ai.OnDied -= HandleEnemyDied;

            _aliveEnemies.RemoveAll(x => x == null);
        }

        private void OnWinWaveClosed() {
            if (_winWaveView != null) _winWaveView.OnHidden -= OnWinWaveClosed;
            EnterBuildPhase();
        }

        private IEnumerator WatchAliveAndEnterBuildWhenClear() {
            while (Phase == WavePhase.Combat) {
                _aliveEnemies.RemoveAll(x => x == null);
                if (_aliveEnemies.Count == 0) {
                    OnWaveCleared?.Invoke(_currentWaveIndex);
                    if (_currentWaveIndex + 1 < _levelSO.WwaveDatasCount()) {
                        if (Phase == WavePhase.Combat && _aliveEnemies.Count == 0) {
                            OnWaveCleared?.Invoke(_currentWaveIndex);

                            int reward = 0;
                            if (_levelSO != null &&
                                _levelSO.WaveDatas != null &&
                                _currentWaveIndex >= 0 &&
                                _currentWaveIndex < _levelSO.WaveDatas.Count) {
                                reward = Mathf.Max(0, _levelSO.WaveDatas[_currentWaveIndex].CoinReward);
                            }

                            // Cộng coin
                            GameplayController.Instance?.AddCoin(reward);

                            // Show WinWaveView và CHỜ đóng rồi mới vào Build
                            if (_winWaveView != null) {
                                _winWaveView.OnHidden -= OnWinWaveClosed; // tránh double-subscribe
                                _winWaveView.SetCoin(reward.ToString());
                                _winWaveView.OnHidden += OnWinWaveClosed;
                                _winWaveView.Show();
                            }
                            else {
                                EnterBuildPhase();
                            }
                        }
                    }
                    else FinishAllWaves();
                    yield break;
                }
                yield return new WaitForSeconds(0.5f);
            }
        }
        private void EnterBuildPhase() {
            Phase = WavePhase.Build;
            OnPhaseChanged?.Invoke(Phase, _currentWaveIndex);

            if (_watchAliveCo != null) { StopCoroutine(_watchAliveCo); _watchAliveCo = null; }
            if (_buildTimerCo != null) StopCoroutine(_buildTimerCo);
            _buildTimerCo = StartCoroutine(BuildCountdown());
        }

        private IEnumerator BuildCountdown() {
            float maxTime = _buildDuration;
            float remain = _buildDuration;
            while (remain > 0f) {
                OnBuildTimeChanged?.Invoke(remain, maxTime);
                yield return null;
                remain -= Time.deltaTime;
            }
            OnBuildTimeChanged?.Invoke(0f, maxTime);
            _buildTimerCo = null;

            StartCoroutine(SpawnWaveAfterDelay(_currentWaveIndex + 1, 0f));
        }

        // Button "Bắt đầu sớm" gọi hàm này
        public void StartNextWaveEarly() {
            if (Phase != WavePhase.Build) return;
            if (_buildTimerCo != null) { StopCoroutine(_buildTimerCo); _buildTimerCo = null; }

            if (_currentWaveIndex + 1 < _levelSO.WwaveDatasCount()) {
                GameplayController.Instance.AddCoin(10);
                StartCoroutine(SpawnWaveAfterDelay(_currentWaveIndex + 1, 0f));
            }
            else
                FinishAllWaves();
        }

        private void FinishAllWaves() {
            Phase = WavePhase.Done;
            OnPhaseChanged?.Invoke(Phase, _currentWaveIndex);
            OnAllWavesDone?.Invoke();

            int gem = (_levelSO != null) ? Mathf.Max(0, _levelSO.GemReward) : 0;
            GameplayController.Instance.Finish(true, gem);
        }

        private Vector3 PickSpawnPosition() {
            // random góc xung quanh tâm, bán kính _outerRadiusWorld, cao _spawnHeight
            float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            Vector3 center = transform.position;
            Vector3 pos = center + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * _outerRadiusWorld;
            pos.y = _spawnHeight;

            return pos;
        }

        private void OnDestroy() {
            if (_watchAliveCo != null) StopCoroutine(_watchAliveCo);
            if (_buildTimerCo != null) StopCoroutine(_buildTimerCo);

            foreach (var go in _aliveEnemies) {
                if (!go) continue;
                var ai = go.GetComponent<CrocodileAI>();
                if (ai != null) ai.OnDied -= HandleEnemyDied;
            }
            _aliveEnemies.Clear();
        }
    }

    // tiện mở rộng LevelSO để lấy số wave an toàn
    static class LevelSOExt {
        public static int WwaveDatasCount(this LevelSO so) => so?.WaveDatas?.Count ?? 0;
    }
}
