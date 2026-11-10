using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    // Minimal object pool keyed by prefab instance ID. Projectiles are spawned in the
    // dozens per second at ×2 speed; pooling removes the GC spikes from Instantiate/Destroy.
    public static class SimplePool
    {
        private static readonly Dictionary<int, Stack<GameObject>> pools = new Dictionary<int, Stack<GameObject>>();

        public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;
            int key = Key(prefab.name);
            if (pools.TryGetValue(key, out Stack<GameObject> stack) && stack.Count > 0)
            {
                GameObject item = stack.Pop();
                if (item != null)
                {
                    item.transform.SetPositionAndRotation(position, rotation);
                    item.SetActive(true);
                    return item;
                }
            }
            return Object.Instantiate(prefab, position, rotation);
        }

        public static void Release(GameObject item)
        {
            if (item == null) return;
            item.SetActive(false);
            if (item.transform.parent == null || item.transform.parent.name != PoolRootName)
                item.transform.SetParent(Root(), false);
            // Key by the source prefab name; clones carry a " (Clone)" suffix.
            string name = item.name;
            const string cloneSuffix = " (Clone)";
            if (name.EndsWith(cloneSuffix)) name = name.Substring(0, name.Length - cloneSuffix.Length);
            int key = Key(name);
            if (!pools.TryGetValue(key, out Stack<GameObject> stack))
            {
                stack = new Stack<GameObject>();
                pools[key] = stack;
            }
            stack.Push(item);
        }

        private static int Key(string prefabName) => prefabName.GetHashCode();

        private const string PoolRootName = "__Pool";
        private static Transform root;

        private static Transform Root()
        {
            if (root == null)
            {
                GameObject go = new GameObject(PoolRootName);
                Object.DontDestroyOnLoad(go);
                root = go.transform;
            }
            return root;
        }
    }
}
