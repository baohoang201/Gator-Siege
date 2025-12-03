using Gameplay.Interface;
using Gameplay.Manager;
using Gameplay.Tool;
using UnityEngine;

namespace Gameplay.Weapon {
    [RequireComponent(typeof(Collider))]
    public class Projectile : MonoBehaviour {
        [System.Serializable]
        public struct Params {
            public int Damage;
            public float Speed;
            public float LifeTime;     // tự hủy sau thời gian (fallback)
            public float Range;        // khoảng bay tối đa (m)
            public LayerMask EnemyLayer;
            public Vector3 Direction;  // hướng world (đã normalize ở caller)
        }

        [SerializeField] GameObject _explosionPrefab;
        private Pool _explosionPool;

        private Params _p;
        private Vector3 _startPos;
        private bool _launched;
        private bool _isDestroy;

        public void Launch(Params p) {
            _p = p;
            _startPos = transform.position;
            _launched = true;
            _isDestroy = false;

            // hướng & vận tốc ban đầu (nếu có Rigidbody thì set velocity)
            transform.rotation = Quaternion.LookRotation(_p.Direction, Vector3.up);
            if (TryGetComponent<Rigidbody>(out var rb)) {
                rb.isKinematic = false;
                rb.linearVelocity = _p.Direction * _p.Speed;
            }
        }

        private void Update() {
            if (!_launched) return;

            // Nếu không có rigidbody → tự translate
            if (!TryGetComponent<Rigidbody>(out var _)) {
                transform.position += _p.Direction * _p.Speed * Time.deltaTime;
            }

            // Hủy khi quá Range
            if (_p.Range > 0f && (transform.position - _startPos).sqrMagnitude >= _p.Range * _p.Range) {
                ObjectPool.Instance.Return(gameObject, true);
            }
        }

        private void OnTriggerEnter(Collider other) {
            if (!_launched || _isDestroy) return;

            // Lọc layer
            if (((1 << other.gameObject.layer) & _p.EnemyLayer) == 0) return;

            if (_explosionPool == null)
                _explosionPool = ObjectPool.Instance.CheckAddPool(_explosionPrefab);

            GameObject explosion = ObjectPool.Instance.Get(_explosionPool);
            Vector3 pos = transform.position;
            pos.y = 4;
            explosion.transform.position = pos;
            explosion.GetComponent<ParticleGroupPlayer>().Play();

            SoundManager.Instance.PlaySoundFX(SoundType.Explosion);

            // Gây sát thương
            if (other.TryGetComponent<IDamageable>(out var dmg)) {
                dmg.TakeDamage(_p.Damage);
                _isDestroy = true;
                ObjectPool.Instance.Return(gameObject, true);
                return;
            }
            // thử parent
            var parent = other.GetComponentInParent<IDamageable>();
            if (parent != null) {
                parent.TakeDamage(_p.Damage);
                _isDestroy = true;
                ObjectPool.Instance.Return(gameObject, true);
            }
        }
    }
}
