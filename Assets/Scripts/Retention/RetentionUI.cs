using System;
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
    // nor the game scene needs wiring: the wallet, the rewards window (daily reward, then tabs
    // for daily and weekly missions, achievements and the event) and, through UpgradeUI and
    // ShopUI, the towers and shop windows.
    public class RetentionUI : MonoBehaviour
    {
        private class Row
        {
            public GoalList list;
            public bool isEvent;
            public int index;
            public Text title;
            public RectTransform fill;
            public Text progress;
            public Button button;
            public Text buttonLabel;
        }

        private static readonly string[] TabNames = { "Ежедневные", "Неделя", "Достижения", "Ивент" };
        private const int EventTab = 3;

        private const float SaveInterval = 5f;
        private const float ToastDuration = 3f;
        private const float GoalRowHeight = 58f;
        private static readonly Color ClaimedDayColor = new Color(0.12f, 0.08f, 0.05f, 1f);
        private static readonly Color TodayColor = new Color(0.45f, 0.30f, 0.15f, 1f);
        private static readonly Color ClaimedTextColor = new Color(0.55f, 0.55f, 0.55f, 1f);

        private GameObject panel;
        private GameObject openButton;
        private GameObject badge;
        private GameObject walletBar;
        private Text walletBarText;
        private Text windowWallet;
        private Text dailyTitle;
        private readonly List<Image> dayCells = new List<Image>();
        private readonly List<Text> dayTexts = new List<Text>();
        private Button dailyButton;
        private Text dailyButtonLabel;
        private Text dailyResetText;
        private Text weeklyResetText;
        private Text eventTitle;
        private Text eventPoints;
        private RectTransform eventFill;
        private GameObject toast;
        private Text toastText;
        private readonly List<Row> rows = new List<Row>();
        private readonly List<Button> tabButtons = new List<Button>();
        private readonly List<GameObject> tabBadges = new List<GameObject>();
        private readonly List<GameObject> tabs = new List<GameObject>();
        private int currentTab;
        private UpgradeUI towers;
        private ShopUI shop;
        private float saveTimer;
        private float clockTimer;
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
            gameObject.AddComponent<LevelSelectUI>();
            gameObject.AddComponent<PauseUI>();
            gameObject.AddComponent<BattleUI>();

            RetentionSystem.OnChanged += Refresh;
            RetentionSystem.OnGoalCompleted += ShowToast;
            MetaProgress.OnChanged += Refresh;
            SceneManager.sceneLoaded += HandleSceneLoaded;

            EnsureEventSystem();
            SelectTab(0);
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
            // Countdowns tick while the window is open; a day or week rolling over refreshes the goals.
            clockTimer += Time.unscaledDeltaTime;
            if (clockTimer >= 1f)
            {
                clockTimer = 0f;
                if (panel.activeSelf) Refresh();
            }

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

        private static string FormatTime(TimeSpan time)
        {
            if (time.TotalDays >= 1) return $"{(int)time.TotalDays} д {time.Hours} ч";
            return $"{time.Hours} ч {time.Minutes} мин";
        }

        private void Refresh()
        {
            // During a level the HUD shows mana only and the battlefield stays clear: the wallet
            // stays inside the windows, rewards are claimed between levels.
            bool inLevel = GameManager.Instance != null;
            walletBar.SetActive(!inLevel);
            openButton.SetActive(!inLevel);
            walletBarText.text = RuntimeUI.WalletText();
            windowWallet.text = RuntimeUI.WalletText();

            bool canClaimDaily = RetentionSystem.CanClaimDaily;
            dailyTitle.text = $"Ежедневная награда — {RetentionSystem.DailyStreakDay} д. подряд   <size=20>пропуск дня начинает серию заново</size>";
            dailyButtonLabel.text = canClaimDaily ? "Забрать" : "Приходи завтра";
            RuntimeUI.SetButtonState(dailyButton, canClaimDaily);
            RefreshDayStrip(canClaimDaily);

            dailyResetText.text = $"Новые задания через {FormatTime(RetentionSystem.UntilDailyReset)}";
            weeklyResetText.text = $"Новые задания через {FormatTime(RetentionSystem.UntilWeeklyReset)}";

            EventDefinition current = RetentionSystem.CurrentEvent;
            int points = RetentionSystem.EventPoints;
            int maxPoints = RetentionSystem.EventMilestones[RetentionSystem.EventMilestones.Length - 1].points;
            eventTitle.text = $"<b>{current.title}</b>   <size=22>до конца {FormatTime(RetentionSystem.UntilEventEnd)}</size>\n<size=22>{current.description}</size>";
            eventPoints.text = $"Очки ивента: {points}";
            eventFill.anchorMax = new Vector2(Mathf.Clamp01((float)points / maxPoints), 1f);

            foreach (Row row in rows)
            {
                if (row.isEvent) RefreshEventRow(row, points);
                else RefreshGoalRow(row);
            }

            for (int i = 0; i < tabButtons.Count; i++)
            {
                RuntimeUI.SetTabState(tabButtons[i], i == currentTab);
                int claimable = i == EventTab ? RetentionSystem.ClaimableEventCount() : RetentionSystem.ClaimableCount((GoalList)i);
                tabBadges[i].SetActive(claimable > 0);
            }
            badge.SetActive(RetentionSystem.ClaimableCount() > 0);
        }

        // Days before today's in the cycle are taken; today's is taken once claimed.
        private void RefreshDayStrip(bool canClaimDaily)
        {
            int today = RetentionSystem.DailyCycleIndex;
            for (int i = 0; i < dayCells.Count; i++)
            {
                bool claimed = i < today || (i == today && !canClaimDaily);
                bool isToday = i == today;
                dayCells[i].color = claimed ? ClaimedDayColor : isToday ? TodayColor : RuntimeUI.RowColor;
                dayCells[i].GetComponent<Outline>().enabled = isToday;

                int crystals = RetentionSystem.CycleCrystals(i);
                string reward = $"<color=#FFD44D>{RetentionSystem.DailyRewards[i]}</color>";
                if (crystals > 0) reward += $" <color=#C084FF>+{crystals}кр</color>";
                dayTexts[i].text = claimed ? $"День {i + 1}\nвзято" : $"День {i + 1}\n{reward}";
                dayTexts[i].color = claimed ? ClaimedTextColor : Color.white;
            }
        }

        private static void RefreshGoalRow(Row row)
        {
            RetentionGoal goal = RetentionSystem.GetGoals(row.list)[row.index];
            int progress = RetentionSystem.GetProgress(row.list, row.index);
            bool claimed = RetentionSystem.IsClaimed(row.list, row.index);

            row.title.text = goal.title;
            row.fill.anchorMax = new Vector2((float)progress / goal.target, 1f);
            row.progress.text = $"{progress}/{goal.target}";
            row.buttonLabel.text = claimed ? "Получено" : RuntimeUI.RewardText(goal.coins, goal.crystals);
            RuntimeUI.SetButtonState(row.button, RetentionSystem.CanClaim(row.list, row.index));
        }

        private static void RefreshEventRow(Row row, int points)
        {
            int target = RetentionSystem.EventMilestones[row.index].points;
            bool claimed = RetentionSystem.IsEventClaimed(row.index);

            row.title.text = RetentionSystem.DescribeEventReward(row.index);
            row.fill.anchorMax = new Vector2(Mathf.Clamp01((float)points / target), 1f);
            row.progress.text = $"{Mathf.Min(points, target)}/{target}";
            row.buttonLabel.text = claimed ? "Получено" : "Забрать";
            RuntimeUI.SetButtonState(row.button, RetentionSystem.CanClaimEvent(row.index));
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
            RuntimeUI.Place(bar.transform, Vector2.one, Vector2.one, new Vector2(-20f, -20f), new Vector2(560f, 60f));
            walletBar = bar.gameObject;
            walletBarText = RuntimeUI.CreateText(bar.transform, "", 28, TextAnchor.MiddleCenter);
            UISkin skin = UISkin.Current;
            if (skin != null && skin.coinIcon != null)
            {
                Image coin = RuntimeUI.CreateImage(bar.transform, "Coin", Color.white);
                coin.sprite = skin.coinIcon;
                coin.preserveAspect = true;
                RuntimeUI.Place(coin.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(40f, 40f));
                walletBarText.rectTransform.offsetMin = new Vector2(56f, 0f);
            }

            // Open button on the left edge, with a dot when something can be claimed.
            Button open = RuntimeUI.CreateButton(transform, "RewardsButton", "Задания", 30, out _);
            RuntimeUI.StyleMenuButton(open);
            RuntimeUI.Place(open.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 80f), new Vector2(200f, 70f));
            open.onClick.AddListener(Toggle);
            openButton = open.gameObject;
            badge = CreateBadge(open.transform);

            Transform w = RuntimeUI.CreateWindow(transform, "Награды и задания", 1040f, out panel, out windowWallet, out float y);

            Transform daily = RuntimeUI.CreateRowBackground(RuntimeUI.TopRow(w, ref y, 140f));
            dailyTitle = RuntimeUI.CreateText(daily, "", 24, TextAnchor.MiddleLeft);
            Place(dailyTitle.rectTransform, 0.02f, 0.98f, 0.70f, 1f);
            BuildDayStrip(daily);
            dailyButton = RuntimeUI.CreateButton(daily, "ClaimDaily", "", 26, out dailyButtonLabel);
            Place((RectTransform)dailyButton.transform, 0.78f, 0.98f, 0.10f, 0.68f);
            dailyButton.onClick.AddListener(RetentionSystem.ClaimDaily);

            Transform tabRow = RuntimeUI.TopRow(w, ref y, 60f);
            for (int i = 0; i < TabNames.Length; i++)
            {
                int index = i;
                Button tab = RuntimeUI.CreateButton(tabRow, "Tab", TabNames[i], 26, out _);
                RuntimeUI.SetAnchors((RectTransform)tab.transform, i * 0.25f + 0.004f, (i + 1) * 0.25f - 0.004f);
                tab.onClick.AddListener(() => SelectTab(index));
                tabButtons.Add(tab);
                tabBadges.Add(CreateBadge(tab.transform));
            }
            y += 6f;

            BuildMissionsTab(CreateTab(w), y, GoalList.Daily, out dailyResetText);
            BuildMissionsTab(CreateTab(w), y, GoalList.Weekly, out weeklyResetText);
            BuildAchievementsTab(CreateTab(w), y);
            BuildEventTab(CreateTab(w), y);

            Image toastBg = RuntimeUI.CreateImage(transform, "Toast", RuntimeUI.PanelColor);
            RuntimeUI.Place(toastBg.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(900f, 70f));
            toast = toastBg.gameObject;
            toastText = RuntimeUI.CreateText(toast.transform, "", 28, TextAnchor.MiddleCenter);
            toastText.color = RuntimeUI.CoinColor;
            toast.SetActive(false);
        }

        // The 7-day login cycle: one cell per day with its reward.
        private void BuildDayStrip(Transform parent)
        {
            int days = RetentionSystem.DailyRewards.Length;
            const float left = 0.02f, right = 0.76f, gap = 0.006f;
            float width = (right - left) / days;
            for (int i = 0; i < days; i++)
            {
                Image cell = RuntimeUI.CreateImage(parent, $"Day{i + 1}", RuntimeUI.RowColor);
                Place(cell.rectTransform, left + i * width + gap / 2f, left + (i + 1) * width - gap / 2f, 0.08f, 0.68f);
                Outline outline = cell.gameObject.AddComponent<Outline>();
                outline.effectColor = RuntimeUI.CoinColor;
                outline.effectDistance = new Vector2(3f, -3f);
                Text text = RuntimeUI.CreateText(cell.transform, "", 19, TextAnchor.MiddleCenter);
                dayCells.Add(cell);
                dayTexts.Add(text);
            }
        }

        private static void Place(RectTransform rect, float xMin, float xMax, float yMin, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject CreateBadge(Transform button)
        {
            GameObject dot = RuntimeUI.CreateImage(button, "Badge", Color.red).gameObject;
            RuntimeUI.Place(dot.transform, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 24f));
            return dot;
        }

        private Transform CreateTab(Transform window)
        {
            RectTransform tab = new GameObject("TabContent", typeof(RectTransform)).GetComponent<RectTransform>();
            tab.SetParent(window, false);
            RuntimeUI.Stretch(tab);
            tabs.Add(tab.gameObject);
            return tab;
        }

        private void SelectTab(int index)
        {
            currentTab = index;
            for (int i = 0; i < tabs.Count; i++)
                tabs[i].SetActive(i == index);
            Refresh();
        }

        private void BuildMissionsTab(Transform tab, float y, GoalList list, out Text resetText)
        {
            resetText = RuntimeUI.CreateText(RuntimeUI.TopRow(tab, ref y, 40f), "", 24, TextAnchor.MiddleLeft);
            for (int i = 0; i < RetentionSystem.GetGoals(list).Length; i++)
                CreateRow(tab, ref y, list, false, i);

            string period = list == GoalList.Daily ? "дня" : "недели";
            string bonus = RuntimeUI.RewardText(0, RetentionSystem.AllClaimedBonus(list));
            RuntimeUI.CreateText(RuntimeUI.TopRow(tab, ref y, 50f), $"За все задания {period}: {bonus}", 24, TextAnchor.MiddleCenter);
        }

        private void BuildAchievementsTab(Transform tab, float y)
        {
            for (int i = 0; i < RetentionSystem.Achievements.Length; i++)
                CreateRow(tab, ref y, GoalList.Achievements, false, i);
        }

        private void BuildEventTab(Transform tab, float y)
        {
            Transform header = RuntimeUI.CreateRowBackground(RuntimeUI.TopRow(tab, ref y, 90f));
            eventTitle = RuntimeUI.CreateText(header, "", 28, TextAnchor.MiddleLeft);
            RuntimeUI.SetAnchors(eventTitle.rectTransform, 0.02f, 0.98f);

            Transform pointsRow = RuntimeUI.TopRow(tab, ref y, 44f);
            eventPoints = RuntimeUI.CreateText(pointsRow, "", 24, TextAnchor.MiddleLeft);
            RuntimeUI.SetAnchors(eventPoints.rectTransform, 0f, 0.35f);
            Image bar = RuntimeUI.CreateImage(pointsRow, "Bar", RuntimeUI.BarColor);
            RuntimeUI.SetAnchors(bar.rectTransform, 0.36f, 1f, 10f);
            Image fill = RuntimeUI.CreateImage(bar.transform, "Fill", RuntimeUI.CoinColor);
            RuntimeUI.Stretch(fill.rectTransform);
            eventFill = fill.rectTransform;

            for (int i = 0; i < RetentionSystem.EventMilestones.Length; i++)
                CreateRow(tab, ref y, GoalList.Daily, true, i);
        }

        private void Toggle()
        {
            bool show = !panel.activeSelf;
            RuntimeUI.CloseAllWindows();
            Refresh();
            panel.SetActive(show);
        }

        private void CreateRow(Transform tab, ref float y, GoalList list, bool isEvent, int index)
        {
            Transform background = RuntimeUI.CreateRowBackground(RuntimeUI.TopRow(tab, ref y, GoalRowHeight));

            Text title = RuntimeUI.CreateText(background, "", 24, TextAnchor.MiddleLeft);
            RuntimeUI.SetAnchors(title.rectTransform, 0.02f, 0.52f);

            Image bar = RuntimeUI.CreateImage(background, "Bar", RuntimeUI.BarColor);
            RuntimeUI.SetAnchors(bar.rectTransform, 0.54f, 0.76f, 16f);
            Image fill = RuntimeUI.CreateImage(bar.transform, "Fill", RuntimeUI.AccentColor);
            RuntimeUI.Stretch(fill.rectTransform);
            Text progress = RuntimeUI.CreateText(bar.transform, "", 22, TextAnchor.MiddleCenter);

            Button button = RuntimeUI.CreateButton(background, "Claim", "", 24, out Text buttonLabel);
            RuntimeUI.SetAnchors((RectTransform)button.transform, 0.78f, 0.98f, 8f);
            if (isEvent)
            {
                button.onClick.AddListener(() =>
                {
                    string received = RetentionSystem.ClaimEvent(index);
                    if (received != null) RewardPopup.Show(RetentionSystem.CurrentEvent.title, received);
                });
            }
            else
            {
                button.onClick.AddListener(() => RetentionSystem.Claim(list, index));
            }

            rows.Add(new Row
            {
                list = list,
                isEvent = isEvent,
                index = index,
                title = title,
                fill = fill.rectTransform,
                progress = progress,
                button = button,
                buttonLabel = buttonLabel,
            });
        }
    }
}
