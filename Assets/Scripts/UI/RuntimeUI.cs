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
        public static readonly Color PanelColor = new Color(0.17f, 0.11f, 0.06f, 0.92f);
        public static readonly Color RowColor = new Color(0.29f, 0.19f, 0.11f, 1f);
        public static readonly Color BarColor = new Color(0.09f, 0.05f, 0.03f, 1f);
        public static readonly Color AccentColor = new Color(0.20f, 0.75f, 0.50f, 1f);
        public static readonly Color DisabledColor = new Color(0.30f, 0.33f, 0.40f, 1f);
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
                    font = Skin != null && Skin.font != null ? Skin.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        private static UISkin Skin => UISkin.Current;

        private static bool SkinnedButtons => Skin != null && Skin.button != null;

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
            // The dim blocks clicks to the game and closes the window.
            Image dim = CreateImage(canvas, title, new Color(0f, 0f, 0f, 0.6f));
            dim.raycastTarget = true;
            Stretch(dim.rectTransform);
            GameObject panelObject = dim.gameObject;
            panelObject.AddComponent<Button>().onClick.AddListener(() => panelObject.SetActive(false));

            Image window = CreateImage(panelObject.transform, "Window", PanelColor);
            window.raycastTarget = true;
            Place(window.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, height));
            if (Skin != null && Skin.windowFrame != null)
            {
                window.sprite = Skin.windowFrame;
                window.type = Image.Type.Sliced;
                window.pixelsPerUnitMultiplier = Skin.framePixelsPerUnitMultiplier;
                window.color = Color.white;
                Image inner = CreateImage(window.transform, "Inner", PanelColor);
                Stretch(inner.rectTransform);
                inner.rectTransform.offsetMin = new Vector2(FrameInset, FrameInset);
                inner.rectTransform.offsetMax = new Vector2(-FrameInset, -FrameInset);
            }

            y = 20f;
            Transform header = TopRow(window.transform, ref y, 60f);
            Text titleText = CreateText(header, title, 38, TextAnchor.MiddleLeft);
            titleText.fontStyle = FontStyle.Bold;
            wallet = CreateText(header, "", 26, TextAnchor.MiddleRight);
            SetAnchors(wallet.rectTransform, 0.4f, 0.9f);

            Button close = CreateButton(window.transform, "CloseButton", "X", 30, out _);
            if (!SkinnedButtons) close.image.color = DisabledColor;
            Place(close.transform, Vector2.one, Vector2.one, new Vector2(-20f, -20f), new Vector2(60f, 60f));
            close.onClick.AddListener(() => panelObject.SetActive(false));

            y += 10f;
            windows.Add(panelObject);
            panelObject.SetActive(false);
            panel = panelObject;
            return window.transform;
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
            text.color = Color.white;
            text.text = content;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
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
            button.transition = Selectable.Transition.None;
            labelText = CreateText(image.transform, label, size, TextAnchor.MiddleCenter);
            if (SkinnedButtons) labelText.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.8f);
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
