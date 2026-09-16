using UnityEngine;

namespace TowerDefense
{
    // Attach to the "Waypoints" GameObject. Enemies walk through its children
    // in hierarchy order (top to bottom), not by name, since duplicated
    // waypoints can end up with messy auto-generated names.
    public class PathHolder : MonoBehaviour
    {
        public Transform[] Waypoints { get; private set; }

        private void Awake()
        {
            Waypoints = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
                Waypoints[i] = transform.GetChild(i);
        }
    }
}
