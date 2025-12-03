using Gameplay.Weapon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Data {
    [Serializable]
    public class LevelData {
        public bool IsUnlocked = false;
        public bool IsCompleted = false;
    }

    [Serializable]
    public class WeaponData {
        public WeaponType WeaponType;
        public bool IsUnlocked = false;
    }

    [Serializable]
    public class UserData : SerializableData {
        public List<LevelData> LevelDatas = new List<LevelData>();
        public List<WeaponData> WeaponDatas = new List<WeaponData>();
        public int CurrentWeaponIndex = 0;
        public int Gems = 0;
        public float MusicValue = 1.0f;
        public float SoundFXValue = 1.0f;

        public UserData() { }

        public UserData(List<LevelData> levelDatas, List<WeaponData> weaponDatas, int gems, int currentWeaponIndex, float musicValue, float soundFXValue) {
            LevelDatas = levelDatas.Select(levelData => new LevelData {
                IsUnlocked = levelData.IsUnlocked,
                IsCompleted = levelData.IsCompleted
            }).ToList();

            WeaponDatas = weaponDatas.Select(weaponData => new WeaponData {
                WeaponType = weaponData.WeaponType,
                IsUnlocked = weaponData.IsUnlocked
            }).ToList();

            Gems = gems;
            CurrentWeaponIndex = currentWeaponIndex;
            MusicValue = musicValue;
            SoundFXValue = soundFXValue;
        }
    }
}