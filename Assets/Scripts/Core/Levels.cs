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

    public enum Challenge
    {
        None = 0,
        NoRepair = 1,  // без ремонта замка, награда ×1.5
        RareOnly = 2,  // колода: только редкие и выше, награда ×2
    }

    // The campaign. One painted scene; MapLayouts turns it into three different maps
    // (pruned build slots, per-level enemy mixes, level-3 boss).
    public static class Levels
    {
        public static readonly LevelDefinition[] All =
        {
            new LevelDefinition(1, "Лесная опушка", "game_scene", 1f, 1f, 0),
            new LevelDefinition(2, "Тёмная чаща", "game_scene", 1.25f, 1.15f, 1),
            new LevelDefinition(3, "Осада замка", "game_scene", 1.5f, 1.3f, 2),
        };

        // Челлендж, выбранный в окне выбора уровня; действует на следующий запуск.
        public static Challenge ActiveChallenge { get; set; } = Challenge.None;

        public static float RewardMultiplier => ActiveChallenge == Challenge.RareOnly ? 2f
            : ActiveChallenge == Challenge.NoRepair ? 1.5f : 1f;

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
