using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using YG;

namespace TowerDefense
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private int startingCurrency = 150;
        [SerializeField] private int startingLives = 20;
        // Level played when the scene is started directly in the Editor (see Levels).
        [SerializeField] private int levelNumber = 1;

        public int Currency { get; private set; }
        public int Lives { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsPaused { get; private set; }
        // Battle speed multiplier (×1 / ×2); combined with pause via Time.timeScale.
        public float SpeedScale { get; private set; } = 1f;
        public bool IsFastForward => SpeedScale > 1.5f;
        public LevelDefinition Level => Levels.Current;
        public int MaxLives => maxLives;

        // Castle repair: +1 life for mana, pricier each time, a few times per level.
        public const int MaxRepairs = 5;
        private const int RepairBaseCost = 40;
        private const int RepairCostStep = 20;
        public int RepairsUsed { get; private set; }
        public int RepairCost => RepairBaseCost + RepairCostStep * RepairsUsed;
        public bool CanRepair => !IsGameOver && !IsPaused && Lives < maxLives && RepairsUsed < MaxRepairs
            && Currency >= RepairCost && Levels.ActiveChallenge != Challenge.NoRepair;

        private int maxLives;
        // Flawless means the castle was never hit, repairs or not.
        private bool castleDamaged;

        public event Action<int> OnCurrencyChanged;
        public event Action<int> OnLivesChanged;
        public event Action<bool> OnGameEnded;
        public event Action<bool> OnPauseChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Levels.EnsureCurrent(levelNumber);
        }

        private void Start()
        {
            Camera cam = Camera.main;
            if (cam != null && cam.GetComponent<LevelCamera>() == null)
                cam.gameObject.AddComponent<LevelCamera>();

            Currency = startingCurrency + MetaProgress.ManaPerLevel * MetaProgress.GetLevel(MetaProgress.BaseManaId);
            maxLives = startingLives + MetaProgress.GetLevel(MetaProgress.BaseLivesId);
            Lives = maxLives;
            OnCurrencyChanged?.Invoke(Currency);
            OnLivesChanged?.Invoke(Lives);
            YG2.GameplayStart();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Time.timeScale = 1f;
                YG2.GameplayStop();
            }
        }

        public void SetPaused(bool paused)
        {
            if (IsGameOver || paused == IsPaused) return;
            IsPaused = paused;
            ApplyTimeScale();
            if (paused) YG2.GameplayStop();
            else YG2.GameplayStart();
            OnPauseChanged?.Invoke(paused);
        }

        public void ToggleSpeed()
        {
            if (IsGameOver || IsPaused) return;
            SpeedScale = IsFastForward ? 1f : 2f;
            ApplyTimeScale();
        }

        private void ApplyTimeScale()
        {
            Time.timeScale = IsPaused ? 0f : SpeedScale;
        }

        public bool TrySpend(int amount)
        {
            if (IsGameOver || IsPaused || amount < 0 || amount > Currency) return false;
            Currency -= amount;
            OnCurrencyChanged?.Invoke(Currency);
            return true;
        }

        public void AddCurrency(int amount)
        {
            if (IsGameOver || amount <= 0) return;
            Currency += amount;
            OnCurrencyChanged?.Invoke(Currency);
        }

        public bool TryRepair()
        {
            if (!CanRepair) return false;
            Currency -= RepairCost;
            RepairsUsed++;
            Lives++;
            OnCurrencyChanged?.Invoke(Currency);
            OnLivesChanged?.Invoke(Lives);
            return true;
        }

        public void DamageBase(int amount)
        {
            if (IsGameOver || amount <= 0) return;
            castleDamaged = true;
            Lives = Mathf.Max(0, Lives - amount);
            OnLivesChanged?.Invoke(Lives);
            if (Lives <= 0)
                EndGame(false);
        }

        public void EndGame(bool won)
        {
            if (IsGameOver) return;
            IsGameOver = true;
            IsPaused = false;
            Time.timeScale = 1f;
            YG2.GameplayStop();
            if (won)
            {
                GameSfx.Play(GameSfx.Event.Victory);
                LevelResult result = MetaProgress.CompleteLevel(Level.number, Lives, maxLives, Currency);
                RetentionSystem.Report(RetentionStat.Wins);
                RetentionSystem.Report(RetentionStat.Stars, result.stars);
                if (!castleDamaged)
                    RetentionSystem.Report(RetentionStat.FlawlessWins);
            }
            else
            {
                GameSfx.Play(GameSfx.Event.Defeat);
            }
            OnGameEnded?.Invoke(won);
        }

        public void RestartLevel()
        {
            Levels.Load(Level);
        }

        public void LoadMainMenu(string mainMenuSceneName)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
