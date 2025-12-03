using UnityEngine;

namespace Gameplay.Controller.Enemy {
    public enum EnemyType { Small, Medium, Large }

    [CreateAssetMenu(menuName = "Enemy/EnemySO")]
    public class EnemySO : ScriptableObject {
        public EnemyType Type;
        public int MaxHealth;
        public float MoveSpeed;
        public int AttackDamage;
        public float AttackRange;
        public float AttackCooldown;
        public int CoinReward;
        public GameObject EnemyPrefab;
    }
}