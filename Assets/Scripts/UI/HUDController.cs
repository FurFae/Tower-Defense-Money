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
            if (currencyText != null) currencyText.text = $"{value}";
        }

        private void UpdateLives(int value)
        {
            if (livesText != null) livesText.text = $"{value}";
        }

        private void UpdateWave(int current, int total)
        {
            if (waveText != null) waveText.text = $"Волна: {current}/{total}";
        }

        private void HandleGameEnded(bool won)
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            if (gameOverText != null) gameOverText.text = won ? "Победа!" : "Поражение";

            LevelDefinition next = Levels.Next(gameManager != null ? gameManager.Level : null);
            if (won && next != null) AddNextLevelButton(next);
        }

        // A copy of the restart button, so it keeps the designer's look.
        private void AddNextLevelButton(LevelDefinition next)
        {
            Transform restart = gameOverPanel != null ? gameOverPanel.transform.Find("RestartButton") : null;
            if (restart == null) return;

            RectTransform panelRect = (RectTransform)gameOverPanel.transform;
            panelRect.sizeDelta += new Vector2(0f, 60f);
            foreach (RectTransform child in panelRect)
                child.anchoredPosition += new Vector2(0f, 30f);

            GameObject copy = Instantiate(restart.gameObject, restart.parent);
            copy.name = "NextLevelButton";
            RectTransform rect = (RectTransform)copy.transform;
            rect.anchoredPosition -= new Vector2(0f, 120f);
            Text label = copy.GetComponentInChildren<Text>();
            if (label != null) label.text = "Следующий уровень";

            Button button = copy.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => Levels.Load(next));
        }

        public void OnRestartClicked() => gameManager?.RestartLevel();
        public void OnMainMenuClicked() => gameManager?.LoadMainMenu(mainMenuSceneName);
    }
}
