using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OcclusionCulling {
    public class OcclusionCullingUIController : MonoBehaviour {
        [SerializeField] private List<OcclusionCullingUIObject> uiObjects = new List<OcclusionCullingUIObject>();

        private Camera uiCamera;
        private Rect screenRect;

        private void Awake() {
            Canvas canvas = GetComponentInParent<Canvas>();
            uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        private void Update() {
            UpdateScreenRect();

            foreach (var uiObj in uiObjects) {
                Rect objRect = uiObj.GetScreenRect();
                bool isVisible = screenRect.Overlaps(objRect, true);

                uiObj.SetVisible(isVisible);
            }
        }

        public void AddOcclusionCullingObject(OcclusionCullingUIObject uiObj) {
            uiObjects.Add(uiObj);
        }

        private void UpdateScreenRect() {
            screenRect = new Rect(Vector2.zero, new Vector2(Screen.width, Screen.height));
        }
    }
}
