using UnityEngine;
using UnityEngine.Tilemaps;

namespace TowerDefense
{
    // Frames the battle for any screen shape: the enemy path, every tower slot and the
    // extra points below, plus margins for the HUD, kept inside the ground tilemap.
    // GameManager adds it to the main camera if the scene has none.
    [RequireComponent(typeof(Camera))]
    public class LevelCamera : MonoBehaviour
    {
        [SerializeField] private float sideMargin = 1.5f;
        [SerializeField] private float topMargin = 2.5f; // lives and mana sit in the top-left corner
        [SerializeField] private float bottomMargin = 1.5f;
        // More things that must stay on screen, e.g. the castle gate.
        [SerializeField] private Transform[] extraPoints = new Transform[0];
        // The view stays where it is fully painted; found by name if not set.
        [SerializeField] private Tilemap ground;

        private Camera cam;
        private Bounds content;
        private Bounds limits;
        private bool hasLimits;
        private int screenWidth;
        private int screenHeight;

        private void Start()
        {
            cam = GetComponent<Camera>();
            content = ContentBounds();
            if (ground == null)
            {
                GameObject groundObject = GameObject.Find("Grid/Tilemap");
                if (groundObject != null) ground = groundObject.GetComponent<Tilemap>();
            }
            if (ground != null)
            {
                BoundsInt cells = FilledCells(ground);
                Vector3 min = ground.CellToWorld(cells.min);
                Vector3 max = ground.CellToWorld(cells.max);
                limits = new Bounds((min + max) / 2f, max - min);
                hasLimits = cells.size.x > 0 && cells.size.y > 0;
            }
            Fit();
        }

        private void LateUpdate()
        {
            // The Yandex Games frame can be resized or rotated at any time.
            if (Screen.width != screenWidth || Screen.height != screenHeight) Fit();
        }

        // Largest box without holes: the map's edges are ragged, so edge rows and columns
        // with a missing tile are trimmed until every side is fully painted.
        private static BoundsInt FilledCells(Tilemap tilemap)
        {
            tilemap.CompressBounds();
            BoundsInt b = tilemap.cellBounds;
            int xMin = b.xMin, xMax = b.xMax - 1, yMin = b.yMin, yMax = b.yMax - 1;

            bool ColumnFull(int x)
            {
                for (int y = yMin; y <= yMax; y++)
                    if (!tilemap.HasTile(new Vector3Int(x, y, 0))) return false;
                return true;
            }
            bool RowFull(int y)
            {
                for (int x = xMin; x <= xMax; x++)
                    if (!tilemap.HasTile(new Vector3Int(x, y, 0))) return false;
                return true;
            }

            bool trimmed = true;
            while (trimmed && xMin < xMax && yMin < yMax)
            {
                trimmed = false;
                if (!ColumnFull(xMin)) { xMin++; trimmed = true; }
                if (!ColumnFull(xMax)) { xMax--; trimmed = true; }
                if (!RowFull(yMin)) { yMin++; trimmed = true; }
                if (!RowFull(yMax)) { yMax--; trimmed = true; }
            }
            return new BoundsInt(xMin, yMin, 0, xMax - xMin + 1, yMax - yMin + 1, 1);
        }

        private Bounds ContentBounds()
        {
            bool any = false;
            Bounds bounds = new Bounds();
            void Add(Vector3 point)
            {
                if (any) bounds.Encapsulate(point);
                else bounds = new Bounds(point, Vector3.zero);
                any = true;
            }

            PathHolder path = FindAnyObjectByType<PathHolder>();
            if (path != null)
                foreach (Transform waypoint in path.Waypoints)
                    Add(waypoint.position);
            foreach (TowerSlot slot in FindObjectsByType<TowerSlot>(FindObjectsSortMode.None))
                Add(slot.transform.position);
            foreach (Transform point in extraPoints)
                if (point != null) Add(point.position);
            return bounds;
        }

        private void Fit()
        {
            screenWidth = Screen.width;
            screenHeight = Screen.height;

            Vector2 min = new Vector2(content.min.x - sideMargin, content.min.y - bottomMargin);
            Vector2 max = new Vector2(content.max.x + sideMargin, content.max.y + topMargin);
            Vector2 size = max - min;
            float halfHeight = Mathf.Max(size.y / 2f, size.x / 2f / cam.aspect);
            Vector2 center = (min + max) / 2f;

            if (hasLimits)
            {
                // Shrink to the ground if it is smaller than the view, then slide inside it.
                halfHeight = Mathf.Min(halfHeight, limits.extents.y, limits.extents.x / cam.aspect);
                float halfWidth = halfHeight * cam.aspect;
                center.x = Mathf.Clamp(center.x, limits.min.x + halfWidth, limits.max.x - halfWidth);
                center.y = Mathf.Clamp(center.y, limits.min.y + halfHeight, limits.max.y - halfHeight);
            }

            cam.orthographicSize = halfHeight;
            transform.position = new Vector3(center.x, center.y, transform.position.z);
        }
    }
}
