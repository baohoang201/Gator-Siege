using Gameplay.Controller.Enemy;
using Gameplay.Manager;
using Gameplay.Tool;
using UnityEngine;

namespace Gameplay.Tower {
    public class DefenseTower : MonoBehaviour {
        [Header("Setup")]
        [SerializeField] private Transform _head;        // phần xoay (nòng)
        [SerializeField] private Transform _firePoint;   // điểm bắn đạn
        [SerializeField] private LayerMask _enemyMask;
        [SerializeField] private float _headTurnSpeed = 360f; // deg/sec
        [SerializeField] private float _projectileSpeed = 16f;
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private GameObject _explosionPrefab;

        [Header("Levels")]
        [SerializeField] private DefenseTowerSO _currentLevel; // SO đang dùng

        private float _cooldown;
        private Pool _projectilePool, _explosionPool;

        public bool CanUpgrade => _nextLevel != null;
        private DefenseTowerSO _nextLevel; // set khi nhấn Upgrade

        public void InitWithLevel(DefenseTowerSO so) {
            _currentLevel = so;
            _cooldown = 0f;
            _nextLevel = so.nextUpgrade;
        }

        private void Update() {
            if (_currentLevel == null) return;

            if (_cooldown > 0f) _cooldown -= Time.deltaTime;

            // tìm mục tiêu trong range
            var target = FindTargetInRange(_currentLevel.attackRange);
            if (target != null) {
                // xoay đầu về mục tiêu (chỉ Y)
                Vector3 dir = target.position - _head.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f) {
                    var look = Quaternion.LookRotation(dir.normalized, Vector3.up);
                    look *= Quaternion.Euler(new Vector3(-90, 0, 0)); // offset nếu cần
                    _head.rotation = Quaternion.RotateTowards(_head.rotation, look, _headTurnSpeed * Time.deltaTime);
                }

                // bắn nếu hết CD và đầu đã gần hướng
                float ang = Quaternion.Angle(_head.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up) * Quaternion.Euler(new Vector3(-90, 0, 0)));
                if (_cooldown <= 0f && ang < 5f) {
                    FireAt(target);
                    _cooldown = 1f / Mathf.Max(0.0001f, _currentLevel.attackRate);
                }
            }
        }

        private Transform FindTargetInRange(float range) {
            // nhẹ nhàng: OverlapSphereNonAlloc
            Collider[] hits = new Collider[16];
            int n = Physics.OverlapSphereNonAlloc(transform.position, range, hits, _enemyMask);
            Transform best = null;
            float bestD2 = float.MaxValue;

            for (int i = 0; i < n; i++) {
                var t = hits[i].transform;
                // lấy root có CrocodileAI / IDamageable
                var ai = t.GetComponentInParent<CrocodileAI>();
                if (!ai) continue;
                float d2 = (ai.transform.position - transform.position).sqrMagnitude;
                if (d2 < bestD2) { bestD2 = d2; best = ai.transform; }
            }
            return best;
        }

        private void FireAt(Transform target) {
            if (!_projectilePrefab || !_firePoint) return;

            if (_projectilePool == null) {
                _projectilePool = ObjectPool.Instance.CheckAddPool(_projectilePrefab);
            }

            if (_explosionPool == null) 
                _explosionPool = ObjectPool.Instance.CheckAddPool(_explosionPrefab);

            var explosion = ObjectPool.Instance.Get(_explosionPool);
            explosion.transform.position = _firePoint.position;
            explosion.GetComponent<ParticleGroupPlayer>().Play();

            var projGo = ObjectPool.Instance.Get(_projectilePool);
            projGo.transform.position = _firePoint.position;
            var proj = projGo.GetComponent<Projectile>();
            if (!proj) {
                proj = projGo.AddComponent<Projectile>();
            }
            proj.Launch(target, _projectileSpeed, _currentLevel.damage);

            SoundManager.Instance.PlaySoundFX(SoundType.TowerShoot);
        }

        // Phương thức tĩnh để tạo tháp mới
        public static bool TryUpgrade(DefenseTowerSO nextLevel, Transform anchor, out DefenseTower newTower) {
            newTower = null;
            if (nextLevel == null || nextLevel.towerPrefab == null) return false;

            // Tạo tháp mới tại anchor
            var newTowerGo = Instantiate(nextLevel.towerPrefab, anchor.position, anchor.rotation, anchor);
            newTower = newTowerGo.GetComponent<DefenseTower>();
            if (!newTower) {
                Destroy(newTowerGo);
                return false;
            }

            newTower.InitWithLevel(nextLevel);
            return true;
        }

        public DefenseTowerSO CurrentLevelSO => _currentLevel;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            if (_currentLevel != null) {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, _currentLevel.attackRange);
            }
        }
#endif
    }
}