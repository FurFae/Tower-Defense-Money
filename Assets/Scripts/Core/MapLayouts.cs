using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    // Campaign variants share a painted road and keep their path aligned with it.
    // Higher levels prune a stable selection of slots and introduce enemy roles.
    // Dedicated map art can replace these variants later.
    public static class MapLayouts
    {
        // Какая доля слотов строительства закрыта на каждой карте.
        private static readonly float[] SlotDisableFraction = { 0f, 0.1f, 0.2f };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) => Apply();

        private static void Apply()
        {
            LevelDefinition level = Levels.Current;
            if (level == null) return;

            PathHolder path = Object.FindFirstObjectByType<PathHolder>();
            if (path == null || path.Waypoints == null || path.Waypoints.Length < 2) return;

            // Keep the route aligned with the painted road until dedicated map art exists.
            PruneSlots(level);
            TintBackground(level);
        }

        // Часть слотов закрыта: на картах 2–3 меньше позиций для строительства.
        private static void PruneSlots(LevelDefinition level)
        {
            TowerSlot[] slots = Object.FindObjectsByType<TowerSlot>(FindObjectsSortMode.None);
            if (slots.Length == 0) return;
            System.Array.Sort(slots, (a, b) => {
                int x = a.transform.position.x.CompareTo(b.transform.position.x);
                return x != 0 ? x : a.transform.position.y.CompareTo(b.transform.position.y);
            });
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
