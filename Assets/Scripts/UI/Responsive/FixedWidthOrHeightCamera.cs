using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class FixedWidthOrHeightCamera : MonoBehaviour {
    public enum CameraMode { FixedWidth, FixedHeight }

    [SerializeField] private CameraMode mode = CameraMode.FixedWidth; // Chọn chế độ cố định chiều rộng hoặc chiều cao
    [SerializeField] private float targetWidth = 10f;  // Chiều rộng mong muốn
    [SerializeField] private float targetHeight = 10f; // Chiều cao mong muốn
    [SerializeField] private float distance = 10f;     // Khoảng cách từ camera đến mặt phẳng (dùng cho Perspective)

    private Camera cam;

    private void Awake() {
        cam = GetComponent<Camera>();
        UpdateCameraSize();
    }

    private void UpdateCameraSize() {
        if (cam == null) return;

        float aspect = (float)Screen.width / Screen.height;

        if (cam.orthographic) {
            // Trường hợp Ortho
            if (mode == CameraMode.FixedWidth) {
                cam.orthographicSize = targetWidth / (2f * aspect);
            }
            else // FixedHeight
            {
                cam.orthographicSize = targetHeight / 2f;
            }
        }
        else {
            // Trường hợp Perspective
            if (mode == CameraMode.FixedWidth) {
                float targetHeightCalc = targetWidth / aspect;
                float fovRad = 2f * Mathf.Atan(targetHeightCalc / (2f * distance));
                cam.fieldOfView = fovRad * Mathf.Rad2Deg;
            }
            else // FixedHeight
            {
                float fovRad = 2f * Mathf.Atan(targetHeight / (2f * distance));
                cam.fieldOfView = fovRad * Mathf.Rad2Deg;
            }
        }
    }

#if UNITY_EDITOR
    private void Update() {
        UpdateCameraSize();
    }
#endif
}