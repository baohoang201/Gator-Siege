using UnityEngine;

namespace Gameplay.Weapon {
    public enum WeaponType { Meele, Range }

    [CreateAssetMenu(menuName = "Weapon/WeaponSO")]
    public class WeaponSO : ScriptableObject {
        [Header("General")]
        public WeaponType Type;
        public GameObject WeaponPrefab;
        public GameObject ProjectilePrefab;
        public Sprite WeaponIcon;
        public int Cost;
        [Header("Attack")]
        public int Damage = 10;
        public float Range = 2f;
        public float AttackSpeed = 1f;
        public AnimationClip IdleAnim;
        public AnimationClip MoveAnim;
        public AnimationClip AttackAnim;
    }
}