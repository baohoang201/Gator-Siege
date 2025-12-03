using System;
using UnityEngine;

namespace Gameplay.Controller.Ground {
    public enum GroundType { Land, Bridge, TreeSmall, TreeBig }

    [Serializable]
    public class GroundEntry {
        public GroundType Type = GroundType.Land;
        public GameObject Prefab;
        [Range(0f, 10f)]
        public float Weight = 1f;       // trọng số cơ bản cho prefab này
    }
}