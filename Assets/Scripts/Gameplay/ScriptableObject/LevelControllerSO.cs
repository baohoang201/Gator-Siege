using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.SO {
    [CreateAssetMenu(fileName = "LevelControllerSO", menuName = "Data/LevelControllerSO")]
    public class LevelControllerSO : ScriptableObject {
        [SerializeField] List<LevelSO> _levels = new List<LevelSO>();

        public LevelSO GetLevel(int level) {
            if (level < 0 || level >= _levels.Count) {
                return null;
            }
            return _levels[level];
        }
    }
}
