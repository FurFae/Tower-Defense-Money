using UnityEngine;

namespace TowerDefense
{
    // A tiny white circle texture generated at runtime, tinted by whoever needs a dot:
    // status icons over enemies, effect markers, etc. Avoids adding art for UI polish.
    public static class IconSprite
    {
        private static Sprite sprite;

        public static Sprite Get()
        {
            if (sprite != null) return sprite;
            const int size = 24;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            float center = (size - 1) * 0.5f;
            float radius = size * 0.42f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return sprite;
        }
    }
}
