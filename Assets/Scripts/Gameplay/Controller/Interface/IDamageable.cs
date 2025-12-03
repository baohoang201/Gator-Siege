using UnityEngine;

namespace Gameplay.Interface {
    public interface IDamageable {
        void TakeDamage(int damage);
        void TakeDamage(int damage, Vector3 hitDir, float knockbackForce);
    }
}