using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense
{
    // Towers window on the meta canvas: the whole collection with rarity, level and
    // blueprints, the deck (towers taken into battle), upgrades for blueprints + coins,
    // and the base upgrades.
    // Added by RetentionUI, which owns the canvas.
    public class UpgradeUI : MonoBehaviour
    {
        private class Row
        {
            public string id;
            public Text title;
            public Button button;
            public Text buttonLabel;
            public Button deckButton;
            public Text deckLabel;
        }

        private GameObject openButton;
        private GameObject panel;
        private Text wallet;
        private Text deckHeader;
        private readonly List<Row> towerRows = new List<Row>();
        private readonly List<Row> baseRows = new List<Row>();

        private void Awake()
        {
            Build();
            MetaProgress.OnChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            MetaProgress.OnChanged -= Refresh;
        }

        public void Refresh()
        {
            // Upgrades apply when a level starts, so they are bought between levels.
            bool inLevel = GameManager.Instance != null;
            openButton.SetActive(!inLevel);
            if (inLevel) panel.SetActive(false);

            wallet.text = RuntimeUI.WalletText();
            deckHeader.text = $"В бой: {MetaProgress.DeckCount}/{MetaProgress.DeckSize}  <size=22>— только эти башни можно строить в уровне</size>";
            foreach (Row row in towerRows)
                RefreshTower(row);
            foreach (Row row in baseRows)
                RefreshBase(row);
        }

        private static void RefreshTower(Row row)
        {
            TowerDefinition tower = TowerCatalog.Find(row.id);
            int level = MetaProgress.GetTowerLevel(row.id);
            bool maxed = MetaProgress.IsTowerMaxed(row.id);

            string details;
            if (level == 0)
            {
                details = tower.campaignLevel > 0
                    ? $"Закрыта · награда за уровень {tower.campaignLevel} или чертёж из сундука"
                    : "Закрыта · чертежи выпадают из сундуков";
            }
            else
            {
                details = $"Ур. {level}/{TowerCatalog.MaxLevel} · урон +{MetaProgress.DamageBonusPercent(row.id)}%";
                if (!maxed)
                    details += $" · чертежи {MetaProgress.GetTowerCopies(row.id)}/{MetaProgress.CopiesNeeded(row.id)}";
            }
            if (!tower.playable) details += " · скоро в игре";

            row.title.text = $"<b>{tower.title}</b>   {TowerCatalog.ColoredRarity(tower.rarity)}\n<size=20>{details}</size>";
            if (level == 0) row.buttonLabel.text = "Закрыта";
            else if (maxed) row.buttonLabel.text = "Максимум";
            else row.buttonLabel.text = $"Улучшить\n{RuntimeUI.CoinsText(MetaProgress.TowerUpgradeCost(row.id))}";
            RuntimeUI.SetButtonState(row.button, MetaProgress.CanUpgradeTower(row.id));

            bool inDeck = MetaProgress.IsInDeck(row.id);
            row.deckButton.gameObject.SetActive(level > 0 && tower.playable);
            row.deckLabel.text = inDeck ? "В бою" : "В бой";
            // The selected state is highlighted; the last tower in the deck can't be removed.
            RuntimeUI.SetTabState(row.deckButton, inDeck);
            row.deckButton.interactable = inDeck ? MetaProgress.DeckCount > 1 : MetaProgress.CanAddToDeck(row.id);
        }

        private static void RefreshBase(Row row)
        {
            UpgradeDefinition upgrade = System.Array.Find(MetaProgress.BaseUpgrades, u => u.id == row.id);
            bool maxed = MetaProgress.IsMaxed(row.id);
            int cost = MetaProgress.GetUpgradeCost(row.id);

            row.title.text = $"<b>{upgrade.title}</b>   ур. {MetaProgress.GetLevel(row.id)}/{MetaProgress.MaxUpgradeLevel}\n<size=20>{upgrade.effect}</size>";
            row.buttonLabel.text = maxed ? "Максимум" : $"Улучшить\n{RuntimeUI.CoinsText(cost)}";
            RuntimeUI.SetButtonState(row.button, !maxed && MetaProgress.Coins >= cost);
        }

        private void Build()
        {
            Button open = RuntimeUI.CreateButton(transform, "TowersButton", "Башни", 30, out _);
            RuntimeUI.StyleMenuButton(open);
            RuntimeUI.Place(open.transform, Vector2.zero, Vector2.zero, new Vector2(264f, 48f), new Vector2(180f, 64f));
            open.onClick.AddListener(Toggle);
            openButton = open.gameObject;

            Transform w = RuntimeUI.CreateWindow(transform, "Башни", 1040f, out panel, out wallet, out float y);

            deckHeader = RuntimeUI.CreateText(RuntimeUI.TopRow(w, ref y, 46f), "", 26, TextAnchor.MiddleLeft);
            foreach (TowerDefinition tower in TowerCatalog.Towers)
            {
                string id = tower.id;
                Row row = CreateRow(w, ref y, id, TowerCatalog.RarityColor(tower.rarity));
                row.button.onClick.AddListener(() => MetaProgress.TryUpgradeTower(id));
                RuntimeUI.SetAnchors(row.title.rectTransform, 0.03f, 0.58f);
                row.deckButton = RuntimeUI.CreateButton(row.title.transform.parent, "Deck", "", 22, out row.deckLabel);
                RuntimeUI.SetAnchors((RectTransform)row.deckButton.transform, 0.60f, 0.74f, 6f);
                row.deckButton.onClick.AddListener(() => MetaProgress.ToggleDeck(id));
                towerRows.Add(row);
            }

            RuntimeUI.CreateHeader(w, ref y, "База");
            foreach (UpgradeDefinition upgrade in MetaProgress.BaseUpgrades)
            {
                string id = upgrade.id;
                Row row = CreateRow(w, ref y, id, RuntimeUI.DisabledColor);
                row.button.onClick.AddListener(() => MetaProgress.TryUpgrade(id));
                baseRows.Add(row);
            }
        }

        // A row with a colored stripe on the left, two lines of text and a button.
        private static Row CreateRow(Transform window, ref float y, string id, Color stripeColor)
        {
            Transform background = RuntimeUI.CreateRowBackground(RuntimeUI.TopRow(window, ref y, 70f));
            Image stripe = RuntimeUI.CreateImage(background, "Stripe", stripeColor);
            RuntimeUI.SetAnchors(stripe.rectTransform, 0f, 0.012f);

            Text title = RuntimeUI.CreateText(background, "", 26, TextAnchor.MiddleLeft);
            RuntimeUI.SetAnchors(title.rectTransform, 0.03f, 0.74f);
            Button button = RuntimeUI.CreateButton(background, "Upgrade", "", 22, out Text buttonLabel);
            RuntimeUI.SetAnchors((RectTransform)button.transform, 0.76f, 0.98f, 6f);

            return new Row { id = id, title = title, button = button, buttonLabel = buttonLabel };
        }

        private void Toggle()
        {
            bool show = !panel.activeSelf;
            RuntimeUI.CloseAllWindows();
            Refresh();
            panel.SetActive(show);
        }
    }
}
