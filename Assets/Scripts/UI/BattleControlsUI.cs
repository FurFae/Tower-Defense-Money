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
            if (waveManager == null) waveManager = FindFirstObjectByType<WaveManager>();

            bool inBattle = gameManager != null && !gameManager.IsGameOver;
            if (root.activeSelf != inBattle) root.SetActive(inBattle);
            if (!inBattle || waveManager == null) return;

            speedLabel.text = gameManager.IsFastForward ? "Скорость ×2" : "Скорость ×1";
            RuntimeUI.SetButtonState(speedButton, !gameManager.IsPaused);

            bool canCall = waveManager.IsInDelay;
            RuntimeUI.SetButtonState(nextWaveButton, canCall);
            nextWaveLabel.text = canCall ? $"Волна сейчас (+{WaveManager.NextWaveBonus} маны)" : "Волна идёт…";

            string preview = waveManager.DescribeNextWave();
            int nextNumber = waveManager.WaveInProgress ? waveManager.CurrentWaveNumber + 1 : waveManager.CurrentWaveNumber;
            previewText.text = preview != null
                ? $"Волна {nextNumber}/{waveManager.TotalWaves}: {preview}"
                : "Это последняя волна!";
        }

        private void Build()
        {
            root = new GameObject("BattleControls", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            RectTransform rect = (RectTransform)root.transform;
            RuntimeUI.Place(rect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(300f, 0f));
            rect.anchorMin = new Vector2(1f, 0.2f);
            rect.anchorMax = new Vector2(1f, 0.8f);
            rect.offsetMin = new Vector2(-300f, 0f);
            rect.offsetMax = new Vector2(-10f, 0f);

            float y = 0f;
            RectTransform row;

            speedButton = RuntimeUI.CreateButton(root.transform, "SpeedButton", "", 26, out speedLabel);
            row = (RectTransform)speedButton.transform;
            RuntimeUI.Place(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -35f), new Vector2(280f, 64f));
            speedButton.onClick.AddListener(() => GameManager.Instance?.ToggleSpeed());

            nextWaveButton = RuntimeUI.CreateButton(root.transform, "NextWaveButton", "", 24, out nextWaveLabel);
            row = (RectTransform)nextWaveButton.transform;
            RuntimeUI.Place(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(280f, 64f));
            nextWaveButton.onClick.AddListener(() =>
            {
                if (waveManager != null && waveManager.IsInDelay)
                    waveManager.RequestSkipDelay();
            });

            Image previewBg = RuntimeUI.CreateImage(root.transform, "Preview", RuntimeUI.PanelColor);
            RuntimeUI.Place(previewBg.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(280f, 160f));
            previewText = RuntimeUI.CreateText(previewBg.transform, "", 20, TextAnchor.UpperLeft);
            previewText.rectTransform.offsetMin = new Vector2(10f, 8f);
            previewText.rectTransform.offsetMax = new Vector2(-10f, -8f);

            root.SetActive(false);
        }
    }
}
