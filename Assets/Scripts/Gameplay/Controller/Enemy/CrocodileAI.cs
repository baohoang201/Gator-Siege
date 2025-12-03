using Gameplay.Controller.Ground;
using Gameplay.Interface;
using Gameplay.Manager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Added for Image component

namespace Gameplay.Controller.Enemy {
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class CrocodileAI : MonoBehaviour, IDamageable {
        [Header("Data")]
        [SerializeField] private EnemySO _enemySO;

        [Header("Runtime (read only)")]
        [SerializeField] private int _hp;
        [SerializeField] private float _cooldownTimer;

        [Header("Movement (kinematic)")]
        [SerializeField] private float _stopDistance = 0.1f;     // khoảng dừng rất nhỏ để tránh rung
        [SerializeField] private float _turnSpeed = 540f;        // deg/sec, xoay mặt về mục tiêu

        [Tooltip("Hệ số chuyển force -> quãng đường knockback (mét = force * coef)")]
        [SerializeField] private float _knockbackDistancePerForce = 0.15f;

        [Header("Knockback")]
        [SerializeField] private bool _hasStunned = true;
        [SerializeField] private float _knockbackStunTime = 0.15f;  // bị khựng trong thời gian ngắn khi dính đòn
        [SerializeField] private AnimationCurve _knockbackCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("Health Bar")]
        [SerializeField] private Image _healthBarImage; // Reference to UI Image for health bar
        [SerializeField] private GameObject _uiHealthBar; // Parent GameObject for health bar

        // ==== Registry để duyệt láng giềng nhanh ====
        public static readonly List<CrocodileAI> Alive = new List<CrocodileAI>();

        public event System.Action<CrocodileAI> OnDied;

        [Header("Separation")]
        [SerializeField] private float _separationRadius = 1.2f;     // bán kính “không gian riêng”
        [SerializeField] private float _separationWeight = 2.0f;     // độ mạnh lực tách (m/s)
        [SerializeField] private float _separationMaxBoost = 1.5f;   // giới hạn tăng tốc do separation
        [SerializeField] private float _horizontalSpeedCap = 6.0f;   // trần tốc ngang tổng

        private Rigidbody _rb;
        private GroundController _target;        // ground mục tiêu hiện tại
        private bool _stunned;                   // đang bị knockback (khóa AI move)
        private Vector3 _wantedVelocity;         // vận tốc mong muốn (XZ) cho FixedUpdate
        private Vector3 _knockbackRemaining;     // phần dịch chuyển knockback còn lại (XZ)

        // Cache stats
        private float MoveSpeed => _enemySO ? _enemySO.MoveSpeed : 2.5f;
        private int AttackDmg => _enemySO ? _enemySO.AttackDamage : 5;
        private float AttackRange => _enemySO ? _enemySO.AttackRange : 1.2f;
        private float AttackCD => _enemySO ? _enemySO.AttackCooldown : 1.0f;
        private int MaxHealth => _enemySO ? _enemySO.MaxHealth : 30; // Added for health bar calculation

        [SerializeField] private Animator _anim;    // drag từ prefab

        // Animator parameter hashes (đừng đổi tên khi set trong Animator)
        static readonly int hSpeed = Animator.StringToHash("Speed");   // float
        static readonly int hAttack = Animator.StringToHash("Attack");  // trigger
        static readonly int hHit = Animator.StringToHash("Hit");     // trigger
        static readonly int hDie = Animator.StringToHash("Die");     // trigger
        static readonly int hStunned = Animator.StringToHash("Stunned"); // bool

        // Cache mục tiêu khi bắt đầu attack để Animation Event đánh trúng đúng target
        private GroundController _attackTargetCache;

        private void Awake() {
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = true;                        // KINEMATIC!
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // không lật bởi physics

            if (_enemySO == null)
                Debug.LogWarning($"{name}: EnemySO is null. Dùng giá trị mặc định.");

            _hp = _enemySO ? _enemySO.MaxHealth : 30;

            // Ensure health bar is set up
            if (_healthBarImage != null) {
                _healthBarImage.fillAmount = 1f; // Initialize full health
            }
            else {
                Debug.LogWarning($"{name}: Health bar Image not assigned!");
            }
        }

