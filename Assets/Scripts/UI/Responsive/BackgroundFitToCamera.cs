using UnityEngine;

namespace UI.Responsive {
    [RequireComponent(typeof(SpriteRenderer))]
    public class BackgroundFitToCamera : MonoBehaviour {
        private void Start() {
            FitToCamera();
        }

        private void FitToCamera() {
            Camera cam = Camera.main;
            SpriteRenderer sr = GetComponent<SpriteRenderer>();

            if (cam == null || sr.sprite == null) return;

            float camHeight = 2f * cam.orthographicSize;
            float camWidth = camHeight * cam.aspect;

            float spriteWidth = sr.sprite.bounds.size.x;
            float spriteHeight = sr.sprite.bounds.size.y;

            float scaleFactor = Mathf.Max(camWidth / spriteWidth, camHeight / spriteHeight);

            transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
        }
    }
}
