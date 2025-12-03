using UnityEngine;

namespace UI.Component
{
    [ExecuteAlways]
    public class FaceToCamera : MonoBehaviour
    {
        [SerializeField] private bool alwaysFace = false;
        private Camera targetCamera; // Camera mà Canvas sẽ hướng tới

        private void Awake()
        {
            // Nếu không gán camera, lấy Main Camera mặc định
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    enabled = false;
                    return;
                }
            }
        }

        private void Start()
        {
            FaceTo();
        }

        private void OnEnable()
        {
            if (alwaysFace)
            {
                FaceTo();
            }
        }

        private void FaceTo()
        {
            // Cập nhật xoay của Canvas
            transform.rotation = Quaternion.LookRotation(targetCamera.transform.forward, Vector3.up);
        }

        private void Update() {
            FaceTo();
        }
    }
}
