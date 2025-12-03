using UnityEngine;

namespace Gameplay.Tower {
    [CreateAssetMenu(menuName = "DefenseTower/DefenseTowerSO")]
    public class DefenseTowerSO : ScriptableObject {
        [Header("General")]
        public string towerName;
        public GameObject towerPrefab;
        public Sprite towerIcon;
        public int cost;
        public DefenseTowerSO nextUpgrade; // tham chiếu đến cấp nâng cấp tiếp theo, null nếu là cấp cao nhất
        [Header("Attack")]
        public float attackRange;
        public float attackRate;
        public int damage;
    }
}