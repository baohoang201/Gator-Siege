using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gameplay.Tool {
    [System.Serializable]
    public class Pool {
        public string name;
        public GameObject go;
        public int count;
        public List<GameObject> Actives;
        public Queue<GameObject> DeActives;

        public Pool(string name, GameObject go, int count) {
            this.name = name;
            this.go = go;
            this.count = count;
        }

        public void InitAPool(Transform container, Dictionary<int, int> dicClones) {
            Actives = new List<GameObject>();
            DeActives = new Queue<GameObject>();
            for (int i = 0; i < count; i++) {
                SpawnAClone(container, dicClones);
            }
        }

        void SpawnAClone(Transform container, Dictionary<int, int> dicClones) {
            var clone = Object.Instantiate(go, container);
            clone.name += (Actives.Count + DeActives.Count);
            DeActives.Enqueue(clone);
            dicClones.Add(clone.GetHashCode(), GetHashCode());
        }

        public GameObject Get(Transform container, Dictionary<int, int> dicClones) {
            if (DeActives.Count == 0)
                SpawnAClone(container, dicClones);

            var clone = DeActives.Dequeue();
            Actives.Add(clone);
            return clone;
        }

        public void Return(GameObject go, bool deactive) {
            go.transform.rotation = Quaternion.identity;

            if (deactive) {
                go.SetActive(false);
            }

            if (Actives.Contains(go))
                Actives.Remove(go);

            if (!DeActives.Contains(go))
                DeActives.Enqueue(go);
        }
    }

    public class ObjectPool : MonoBehaviour {
        public static ObjectPool Instance { get; private set; }

        public List<Pool> poolList;

        private Dictionary<int, int> dicClones = new Dictionary<int, int>();
        private List<Pool> pools = new List<Pool>();

        private void Awake() {
            if (Instance == null) {
                Instance = this;
            }
            else {
                Destroy(gameObject);
            }
        }

        private void Start() {
            pools.AddRange(poolList);

            foreach (var pool in pools) {
                pool.InitAPool(transform, dicClones);
            }
        }

        public void ReturnAllPool() {
            foreach (var p in pools) {
                while (p.Actives.Count > 0) {
                    p.Actives[p.Actives.Count - 1].transform.SetParent(transform);
                    p.Return(p.Actives[p.Actives.Count - 1], true);
                }
            }
        }

        public Pool TryAddPoolByScript(Pool p) {
            var existedPool = pools.Find(x => x.go == p.go);
            if (existedPool != null) {
                Debug.LogWarning($"existed pool: {p.go.name}", p.go.transform);
                return existedPool;
            }
            pools.Add(p);
            p.InitAPool(transform, dicClones);
            return p;
        }

        public GameObject Get(Pool p, bool active = true) {
            var obj = p.Get(transform, dicClones);
            obj.SetActive(active);
            return obj;
        }

        public Pool CheckAddPool(GameObject obj) {
            foreach (var p in pools) {
                if (p.go == obj) {
                    return p;
                }
            }

            var pool = new Pool(obj.name, obj, 0);
            TryAddPoolByScript(pool);
            return pool;

        }

        public void Return(GameObject clone, bool deactive) {
            clone.transform.SetParent(transform);
            var hash = clone.GetHashCode();
            if (dicClones.ContainsKey(hash)) {
                var p = GetPool(dicClones[hash]);
                p.Return(clone, deactive);
            }
            else {
                Destroy(clone);
            }
        }

        Pool GetPool(int hash) {
            foreach (var pool in pools) {
                if (pool.GetHashCode() == hash)
                    return pool;
            }

            return null;
        }
    }
}