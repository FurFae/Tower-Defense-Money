using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TowerDefense
{
    // Build menu for an empty slot: a grid of tower icons (4 per row) with the cost on
    // the icon, availability highlighting and a hover tooltip (role, damage, effects,
    // attack radius). The old scene used one text button per tower; this rebuilds the
    // panel in code from the buttons' (TowerData, prefab) pairs, so the scene stays as is.
    public class BuildMenuUI : MonoBehaviour
    {
        public static BuildMenuUI Instance { get; private set; }

        [SerializeField] private RectTransform panel;
        [SerializeField] private Camera worldCamera;

        private const int Columns = 4;
        private const float CellSize = 110f;
        private const float CellGap = 12f;

        private class Option
        {
            public TowerData data;
            public GameObject prefab;
        }

        private readonly List<Option> options = new List<Option>();
        private readonly List<Cell> cells = new List<Cell>();
        private TowerSlot currentSlot;
        private RectTransform grid;
        private Text tooltipText;
        private GameObject tooltip;
        private GameObject blocker;
        private RangeCircle rangeCircle;
        private bool built;

        private class Cell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
        {
            public Option option;
            public Image background;
            public Image icon;
            public Text cost;
            public Image lockOverlay;
            public BuildMenuUI menu;
            public bool locked;

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (menu == null || option == null) return;
                menu.ShowTooltip(option.data, locked);
                if (!locked && menu.rangeCircle != null)
                    menu.rangeCircle.Show(option.data.range);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (menu == null) return;
                menu.HideTooltip();
                menu.rangeCircle?.Hide();
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (menu == null || locked) return;
                menu.BuildSelected(option.data, option.prefab);
            }
        }

        private void Awake()
        {
            Instance = this;
            if (worldCamera == null) worldCamera = Camera.main;
            if (panel != null) panel.gameObject.SetActive(false);
            rangeCircle = RangeCircle.Create(transform, new Color(1f, 1f, 1f, 0.6f));
            rangeCircle.Hide();
        }

        // The panel's old text buttons carry the only data->prefab mapping; harvest them.
        // Only the deck (колода из 4 башен) is shown — everything else is hidden, not greyed out.
        private void EnsureBuilt()
        {
            if (built) return;
            built = true;

            if (panel == null)
            {
                Debug.LogWarning("[BuildMenuUI] panel is not wired in the scene");
                return;
            }

            BuildBlocker();

            List<BuildOptionButton> oldButtons = new List<BuildOptionButton>(panel.GetComponentsInChildren<BuildOptionButton>(true));
            foreach (BuildOptionButton button in oldButtons)
            {
                Option option = new Option { data = button.TowerData, prefab = button.TowerPrefab };
                bool inDeck = option.data != null && MetaProgress.IsInDeck(option.data.name);
                if (inDeck && option.prefab != null) options.Add(option);
                Destroy(button.gameObject);
            }
            BuildGrid();
        }

        // Клик по любому пустому месту экрана закрывает меню (как у панели башни).
        private void BuildBlocker()
        {
            Image catcher = RuntimeUI.CreateImage(panel.parent, "BuildMenuBlocker", new Color(0f, 0f, 0f, 0f));
            catcher.raycastTarget = true;
            RuntimeUI.Stretch(catcher.rectTransform);
            catcher.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            blocker = catcher.gameObject;
            blocker.transform.SetSiblingIndex(panel.GetSiblingIndex());
            blocker.SetActive(false);
        }

        private void BuildGrid()
        {
            int rows = Mathf.Max(1, Mathf.CeilToInt(options.Count / (float)Columns));
            float width = Columns * CellSize + (Columns - 1) * CellGap + 28f;
            float height = rows * (CellSize + 28f) + (rows - 1) * CellGap + 28f;
            panel.sizeDelta = new Vector2(width, height);

            GameObject gridObject = new GameObject("Grid", typeof(RectTransform));
            grid = (RectTransform)gridObject.transform;
            grid.SetParent(panel, false);
            RuntimeUI.Place(grid, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(Columns * CellSize + (Columns - 1) * CellGap, rows * (CellSize + 28f) + (rows - 1) * CellGap));

            foreach (Option option in options)
            {
                int index = cells.Count;
                int column = index % Columns;
                int row = index / Columns;

                GameObject cellObject = new GameObject($"Cell_{option.data.name}", typeof(RectTransform));
                RectTransform cellRect = (RectTransform)cellObject.transform;
                cellRect.SetParent(grid, false);
                cellRect.anchorMin = new Vector2(0f, 1f);
                cellRect.anchorMax = new Vector2(0f, 1f);
                cellRect.pivot = new Vector2(0f, 1f);
                cellRect.anchoredPosition = new Vector2(column * (CellSize + CellGap), -row * (CellSize + 28f + CellGap));
                cellRect.sizeDelta = new Vector2(CellSize, CellSize + 28f);

                Cell cell = cellObject.AddComponent<Cell>();
                cell.option = option;
                cell.menu = this;

                cell.background = RuntimeUI.CreateImage(cellRect, "Background", RuntimeUI.RowColor);
                cell.background.raycastTarget = true;

                cell.icon = RuntimeUI.CreateImage(cellRect, "Icon", Color.white);
                cell.icon.sprite = option.data.baseSprite;
                cell.icon.preserveAspect = true;
                cell.icon.raycastTarget = false;
                RuntimeUI.Place(cell.icon.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(CellSize - 12f, CellSize - 12f));

                cell.cost = RuntimeUI.CreateText(cellRect, $"{option.data.cost}", 24, TextAnchor.MiddleCenter);
                cell.cost.fontStyle = FontStyle.Bold;
                cell.cost.raycastTarget = false;
                RuntimeUI.Place(cell.cost.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(6f, 2f), new Vector2(CellSize - 12f, 26f));

                cell.lockOverlay = RuntimeUI.CreateImage(cellRect, "Lock", new Color(0f, 0f, 0f, 0.65f));
                cell.lockOverlay.raycastTarget = false;
                RuntimeUI.Place(cell.lockOverlay.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, cellRect.sizeDelta);
                Text lockText = RuntimeUI.CreateText(cell.lockOverlay.transform, "🔒", 30, TextAnchor.MiddleCenter);
                lockText.raycastTarget = false;

                cells.Add(cell);
            }

            BuildTooltip();
        }

        private void BuildTooltip()
        {
            tooltip = RuntimeUI.CreateImage(panel, "Tooltip", RuntimeUI.PanelColor).gameObject;
            tooltip.SetActive(false);
            RectTransform rect = (RectTransform)tooltip.transform;
            rect.SetParent(panel, false);
            tooltipText = RuntimeUI.CreateText(rect, "", 22, TextAnchor.UpperLeft);
            tooltipText.rectTransform.offsetMin = new Vector2(12f, 8f);
            tooltipText.rectTransform.offsetMax = new Vector2(-12f, -8f);
        }

        private void ShowTooltip(TowerData data, bool locked)
        {
            if (tooltip == null) return;
            TowerDefinition definition = TowerCatalog.Find(data.name);
            string title = definition != null ? TowerCatalog.ColoredTitle(definition) : $"<b>{data.displayName}</b>";
            string role = DescribeRole(data);
            string dps = $"Урон {data.damage:0.#} · {data.fireRate:0.##}/с (ДПС {data.damage * data.fireRate:0.#}) · радиус {data.range:0.#}";
            tooltipText.text = locked
                ? $"{title}\n<size=20>Нужно взять эту башню в колоду перед боем</size>"
                : $"{title}\n<size=20>{role}\n{dps}{DescribeEffects(data)}</size>";
            tooltip.SetActive(true);
            // Place the tooltip above the panel, clamped to the panel's width.
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)tooltip.transform);
            float tooltipHeight = 120f;
            RectTransform tooltipRect = (RectTransform)tooltip.transform;
            tooltipRect.anchorMin = tooltipRect.anchorMax = new Vector2(0.5f, 0f);
            tooltipRect.pivot = new Vector2(0.5f, 0f);
            tooltipRect.anchoredPosition = new Vector2(0f, panel.sizeDelta.y + 8f);
            tooltipRect.sizeDelta = new Vector2(panel.sizeDelta.x, tooltipHeight);
        }

        private void HideTooltip()
        {
            if (tooltip != null) tooltip.SetActive(false);
        }

        private static string DescribeRole(TowerData data)
        {
            if (data.chainCount > 0) return "Цепные удары по группе";
            if (data.splashRadius > 0f && data.burnDamagePerSecond > 0f) return "Поджог области";
            if (data.splashRadius > 0f) return "Урон по области";
            if (data.slowDuration > 0f) return "Контроль: замедление";
            if (data.damageOverTimePerSecond > 0f) return "Урон во времени (яд)";
            if (data.pierceCount > 0) return "Пробитие насквозь";
            return "Базовый урон";
        }

        private static string DescribeEffects(TowerData data)
        {
            string effects = "";
            if (data.damageOverTimePerSecond > 0f) effects += $"\nЯд {data.damageOverTimePerSecond:0.#}/с на {data.damageOverTimeDuration:0.#} с";
            if (data.slowDuration > 0f)
                effects += $"\nЗамедление {Mathf.RoundToInt((1f - data.slowMultiplier) * 100f)}% на {data.slowDuration:0.#} с";
            if (data.burnDamagePerSecond > 0f) effects += $"\nПоджог {data.burnDamagePerSecond:0.#}/с";
            if (data.pierceCount > 0) effects += $"\nПробитие: {data.pierceCount} цели";
            if (data.chainCount > 0) effects += $"\nЦепь: {data.chainCount} цели в радиусе {data.chainRadius:0.#}";
            if (data.splashRadius > 0f) effects += $"\nВзрыв: радиус {data.splashRadius:0.#}";
            if (!data.canHitFlying) effects += "\nНе бьёт летающих";
            return effects;
        }

        private void Update()
        {
            if (panel == null || !panel.gameObject.activeSelf) return;
            RefreshStates();
        }

        private void RefreshStates()
        {
            GameManager game = GameManager.Instance;
            int currency = game != null ? game.Currency : 0;
            foreach (Cell cell in cells)
            {
                if (cell.option == null) continue;
                TowerDefinition definition = TowerCatalog.Find(cell.option.data.name);
                bool lockedByChallenge = Levels.ActiveChallenge == Challenge.RareOnly
                    && definition != null && definition.rarity < Rarity.Rare;
                cell.locked = !MetaProgress.IsInDeck(cell.option.data.name) || lockedByChallenge;
                cell.lockOverlay.enabled = cell.locked;
                bool affordable = !cell.locked && currency >= cell.option.data.cost;
                cell.background.color = cell.locked ? RuntimeUI.DisabledColor
                    : affordable ? RuntimeUI.RowColor : new Color(0.45f, 0.16f, 0.12f, 1f);
                cell.icon.color = cell.locked || !affordable ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
                cell.cost.color = affordable ? RuntimeUI.CoinColor : new Color(1f, 0.4f, 0.35f, 1f);
            }
        }

        public void Open(TowerSlot slot)
        {
            currentSlot = slot;
            if (panel == null) return;
            EnsureBuilt();

            Vector3 screenPos = worldCamera.WorldToScreenPoint(slot.transform.position);
            screenPos.y += 50f;
            panel.position = screenPos;
            KeepOnScreen();
            BattleUI.CloseTower();
            blocker.transform.SetAsLastSibling();
            panel.transform.SetAsLastSibling();
            blocker.SetActive(true);
            panel.gameObject.SetActive(true);
            RefreshStates();
        }

        private void KeepOnScreen()
        {
            // Panel is screen-space; nudge it back if it would leave the viewport.
            Vector3 pos = panel.position;
            float halfW = panel.sizeDelta.x * 0.5f;
            float halfH = panel.sizeDelta.y * 0.5f;
            pos.x = Mathf.Clamp(pos.x, halfW, Screen.width - halfW);
            pos.y = Mathf.Clamp(pos.y, halfH + 60f, Screen.height - halfH - 60f);
            panel.position = pos;
        }

        public void Close()
        {
            currentSlot = null;
            if (panel != null) panel.gameObject.SetActive(false);
            if (blocker != null) blocker.SetActive(false);
            HideTooltip();
            rangeCircle?.Hide();
        }

        public void BuildSelected(TowerData data, GameObject prefab)
        {
            if (currentSlot == null) return;
            if (currentSlot.Build(data, prefab))
            {
                GameSfx.Play(GameSfx.Event.Build, currentSlot.transform.position);
                Close();
            }
        }
    }
}