        private void OnEnable() {
            if (!Alive.Contains(this)) Alive.Add(this);
            GroundController.OnGroundVanished += OnGroundVanished;
            PickNearestGround();
        }

        private void OnDisable() {
            Alive.Remove(this);
            GroundController.OnGroundVanished -= OnGroundVanished;
            // Hide health bar when disabled
            if (_healthBarImage != null) {
                _healthBarImage.gameObject.SetActive(false);
            }
        }

        private void Update() {
            if (_hp <= 0) {
                // Hide health bar when dead
                if (_healthBarImage != null) {
                    _healthBarImage.gameObject.SetActive(false);
                }
                return;
            }

            // cooldown
            if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;

            // nếu không có mục tiêu, tìm lại
            if (_target == null || !_target.IsAlive)
                PickNearestGround();

            if (!_stunned) {
                Vector3 sep = ComputeSeparation();
                if (sep.sqrMagnitude > 0.0001f) {
                    // boost giới hạn
                    Vector3 boost = sep.normalized * Mathf.Min(sep.magnitude * _separationWeight, _separationMaxBoost);
                    _wantedVelocity += new Vector3(boost.x, 0f, boost.z);

                    // trần tốc tổng cho ổn định
                    Vector3 horiz = new Vector3(_wantedVelocity.x, 0f, _wantedVelocity.z);
                    float mag = horiz.magnitude;
                    if (mag > _horizontalSpeedCap) {
                        horiz = horiz / mag * _horizontalSpeedCap;
                        _wantedVelocity = new Vector3(horiz.x, _wantedVelocity.y, horiz.z);
                    }
                }
            }

            // Ưu tiên DI CHUYỂN; nếu đủ tầm + hết CD thì tấn công
            if (_target != null) {
                float dist = Vector3.Distance(transform.position, _target.Center);

                // luôn quay mặt về mục tiêu
                FaceTowards(_target.Center);

                if (dist > AttackRange) // chưa đủ tầm -> tiến lên
                {
                    Vector3 dir = (_target.Center - transform.position);
                    dir.y = 0f;
                    if (dir.sqrMagnitude <= _stopDistance * _stopDistance)
                        _wantedVelocity = Vector3.zero;
                    else
                        _wantedVelocity = dir.normalized * MoveSpeed;
                }
                else {
                    if (_cooldownTimer <= 0f) {
                        BeginAttack(_target);
                        _cooldownTimer = AttackCD;
                    }
                    _wantedVelocity = Vector3.zero;
                }
            }
            else {
                _wantedVelocity = Vector3.zero;
            }

            float horizSpeed = new Vector3(_wantedVelocity.x, 0f, _wantedVelocity.z).magnitude;
            if (_anim) {
                _anim.SetFloat(hSpeed, horizSpeed); // có thể thêm damping nếu muốn mượt
            }
        }

        private Vector3 ComputeSeparation() {
            Vector3 push = Vector3.zero;
            Vector3 myPos = transform.position;

            foreach (var other in Alive) {
                if (other == null || other == this) continue;
                Vector3 diff = myPos - other.transform.position;
                diff.y = 0f;
                float d2 = diff.sqrMagnitude;

                if (d2 < _separationRadius * _separationRadius && d2 > 0.0001f) {
                    float d = Mathf.Sqrt(d2);
                    // trọng số ngược khoảng cách, mạnh khi rất gần
                    float w = 1f - Mathf.Clamp01(d / _separationRadius);
                    push += (diff / d) * w;
                }
            }
            return push; // hướng đẩy ra
        }

        private void FixedUpdate() {
            if (_hp <= 0) return;

            Vector3 delta = Vector3.zero;

            // Di chuyển chính (XZ)
            if (!_stunned && _wantedVelocity.sqrMagnitude > 0.0001f)
                delta += _wantedVelocity * Time.fixedDeltaTime;

            // Knockback còn lại (nếu có) sẽ được áp trong coroutine,
            // ở đây chỉ cộng thêm nếu knockbackRemaining được cập nhật theo thời gian.
            if (_knockbackRemaining.sqrMagnitude > 0.000001f) {
                // _knockbackRemaining được giảm dần trong coroutine,
                // FixedUpdate chỉ "tiêu" nó dần qua MovePosition.
                delta += _knockbackRemaining;
                _knockbackRemaining = Vector3.zero; // đã áp frame này
            }

            if (delta.sqrMagnitude > 0f)
                _rb.MovePosition(_rb.position + new Vector3(delta.x, 0f, delta.z)); // kinematic move
        }

