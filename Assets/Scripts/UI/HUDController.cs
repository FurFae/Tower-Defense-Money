using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private WaveManager waveManager;

        [SerializeField] private Text currencyText;
        [SerializeField] private Text livesText;
        [SerializeField] private Text waveText;
        [SerializeField] private Button startWaveButton;

        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Text gameOverText;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.OnCurrencyChanged += UpdateCurrency;
                gameManager.OnLivesChanged += UpdateLives;
                gameManager.OnGameEnded += HandleGameEnded;
            }
            if (waveManager != null)
                waveManager.OnWaveChanged += UpdateWave;
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.OnCurrencyChanged -= UpdateCurrency;
                gameManager.OnLivesChanged -= UpdateLives;
                gameManager.OnGameEnded -= HandleGameEnded;
            }
            if (waveManager != null)
                waveManager.OnWaveChanged -= UpdateWave;
        }

        private void Start()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (startWaveButton != null)
                startWaveButton.onClick.AddListener(() => waveManager?.RequestSkipDelay());
        }

        private void UpdateCurrency(int value)
        {
            if (currencyText != null) currencyText.text = $"Gold: {value}";
        }

        private void UpdateLives(int value)
        {
            if (livesText != null) livesText.text = $"Lives: {value}";
        }

        private void UpdateWave(int current, int total)
        {
            if (waveText != null) waveText.text = $"Wave: {current}/{total}";
        }

        private void HandleGameEnded(bool won)
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            if (gameOverText != null) gameOverText.text = won ? "Victory!" : "Defeat";
        }

        public void OnRestartClicked() => gameManager?.RestartLevel();
        public void OnMainMenuClicked() => gameManager?.LoadMainMenu(mainMenuSceneName);
    }
}
