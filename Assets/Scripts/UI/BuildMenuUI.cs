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
        public static bool IsOpen => Instance != null && Instance.panel != null && Instance.panel.gameObject.activeInHierarchy;

        [SerializeField] private RectTransform panel;
        [SerializeField] private Camera worldCamera;


        private class Option
        {
            public TowerData data;
            public GameObject prefab;
        }

        private readonly List<Option> options = new List<Option>();
        private readonly List<Cell> cells = new List<Cell>();
        private TowerSlot currentSlot;
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
            public Text lockLabel;
            public BuildMenuUI menu;
            public bool locked;

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (menu == null || option == null) return;

                if (!locked && menu.rangeCircle != null)
                    menu.rangeCircle.Show(option.data.range);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (menu == null) return;

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

            if (panel.TryGetComponent<LayoutGroup>(out var legacyLayout)) legacyLayout.enabled = false;
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
            var surface = panel.GetComponent<Image>();
            if (surface != null)
            {
                RuntimeUI.DecoratePanel(surface);
                Color opaque = RuntimeUI.PanelColor; opaque.a = 1f; surface.color = opaque;
            }
            var layer = panel.gameObject.AddComponent<Canvas>();
            layer.overrideSorting = true;
            layer.sortingOrder = 105;
            panel.gameObject.AddComponent<GraphicRaycaster>();
            Text heading = RuntimeUI.CreateText(panel, "Построить башню", 26, TextAnchor.MiddleLeft);
            RuntimeUI.SetAnchors(heading.rectTransform, 0.015f, 0.145f);
            int count = Mathf.Max(1, options.Count);
            float span = 0.78f / Mathf.Max(3, count);
            foreach (var option in options)
            {
                int index = cells.Count;
                Image card = RuntimeUI.CreateImage(panel, option.data.name, RuntimeUI.RowColor);
                card.raycastTarget = true;
                float left = 0.15f + index * span;
                RuntimeUI.SetAnchors(card.rectTransform, left, left + span - 0.008f, 8);
                Cell cell = card.gameObject.AddComponent<Cell>();
                cell.menu = this; cell.option = option; cell.background = card;
                cell.icon = RuntimeUI.CreateImage(card.transform, "Tower", Color.white);
                cell.icon.sprite = option.data.baseSprite; cell.icon.preserveAspect = true;
                RuntimeUI.SetAnchors(cell.icon.rectTransform, 0.02f, 0.31f, 5);
                string name = TowerCatalog.Find(option.data.name)?.title ?? option.data.displayName;
                Text title = RuntimeUI.CreateText(card.transform, name, 22, TextAnchor.MiddleLeft);
                Band(title.rectTransform, 0.55f, 0.98f);
                cell.cost = RuntimeUI.CreateText(card.transform, $"{option.data.cost} маны", 23, TextAnchor.MiddleLeft);
                Band(cell.cost.rectTransform, 0.28f, 0.57f);
                cell.lockOverlay = RuntimeUI.CreateImage(card.transform, "Availability", Color.clear);
                Band(cell.lockOverlay.rectTransform, 0.02f, 0.29f);
                cell.lockLabel = RuntimeUI.CreateText(cell.lockOverlay.transform, "", 19, TextAnchor.MiddleLeft);
                cell.lockLabel.color = new Color(1, 0.76f, 0.64f);
                cells.Add(cell);
            }
            var close = RuntimeUI.CreateButton(panel, "CloseBuild", "×", 32, out _);
            RuntimeUI.SetAnchors((RectTransform)close.transform, 0.94f, 0.99f, 10);
            close.onClick.AddListener(Close);
        }

        private static void Band(RectTransform rect, float bottom, float top)
        {
            rect.anchorMin = new Vector2(0.33f, bottom);
            rect.anchorMax = new Vector2(0.98f, top);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        private void Update()
        {
            if (panel == null || currentSlot == null || !panel.gameObject.activeSelf) return;
            RefreshStates();
        }

        private void RefreshStates()
        {
            GameManager game = GameManager.Instance;
            int currency = game != null ? game.Currency : 0;
            foreach (Cell cell in cells)
            {
                if (cell.option == null) continue;
                string reason = currentSlot.BuildRestriction(cell.option.data, cell.option.prefab);
                cell.locked = reason != null;
                cell.lockOverlay.gameObject.SetActive(cell.locked);
                cell.lockLabel.text = reason ?? "";
                bool affordable = !cell.locked;
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

            panel.anchorMin = new Vector2(0, 0.86f); panel.anchorMax = Vector2.one;
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            rangeCircle.transform.SetParent(null, true);
            rangeCircle.transform.position = slot.transform.position;
            rangeCircle.transform.localScale = Vector3.one;
            BattleUI.CloseTower();
            blocker.transform.SetAsLastSibling();
            panel.transform.SetAsLastSibling();
            blocker.SetActive(true);
            panel.gameObject.SetActive(true);
            RefreshStates();
        }

        public void Close()
        {
            currentSlot = null;
            if (panel != null) panel.gameObject.SetActive(false);
            if (blocker != null) blocker.SetActive(false);

            rangeCircle?.Hide();
            if (rangeCircle != null) rangeCircle.transform.SetParent(transform, false);
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
