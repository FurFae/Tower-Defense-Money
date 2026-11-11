using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense
{
    // Shop window on the meta canvas, one tab per category: blueprints for coins,
    // chests for crystals, crystal-to-coin exchange and crystal packs.
    // Added by RetentionUI, which owns the canvas.
    public class ShopUI : MonoBehaviour
    {
        private static readonly string[] TabNames = { "Чертежи", "Сундуки", "Кристаллы → монеты" };

        private GameObject openButton;
        private GameObject panel;
        private Text wallet;
        private readonly List<Button> tabButtons = new List<Button>();
        private readonly List<GameObject> tabs = new List<GameObject>();
        private readonly List<Text> offerTitles = new List<Text>();
        private readonly List<Button> offerButtons = new List<Button>();
        private readonly List<Text> offerLabels = new List<Text>();
        private readonly List<Button> chestButtons = new List<Button>();
        private readonly List<Button> exchangeButtons = new List<Button>();
        private readonly List<Button> packButtons = new List<Button>();
        private int currentTab;

        private void Awake()
        {
            Build();
            MetaProgress.OnChanged += Refresh;
            SelectTab(0);
        }

        private void OnDestroy()
        {
            MetaProgress.OnChanged -= Refresh;
        }

        public void Refresh()
        {
            bool inLevel = GameManager.Instance != null;
            openButton.SetActive(!inLevel);
            if (inLevel) panel.SetActive(false);

            wallet.text = RuntimeUI.WalletText();

            BlueprintOffer[] offers = Shop.Offers;
            for (int i = 0; i < offers.Length; i++)
            {
                bool bought = Shop.IsOfferBought(i);
                offerTitles[i].text = $"{TowerCatalog.ColoredTitle(offers[i].tower)} ×{offers[i].amount}\n<size=20>чертежи · {TowerCatalog.ColoredRarity(offers[i].tower.rarity)}</size>";
                offerLabels[i].text = bought ? "Куплено" : RuntimeUI.CoinsText(offers[i].price);
                RuntimeUI.SetButtonState(offerButtons[i], !bought && MetaProgress.Coins >= offers[i].price);
            }

            for (int i = 0; i < chestButtons.Count; i++)
                RuntimeUI.SetButtonState(chestButtons[i], MetaProgress.Crystals >= Chests.ShopChests[i].price);
            for (int i = 0; i < exchangeButtons.Count; i++)
                RuntimeUI.SetButtonState(exchangeButtons[i], MetaProgress.Crystals >= Shop.Exchanges[i][0]);
            foreach (Button button in packButtons)
                RuntimeUI.SetButtonState(button, Shop.CanBuyCrystals);

            for (int i = 0; i < tabButtons.Count; i++)
                RuntimeUI.SetTabState(tabButtons[i], i == currentTab);
        }

        private void SelectTab(int index)
        {
            currentTab = index;
            for (int i = 0; i < tabs.Count; i++)
                tabs[i].SetActive(i == index);
            Refresh();
        }

        private void Build()
        {
            Button open = RuntimeUI.CreateButton(transform, "ShopButton", "Магазин", 30, out _);
            RuntimeUI.StyleMenuButton(open);
            RuntimeUI.Place(open.transform, Vector2.zero, Vector2.zero, new Vector2(464f, 48f), new Vector2(180f, 64f));
            open.onClick.AddListener(Toggle);
            openButton = open.gameObject;

            Transform w = RuntimeUI.CreateWindow(transform, "Магазин", 600f, out panel, out wallet, out float y);

            Transform tabRow = RuntimeUI.TopRow(w, ref y, 60f);
            for (int i = 0; i < TabNames.Length; i++)
            {
                int index = i;
                Button tab = RuntimeUI.CreateButton(tabRow, "Tab", TabNames[i], 26, out _);
                RuntimeUI.SetAnchors((RectTransform)tab.transform, i / (float)TabNames.Length + 0.004f, (i + 1) / (float)TabNames.Length - 0.004f);
                tab.onClick.AddListener(() => SelectTab(index));
                tabButtons.Add(tab);
            }
            y += 6f;

            BuildOffers(CreateTab(w), y);
            BuildChests(CreateTab(w), y);
            BuildExchange(CreateTab(w), y);

        }

        private Transform CreateTab(Transform window)
        {
            RectTransform tab = new GameObject("TabContent", typeof(RectTransform)).GetComponent<RectTransform>();
            tab.SetParent(window, false);
            RuntimeUI.Stretch(tab);
            tabs.Add(tab.gameObject);
            return tab;
        }

        private void BuildOffers(Transform tab, float y)
        {
            for (int i = 0; i < Shop.OfferCount; i++)
            {
                int index = i;
                Button button = CreateRow(tab, ref y, "", "", out Text title, out Text label);
                button.onClick.AddListener(() => Shop.TryBuyOffer(index));
                offerTitles.Add(title);
                offerButtons.Add(button);
                offerLabels.Add(label);
            }
            CreateNote(tab, ref y, "Цена указана за весь набор. Первый чертёж открывает башню, остальные идут на улучшения.");
        }

        private void BuildChests(Transform tab, float y)
        {
            foreach (ChestDefinition chest in Chests.ShopChests)
            {
                ChestDefinition current = chest;
                string details = $"{chest.blueprints} чертежей и монеты";
                if (chest.guaranteeRare) details += " · гарантирована редкая башня";
                Button button = CreateRow(tab, ref y, $"<b>{chest.title}</b>\n<size=20>{details}</size>",
                    RuntimeUI.CrystalsText(chest.price), out _, out _);
                button.onClick.AddListener(() =>
                {
                    if (Chests.TryBuy(current, out ChestResult result))
                        RewardPopup.Show(current.title, result.Describe());
                });
                chestButtons.Add(button);
            }
            CreateNote(tab, ref y, $"Каждый {Chests.GuaranteeEvery}-й сундук — с гарантией редкой башни.\n{Chests.OddsText}");
        }

        private void BuildExchange(Transform tab, float y)
        {
            for (int i = 0; i < Shop.Exchanges.Length; i++)
            {
                int index = i;
                string offer = $"{RuntimeUI.CoinsText(Shop.Exchanges[i][1])}";
                Button button = CreateRow(tab, ref y, offer, RuntimeUI.CrystalsText(Shop.Exchanges[i][0]), out _, out _);
                button.onClick.AddListener(() => Shop.TryExchange(index));
                exchangeButtons.Add(button);
            }
        }

        private void BuildCrystals(Transform tab, float y)
        {
            for (int i = 0; i < Shop.CrystalPacks.Length; i++)
            {
                int index = i;
                Button button = CreateRow(tab, ref y, RuntimeUI.CrystalsText(Shop.CrystalPacks[i][0]),
                    $"{Shop.CrystalPacks[i][1]} ян", out _, out _);
                button.onClick.AddListener(() => Shop.BuyCrystals(index));
                packButtons.Add(button);
            }
            CreateNote(tab, ref y, Shop.CanBuyCrystals
                ? "Тестовый режим: в редакторе кристаллы выдаются без оплаты."
                : "Покупки появятся после подключения платежей Яндекс Игр.");
        }

        private static Button CreateRow(Transform tab, ref float y, string titleText, string buttonText,
            out Text title, out Text buttonLabel)
        {
            Transform background = RuntimeUI.CreateRowBackground(RuntimeUI.TopRow(tab, ref y, 90f));
            title = RuntimeUI.CreateText(background, titleText, 28, TextAnchor.MiddleLeft);
            RuntimeUI.SetAnchors(title.rectTransform, 0.02f, 0.70f);
            Button button = RuntimeUI.CreateButton(background, "Buy", buttonText, 26, out buttonLabel);
            RuntimeUI.SetAnchors((RectTransform)button.transform, 0.72f, 0.98f, 12f);
            return button;
        }

        private static void CreateNote(Transform tab, ref float y, string text)
        {
            RuntimeUI.CreateText(RuntimeUI.TopRow(tab, ref y, 70f), text, 22, TextAnchor.MiddleCenter);
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
