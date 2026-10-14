using UnityEngine;

namespace TowerDefense
{
    // The designer's art for the windows built in code (RuntimeUI). Loaded from
    // Resources/UISkin; without it the windows fall back to flat colors.
    [CreateAssetMenu(menuName = "Tower Defense/UI Skin", fileName = "UISkin")]
    public class UISkin : ScriptableObject
    {
        public Font font;
        // Nine-sliced frame behind every window.
        public Sprite windowFrame;
        public Sprite button;
        public Sprite coinIcon;
        // Below 1 makes the pixel-art frame chunkier.
        public float framePixelsPerUnitMultiplier = 0.5f;

        private static UISkin current;
        private static bool loaded;

        public static UISkin Current
        {
            get
            {
                if (!loaded)
                {
                    loaded = true;
                    current = Resources.Load<UISkin>("UISkin");
                }
                return current;
            }
        }
    }
}
