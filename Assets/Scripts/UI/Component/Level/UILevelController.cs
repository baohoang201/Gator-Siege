using Data;
using Gameplay.Manager;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Component.Level {
    public class UILevelController : MonoBehaviour {
        [SerializeField] List<UILevel> uiLevels = new List<UILevel>();

        private void Start() {
            UpdateLevels();
        }

        private void UpdateLevels() {
            UserModel userModel = DataManager.Instance.GetUserModel();
            List<LevelData> levelDatas = userModel.GetLevelDatas();

            if (userModel != null) {
                for (int i = 0; i < uiLevels.Count; i++) {
                    if (i < levelDatas.Count) {
                        uiLevels[i].SetData(i, levelDatas[i].IsUnlocked, levelDatas[i].IsCompleted);
                    }
                }
            }
        }
    }
}