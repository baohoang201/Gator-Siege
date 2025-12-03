using UnityEngine;
using OcclusionCulling;

namespace OcclusionCulling {
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class OcclusionCullingUIObject : MonoBehaviour {
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;

        private void Awake() {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            OcclusionCullingUIController controller = FindFirstObjectByType<OcclusionCullingUIController>();
            if (controller != null) {
                controller.AddOcclusionCullingObject(this);
            }
        }

        public void SetVisible(bool visible) {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        public Rect GetScreenRect() {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            // Không cần camera ở chế độ Screen Space - Overlay
            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);

            return new Rect(min, max - min);
        }
    }
}
