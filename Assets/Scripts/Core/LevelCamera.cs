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

        private Transform entrance;
        private Camera cam;
        private Bounds content;


        private int screenWidth;
        private int screenHeight;

        private void Start()
        {
            cam = GetComponent<Camera>();
            cam.rect = new Rect(0f, 0f, 1f, 0.86f);
            cam.backgroundColor = new Color(0.055f, 0.10f, 0.085f);
            content = ContentBounds();
            var route = FindAnyObjectByType<PathHolder>();
            if (route != null && route.Waypoints.Length > 0) entrance = route.Waypoints[0];
            if (ground == null)
            {
                GameObject groundObject = GameObject.Find("Grid/Tilemap");
                if (groundObject != null) ground = groundObject.GetComponent<Tilemap>();
            }
            if (ground != null)
            {
                var used = new TileBase[ground.GetUsedTilesCount()];
                ground.GetUsedTilesNonAlloc(used);
                CacheWesternTerrain(ground);
                grass = System.Array.Find(used, tile => tile != null && tile.name == "grass_rule");
            }
            SculptLowerWoods();
            Fit();
        }

        private void LateUpdate()
        {
            // The Yandex Games frame can be resized or rotated at any time.
            if (Screen.width != screenWidth || Screen.height != screenHeight) Fit();
        }

        // Remember the painted silhouette before filling any camera margins.
        private readonly System.Collections.Generic.Dictionary<int, (int x, TileBase tile)> westernEdges =
            new System.Collections.Generic.Dictionary<int, (int x, TileBase tile)>();

        private void CacheWesternTerrain(Tilemap map)
        {
            BoundsInt bounds = map.cellBounds;
            for (int y = bounds.yMin; y < bounds.yMax; y++)
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    TileBase tile = map.GetTile(cell);
                    if (tile == null) continue;
                    if (tile.name == "road_rule" || tile.name == "forest_rule")
                        westernEdges[y] = (x, tile);
                    break;
                }
        }
        private TileBase grass;

        // Grass clearings cut into the lower canopy, including the new build site.
        private static bool Clearing(int x, int y)
        {
            float dx = (x + 0.5f + 25f) / 4.3f, dy = (y + 0.5f + 7.3f) / 2.8f;
            float u = (x + 0.5f + 16f) / 3.4f, v = (y + 0.5f + 13.8f) / 2.1f;
            float rim = -8.3f + 1.35f * Mathf.Sin(x * .46f) + .65f * Mathf.Sin(x * .9f);
            return dx*dx + dy*dy < 1f || u*u + v*v < 1f || y >= rim;
        }

        private void SculptLowerWoods()
        {
            if (ground == null || grass == null) return;
            foreach (var cell in ground.cellBounds.allPositionsWithin)
            {
                var tile = ground.GetTile(cell);
                if (cell.y < -5 && tile != null && tile.name == "forest_rule" && Clearing(cell.x, cell.y))
                    ground.SetTile(cell, grass);
            }
        }

        private void FillVisibleGround()
        {
            if (ground == null || grass == null) return;
            float depth = ground.transform.position.z - cam.transform.position.z;
            Vector3Int min = ground.WorldToCell(cam.ViewportToWorldPoint(new Vector3(0, 0, depth)));
            Vector3Int max = ground.WorldToCell(cam.ViewportToWorldPoint(new Vector3(1, 1, depth)));
            // Include a border so the RuleTile's outer transition lies beyond the camera.
            for (int y = min.y - 2; y <= max.y + 2; y++)
                for (int x = min.x - 2; x <= max.x + 2; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    if (ground.HasTile(cell)) continue;
                    TileBase fill = grass;
                    if (westernEdges.TryGetValue(y, out var edge) && x < edge.x)
                        fill = edge.tile.name == "forest_rule" && Clearing(x, y) ? grass : edge.tile;
                    ground.SetTile(cell, fill);
                }
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
            {
                Add(slot.transform.position);
                if (slot.TryGetComponent<SpriteRenderer>(out var marker))
                {
                    Add(marker.bounds.min); Add(marker.bounds.max);
                }
            }
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

            cam.orthographicSize = halfHeight;
            transform.position = new Vector3(center.x, center.y, transform.position.z);
            if (entrance != null)
            {
                Vector3 pos = entrance.position;
                pos.x = center.x - halfHeight * cam.aspect - 1.3f;
                entrance.position = pos;
            }
            FillVisibleGround();
        }
    }
}
