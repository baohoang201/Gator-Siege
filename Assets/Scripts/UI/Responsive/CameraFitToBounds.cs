using UnityEngine;

namespace UI.Responsive {
    [RequireComponent(typeof(Camera))]
    public class CameraFitToBounds : MonoBehaviour {
        [SerializeField] private CustomBounds bounds;
        [SerializeField] private bool fitHorizontally = true;
        [SerializeField] private bool fitVertically = true;
        [SerializeField] private float padding = 0.5f;

        private Camera cam;

        private void Awake() {
            cam = GetComponent<Camera>();
        }

        private void Start() {
            FitToBounds();
        }

        public void FitToBounds() {
            if (bounds == null) return;

            Bounds b = bounds.GetWorldBounds();
            if (b.size == Vector3.zero) return;

            Vector3 center = b.center;
            float width = b.size.x + padding * 2f;
            float height = b.size.y + padding * 2f;

            // Nếu camera là orthographic
            if (cam.orthographic) {
                float aspect = cam.aspect;

                float orthoSizeW = width / (2f * aspect);
                float orthoSizeH = height / 2f;
                cam.orthographicSize = fitHorizontally && fitVertically
                    ? Mathf.Max(orthoSizeW, orthoSizeH)
                    : (fitVertically ? orthoSizeH : orthoSizeW);
            }

            transform.position = new Vector3(center.x, center.y, transform.position.z);
        }
    }
}