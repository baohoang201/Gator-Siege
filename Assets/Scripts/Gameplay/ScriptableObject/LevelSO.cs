using Gameplay.Controller.Enemy;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.SO {
    [Serializable]
    public class WaveData {
        public List<int> EnemyCount = new List<int>();
        public List<EnemyType> EnemyTypes = new List<EnemyType>();
        public int CoinReward;
    }

    [CreateAssetMenu(fileName = "LevelSO", menuName = "Data/LevelSO")]
    public class LevelSO : ScriptableObject {
        public int GroundCount;
        public int LandCount;
        public List<WaveData> WaveDatas = new List<WaveData>();
        public int GemReward;
    }
}