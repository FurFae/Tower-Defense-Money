using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense
{
    // In-battle controls built in code on the meta canvas: ×2 speed toggle and the
    // "next wave" button with a mana bonus plus a preview of the upcoming wave's
    // composition. No scene wiring: RetentionUI adds this component at startup.
    public class BattleControlsUI : MonoBehaviour
    {
        private GameObject root;
        private Button speedButton;
        private Text speedLabel;
        private Button nextWaveButton;
        private Text nextWaveLabel;
        private Text previewText;
        private Text previewHeading;
        private int previewIndex = -1;
        private Text statusText;
        private WaveManager waveManager;
        private GameManager gameManager;

        private void Awake()
        {
            Build();
        }

        private void Update()
        {
            // The scene loads after this component; pick the managers up once they exist.
            if (gameManager == null) gameManager = GameManager.Instance;
            if (waveManager == null)
            {
                waveManager = FindFirstObjectByType<WaveManager>();
                previewIndex = -1;
            }

            bool inBattle = gameManager != null && !gameManager.IsGameOver;
            bool visible = inBattle && !BuildMenuUI.IsOpen;
            if (root.activeSelf != visible) root.SetActive(visible);
            if (!visible || waveManager == null) return;

            speedLabel.text = gameManager.IsFastForward ? "×2" : "×1";
            RuntimeUI.SetButtonState(speedButton, !gameManager.IsPaused);

            bool canCall = waveManager.IsInDelay && !gameManager.IsPaused;
            RuntimeUI.SetButtonState(nextWaveButton, canCall);
            nextWaveLabel.text = waveManager.IsPreparing ? "Начать бой" : canCall ? $"Раньше  +{WaveManager.NextWaveBonus}" : "Волна идёт…";

            int index = waveManager.CurrentWaveNumber + (waveManager.WaveInProgress ? 1 : 0);
            if (previewIndex != index || previewText.text.Length == 0)
            {
                previewIndex = index;
                previewText.text = waveManager.DescribeNextWave() ?? "Подкреплений больше нет";
            }
            previewHeading.text = waveManager.IsPreparing ? "Первая волна"
                : waveManager.IsInDelay ? $"До атаки · {Mathf.CeilToInt(waveManager.DelayRemaining)} с"
                : index <= waveManager.TotalWaves ? $"Далее · волна {index}" : "Последняя волна";
            statusText.text = $"<color=#9CD7F1>Мана  {gameManager.Currency}</color>    Замок  {gameManager.Lives}\nВолна  {waveManager.CurrentWaveNumber} / {waveManager.TotalWaves}";

        }

        private static void SetPreviewArea(RectTransform rect, float bottom, float top)
        {
            rect.anchorMin = new Vector2(.26f, bottom);
            rect.anchorMax = new Vector2(.535f, top);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private void Build()
        {
            Image bar = RuntimeUI.CreateImage(transform, "BattleDock", new Color(.055f, .10f, .09f, 1f));
            root = bar.gameObject;
            bar.raycastTarget = true;
            RectTransform rect = bar.rectTransform;
            rect.anchorMin = new Vector2(0, 0.86f); rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0); rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            root.transform.SetAsFirstSibling();
            statusText = RuntimeUI.CreateText(root.transform, "", 27, TextAnchor.MiddleLeft);
            RuntimeUI.SetAnchors(statusText.rectTransform, 0.02f, 0.23f);
            var rule = RuntimeUI.CreateImage(root.transform, "BrassDivider", new Color(0.56f,0.48f,0.30f));
            RuntimeUI.SetAnchors(rule.rectTransform, 0.24f, 0.242f, 18f);
            previewHeading = RuntimeUI.CreateText(root.transform, "", 23, TextAnchor.LowerLeft);
            previewHeading.color = new Color(.88f, .77f, .51f);
            previewHeading.fontStyle = FontStyle.Bold;
            previewText = RuntimeUI.CreateText(root.transform, "", 24, TextAnchor.UpperLeft);
            previewText.resizeTextForBestFit = true;
            previewText.resizeTextMinSize = 14;
            previewText.resizeTextMaxSize = 24;
            SetPreviewArea(previewHeading.rectTransform, .64f, .94f);
            SetPreviewArea(previewText.rectTransform, .06f, .61f);
            speedButton = RuntimeUI.CreateButton(root.transform, "SpeedButton", "", 25, out speedLabel);
            RuntimeUI.StyleMenuButton(speedButton);
            RuntimeUI.SetAnchors((RectTransform)speedButton.transform, 0.67f, 0.72f, 16f);
            speedButton.onClick.AddListener(() => GameManager.Instance?.ToggleSpeed());
            nextWaveButton = RuntimeUI.CreateButton(root.transform, "NextWaveButton", "", 26, out nextWaveLabel);
            RuntimeUI.SetAnchors((RectTransform)nextWaveButton.transform, 0.73f, 0.89f, 16f);
            nextWaveButton.onClick.AddListener(() => { if (waveManager != null) waveManager.RequestSkipDelay(); });
            root.SetActive(false);
        }
    }
}