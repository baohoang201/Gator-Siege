using UnityEngine;

namespace UI.Responsive {
    public class SpriteBounds : CustomBounds {
        private SpriteRenderer sr;

        private void Awake() {
            sr = GetComponent<SpriteRenderer>();
        }

        public override Bounds GetWorldBounds() {
            return sr.bounds;
        }
    }
}