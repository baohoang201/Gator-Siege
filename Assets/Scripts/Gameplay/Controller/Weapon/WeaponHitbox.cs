using Gameplay.Interface;
using Gameplay.Manager;
using Gameplay.Tool; // IDamageable
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Weapon {
    [RequireComponent(typeof(Collider))]
    public class WeaponHitbox : MonoBehaviour {
        [Header("Setup")]
        [SerializeField] private LayerMask _enemyLayer;
        [SerializeField] private int _damage = 10;
        [SerializeField] private GameObject _hitPrefab;

        // Runtime
        private bool _active;
        private readonly HashSet<Collider> _hitThisSwing = new();
        private Pool _hitPool;

        // Cho phép WeaponHandler set damage động theo vũ khí hiện tại:
        public void Configure(int damage, LayerMask enemyLayer) {
            _damage = damage;
            _enemyLayer = enemyLayer;
        }

        /// <summary>Gọi ở frame bắt đầu cửa sổ chém (Animation Event)</summary>
        public void BeginSwing() {
            _active = true;
            _hitThisSwing.Clear();
        }

        /// <summary>Gọi ở frame kết thúc cửa sổ chém (Animation Event)</summary>
        public void EndSwing() {
            _active = false;
            _hitThisSwing.Clear();
        }

        private void OnTriggerStay(Collider other) {
            if (!_active) return;

            // Lọc theo layer
            if (((1 << other.gameObject.layer) & _enemyLayer) == 0) return;

            if (_hitThisSwing.Contains(other)) return;
            _hitThisSwing.Add(other);

            if (_hitPool == null)
                _hitPool = ObjectPool.Instance.CheckAddPool(_hitPrefab);

            SoundManager.Instance.PlaySoundFX(SoundType.Punch);

            GameObject hitObj = ObjectPool.Instance.Get(_hitPool);
            hitObj.transform.position = (transform.position + other.transform.position) / 2;
            hitObj.GetComponent<ParticleGroupPlayer>().Play();

            // Gây damage
            if (other.TryGetComponent<IDamageable>(out var dmg)) {
                dmg.TakeDamage(_damage, (other.transform.position - transform.position).normalized, 10f);
            }
            else {
                // Có thể kẻ địch nằm trên child -> tìm ở parent
                var parent = other.GetComponentInParent<IDamageable>();
                parent?.TakeDamage(_damage, (other.transform.position - transform.position).normalized, 10f);
            }
        }
    }
}
