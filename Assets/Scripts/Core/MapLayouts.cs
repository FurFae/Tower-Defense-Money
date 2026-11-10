using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    // Three map variants from the one painted scene: the campaign repositions the path
    // and prunes build slots per level, so routes, pressure points and slot counts differ
    // without shipping three scenes yet. Painted maps can replace these later.
    public static class MapLayouts
    {
        // Какая доля слотов строительства закрыта на каждой карте.
        private static readonly float[] SlotDisableFraction = { 0f, 0.25f, 0.4f };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Apply()
        {
            LevelDefinition level = Levels.Current;
            if (level == null) return;

            PathHolder path = Object.FindFirstObjectByType<PathHolder>();
            if (path == null || path.Waypoints == null || path.Waypoints.Length < 2) return;

            switch (Mathf.Clamp(level.number, 1, 3))
            {
                case 2:
                    MirrorPath(path);
                    break;
                case 3:
                    MirrorPath(path);
                    OffsetPath(path, new Vector3(0f, 2.5f, 0f));
                    break;
            }

            PruneSlots(level);
            TintBackground(level);
        }

        // Карта 2/3: путь зеркалится по горизонтали относительно центра маршрута —
        // враги идут с другой стороны, слоты покрывают маршрут иначе.
        private static void MirrorPath(PathHolder path)
        {
            Transform[] waypoints = path.Waypoints;
            Vector3 center = Vector3.zero;
            foreach (Transform waypoint in waypoints) center += waypoint.position;
            center /= waypoints.Length;
            foreach (Transform waypoint in waypoints)
            {
                Vector3 position = waypoint.position;
                position.x = 2f * center.x - position.x;
                waypoint.position = position;
            }
        }

        private static void OffsetPath(PathHolder path, Vector3 offset)
        {
            foreach (Transform waypoint in path.Waypoints)
                waypoint.position += offset;
        }

        // Часть слотов закрыта: на картах 2–3 меньше позиций для строительства.
        private static void PruneSlots(LevelDefinition level)
        {
            TowerSlot[] slots = Object.FindObjectsByType<TowerSlot>(FindObjectsSortMode.None);
            if (slots.Length == 0) return;
            float fraction = SlotDisableFraction[Mathf.Clamp(level.number - 1, 0, SlotDisableFraction.Length - 1)];
            if (fraction <= 0f) return;

            int toDisable = Mathf.RoundToInt(slots.Length * fraction);
            if (toDisable <= 0) return;

            // Deterministic pseudo-random pick so the same level always prunes the same slots.
            System.Random random = new System.Random(level.number * 1009);
            List<TowerSlot> pool = new List<TowerSlot>(slots);
            while (toDisable > 0 && pool.Count > 1)
            {
                int index = random.Next(pool.Count);
                TowerSlot slot = pool[index];
                pool.RemoveAt(index);
                // Keep a minimal buildable set near the path start.
                if (slot.GetComponent<Collider2D>() != null)
                    slot.GetComponent<Collider2D>().enabled = false;
                SpriteRenderer indicator = slot.GetComponentInChildren<SpriteRenderer>();
                if (indicator != null) indicator.color = new Color(0.2f, 0.2f, 0.2f, 0.4f);
                toDisable--;
            }
        }

        private static void TintBackground(LevelDefinition level)
        {
            if (Camera.main == null) return;
            Color tint = level.number switch
            {
                2 => new Color(0.85f, 0.9f, 1f),
                3 => new Color(1f, 0.82f, 0.75f),
                _ => Color.white,
            };
            Camera.main.backgroundColor = Color.Lerp(Camera.main.backgroundColor, tint, 0.35f);
        }
    }
}
