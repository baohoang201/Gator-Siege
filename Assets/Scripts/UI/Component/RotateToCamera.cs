using UnityEngine;

namespace UI.Component {
    [ExecuteAlways] 
    public class RotateToCamera : MonoBehaviour {
        private Camera targetCamera; // Camera mà đối tượng sẽ hướng tới

        private void Awake() {
            // Nếu không gán camera, lấy Main Camera mặc định
            if (targetCamera == null) {
                targetCamera = Camera.main;
                if (targetCamera == null) {
                    enabled = false;
                    return;
                }
            }
        }

        private void Start() {
            RotateTo();
        }

        private void RotateTo() {
            // Lấy vị trí của camera và đối tượng
            Vector3 cameraPosition = targetCamera.transform.position;
            Vector3 objectPosition = transform.position;

            // Tính vector từ đối tượng đến camera, chỉ giữ lại thành phần trên mặt phẳng XZ (bỏ qua Y)
            Vector3 direction = cameraPosition - objectPosition;
            direction.y = 0; // Bỏ qua chênh lệch độ cao để chỉ xoay quanh trục Y

            // Nếu direction là vector 0, không cần xoay
            if (direction.sqrMagnitude < 0.0001f) {
                return;
            }

            // Tạo rotation để trục X hướng về camera, chỉ xoay quanh trục Y
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

            // Chỉ lấy góc xoay quanh trục Y, giữ nguyên các trục X và Z
            transform.rotation = Quaternion.Euler(transform.eulerAngles.x, targetRotation.eulerAngles.y, transform.eulerAngles.z);
        }

#if UNITY_EDITOR
        private void Update() {
            // Đảm bảo cập nhật trong Editor để xem trước
            if (targetCamera != null) {
                RotateTo();
            }
        }
#endif
    }
}