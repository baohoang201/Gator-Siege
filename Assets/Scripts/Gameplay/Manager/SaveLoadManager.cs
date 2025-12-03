using Data;
using Newtonsoft.Json;
using System.IO;
using UnityEngine;

namespace Gameplay.Manager {
    public static class SaveLoadManager {
        public static void Save(SerializableData data, string filename) {
            string filePath = Application.persistentDataPath + $"/{filename}.json";
            string json = JsonConvert.SerializeObject(data, Formatting.None, new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            File.WriteAllText(filePath, json);
        }

        public static SerializableData Load(string filename) {
            string filePath = Application.persistentDataPath + $"/{filename}.json";

            if (!File.Exists(filePath))
                return null;

            string json = File.ReadAllText(filePath);

            SerializableData data = JsonConvert.DeserializeObject<SerializableData>(json, new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.All
            });
            return data;
        }
    }
}
