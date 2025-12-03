using Data;
using Gameplay.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Component.Shop {
    public class UIShopItem : MonoBehaviour {
        [Header("References")]
        [SerializeField] Image _icon;
        [SerializeField] TMP_Text _priceText;
        [SerializeField] Image buyImage;
        [SerializeField] Button _buyButton;

        [Header("Assets")]
        [SerializeField] Sprite _buySprite;
        [SerializeField] Sprite _selectSprite;
        [SerializeField] Sprite _unselectSprite;
        [SerializeField] Color _canBuyColor;
        [SerializeField] Color _cannotBuyColor;

        private int _itemIndex = -1;
        private int _price = 0;
        private bool _isBought = false;
        private bool _isSelected = false;
        private UserModel _userModel;

        private void Start() {
            _userModel = DataManager.Instance.GetUserModel();
        }

        public void A() {

        }

        public void SetData(int itemIndex, Sprite icon, int price, bool isBought, bool isSelected) {
            _itemIndex = itemIndex;
            _icon.sprite = icon;
            _price = price;
            _priceText.text = isBought ? "Owned" : price.ToString();

            UpdateVisual(isBought, isSelected);
        }

        public void UpdateVisual(bool isBought, bool isSelected) {
            _isBought = isBought;
            _isSelected = isSelected;

            if (_userModel == null)
                _userModel = DataManager.Instance.GetUserModel();

            if (isBought) {
                buyImage.sprite = isSelected ? _selectSprite : _unselectSprite;
                buyImage.color = _canBuyColor;
                _buyButton.interactable = true;
            }
            else {
                buyImage.sprite = _buySprite;
                if (_userModel.GetGems() >= _price) {
                    buyImage.color = _canBuyColor;
                    _buyButton.interactable = true;
                }
                else {
                    buyImage.color = _cannotBuyColor;
                    _buyButton.interactable = false;
                }
            }
        }

        private void Clicked() {
            if (_isBought) {
                if (_isSelected) return;

                _userModel.SetCurrentWeaponIndex(_itemIndex);

            } else {
                if (_userModel.SpendGems(_price)) {
                    _userModel.UnlockWeapon(_itemIndex);
                    _userModel.SetCurrentWeaponIndex(_itemIndex);
                }
            }
        }

        private void OnEnable() {
            _buyButton.onClick.AddListener(Clicked);
        }

        private void OnDisable() {
            _buyButton.onClick.RemoveListener(Clicked);
        }
    }
}