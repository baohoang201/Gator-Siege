using Data;
using Gameplay.Controller.Ground;
using Gameplay.Controller.Level;
using Gameplay.Manager;
using Gameplay.Weapon;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Controller.Player {
    public class PlayerController : MonoBehaviour {
        [SerializeField] private PlayerVisual _visual;
        [SerializeField] private InputActionReference _moveAction, _attackAction;
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _rotationSpeed = 10f; // Tốc độ xoay (độ/giây)
        [SerializeField] private float _yawOffsetDeg = 45f;
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private WeaponControllerSO _weaponControllerSO;
        [SerializeField] private WeaponHandler _weaponHandler; // Tham chiếu đến WeaponHandler

        [Header("Ground Detect")]
        [SerializeField] private float _groundRayLength = 3f;
        [SerializeField] private float _standYOffset = 0.6f;  // nhảy lên vị trí đứng an toàn

        [SerializeField] private float _groundDistanceFind = 7f;
        [SerializeField] private float _fallLoseDelay = 2f;  // hết ground > 2s thì thua
        [SerializeField] private float _yLoseThreshold = -20f; // rơi quá thấp cũng thua
        private float _noGroundTimer;

        [Header("Attack Settings")]
        [SerializeField] private float _attackDuration = 0.5f; // Thời gian tấn công (giây)

        private float _verticalVelocity;
        private bool _isAttacking; // Trạng thái tấn công

        public static event Action<GroundController, GroundController> OnPlayerGroundChanged;
        private GroundController _currentGround = null;

        private CharacterController _characterController;
        private Vector3 _moveDir;
        private Vector3 _faceDir;

        private bool _canMove = true;
        private bool _canRotate = true;

        private void Awake() {
            _characterController = GetComponent<CharacterController>();
        }

        private void Start() {
            UserModel user = DataManager.Instance.GetUserModel();
            if (user != null) {
                _weaponHandler.SetWeapon(_weaponControllerSO.GetWeapon(user.GetCurrentWeaponIndex()));
            }
        }

        private void MoveToSafe() {
            GroundController fallback = FindNearestAliveGround(transform.position, null, float.MaxValue);

            if (fallback != null) {
                // Teleport (nhanh gọn); nếu muốn mượt hơn có thể tween/nhảy
                Vector3 target = fallback.GetStandPoint(_standYOffset);

                // Nếu dùng CharacterController: tránh set y đột ngột -> tắt bật nhanh
                bool hadCC = TryGetComponent<CharacterController>(out var cc) && cc.enabled;
                if (hadCC) cc.enabled = false;

                transform.position = target;
                _currentGround = fallback; // cập nhật tham chiếu

                if (hadCC) cc.enabled = true;
            }
        }

        private void Update() {
            Vector2 input = _moveAction.action.ReadValue<Vector2>();
            Vector3 raw = new Vector3(input.x, 0, input.y).normalized;

            // Quay input theo góc bù quanh trục Y
            Quaternion yaw = Quaternion.Euler(0f, _yawOffsetDeg, 0f);
            _moveDir = yaw * raw;

            if (_moveDir.sqrMagnitude > 0.0001f && _canRotate && _faceDir != _moveDir.normalized) {
                Rotate(_moveDir);
                _visual.SetMoving(true);
            }
            else if (_moveDir == Vector3.zero) {
                _visual.SetMoving(false);
            }

            UpdateCurrentGroundUnderFeet();
        }

        private void FixedUpdate() {
            // Cập nhật vertical
            _characterController.SimpleMove(Vector3.zero);
            if (!_characterController.isGrounded) return;

            // Tính chuyển động ngang
            Vector3 horizontal = Vector3.zero;
            if (_moveDir != Vector3.zero && CanMove()) {
                horizontal = _moveSpeed * _moveDir;
            }

            // Gộp lại và Move 1 lần
            Vector3 velocity = horizontal;
            velocity.y = _verticalVelocity;

            _characterController.Move(velocity * Time.fixedDeltaTime);
        }

        private bool CanMove() {
            if (!_canMove || _isAttacking) return false; // Ngăn di chuyển khi đang tấn công

            Vector3 origin = transform.position + Vector3.up;
            Vector3 checkDir1 = new Vector3(_moveDir.x == 0 ? 0 : Mathf.Sign(_moveDir.x), 0, 0);
            Vector3 checkDir2 = new Vector3(0, 0, _moveDir.z == 0 ? 0 : Mathf.Sign(_moveDir.z));

            bool hasGround1 = HasGround(origin, (transform.position + checkDir1 * _characterController.radius / 2) - origin) && checkDir1 != Vector3.zero;
            bool hasGround2 = HasGround(origin, (transform.position + checkDir2 * _characterController.radius / 2) - origin) && checkDir2 != Vector3.zero;

            if (!hasGround1 && !hasGround2) {
                return false;
            }
            else {
                if (hasGround1 && !hasGround2) {
                    _moveDir = new Vector3(_moveDir.x, 0, 0);
                    Rotate(_moveDir);
                }
                else if (hasGround2 && !hasGround1) {
                    _moveDir = new Vector3(0, 0, _moveDir.z);
                    Rotate(_moveDir);
                }
                return true;
            }
        }

        private void UpdateCurrentGroundUnderFeet() {
            Vector3 origin = transform.position + Vector3.up * 1.0f;
            GroundController old = _currentGround;

            if (Physics.Raycast(origin, Vector3.down, out var hit, _groundRayLength, _groundLayer))
                _currentGround = hit.collider.GetComponentInParent<GroundController>();
            else
                _currentGround = null;

            if (_currentGround == null) _noGroundTimer += Time.deltaTime;
            else _noGroundTimer = 0f;

            // điều kiện thua do rơi
            if (_noGroundTimer >= _fallLoseDelay || transform.position.y <= _yLoseThreshold) {
                GameplayController.Instance.Finish(false);
                enabled = false; // khoá player nếu muốn
                return;
            }

            if (old != _currentGround || old == null)
                OnPlayerGroundChanged?.Invoke(old, _currentGround);
        }

        private void OnGroundVanishedHandler(GroundController vanished) {
            // Chỉ xử lý nếu player ĐANG đứng trên chính ground vừa bị phá
            if (_currentGround == null || vanished != _currentGround) return;

            // Tìm ground gần nhất còn sống (loại trừ ground vừa vanish)
            GroundController fallback = FindNearestAliveGround(transform.position, vanished, _groundDistanceFind);

            if (fallback != null) {
                // Teleport (nhanh gọn); nếu muốn mượt hơn có thể tween/nhảy
                Vector3 target = fallback.GetStandPoint(_standYOffset);

                // Nếu dùng CharacterController: tránh set y đột ngột -> tắt bật nhanh
                bool hadCC = TryGetComponent<CharacterController>(out var cc) && cc.enabled;
                if (hadCC) cc.enabled = false;

                transform.position = target;
                _currentGround = fallback; // cập nhật tham chiếu

                if (hadCC) cc.enabled = true;
            }
            else {
                // Không có ground nào còn sống xung quanh -> để rơi theo logic cũ (không can thiệp)
            }
        }

        private GroundController FindNearestAliveGround(Vector3 from, GroundController except, float maxDistance) {
            GroundController best = null;
            float bestDist = float.MaxValue;

            // Duyệt danh sách ground, chọn cái IsAlive và khác "except"
            foreach (var g in GroundController.Alive) {
                if (g == null || g == except || !g.IsAlive) continue;
                float d = (g.Center - from).magnitude;
                if (d < bestDist && d < maxDistance) {
                    bestDist = d;
                    best = g;
                }
            }
            return best;
        }

        private bool HasGround(Vector3 origin, Vector3 dir) {
            return Physics.Raycast(origin, dir, 10, _groundLayer);
        }

        private void Rotate(Vector3 dir) {
            if (dir == Vector3.zero) return;

            _faceDir = dir.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(_faceDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }

        private void Attack_performed(InputAction.CallbackContext obj) {
            if (_weaponHandler != null && !_isAttacking) {
                _isAttacking = true;
                _weaponHandler.Attack(_faceDir);
                StartCoroutine(AttackCooldown());
            }
        }

        private IEnumerator AttackCooldown() {
            yield return new WaitForSeconds(_attackDuration);
            _isAttacking = false;
        }

        private void OnEnable() {
            _moveAction.action.Enable();
            _attackAction.action.Enable();
            _attackAction.action.performed += Attack_performed;

            GroundController.OnGroundVanished += OnGroundVanishedHandler;
            LevelGenerator.OnLevelReady += MoveToSafe;
        }

        private void OnDisable() {
            _attackAction.action.performed -= Attack_performed;
            _moveAction.action.Disable();
            _attackAction.action.Disable();

            GroundController.OnGroundVanished -= OnGroundVanishedHandler;
            LevelGenerator.OnLevelReady -= MoveToSafe;
        }
    }
}