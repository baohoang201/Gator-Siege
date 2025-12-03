using UnityEngine;

namespace UI.Responsive {
    public abstract class CustomBounds : MonoBehaviour {
        public abstract Bounds GetWorldBounds();
    }
}
