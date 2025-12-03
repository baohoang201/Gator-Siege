using Gameplay.Controller.Level;
using Gameplay.Manager;
using Gameplay.Tool;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Weapon {
    public class WeaponHandler : MonoBehaviour {
        [Header("Setup")]
        [SerializeField] private LayerMask _enemyLayer;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _weaponAnchor;               // chỗ gắn vũ khí & cũng là muzzle mặc định
        [SerializeField] private AnimatorOverrideController _overrideController;

        [Header("Projectile Defaults (Range)")]
        [SerializeField] private float _projectileSpeed = 20f;
        [SerializeField] private float _projectileLifeSeconds = 3f;     // fallback nếu prefab không tự hủy
        [SerializeField] private Transform _muzzleOverride;              // nếu có muzzle riêng
        [SerializeField] private GameObject _explosionPrefab;

        private readonly List<WeaponHitbox> _meleeHitboxes = new();

        // UI events
        public static event Action<float, float> OnCooldownTick;
        public event Action OnAttackTriggered;
        public event Action OnAttackAvailable;

        private float _cooldownRemain;
        private float _cooldownTotal;
        private bool _isAttacking;

        private WeaponSO _currentWeapon;
        public WeaponSO CurrentWeapon => _currentWeapon;

        private GameObject _spawnedWeapon; // giữ instance để thay vũ khí thì destroy cũ
        private Pool _projectilePool, _explosionPool;

        private void Awake() {
            UpdateAnimations();
            RecomputeCooldownTotal();
            ApplyHitboxConfig();
        }

        private void Update() {
            if (_cooldownRemain > 0f) {
                _cooldownRemain = Mathf.Max(0f, _cooldownRemain - Time.deltaTime);
                OnCooldownTick?.Invoke(_cooldownRemain, _cooldownTotal);

                if (_cooldownRemain <= 0f) {
                    OnAttackAvailable?.Invoke();
                    _isAttacking = false;
                }
            }
        }

        public void SetWeapon(WeaponSO weapon) {
            _currentWeapon = weapon;
            UpdateAnimations();
            RecomputeCooldownTotal();

            // clear vũ khí cũ
            if (_spawnedWeapon) Destroy(_spawnedWeapon);

            // spawn vũ khí mới
            if (weapon && weapon.WeaponPrefab && _weaponAnchor)
                _spawnedWeapon = Instantiate(weapon.WeaponPrefab, _weaponAnchor);

            _cooldownRemain = 0f;
            _isAttacking = false;
            OnCooldownTick?.Invoke(0f, _cooldownTotal);

            _meleeHitboxes.Clear();
            if (_spawnedWeapon) {
                var hb = _spawnedWeapon.GetComponentInChildren<WeaponHitbox>();
                if (hb) _meleeHitboxes.Add(hb);
            }
            ApplyHitboxConfig();
        }

        public bool CanAttack() {
            if (_currentWeapon == null) return false;
            return _cooldownRemain <= 0f && !_isAttacking;
        }

        public void Attack(Vector3 attackDir) {
            if (!CanAttack() || _currentWeapon == null) return;

            _isAttacking = true;
            SoundManager.Instance.PlaySoundFX(SoundType.Click);

            // cooldown
            RecomputeCooldownTotal();
            _cooldownRemain = _cooldownTotal;
            OnCooldownTick?.Invoke(_cooldownRemain, _cooldownTotal);
            OnAttackTriggered?.Invoke();

            // anim
            if (_animator) _animator.SetTrigger("Attack");
        }

        // ====== PUBLIC API để bắn đạn (Range) ======
        /// <summary>
        /// Bắn một viên đạn theo hướng world (đã normalize bên trong).
        /// Có thể gọi từ Animation Event hoặc code khác.
        /// </summary>
        public void FireProjectile() {
            if (_currentWeapon == null || _currentWeapon.ProjectilePrefab == null) return;

            // vị trí bắn: muzzleOverride > weaponAnchor > transform
            Transform muzzle = _muzzleOverride ? _muzzleOverride : (_weaponAnchor ? _weaponAnchor : transform);

            if (_projectilePool == null)
                _projectilePool = ObjectPool.Instance.CheckAddPool(_currentWeapon.ProjectilePrefab);

            if (_explosionPool == null) 
                _explosionPool = ObjectPool.Instance.CheckAddPool(_explosionPrefab);

            SoundManager.Instance.PlaySoundFX(SoundType.Shoot);

            var go = ObjectPool.Instance.Get(_projectilePool);
            go.transform.position = muzzle.position;

            var explosion = ObjectPool.Instance.Get(_explosionPool);
            explosion.transform.position = _muzzleOverride.position;
            explosion.GetComponent<ParticleGroupPlayer>().Play();

            var proj = go.GetComponent<Projectile>();
            if (proj != null) {
                // gửi đủ thông tin cho đạn
                proj.Launch(new Projectile.Params {
                    Damage = _currentWeapon.Damage,
                    Speed = _projectileSpeed,
                    LifeTime = _projectileLifeSeconds,
                    Range = _currentWeapon.Range,
                    EnemyLayer = _enemyLayer,
                    Direction = _muzzleOverride.forward
                });
            }
            else {
                // fallback: tự đẩy rigidbody nếu prefab chưa có script
                var rb = go.GetComponent<Rigidbody>();
                Vector3 dir = _muzzleOverride.forward;
                if (rb) rb.linearVelocity = dir * _projectileSpeed;
                Destroy(go, _projectileLifeSeconds);
            }
        }

        private void UpdateAnimations() {
            if (_overrideController == null || _currentWeapon == null) return;

            if (_currentWeapon.IdleAnim) _overrideController["Idle"] = _currentWeapon.IdleAnim;
            if (_currentWeapon.MoveAnim) _overrideController["Move"] = _currentWeapon.MoveAnim;
            if (_currentWeapon.AttackAnim) _overrideController["Attack"] = _currentWeapon.AttackAnim;

            if (_animator && _animator.runtimeAnimatorController != _overrideController)
                _animator.runtimeAnimatorController = _overrideController;
        }

        private void RecomputeCooldownTotal() {
            if (_currentWeapon == null || _currentWeapon.AttackSpeed <= 0f)
                _cooldownTotal = 0.5f;
            else
                _cooldownTotal = 1f / Mathf.Max(0.0001f, _currentWeapon.AttackSpeed);
        }

        private void ApplyHitboxConfig() {
            if (_meleeHitboxes == null) return;
            int dmg = _currentWeapon ? _currentWeapon.Damage : 10;
            foreach (var hb in _meleeHitboxes)
                if (hb) hb.Configure(dmg, _enemyLayer);
        }

        // ===== Animation Events cho melee =====
        public void Attack_BeginSwing() {
            if (_currentWeapon == null) return;
            if (_currentWeapon.Type == WeaponType.Meele)
                foreach (var hb in _meleeHitboxes) hb?.BeginSwing();
        }

        public void Attack_EndSwing() {
            foreach (var hb in _meleeHitboxes) hb?.EndSwing();
        }

        private void ChangePhase(WavePhase wavePhase, int i) {
            if (wavePhase == WavePhase.Combat) {
                OnCooldownTick?.Invoke(0f, 1f);
            }
        }

        private void OnEnable() {
            LevelGenerator.OnPhaseChanged += ChangePhase;
        }

        private void OnDisable() {
            LevelGenerator.OnPhaseChanged -= ChangePhase;
        }

        private void OnDrawGizmosSelected() {
            if (_currentWeapon != null && _currentWeapon.Type == WeaponType.Range) {
                Gizmos.color = Color.yellow;
                var start = (_muzzleOverride ? _muzzleOverride.position : (_weaponAnchor ? _weaponAnchor.position : transform.position)) + Vector3.up * 0.0f;
                Gizmos.DrawLine(start, start + transform.forward * _currentWeapon.Range);
            }
        }
    }
}
