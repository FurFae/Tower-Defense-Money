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
            if (gameManager != null) gameManager.OnGameEnded += HandleGameEnded;
        }

        private void OnDisable()
        {
            if (gameManager != null) gameManager.OnGameEnded -= HandleGameEnded;
        }
        private void Start()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (startWaveButton != null) startWaveButton.gameObject.SetActive(false);
            HideSceneCounters();
        }

        private void HideSceneCounters()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = GameObject.Find("HUDCanvas")?.GetComponent<Canvas>();
            if (canvas == null) return;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null) scaler.matchWidthOrHeight = 0.5f;
            foreach (Text old in new[] { currencyText, livesText, waveText })
            {
                if (old == null) continue;
                Transform hide = old.transform;
                while (hide.parent != null && hide.parent != canvas.transform) hide = hide.parent;
                if (hide != canvas.transform) hide.gameObject.SetActive(false);
            }
        }

        private void HandleGameEnded(bool won)
        {
            if (gameOverPanel == null || gameOverPanel.activeSelf) return;
            foreach (Transform child in gameOverPanel.transform) child.gameObject.SetActive(false);
            RuntimeUI.Stretch((RectTransform)gameOverPanel.transform);
            gameOverPanel.GetComponent<Image>().color = new Color(.025f, .045f, .04f, .72f);
            gameOverPanel.transform.SetAsLastSibling();
            gameOverPanel.SetActive(true);

            var next = won ? Levels.Next(gameManager.Level) : null;
            var card = RuntimeUI.CreateImage(gameOverPanel.transform, "ResultCard", RuntimeUI.PanelColor);
            RuntimeUI.DecoratePanel(card);
            RuntimeUI.Place(card.transform, Vector2.one * .5f, Vector2.one * .5f, Vector2.zero,
                new Vector2(560f, next != null ? 376f : 306f));
            float y = 22f;
            var title = RuntimeUI.CreateText(RuntimeUI.TopRow(card.transform, ref y, 64f),
                won ? "Победа!" : "Замок пал", 44, TextAnchor.MiddleCenter);
            title.color = won ? RuntimeUI.CoinColor : new Color(.96f, .65f, .52f);
            title.fontStyle = FontStyle.Bold;
            if (gameManager.Level != null)
                RuntimeUI.CreateText(RuntimeUI.TopRow(card.transform, ref y, 32f),
                    $"Уровень {gameManager.Level.number} · {gameManager.Level.title}", 24, TextAnchor.MiddleCenter);
            y += 8f;
            if (next != null) AddAction("Следующий уровень", () => Levels.Load(next), true);
            AddAction("Сыграть снова", OnRestartClicked, !won);
            AddAction("В главное меню", OnMainMenuClicked, won && next == null);

            void AddAction(string label, UnityEngine.Events.UnityAction action, bool primary)
            {
                var button = RuntimeUI.CreateButton(RuntimeUI.TopRow(card.transform, ref y, 60f),
                    label, label, 27, out _);
                RuntimeUI.Stretch((RectTransform)button.transform);
                if (!primary) RuntimeUI.StyleMenuButton(button);
                button.onClick.AddListener(action);
            }
        }

        public void OnRestartClicked() => gameManager?.RestartLevel();
        public void OnMainMenuClicked() => gameManager?.LoadMainMenu(mainMenuSceneName);
    }
}
