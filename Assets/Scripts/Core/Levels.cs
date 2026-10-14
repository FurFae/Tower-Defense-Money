using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense
{
    public class LevelDefinition
    {
        public readonly int number;
        public readonly string title;
        public readonly string sceneName;
        // Difficulty on top of the waves set up in the scene.
        public readonly float healthMultiplier;
        public readonly float countMultiplier;
        // Copies of the scene's last wave appended at the end, each a bit bigger.
        public readonly int extraWaves;

        public LevelDefinition(int number, string title, string sceneName, float healthMultiplier,
            float countMultiplier, int extraWaves)
        {
            this.number = number;
            this.title = title;
            this.sceneName = sceneName;
            this.healthMultiplier = healthMultiplier;
            this.countMultiplier = countMultiplier;
            this.extraWaves = extraWaves;
        }
    }

    // The campaign. Levels 2 and 3 replay game_scene with harder waves until the designer
    // paints their own maps: then only sceneName changes (and the scene goes into Build Settings).
    public static class Levels
    {
        public static readonly LevelDefinition[] All =
        {
            new LevelDefinition(1, "Лесная опушка", "game_scene", 1f, 1f, 0),
            new LevelDefinition(2, "Тёмная чаща", "game_scene", 1.4f, 1.3f, 1),
            new LevelDefinition(3, "Осада замка", "game_scene", 1.9f, 1.6f, 2),
        };

        // The level being played; null when a scene is started directly in the Editor.
        public static LevelDefinition Current { get; private set; }

        public static LevelDefinition Find(int number)
        {
            return number >= 1 && number <= All.Length ? All[number - 1] : null;
        }

        public static bool IsUnlocked(LevelDefinition level) => level.number <= MetaProgress.LevelsCompleted + 1;

        public static LevelDefinition Next(LevelDefinition level) => level == null ? null : Find(level.number + 1);

        public static void Load(LevelDefinition level)
        {
            Current = level;
            Time.timeScale = 1f;
            RuntimeUI.CloseAllWindows();
            SceneManager.LoadScene(level.sceneName);
        }

        // Scenes started from the Editor play the level their GameManager is set to.
        public static void EnsureCurrent(int fallbackNumber)
        {
            if (Current == null) Current = Find(fallbackNumber) ?? All[0];
        }
    }
}