        private void FaceTowards(Vector3 worldTarget) {
            Vector3 dir = worldTarget - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;

            Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, _turnSpeed * Time.deltaTime);
        }

        private void BeginAttack(GroundController g) {
            if (g == null || !g.IsAlive) return;

            // cache mục tiêu tại thời điểm ra đòn
            _attackTargetCache = g;

            // phát trigger animation
            if (_anim) _anim.SetTrigger(hAttack);
            // Lưu ý: damage sẽ được gọi bởi Animation Event ANIM_AttackHit()
        }

        // Hàm này sẽ được Animation Event gọi đúng frame chạm
        public void ANIM_AttackHit() {
            if (_attackTargetCache != null && _attackTargetCache.IsAlive) {
                _attackTargetCache.TakeDamage(AttackDmg);
            }
            SoundManager.Instance.PlaySoundFX(SoundType.Bite);
        }

        private void PickNearestGround() {
            _target = null;
            float best = float.MaxValue;

            foreach (var g in GroundController.Alive) {
                if (g == null || !g.IsAlive) continue;
                float d = (g.Center - transform.position).sqrMagnitude;
                if (d < best) {
                    best = d;
                    _target = g;
                }
            }
        }

        private void OnGroundVanished(GroundController g) {
            if (g == _target) PickNearestGround();
        }

        // ----------------- Damage & Knockback (KINEMATIC) -----------------
        public void TakeDamage(int damage) {
            TakeDamage(damage, Vector3.zero, 0f);
        }

        public void TakeDamage(int damage, Vector3 hitDir, float knockbackForce) {
            if (_hp <= 0) return;

            _hp = Mathf.Max(0, _hp - Mathf.Max(0, damage));

            // Cập nhật thanh máu
            if (_healthBarImage != null) {
                _healthBarImage.fillAmount = (float)_hp / MaxHealth;
                if (_uiHealthBar != null) {
                    _uiHealthBar.SetActive(_hp > 0);
                }
            }

            SoundManager.Instance.PlaySoundFX(SoundType.Stream);

            if (_hp == 0) {
                StartCoroutine(DieRoutine());
                GameplayController.Instance.AddCoin(_enemySO.CoinReward);
                return;
            }

            // Gián đoạn animation tấn công
            if (_anim != null && _hasStunned) {
                _anim.ResetTrigger(hAttack); // Reset trigger tấn công để dừng animation
                _anim.SetTrigger(hHit); // Chạy animation bị đánh
                _attackTargetCache = null; // Xóa mục tiêu tấn công để ngăn ANIM_AttackHit gây sát thương
            }

            if (knockbackForce > 0.01f && hitDir.sqrMagnitude > 0.0001f)
                StartCoroutine(KnockbackKinematic(hitDir.normalized, knockbackForce, _knockbackStunTime));
        }

        private IEnumerator KnockbackKinematic(Vector3 dir, float force, float stunTime) {
            _stunned = true;

            float totalDist = Mathf.Max(0f, force) * _knockbackDistancePerForce;
            float t = 0f;

            while (t < stunTime) {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, stunTime));
                float stepDist = totalDist * _knockbackCurve.Evaluate(k) * Time.deltaTime / stunTime;

                Vector3 delta = dir * stepDist;

                _knockbackRemaining += new Vector3(delta.x, 0f, delta.z);
                yield return null;
            }

            _stunned = false;
        }

        private IEnumerator DieRoutine() {
            // báo wave manager ngay khi chết (tuỳ bạn muốn lúc nào)
            OnDied?.Invoke(this);

            if (_anim) _anim.SetTrigger(hDie);

            float wait = 1.5f;
            yield return new WaitForSeconds(wait);

            Destroy(gameObject);
        }
    }
}