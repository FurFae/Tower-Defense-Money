using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense
{
    // Mana sinks during a level: the castle repair button under the lives counter and the
    // upgrade popup opened by clicking a built tower. Added by RetentionUI, which owns the canvas.
    public class BattleUI : MonoBehaviour
    {
        private static BattleUI instance;

        private RectTransform canvasRect;
        private GameObject repairObject;
        private Button repairButton;
        private Text repairLabel;
        private GameObject blocker;
        private RectTransform popup;
        private Text towerTitle;
        private Text towerStats;
        private Button upgradeButton;
        private Text upgradeLabel;
        private Button targetingButton;
        private Text targetingLabel;
        private Button poisonButton;
        private Text poisonLabel;
        private Button sellButton;
        private Text sellLabel;
        private RangeCircle rangeCircle;
        private Tower current;

        private static readonly string[] TargetingNames = { "Цель: к замку", "Цель: к башне", "Цель: живучий", "Цель: быстрый" };
        private static readonly string[] PoisonNames = { "Яд: новые цели", "Яд: сильный яд", "Яд: живучий" };

        private void Awake()
        {
            instance = this;
            canvasRect = (RectTransform)transform;
            Build();
            repairObject.SetActive(false);
            blocker.SetActive(false);
            rangeCircle = RangeCircle.Create(transform, new Color(0.4f, 1f, 0.7f, 0.8f));
            rangeCircle.Hide();
        }

        private void OnDestroy()
        {
            // Круг живёт на канвасе (DontDestroyOnLoad), просто отвязываем от башни.
            if (rangeCircle != null)
            {
                rangeCircle.transform.SetParent(null, false);
                Object.DontDestroyOnLoad(rangeCircle.gameObject);
            }
        }

        public static void OpenTower(Tower tower)
        {
            if (instance != null && tower != null) instance.Show(tower);
        }

        public static void CloseTower()
        {
            if (instance != null) instance.Close();
        }

        private void Update()
        {
            GameManager game = GameManager.Instance;
            bool inBattle = game != null && !game.IsGameOver;
            if (repairObject.activeSelf != inBattle) repairObject.SetActive(inBattle);
            if (!inBattle)
            {
                if (blocker.activeSelf) Close();
                return;
            }

            if (game.RepairsUsed >= GameManager.MaxRepairs) repairLabel.text = "Ремонт исчерпан";
            else if (game.Lives >= game.MaxLives) repairLabel.text = "Замок цел";
            else repairLabel.text = $"Ремонт +1 жизнь\n{game.RepairCost} маны";
            RuntimeUI.SetButtonState(repairButton, game.CanRepair);

            if (!blocker.activeSelf) return;
            if (current == null || game.IsPaused) Close();
            else RefreshTower(game);
        }

        private void EnsureRangeCircle()
        {
            if (rangeCircle != null) return;
            rangeCircle = RangeCircle.Create(transform, new Color(0.4f, 1f, 0.7f, 0.8f));
            rangeCircle.Hide();
        }

        private void Show(Tower tower)
        {
            GameManager game = GameManager.Instance;
            if (game == null || game.IsPaused || game.IsGameOver) return;
            BuildMenuUI.Instance?.Close();
            current = tower;
            EnsureRangeCircle();

            // Above the tower, kept on screen.
            Camera cam = Camera.main;
            Vector2 local = Vector2.zero;
            if (cam != null)
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,
                    cam.WorldToScreenPoint(tower.transform.position), null, out local);
            Vector2 half = canvasRect.rect.size / 2f - popup.sizeDelta / 2f - new Vector2(10f, 10f);
            local += new Vector2(0f, 170f);
            popup.anchoredPosition = new Vector2(Mathf.Clamp(local.x, -half.x, half.x), Mathf.Clamp(local.y, -half.y, half.y));

            blocker.transform.SetAsLastSibling();
            blocker.SetActive(true);
            if (rangeCircle != null) rangeCircle.transform.SetParent(tower.transform, false);
            RefreshTower(game);
        }

        private void Close()
        {
            current = null;
            blocker.SetActive(false);
            if (rangeCircle != null)
            {
                rangeCircle.Hide();
                rangeCircle.transform.SetParent(transform, false);
            }
        }

        private void RefreshTower(GameManager game)
        {
            Tower t = current;
            EnsureRangeCircle();
            rangeCircle.Show(t.Range);
            TowerDefinition definition = TowerCatalog.Find(t.Data.name);
            string title = definition != null ? definition.title : t.Data.displayName;
            string poison = t.Data.damageOverTimePerSecond > 0f
                ? $" · яд {t.Data.damageOverTimePerSecond:0.#}/с" : "";
            string pierce = t.Data.pierceCount > 0 ? $" · пробитие {t.Data.pierceCount}" : "";
            string chain = t.Data.chainCount > 0 ? $" · цепь {t.Data.chainCount}" : "";
            int slowPercent = Mathf.RoundToInt((1f - t.Data.slowMultiplier) * 100f);
            string slow = t.Data.slowDuration > 0f ? $" · замедление {slowPercent}%" : "";
            string burn = t.Data.burnDamagePerSecond > 0f ? $" · огонь {t.Data.burnDamagePerSecond:0.#}/с" : "";
            string effects = poison + pierce + chain + slow + burn;
            towerTitle.text = $"<b>{title}</b>   улучшение {t.BattleTier}/{Tower.MaxBattleTier}";

            if (t.IsBattleMaxed)
            {
                towerStats.text = $"Урон {t.Damage:0.#} · дальность {t.Range:0.#}{effects}";
                upgradeLabel.text = "Максимум";
                RuntimeUI.SetButtonState(upgradeButton, false);
            }
            else
            {
                int next = t.BattleTier + 1;
                towerStats.text = $"Урон {t.Damage:0.#} → {t.DamageAt(next):0.#}{effects}\nДальность {t.Range:0.#} → {t.RangeAt(next):0.#}";
                upgradeLabel.text = $"Улучшить — {t.UpgradeCost} маны";
                RuntimeUI.SetButtonState(upgradeButton, game.Currency >= t.UpgradeCost);
            }

            targetingLabel.text = TargetingNames[(int)t.Targeting];
            bool isPoison = t.Data.preferUnpoisonedTarget;
            poisonButton.gameObject.SetActive(isPoison);
            if (isPoison) poisonLabel.text = PoisonNames[(int)t.PoisonTargeting];
            sellLabel.text = $"Продать +{t.SellPrice} маны";
        }

        private void Build()
        {
            Button repair = RuntimeUI.CreateButton(transform, "RepairButton", "", 24, out repairLabel);
            RuntimeUI.Place(repair.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -170f), new Vector2(280f, 70f));
            repair.onClick.AddListener(() => GameManager.Instance?.TryRepair());
            repairButton = repair;
            repairObject = repair.gameObject;

            // An invisible full-screen catcher: clicking anywhere outside the popup closes it.
            Image catcher = RuntimeUI.CreateImage(transform, "TowerPopupBlocker", new Color(0f, 0f, 0f, 0f));
            catcher.raycastTarget = true;
            RuntimeUI.Stretch(catcher.rectTransform);
            catcher.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            blocker = catcher.gameObject;

            Image panel = RuntimeUI.CreateImage(blocker.transform, "TowerPopup", RuntimeUI.PanelColor);
            panel.raycastTarget = true;
            popup = panel.rectTransform;
            RuntimeUI.Place(popup, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480f, 400f));
            UISkin skin = UISkin.Current;
            if (skin != null && skin.windowFrame != null)
            {
                panel.sprite = skin.windowFrame;
                panel.type = Image.Type.Sliced;
                panel.pixelsPerUnitMultiplier = skin.framePixelsPerUnitMultiplier;
                panel.color = Color.white;
                Image inner = RuntimeUI.CreateImage(popup, "Inner", RuntimeUI.PanelColor);
                RuntimeUI.Stretch(inner.rectTransform);
                inner.rectTransform.offsetMin = new Vector2(12f, 12f);
                inner.rectTransform.offsetMax = new Vector2(-12f, -12f);
            }

            float y = 20f;
            towerTitle = RuntimeUI.CreateText(RuntimeUI.TopRow(popup, ref y, 44f), "", 26, TextAnchor.MiddleCenter);
            towerStats = RuntimeUI.CreateText(RuntimeUI.TopRow(popup, ref y, 70f), "", 24, TextAnchor.MiddleCenter);
            upgradeButton = RuntimeUI.CreateButton(RuntimeUI.TopRow(popup, ref y, 62f), "Upgrade", "", 26, out upgradeLabel);
            RuntimeUI.SetAnchors((RectTransform)upgradeButton.transform, 0.1f, 0.9f);
            upgradeButton.onClick.AddListener(() => current?.TryUpgrade());

            targetingButton = RuntimeUI.CreateButton(RuntimeUI.TopRow(popup, ref y, 56f), "Targeting", "", 24, out targetingLabel);
            RuntimeUI.SetAnchors((RectTransform)targetingButton.transform, 0.1f, 0.9f);
            targetingButton.onClick.AddListener(() =>
            {
                if (current == null) return;
                current.Targeting = (TargetingMode)(((int)current.Targeting + 1) % 4);
            });

            poisonButton = RuntimeUI.CreateButton(RuntimeUI.TopRow(popup, ref y, 56f), "PoisonMode", "", 24, out poisonLabel);
            RuntimeUI.SetAnchors((RectTransform)poisonButton.transform, 0.1f, 0.9f);
            poisonButton.onClick.AddListener(() =>
            {
                if (current == null) return;
                current.PoisonTargeting = (PoisonMode)(((int)current.PoisonTargeting + 1) % 3);
            });

            sellButton = RuntimeUI.CreateButton(RuntimeUI.TopRow(popup, ref y, 62f), "Sell", "", 26, out sellLabel);
            RuntimeUI.SetAnchors((RectTransform)sellButton.transform, 0.1f, 0.9f);
            sellButton.onClick.AddListener(() =>
            {
                Tower tower = current;
                if (tower == null) return;
                TowerSlot slot = tower.GetComponentInParent<TowerSlot>();
                if (!tower.TrySell()) return;
                if (slot != null) slot.ClearTower();
                Close();
            });
        }
    }
}
