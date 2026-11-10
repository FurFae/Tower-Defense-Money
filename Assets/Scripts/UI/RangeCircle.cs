using UnityEngine;

namespace TowerDefense
{
    // A world-space ring (attack range, splash area, chain radius) drawn with a
    // LineRenderer, created at runtime so no scene wiring is needed.
    public class RangeCircle : MonoBehaviour
    {
        private LineRenderer line;
        private int points = 64;

        public static RangeCircle Create(Transform parent, Color color, float width = 0.05f)
        {
            GameObject go = new GameObject("RangeCircle");
            go.transform.SetParent(parent, false);
            RangeCircle circle = go.AddComponent<RangeCircle>();
            circle.line = go.AddComponent<LineRenderer>();
            circle.line.useWorldSpace = false;
            circle.line.loop = true;
            circle.line.startWidth = width;
            circle.line.endWidth = width;
            circle.line.material = new Material(Shader.Find("Sprites/Default"));
            circle.line.startColor = color;
            circle.line.endColor = color;
            circle.line.sortingOrder = 30;
            circle.line.positionCount = circle.points;
            return circle;
        }

        public void Show(float radius)
        {
            for (int i = 0; i < points; i++)
            {
                float angle = i * Mathf.PI * 2f / points;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
