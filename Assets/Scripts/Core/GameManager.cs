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
        // Position of this level in the campaign, starting at 1.
        [SerializeField] private int levelNumber = 1;

        public int Currency { get; private set; }
        public int Lives { get; private set; }
        public bool IsGameOver { get; private set; }

        private int maxLives;

        public event Action<int> OnCurrencyChanged;
        public event Action<int> OnLivesChanged;
        public event Action<bool> OnGameEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
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
                YG2.GameplayStop();
        }

        public bool TrySpend(int amount)
        {
            if (IsGameOver || amount > Currency) return false;
            Currency -= amount;
            OnCurrencyChanged?.Invoke(Currency);
            return true;
        }

        public void AddCurrency(int amount)
        {
            if (IsGameOver) return;
            Currency += amount;
            OnCurrencyChanged?.Invoke(Currency);
        }

        public void DamageBase(int amount)
        {
            if (IsGameOver) return;
            Lives -= amount;
            OnLivesChanged?.Invoke(Lives);
            if (Lives <= 0)
                EndGame(false);
        }

        public void EndGame(bool won)
        {
            if (IsGameOver) return;
            IsGameOver = true;
            YG2.GameplayStop();
            if (won)
            {
                MetaProgress.CompleteLevel(levelNumber, Lives, maxLives);
                RetentionSystem.Report(RetentionStat.Wins);
                if (Lives >= maxLives)
                    RetentionSystem.Report(RetentionStat.FlawlessWins);
            }
            OnGameEnded?.Invoke(won);
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoadMainMenu(string mainMenuSceneName)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
