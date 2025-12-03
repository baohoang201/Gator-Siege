using Gameplay.Interface;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Gameplay.Tower;
using Gameplay.Tool;
using Gameplay.Manager;



#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Gameplay.Controller.Ground {
    public class GroundController : MonoBehaviour, IDamageable {
        public enum State { Normal, SlightlyDamaged, HeavilyDamaged, Vanished }

        [Header("Health")]
        [SerializeField] private int _maxHealth = 100;
        [SerializeField] private int _currentHealth = 100;

        [Tooltip("Ngưỡng theo % Max HP để đổi trạng thái: Slight <= this, Heavy <= this")]
        [Range(0f, 1f)][SerializeField] private float _slightThreshold = 0.7f;
        [Range(0f, 1f)][SerializeField] private float _heavyThreshold = 0.35f;

        [Header("Visuals per State (bật/tắt)")]
        [SerializeField] private GameObject _normalVisual;
        [SerializeField] private GameObject _slightVisual;
        [SerializeField] private GameObject _heavyVisual;

        [Header("Props on top (drag & drop here)")]
        [SerializeField] private Transform _propsRoot;
        [SerializeField] private List<Transform> _props = new List<Transform>();

        [Header("Vanish sequence")]
        [SerializeField] private float _sinkDuration = 2.0f;
        [SerializeField] private float _sinkDepth = 5.0f;
        [SerializeField] private AnimationCurve _sinkCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private bool _randomSpinWhileSinking = true;
        [SerializeField] private float _spinSpeed = 90f;
        [SerializeField] private float _timeToEffect = 1f;

        [Header("Colliders/Gameplay")]
        [SerializeField] private Collider[] _groundColliders;

        [Header("Build rules")]
        [SerializeField] private bool _isLand = false;  // ô này có được đặt tháp không
        [SerializeField] private int _repairCost = 5; // chi phí sửa chữa (nếu có)
        public bool IsLand => _isLand;

        [Header("Tower slot")]
        [SerializeField] private Transform _towerAnchor;   // điểm đặt prefab tháp (đặt giữa ô)
        private DefenseTower _placedTower;

        [SerializeField] private GameObject _waterEffectPrefab;

        public bool HasTower => _placedTower != null;
        public DefenseTower PlacedTower => _placedTower;

        public State CurrentState { get; private set; } = State.Normal;

        private bool _vanishing = false;
        public IReadOnlyList<Transform> Props => _props;

        // === Registry & Event ===
        public static readonly List<GroundController> Alive = new List<GroundController>();
        public static event Action<GroundController> OnGroundVanished;  // bắn khi ground bị phá (bắt đầu vanish)

        public bool IsAlive => CurrentState != State.Vanished && !_vanishing;
        public Vector3 Center => transform.position;

        private Pool _waterEffectPool;

        private void Awake() {
            _maxHealth = Mathf.Max(1, _maxHealth);
            _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);

            if (_groundColliders == null || _groundColliders.Length == 0)
                _groundColliders = GetComponentsInChildren<Collider>(includeInactive: true);

            // _props đã được gán sẵn từ Inspector
            _props.RemoveAll(p => p == null);

            UpdateStateAndVisual();

            if (!Alive.Contains(this)) Alive.Add(this);
        }

        private void OnDestroy() {
            Alive.Remove(this);
        }

        // ----------- Damage/Repair (int) -----------
        public void TakeDamage(int damage) {
            if (damage <= 0 || CurrentState == State.Vanished) return;
            _currentHealth = Mathf.Max(0, _currentHealth - damage);
            UpdateStateAndVisual();
            if (_currentHealth <= 0) BeginVanishSequence();
        }

        public void Repair(int amount) {
            if (amount <= 0 || CurrentState == State.Vanished) return;
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            UpdateStateAndVisual();
        }

        // ----------- State & Visual -----------
        private void UpdateStateAndVisual() {
            float ratio = (_maxHealth <= 0) ? 0f : (float)_currentHealth / _maxHealth;

            var prev = CurrentState;
            if (_currentHealth <= 0) CurrentState = State.Vanished;
            else if (ratio <= _heavyThreshold) CurrentState = State.HeavilyDamaged;
            else if (ratio <= _slightThreshold) CurrentState = State.SlightlyDamaged;
            else CurrentState = State.Normal;

            ApplyVisualForState();

            if (CurrentState == State.Vanished && prev != State.Vanished) {
                BeginVanishSequence();
            }
        }

        private void ApplyVisualForState() {
            if (_normalVisual) _normalVisual.SetActive(CurrentState == State.Normal);
            if (_slightVisual) _slightVisual.SetActive(CurrentState == State.SlightlyDamaged);
            if (_heavyVisual) _heavyVisual.SetActive(CurrentState == State.HeavilyDamaged);
        }

        // ----------- Vanish sequence -----------
        private void BeginVanishSequence() {
            if (_vanishing) return;
            _vanishing = true;

            CurrentState = State.Vanished;
            ApplyVisualForState();
            SetGroundColliders(false);

            // BẮN EVENT: báo cho Player/AI biết ground này vừa biến mất
            OnGroundVanished?.Invoke(this);

            bool anyAlive = false;
            for (int i = 0; i < Alive.Count; i++) {
                if (Alive[i] && Alive[i].IsAlive) { anyAlive = true; break; }
            }
            if (!anyAlive) {
                GameplayController.Instance.Finish(false, 0);
            }

            if (_props.Count == 0) {
                Destroy(gameObject);
                return;
            }

            StartCoroutine(SinkPropsThenDestroy());
        }

        private IEnumerator SinkPropsThenDestroy() {
            var fromPos = new List<Vector3>(_props.Count);
            for (int i = 0; i < _props.Count; i++)
                fromPos.Add(_props[i].position);

            if (_waterEffectPool == null) {
                _waterEffectPool = ObjectPool.Instance.CheckAddPool(_waterEffectPrefab);
            }

            bool effected = false;
            float t = 0f;
            while (t < _sinkDuration) {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, _sinkDuration));
                float eval = _sinkCurve.Evaluate(k);

                for (int i = 0; i < _props.Count; i++) {
                    var p = _props[i];
                    if (!p) continue;
                    Vector3 start = fromPos[i];
                    Vector3 target = start + Vector3.down * _sinkDepth;
                    p.position = Vector3.LerpUnclamped(start, target, eval);

                    if (_randomSpinWhileSinking)
                        p.Rotate(Vector3.up, _spinSpeed * Time.deltaTime, Space.World);
                }

                if (!effected && t > _timeToEffect) {
                    GameObject waterEffect = ObjectPool.Instance.Get(_waterEffectPool);
                    waterEffect.transform.position = transform.position;
                    ParticleGroupPlayer particleGroupPlayer = waterEffect.GetComponent<ParticleGroupPlayer>();
                    particleGroupPlayer.Play();
                    SoundManager.Instance.PlaySoundFX(SoundType.WaterEffect);
                    effected = true;
                }

                yield return null;
            }

            for (int i = 0; i < _props.Count; i++)
                if (_props[i]) _props[i].position = fromPos[i] + Vector3.down * _sinkDepth;

            Destroy(gameObject);
        }

        private void SetGroundColliders(bool enable) {
            if (_groundColliders == null) return;
            foreach (var c in _groundColliders)
                if (c != null) c.enabled = enable;
        }

        // ----------- API tiện dụng -----------
        public void SetHealthPercent(float percent01) {
            percent01 = Mathf.Clamp01(percent01);
            _currentHealth = Mathf.RoundToInt(percent01 * _maxHealth);
            UpdateStateAndVisual();
            if (_currentHealth <= 0) BeginVanishSequence();
        }

        public void SetMaxHealth(int max, bool fill = true) {
            _maxHealth = Mathf.Max(1, max);
            if (fill) _currentHealth = _maxHealth;
            else _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);
            UpdateStateAndVisual();
        }

        public int GetHealth() => _currentHealth;
        public int GetMaxHealth() => _maxHealth;

        public Vector3 GetStandPoint(float yOffset = 0.5f) => Center + Vector3.up * yOffset;

        public void SetAsLand(bool isLand) => _isLand = isLand;

        public bool CanPlaceTower() {
            return _isLand && _currentHealth == _maxHealth && !HasTower; // chỉ Normal mới đặt
        }

        public bool CanRepair() {
            return CurrentState == State.SlightlyDamaged || CurrentState == State.HeavilyDamaged;
        }

        public bool CanUpgradeTower() {
            return HasTower && _placedTower.CanUpgrade;
        }

        public bool TryPlaceTower(DefenseTowerSO level1So, out DefenseTower tower) {
            tower = null;
            if (!CanPlaceTower() || level1So == null) return false;

            var prefab = level1So.towerPrefab;
            if (!prefab) return false;

            var anchor = _towerAnchor ? _towerAnchor : this.transform;
            var go = Instantiate(prefab, anchor.position, anchor.rotation, anchor);
            tower = go.GetComponent<DefenseTower>();
            if (!tower) { Destroy(go); return false; }

            tower.InitWithLevel(level1So);
            _placedTower = tower;

            // Đưa tower vào props
            RegisterProp(go.transform);

            return true;
        }

        public bool TryUpgradeTower(DefenseTowerSO nextLevel) {
            if (!HasTower || nextLevel == null) return false;

            var anchor = _towerAnchor ? _towerAnchor : this.transform;
            // Xóa tháp cũ khỏi props và hủy
            if (_placedTower != null) {
                UnregisterProp(_placedTower.transform);
                Destroy(_placedTower.gameObject);
                _placedTower = null;
            }

            // Tạo tháp mới
            if (DefenseTower.TryUpgrade(nextLevel, anchor, out var newTower)) {
                _placedTower = newTower;
                RegisterProp(_placedTower.transform);
                return true;
            }

            return false;
        }

        // Ví dụ: sửa 100% HP
        public void RepairFull() {
            if (!CanRepair()) return;
            _currentHealth = _maxHealth;
            UpdateStateAndVisual();
        }

        public void RegisterProp(Transform prop) {
            if (!prop) return;
            if (!_props.Contains(prop)) _props.Add(prop);
            // Đưa prop vào nhánh props
            var parent = _propsRoot ? _propsRoot : transform;
            prop.SetParent(parent, true);
        }

        public void UnregisterProp(Transform prop) {
            if (!prop) return;
            _props.Remove(prop);
        }

        public void RemoveTower() {
            if (!_placedTower) return;
            UnregisterProp(_placedTower.transform);
            Destroy(_placedTower.gameObject);
            _placedTower = null;
        }

        public int GetRepairCost() => _repairCost;

        public DefenseTower GetPlacedTower() => _placedTower;

        public void TakeDamage(int damage, Vector3 hitDir, float knockbackForce) {
            TakeDamage(damage);
        }

#if UNITY_EDITOR
        [ContextMenu("Editor: Damage -100 HP (Preview Only)")]
        private void Editor_Damage20() {
            Undo.RecordObject(this, "Ground Damage -100");
            TakeDamage(100);
            EditorUtility.SetDirty(this);
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }
}