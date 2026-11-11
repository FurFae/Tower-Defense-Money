using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    // Inactive objects persist; checked-out projectiles belong to the current scene.
    public static class SimplePool
    {
        private static readonly Dictionary<int, Stack<GameObject>> pools = new Dictionary<int, Stack<GameObject>>();
        private static readonly Dictionary<int, int> owners = new Dictionary<int, int>();
        private static readonly HashSet<int> pooled = new HashSet<int>();
        private static Transform root;

        public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;
            int key = prefab.GetInstanceID();
            if (pools.TryGetValue(key, out var stack))
                while (stack.Count > 0)
                {
                    GameObject item = stack.Pop();
                    if (item == null) continue;
                    pooled.Remove(item.GetInstanceID());
                    item.transform.SetParent(null, true);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(item,
                        UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                    item.transform.SetPositionAndRotation(position, rotation);
                    item.SetActive(true);
                    return item;
                }
            GameObject created = Object.Instantiate(prefab, position, rotation);
            owners[created.GetInstanceID()] = key;
            return created;
        }

        public static void Release(GameObject item)
        {
            if (item == null) return;
            int id = item.GetInstanceID();
            if (!owners.TryGetValue(id, out int key)) { Object.Destroy(item); return; }
            if (!pooled.Add(id)) return;
            item.SetActive(false);
            if (root == null)
            {
                GameObject container = new GameObject("__Pool");
                Object.DontDestroyOnLoad(container);
                root = container.transform;
            }
            item.transform.SetParent(root, false);
            if (!pools.TryGetValue(key, out var stack)) pools[key] = stack = new Stack<GameObject>();
            stack.Push(item);
        }
    }
}