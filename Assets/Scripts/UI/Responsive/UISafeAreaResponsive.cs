using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UI.Responsive {
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class UISafeAreaResponsive : MonoBehaviour {
        [Header("Safe Area Settings")]
        [SerializeField] private RectTransform rectTransform;

        [Header("Horizontal")]
        [SerializeField] private bool enableHorizontal = false;
        [SerializeField] private float minSafeSideMargin;

        [Header("Vertical")]
        [SerializeField] private bool enableVertical = false;
        [SerializeField] private float minSafeTopBottomMargin;

        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;

        private void Awake() {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
        }

        private void Start() {
            ApplySafeArea();
        }

        private void Update() {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                if (Screen.safeArea != _lastSafeArea ||
                    Screen.width != _lastScreenSize.x ||
                    Screen.height != _lastScreenSize.y) {
                    ApplySafeArea();
                }
            }
#endif
        }

#if UNITY_EDITOR
        private void OnEnable() {
            if (!Application.isPlaying)
                EditorApplication.update += EditorSafeAreaWatcher;
        }

        private void OnDisable() {
            if (!Application.isPlaying)
                EditorApplication.update -= EditorSafeAreaWatcher;
        }

        private void EditorSafeAreaWatcher() {
            if (this == null || rectTransform == null) return;

            if (Screen.safeArea != _lastSafeArea ||
                Screen.width != _lastScreenSize.x ||
                Screen.height != _lastScreenSize.y) {
                ApplySafeArea();
            }
        }

        private void OnValidate() {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) {
                if (rectTransform == null)
                    rectTransform = GetComponent<RectTransform>();

                // Delay để tránh lỗi SetAnchor trong OnValidate
                EditorApplication.delayCall += () => {
                    if (this != null && rectTransform != null)
                        ApplySafeArea();
                };
            }
        }
#endif

        private void ApplySafeArea() {
            if (rectTransform == null) return;

            Rect safeRect = Screen.safeArea;

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            float left = safeRect.xMin;
            float right = screenWidth - safeRect.xMax;
            float bottom = safeRect.yMin;
            float top = screenHeight - safeRect.yMax;

            // ⬅ Horizontal
            if (enableHorizontal) {
                if (left < minSafeSideMargin)
                    safeRect.xMin = minSafeSideMargin;

                if (right < minSafeSideMargin)
                    safeRect.xMax = screenWidth - minSafeSideMargin;
            }
            else {
                safeRect.xMin = 0;
                safeRect.xMax = screenWidth;
            }

            // ⬆ Vertical
            if (enableVertical) {
                if (bottom < minSafeTopBottomMargin)
                    safeRect.yMin = minSafeTopBottomMargin;

                if (top < minSafeTopBottomMargin)
                    safeRect.yMax = screenHeight - minSafeTopBottomMargin;
            }
            else {
                safeRect.yMin = 0;
                safeRect.yMax = screenHeight;
            }

            // Convert to normalized anchors
            Vector2 anchorMin = new Vector2(safeRect.xMin / screenWidth, safeRect.yMin / screenHeight);
            Vector2 anchorMax = new Vector2(safeRect.xMax / screenWidth, safeRect.yMax / screenHeight);

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            _lastSafeArea = Screen.safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }
    }
}