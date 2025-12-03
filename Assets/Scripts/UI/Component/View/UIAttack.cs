namespace UI.Component.Game_View {
    using Gameplay.Weapon;
    using UnityEngine;
    using UnityEngine.UI;

    public class UIAttack : MonoBehaviour {
        [SerializeField] private Image fillImage;

        private void OnEnable() {
            WeaponHandler.OnCooldownTick += HandleTick;
        }
        private void OnDisable() {
            WeaponHandler.OnCooldownTick -= HandleTick;
        }

        private void HandleTick(float remain, float total) {
            fillImage.fillAmount = total <= 0f ? 0f : remain / total;
        }
    }
}