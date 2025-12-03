using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Weapon {
    [CreateAssetMenu(menuName = "Weapon/WeaponControllerSO")]
    public class WeaponControllerSO : ScriptableObject {
        [SerializeField] List<WeaponSO> _weapons = new List<WeaponSO>();

        public WeaponSO GetWeapon(int index) {
            return (index >= 0 && index < _weapons.Count) ? _weapons[index] : null;
        }
    }
}