using System.Collections.Generic;
using UnityEngine;

namespace Data {
    [CreateAssetMenu(menuName = "Data/UserDataSO")]
    public class UserDataSO : ScriptableObject {
        [SerializeField] List<LevelData> _levelDatas = new List<LevelData>();
        [SerializeField] List<WeaponData> _weaponDatas = new List<WeaponData>();
        [SerializeField] int _gems = 0;
        [SerializeField] int _currentWeaponIndex = 0;

        public UserData GetUserData() {
            return new UserData(_levelDatas, _weaponDatas, _gems, _currentWeaponIndex, 0.5f, 0.5f);
        }
    }
}