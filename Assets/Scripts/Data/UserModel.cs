using Gameplay.Manager;
using System.Collections.Generic;
using UI.Event;

namespace Data {
    public class UserModel {
        private readonly UserData _data;
        public static string DataKey = "UserData";

        private int _currentLevelIndex = 0;

        public UserModel(UserData data) {
            _data = data;
        }

        public List<LevelData> GetLevelDatas() {
            return _data.LevelDatas;
        }

        public void SetCurrentLevelIndex(int levelIndex) {
            if (levelIndex < 0 || levelIndex >= _data.LevelDatas.Count)
                return;
            _currentLevelIndex = levelIndex;
        }

        public int GetCurrentLevelIndex() {
            return _currentLevelIndex;
        }

        public void CompleteLevel() {
            if (_currentLevelIndex < 0 || _currentLevelIndex >= _data.LevelDatas.Count)
                return;
            _data.LevelDatas[_currentLevelIndex].IsCompleted = true;
            if (_currentLevelIndex + 1 < _data.LevelDatas.Count) {
                _data.LevelDatas[_currentLevelIndex + 1].IsUnlocked = true;
            }
        }

        public bool TrySetNextLevel() {
            if (_currentLevelIndex + 1 < _data.LevelDatas.Count) {
                _currentLevelIndex++;
                return true;
            }
            return false;
        }

        public List<WeaponData> GetWeaponDatas() {
            return _data.WeaponDatas;
        }

        public void UnlockWeapon(int weaponIndex) {
            if (weaponIndex < 0 || weaponIndex >= _data.WeaponDatas.Count)
                return;
            _data.WeaponDatas[weaponIndex].IsUnlocked = true;
        }

        public void SetCurrentWeaponIndex(int weaponIndex) {
            if (weaponIndex < 0 || weaponIndex >= _data.WeaponDatas.Count)
                return;
            _data.CurrentWeaponIndex = weaponIndex;
            UIEvent.RaiseSelectWeapon(weaponIndex);
        }

        public int GetCurrentWeaponIndex() {
            return _data.CurrentWeaponIndex;
        }

        public void AddGems(int amount) {
            if (amount < 0)
                return;
            _data.Gems += amount;
            UIEvent.RaiseUpdateGems();
        }

        public bool SpendGems(int amount) {
            if (amount < 0 || amount > _data.Gems)
                return false;
            _data.Gems -= amount;
            UIEvent.RaiseUpdateGems();
            return true;
        }

        public int GetGems() {
            return _data.Gems;
        }

        public void SetMusicValue(float value) {
            _data.MusicValue = value;
        }

        public float GetMusicValue() {
            return _data.MusicValue;
        }

        public void SetSoundFXValue(float value) {
            _data.SoundFXValue = value;
        }

        public float GetSoundFXValue() {
            return _data.SoundFXValue;
        }

        public void SaveData() {
            SaveLoadManager.Save(_data, DataKey);
        }
    }
}