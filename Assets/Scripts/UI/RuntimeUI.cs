using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense
{
    // Helpers for the windows that are built in code (rewards, upgrades). With a UISkin the
    // windows get the designer's paper frame, buttons and font; text stays on dark plaques
    // so the rarity and currency colors read the same either way.
    public static class RuntimeUI
    {
        public static readonly Color PanelColor = new Color(0.055f, 0.10f, 0.09f, 0.98f);
        public static readonly Color RowColor = new Color(0.105f, 0.17f, 0.15f, 1f);
        public static readonly Color BarColor = new Color(0.035f, 0.065f, 0.06f, 1f);
        public static readonly Color AccentColor = new Color(0.20f, 0.39f, 0.30f, 1f);
        public static readonly Color DisabledColor = new Color(0.14f, 0.19f, 0.17f, 1f);
        public static readonly Color CoinColor = new Color(1f, 0.83f, 0.30f, 1f);
        // Tints for the skinned button sprite.
        private static readonly Color SkinnedDisabled = new Color(0.45f, 0.45f, 0.45f, 1f);
        private static readonly Color SkinnedUnselected = new Color(0.65f, 0.65f, 0.65f, 1f);
        // Width of the paper frame left visible around the dark inner panel.
        private const float FrameInset = 16f;

        private const string CoinTag = "<color=#FFD44D>";
        private const string CrystalTag = "<color=#C084FF>";

        private static Font font;
        private static readonly List<GameObject> windows = new List<GameObject>();

        // Only one window is shown at a time: call before opening another.
        public static void CloseAllWindows()
        {
            windows.RemoveAll(window => window == null);
            foreach (GameObject window in windows)
                window.SetActive(false);
        }

        private static Font Font
        {
            get
            {
                if (font == null)
                {
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (font.material.mainTexture != null) font.material.mainTexture.filterMode = FilterMode.Bilinear;
                }
                return font;
            }
        }

        private static UISkin Skin => UISkin.Current;

        private static bool SkinnedButtons => false;

        public static string CoinsText(int amount) => $"{CoinTag}{amount} монет</color>";

        public static string CrystalsText(int amount) => $"{CrystalTag}{amount} крист.</color>";

        public static string RewardText(int coins, int crystals)
        {
            if (crystals == 0) return "+" + CoinsText(coins);
            if (coins == 0) return "+" + CrystalsText(crystals);
            return $"+{CoinsText(coins)} +{CrystalsText(crystals)}";
        }

        public static string WalletText()
        {
            return $"Монеты: {CoinTag}{MetaProgress.Coins}</color>    Кристаллы: {CrystalTag}{MetaProgress.Crystals}</color>";
        }

        // Dimmed full-screen panel with a centered window, a title, the wallet and a close
        // button. Returns the window; rows are then stacked into it with TopRow.
        public static Transform CreateWindow(Transform canvas, string title, float height,
            out GameObject panel, out Text wallet, out float y)
        {
            Image dim = CreateImage(canvas, title, new Color(0.015f, 0.025f, 0.025f, 0.8f));
            dim.raycastTarget = true;
            Stretch(dim.rectTransform);
            GameObject panelObject = dim.gameObject;
            panelObject.AddComponent<Button>().onClick.AddListener(() => panelObject.SetActive(false));
            Image window = CreateImage(dim.transform, "Window", PanelColor);
            window.raycastTarget = true;
            float visibleHeight = Mathf.Min(height, 900f);
            Place(window.transform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(1100f, visibleHeight));
            DecoratePanel(window);
            Text heading = CreateText(window.transform, title, 36, TextAnchor.MiddleLeft);
            heading.fontStyle = FontStyle.Bold;
            Place(heading.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -22), new Vector2(900, 48));
            wallet = CreateText(window.transform, "", 24, TextAnchor.MiddleLeft);
            wallet.color = new Color(0.65f, 0.75f, 0.69f);
            Place(wallet.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -72), new Vector2(940, 32));
            Button close = CreateButton(window.transform, "CloseButton", "×", 34, out _);
            StyleMenuButton(close);
            Place(close.transform, Vector2.one, Vector2.one, new Vector2(-24, -24), new Vector2(56, 56));
            close.onClick.AddListener(() => panelObject.SetActive(false));

            Image viewport = CreateImage(window.transform, "Viewport", Color.clear);
            viewport.raycastTarget = true;
            Stretch(viewport.rectTransform);
            viewport.rectTransform.offsetMin = new Vector2(16, 24);
            viewport.rectTransform.offsetMax = new Vector2(-16, -120);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport.transform, false);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = new Vector2(0, height - 110);
            content.anchoredPosition = Vector2.zero;
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport.rectTransform; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 45;
            y = 0;
            windows.Add(panelObject);
            panelObject.SetActive(false);
            panel = panelObject;
            return content;
        }

        public static void DecoratePanel(Image panel)
        {
            panel.sprite = null;
            panel.color = PanelColor;
            Outline border = panel.gameObject.GetComponent<Outline>() ?? panel.gameObject.AddComponent<Outline>();
            border.effectColor = new Color(0.55f, 0.47f, 0.29f, 0.65f);
            border.effectDistance = new Vector2(1, -1);
            Shadow shadow = panel.gameObject.GetComponent<Shadow>() ?? panel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.3f);
            shadow.effectDistance = new Vector2(0, -5);
        }
        public static Text CreateHeader(Transform window, ref float y, string text)
        {
            y += 10f;
            Text header = CreateText(TopRow(window, ref y, 46f), text, 28, TextAnchor.MiddleLeft);
            header.fontStyle = FontStyle.Bold;
            return header;
        }

        // Next full-width slot of the given height, stacked from the top of the window.
        public static Transform TopRow(Transform window, ref float y, float height)
        {
            RectTransform rect = new GameObject("Row", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(window, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -y);
            rect.sizeDelta = new Vector2(-60f, height);
            y += height + 8f;
            return rect;
        }

        public static Transform CreateRowBackground(Transform slot)
        {
            Image background = CreateImage(slot, "Background", RowColor);
            Stretch(background.rectTransform);
            return background.transform;
        }

        public static Image CreateImage(Transform parent, string name, Color color)
        {
            Image image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Text CreateText(Transform parent, string content, int size, TextAnchor alignment)
        {
            Text text = new GameObject("Text", typeof(RectTransform)).AddComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = Font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = new Color(0.93f, 0.93f, 0.85f);
            text.text = content;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            Stretch(text.rectTransform);
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label, int size, out Text labelText)
        {
            Image image = CreateImage(parent, name, SkinnedButtons ? Color.white : AccentColor);
            image.raycastTarget = true;
            if (SkinnedButtons) image.sprite = Skin.button;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.18f, 1.18f, 1.12f);
            colors.pressedColor = new Color(0.75f, 0.85f, 0.75f);
            colors.disabledColor = new Color(0.6f, 0.65f, 0.6f);
            colors.fadeDuration = 0.12f;
            button.colors = colors;
            Navigation navigation = button.navigation; navigation.mode = Navigation.Mode.None; button.navigation = navigation;
            labelText = CreateText(image.transform, label, size, TextAnchor.MiddleCenter);
            if (SkinnedButtons) labelText.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            labelText.rectTransform.offsetMin = new Vector2(10, 5);
            labelText.rectTransform.offsetMax = new Vector2(-10, -5);
            return button;
        }

        public static void SetButtonState(Button button, bool active)
        {
            button.interactable = active;
            if (SkinnedButtons) button.image.color = active ? Color.white : SkinnedDisabled;
            else button.image.color = active ? AccentColor : DisabledColor;
        }

        // Tab buttons stay clickable; the selected one is highlighted.
        public static void SetTabState(Button button, bool selected)
        {
            if (SkinnedButtons) button.image.color = selected ? Color.white : SkinnedUnselected;
            else button.image.color = selected ? AccentColor : RowColor;
        }

        // Buttons that open windows, kept dim in the flat style so they don't look like rewards.
        public static void StyleMenuButton(Button button)
        {
            if (!SkinnedButtons) button.image.color = RowColor;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // Horizontal span as fractions of the parent, full height minus a vertical margin.
        public static void SetAnchors(RectTransform rect, float xMin, float xMax, float verticalMargin = 0f)
        {
            rect.anchorMin = new Vector2(xMin, 0f);
            rect.anchorMax = new Vector2(xMax, 1f);
            rect.offsetMin = new Vector2(0f, verticalMargin);
            rect.offsetMax = new Vector2(0f, -verticalMargin);
        }

        public static void Place(Transform transform, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            RectTransform rect = (RectTransform)transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
