using Data;
using Gameplay.Controller.Ground;
using Gameplay.Controller.Level;
using Gameplay.Tower;
using UnityEngine;
using UnityEngine.UI;
using Gameplay.Controller.Player;
using Gameplay.Controller;
using System.Collections.Generic;

public class GroundBuildPanel : MonoBehaviour {
    [Header("Wires")]
    [SerializeField] private GroundController _ground;   // gắn chính ground này
    [SerializeField] private GameObject _root;           // panel root
    [SerializeField] private Button _btnRepair;
    [SerializeField] private Button _btnPlace;
    [SerializeField] private Button _btnUpgrade;

    [Header("Tower Levels (low → high)")]
    [Tooltip("Kéo các DefenseTowerSO theo thứ tự level tăng dần (Level1, Level2, Level3, ...)")]
    [SerializeField] private List<DefenseTowerSO> _towerLevels = new List<DefenseTowerSO>();

    private bool _isPlayerHere;

    private void Awake() {
        if (!_ground) _ground = GetComponentInParent<GroundController>();
        Hide();
    }

    private void OnEnable() {
        PlayerController.OnPlayerGroundChanged += OnPlayerGroundChanged;
        LevelGenerator.OnPhaseChanged += OnPhaseChanged;
        GroundController.OnGroundVanished += OnGroundVanished;
        Refresh();
    }

    private void OnDisable() {
        PlayerController.OnPlayerGroundChanged -= OnPlayerGroundChanged;
        LevelGenerator.OnPhaseChanged -= OnPhaseChanged;
        GroundController.OnGroundVanished -= OnGroundVanished;
    }

    private void OnPlayerGroundChanged(GroundController oldG, GroundController newG) {
        _isPlayerHere = (newG == _ground);
        Refresh();
    }

    private void OnPhaseChanged(WavePhase phase, int waveIdx) {
        Refresh();
    }

    private void OnGroundVanished(GroundController g) {
        if (g == _ground) Hide();
    }

    private void Refresh() {
        if (_ground == null || LevelGenerator.Instance == null) {
            Hide();
            return;
        }

        // chỉ show trong Build + player đang ở đúng ô
        if (LevelGenerator.Instance.Phase != WavePhase.Build || !_isPlayerHere) {
            Hide();
            return;
        }

        bool canRepair = _ground.CanRepair();
        bool canPlace = _ground.CanPlaceTower();
        bool canUpgrade = _ground.CanUpgradeTower() && GetNextLevelSO() != null; // chỉ khi còn cấp sau

        // Nếu đang hư hại -> KHÔNG cho đặt/nâng cấp (theo yêu cầu)
        if (canRepair) {
            canPlace = false;
            canUpgrade = false;
        }

        _btnRepair?.gameObject.SetActive(canRepair);
        _btnPlace?.gameObject.SetActive(canPlace);
        _btnUpgrade?.gameObject.SetActive(canUpgrade);

        _btnRepair?.onClick.RemoveAllListeners();
        _btnPlace?.onClick.RemoveAllListeners();
        _btnUpgrade?.onClick.RemoveAllListeners();

        if (canRepair) _btnRepair?.onClick.AddListener(DoRepair);
        if (canPlace) _btnPlace?.onClick.AddListener(DoPlace);
        if (canUpgrade) _btnUpgrade?.onClick.AddListener(DoUpgrade);

        Show();
    }

    // ===== Actions =====

    private void DoRepair() {
        if (!_ground || !_ground.CanRepair()) return;
        int cost = _ground.GetRepairCost();
        if (!GameplayController.Instance.SpendCoin(cost)) return;

        _ground.RepairFull();
        Refresh();
    }

    private void DoPlace() {
        if (!_ground || !_ground.CanPlaceTower()) return;
        var level1 = GetLevel1SO();
        if (!level1) return;

        if (GameplayController.Instance.GetCurrentCoin() < level1.cost) return;

        if (_ground.TryPlaceTower(level1, out var tower)) {
            GameplayController.Instance.SpendCoin(level1.cost);
            Refresh();
        }
    }

    private void DoUpgrade() {
        if (!_ground || !_ground.CanUpgradeTower()) return;

        var next = GetNextLevelSO();
        if (!next) return;

        if (GameplayController.Instance.GetCurrentCoin() < next.cost) return;

        if (_ground.TryUpgradeTower(next)) {
            GameplayController.Instance.SpendCoin(next.cost);
            Refresh();
        }
    }

    // ===== Helpers: cấp tháp =====

    // Level1 là phần tử đầu tiên của danh sách
    private DefenseTowerSO GetLevel1SO() {
        return _towerLevels != null && _towerLevels.Count > 0 ? _towerLevels[0] : null;
    }

    // Tìm cấp hiện tại của tower trên ground trong list, trả về SO hiện tại (hoặc null nếu chưa có)
    private DefenseTowerSO GetCurrentTowerSO() {
        var tower = _ground.GetPlacedTower(); // yêu cầu GroundController có API này; trả DefenseTower hoặc null
        return tower ? tower.CurrentLevelSO : null;         // yêu cầu DefenseTower cung cấp CurrentLevelSO
    }

    // Tìm chỉ số cấp hiện tại trong list
    private int GetCurrentLevelIndex() {
        var cur = GetCurrentTowerSO();
        if (!cur || _towerLevels == null) return -1;
        for (int i = 0; i < _towerLevels.Count; i++)
            if (_towerLevels[i] == cur) return i;
        return -1;
    }

    // Cấp kế tiếp trong list (nếu còn), dùng cho nâng cấp
    private DefenseTowerSO GetNextLevelSO() {
        if (_towerLevels == null || _towerLevels.Count == 0) return null;

        int idx = GetCurrentLevelIndex();
        int nextIdx = idx + 1;
        if (idx < 0) {
            // chưa có tháp → next = level1 (nhưng trường hợp này chỉ dùng cho Place)
            return GetLevel1SO();
        }
        if (nextIdx >= 0 && nextIdx < _towerLevels.Count)
            return _towerLevels[nextIdx];

        return null; // đã max
    }

    private void Show() => _root?.SetActive(true);
    private void Hide() => _root?.SetActive(false);
}
