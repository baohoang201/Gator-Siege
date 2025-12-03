using Data;
using Gameplay.Manager;
using Gameplay.Weapon;
using System.Collections.Generic;
using TMPro;
using UI.Event;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Component.Shop {
    public class UIShopController : MonoBehaviour {
        [Header("References")]
        [SerializeField] Transform _content;
        [SerializeField] UIShopItem _itemPrefab;
        [SerializeField] Button _nextPageButton;
        [SerializeField] Button _prevPageButton;
        [SerializeField] TMP_Text _gemText;
 
        [Header("Assets")]
        [SerializeField] WeaponControllerSO _weaponControllerSO;

        [Header("Settings")]
        [SerializeField] int _itemsPerPage = 3;

        private UserModel _userModel;
        private List<UIShopItem> _shopItems = new List<UIShopItem>();

        private int _totalPages = 0;
        private int _currentPageIndex = 0;

        private void Start() {
            _userModel = DataManager.Instance.GetUserModel();
            LoadShopItems();

            _currentPageIndex = 0;
            LoadShopData();
            UpdatePageButtons();
            UpdateGems();
        }

        private void LoadShopItems() {
            var weaponDatas = _userModel.GetWeaponDatas();

            _totalPages = Mathf.CeilToInt((float)weaponDatas.Count / _itemsPerPage);

            for (int i = 0; i < _itemsPerPage; i++) {
                var item = Instantiate(_itemPrefab, _content);
                var uiItem = item.GetComponent<UIShopItem>();
                _shopItems.Add(uiItem);
            }
        }

        private void LoadShopData() {
            for (int i = 0; i < _itemsPerPage; i++) {
                int dataIndex = _currentPageIndex * _itemsPerPage + i;
                if (dataIndex >= _userModel.GetWeaponDatas().Count) {
                    _shopItems[i].gameObject.SetActive(false);
                    continue;
                }
                var weaponData = _userModel.GetWeaponDatas()[dataIndex];
                var weaponSO = _weaponControllerSO.GetWeapon(dataIndex);
                if (weaponSO == null) {
                    _shopItems[i].gameObject.SetActive(false);
                    continue;
                }
                _shopItems[i].gameObject.SetActive(true);
                _shopItems[i].SetData(dataIndex, weaponSO.WeaponIcon, weaponSO.Cost, weaponData.IsUnlocked, dataIndex == _userModel.GetCurrentWeaponIndex());
            }
        }

        private void UpdateGems() {
            _gemText.text = _userModel.GetGems().ToString();
        }

        private void OnSelectWeapon(int weaponIndex) {
            for (int i = 0; i < _shopItems.Count; i++) {
                int dataIndex = _currentPageIndex * _itemsPerPage + i;
                var item = _shopItems[i];
                item.UpdateVisual(_userModel.GetWeaponDatas()[dataIndex].IsUnlocked, dataIndex == weaponIndex);
            }
        }

        private void UpdatePageButtons() {
            _prevPageButton.gameObject.SetActive(_currentPageIndex > 0);
            _nextPageButton.gameObject.SetActive(_currentPageIndex < _totalPages - 1);
        }

        private void NextPage() {
            if (_currentPageIndex < _totalPages - 1) {
                _currentPageIndex++;
                LoadShopData();
                UpdatePageButtons();
            }
        }

        private void PrevPage() {
            if (_currentPageIndex > 0) {
                _currentPageIndex--;
                LoadShopData();
                UpdatePageButtons();
            }
        }

        private void OnEnable() {
            UIEvent.OnSelectWeaponEvent += OnSelectWeapon;
            UIEvent.OnUpdateGemsEvent += UpdateGems;
            _nextPageButton.onClick.AddListener(NextPage);
            _prevPageButton.onClick.AddListener(PrevPage);
        }

        private void OnDisable() {
            UIEvent.OnSelectWeaponEvent -= OnSelectWeapon;
            UIEvent.OnUpdateGemsEvent -= UpdateGems;
            _nextPageButton.onClick.RemoveListener(NextPage);
            _prevPageButton.onClick.RemoveListener(PrevPage);
        }
    }
}