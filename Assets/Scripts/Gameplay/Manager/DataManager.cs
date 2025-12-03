using Data;
using Gameplay.Event;
using System;
using System.Linq;
using UnityEngine;

namespace Gameplay.Manager {
    public class DataManager : MonoBehaviour {
        public static DataManager Instance { get; private set; }

        [SerializeField] UserDataSO _userDataSO;

        private UserModel _userModel;

        private void Awake() {
            if (Instance == null) {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else {
                Destroy(gameObject);
            }
        }

        private T GetModel<T, TData>(ref T model, string filename, Func<TData> getDataFunc, params object[] constructorParams) where T : class where TData : SerializableData
        {
            if (model == null)
            {
                TData data = SaveLoadManager.Load(filename) as TData;
                if (data == null)
                {
                    data = getDataFunc();
                }
                model = Activator.CreateInstance(typeof(T), new object[] { data }.Concat(constructorParams).ToArray()) as T;
            }
            return model;
        }

        public UserModel GetUserModel()
        {
            return GetModel(ref _userModel, UserModel.DataKey, _userDataSO.GetUserData);
        }

        public void SaveData() {
            _userModel?.SaveData();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                SaveData();
            }
        }

        private void OnApplicationQuit() {
            SaveData();
        }
    }
}
