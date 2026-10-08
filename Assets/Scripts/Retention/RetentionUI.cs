using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TowerDefense
{
    // Builds the meta canvas in code and keeps it alive across scenes, so neither the menu
    // nor the game scene needs wiring: the wallet, the rewards window (daily reward,
    // missions, achievements) and, through UpgradeUI and ShopUI, the towers and shop windows.
    public class RetentionUI : MonoBehaviour
    {
        private class Row
        {
            public bool mission;
            public int index;
            public RectTransform fill;
            public Text progress;
            public Button button;
            public Text buttonLabel;
        }

        private const float SaveInterval = 5f;
        private const float ToastDuration = 3f;

        private GameObject panel;
        private GameObject badge;
        private GameObject walletBar;
        private Text walletBarText;
        private Text windowWallet;
        private Text dailyTitle;
        private Button dailyButton;
        private Text dailyButtonLabel;
        private GameObject toast;
        private Text toastText;
        private readonly List<Row> rows = new List<Row>();
        private UpgradeUI towers;
        private ShopUI shop;
        private float saveTimer;
        private Coroutine toastRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            GameObject go = new GameObject("MetaUI");
            DontDestroyOnLoad(go);
            go.AddComponent<RetentionUI>();
        }

        private void Awake()
        {
            Build();
            towers = gameObject.AddComponent<UpgradeUI>();
            shop = gameObject.AddComponent<ShopUI>();
            gameObject.AddComponent<RewardPopup>();

            RetentionSystem.OnChanged += Refresh;
            RetentionSystem.OnGoalCompleted += ShowToast;
            MetaProgress.OnChanged += Refresh;
            SceneManager.sceneLoaded += HandleSceneLoaded;

            EnsureEventSystem();
            Refresh();
            // The session starts with the daily reward.
            panel.SetActive(RetentionSystem.CanClaimDaily);
        }

        private void OnDestroy()
        {
            RetentionSystem.OnChanged -= Refresh;
            RetentionSystem.OnGoalCompleted -= ShowToast;
            MetaProgress.OnChanged -= Refresh;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Update()
        {
            saveTimer += Time.unscaledDeltaTime;
            if (saveTimer < SaveInterval) return;
            saveTimer = 0f;
            RetentionSystem.Flush();
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus) RetentionSystem.Flush();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) RetentionSystem.Flush();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RetentionSystem.Flush();
            EnsureEventSystem();
            Refresh();
            towers.Refresh();
            shop.Refresh();
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private void Refresh()
        {
            // During a level the HUD shows mana only; the wallet stays inside the windows.
            walletBar.SetActive(GameManager.Instance == null);
            walletBarText.text = RuntimeUI.WalletText();
            windowWallet.text = RuntimeUI.WalletText();

            bool canClaimDaily = RetentionSystem.CanClaimDaily;
            string dailyReward = RuntimeUI.RewardText(RetentionSystem.DailyCoins, RetentionSystem.DailyCrystals);
            dailyTitle.text = $"Ежедневная награда — день {RetentionSystem.DailyStreakDay}: {dailyReward}";
            dailyButtonLabel.text = canClaimDaily ? "Забрать" : "Приходи завтра";
            RuntimeUI.SetButtonState(dailyButton, canClaimDaily);

            foreach (Row row in rows)
            {
                RetentionGoal goal = row.mission ? RetentionSystem.Missions[row.index] : RetentionSystem.Achievements[row.index];
                int progress = RetentionSystem.GetProgress(row.mission, row.index);
                bool claimed = RetentionSystem.IsClaimed(row.mission, row.index);

                row.fill.anchorMax = new Vector2((float)progress / goal.target, 1f);
                row.progress.text = $"{progress}/{goal.target}";
                row.buttonLabel.text = claimed ? "Получено" : RuntimeUI.RewardText(goal.coins, goal.crystals);
                RuntimeUI.SetButtonState(row.button, RetentionSystem.CanClaim(row.mission, row.index));
            }

            badge.SetActive(RetentionSystem.ClaimableCount() > 0);
        }

        private void ShowToast(string goalTitle)
        {
            toastText.text = $"Выполнено: {goalTitle}";
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(ToastRoutine());
        }

        private IEnumerator ToastRoutine()
        {
            toast.SetActive(true);
            yield return new WaitForSecondsRealtime(ToastDuration);
            toast.SetActive(false);
            toastRoutine = null;
        }

        private void Build()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            Image bar = RuntimeUI.CreateImage(transform, "WalletBar", RuntimeUI.PanelColor);
            RuntimeUI.Place(bar.transform, Vector2.one, Vector2.one, new Vector2(-20f, -20f), new Vector2(520f, 60f));
            walletBar = bar.gameObject;
            walletBarText = RuntimeUI.CreateText(bar.transform, "", 28, TextAnchor.MiddleCenter);

            // Open button on the left edge, with a dot when something can be claimed.
            Button open = RuntimeUI.CreateButton(transform, "RewardsButton", "Задания", 30, out _);
            open.image.color = RuntimeUI.RowColor;
            RuntimeUI.Place(open.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 80f), new Vector2(200f, 70f));
            open.onClick.AddListener(Toggle);
            badge = RuntimeUI.CreateImage(open.transform, "Badge", Color.red).gameObject;
            RuntimeUI.Place(badge.transform, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 24f));

            Transform w = RuntimeUI.CreateWindow(transform, "Награды и задания", 980f, out panel, out windowWallet, out float y);

            Transform daily = RuntimeUI.CreateRowBackground(RuntimeUI.TopRow(w, ref y, 90f));
            dailyTitle = RuntimeUI.CreateText(daily, "", 26, TextAnchor.MiddleLeft);
            RuntimeUI.SetAnchors(dailyTitle.rectTransform, 0.02f, 0.70f);
            dailyButton = RuntimeUI.CreateButton(daily, "ClaimDaily", "", 26, out dailyButtonLabel);
            RuntimeUI.SetAnchors((RectTransform)dailyButton.transform, 0.72f, 0.98f, 12f);
            dailyButton.onClick.AddListener(RetentionSystem.ClaimDaily);

            RuntimeUI.CreateHeader(w, ref y, "Ежедневные задания");
            for (int i = 0; i < RetentionSystem.Missions.Length; i++)
                CreateGoalRow(w, ref y, true, i);

            RuntimeUI.CreateHeader(w, ref y, "Достижения");
            for (int i = 0; i < RetentionSystem.Achievements.Length; i++)
                CreateGoalRow(w, ref y, false, i);

            string bonus = RuntimeUI.RewardText(0, RetentionSystem.AllMissionsCrystals);
            RuntimeUI.CreateText(RuntimeUI.TopRow(w, ref y, 50f), $"За все задания дня: {bonus}", 24, TextAnchor.MiddleCenter);

            Image toastBg = RuntimeUI.CreateImage(transform, "Toast", RuntimeUI.PanelColor);
            RuntimeUI.Place(toastBg.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(900f, 70f));
            toast = toastBg.gameObject;
            toastText = RuntimeUI.CreateText(toast.transform, "", 28, TextAnchor.MiddleCenter);
            toastText.color = RuntimeUI.CoinColor;
            toast.SetActive(false);
        }

        private void Toggle()
        {
            bool show = !panel.activeSelf;
            RuntimeUI.CloseAllWindows();
            Refresh();
            panel.SetActive(show);
        }

        private void CreateGoalRow(Transform window, ref float y, bool mission, int index)
        {
            RetentionGoal goal = mission ? RetentionSystem.Missions[index] : RetentionSystem.Achievements[index];
            Transform background = RuntimeUI.CreateRowBackground(RuntimeUI.TopRow(window, ref y, 66f));

            Text title = RuntimeUI.CreateText(background, goal.title, 24, TextAnchor.MiddleLeft);
            RuntimeUI.SetAnchors(title.rectTransform, 0.02f, 0.52f);

            Image bar = RuntimeUI.CreateImage(background, "Bar", RuntimeUI.BarColor);
            RuntimeUI.SetAnchors(bar.rectTransform, 0.54f, 0.76f, 16f);
            Image fill = RuntimeUI.CreateImage(bar.transform, "Fill", RuntimeUI.AccentColor);
            RuntimeUI.Stretch(fill.rectTransform);
            Text progress = RuntimeUI.CreateText(bar.transform, "", 22, TextAnchor.MiddleCenter);

            Button button = RuntimeUI.CreateButton(background, "Claim", "", 24, out Text buttonLabel);
            RuntimeUI.SetAnchors((RectTransform)button.transform, 0.78f, 0.98f, 8f);
            button.onClick.AddListener(() => RetentionSystem.Claim(mission, index));

            rows.Add(new Row
            {
                mission = mission,
                index = index,
                fill = fill.rectTransform,
                progress = progress,
                button = button,
                buttonLabel = buttonLabel,
            });
        }
    }
}
