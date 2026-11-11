using System;
using UnityEngine;

namespace TowerDefense
{
    public class BlueprintOffer
    {
        public TowerDefinition tower;
        public int amount;
        public int price; // coins
    }

    // Shop: daily blueprint offers for coins, crystal-to-coin exchange and crystal packs.
    // Chests are sold through Chests.TryBuy.
    public static class Shop
    {
        public const int OfferCount = 3;

        // { crystals, coins }
        public static readonly int[][] Exchanges =
        {
            new[] { 10, 100 },
            new[] { 50, 500 },
        };

        // { crystals, price in Yandex "yans" }
        public static readonly int[][] CrystalPacks =
        {
            new[] { 50, 29 },
            new[] { 150, 79 },
            new[] { 400, 179 },
        };

        // Blueprints per offer and coins per blueprint, by rarity (legendary is never sold).
        private static readonly int[] OfferAmounts = { 10, 5, 2, 1 };
        private static readonly int[] OfferUnitPrices = { 5, 15, 60, 300 };
        private const double EpicOfferChance = 0.2;

        private static BlueprintOffer[] offers;
        private static string offersDay;

        private static string Today => DateTime.Today.ToString("yyyy-MM-dd");

        // Three offers a day (common, uncommon, rare or sometimes epic), the same for the whole day.
        public static BlueprintOffer[] Offers
        {
            get
            {
                if (offers != null && offersDay == Today) return offers;

                offersDay = Today;
                DateTime day = DateTime.Today;
                System.Random random = new System.Random(day.Year * 10000 + day.Month * 100 + day.Day);
                Rarity last = random.NextDouble() < EpicOfferChance ? Rarity.Epic : Rarity.Rare;
                offers = new[]
                {
                    CreateOffer(Rarity.Common, random),
                    CreateOffer(Rarity.Uncommon, random),
                    CreateOffer(last, random),
                };
                return offers;
            }
        }

        private static BlueprintOffer CreateOffer(Rarity rarity, System.Random random)
        {
            TowerDefinition[] pool = Array.FindAll(TowerCatalog.Towers, tower => tower.playable && tower.rarity == rarity);
            int amount = OfferAmounts[(int)rarity];
            return new BlueprintOffer
            {
                tower = pool[random.Next(pool.Length)],
                amount = amount,
                price = amount * OfferUnitPrices[(int)rarity],
            };
        }

        private static void ResetDayIfNeeded()
        {
            MetaData data = MetaProgress.Data;
            if (data.shopDay == Today) return;
            data.shopDay = Today;
            data.shopBought.Clear();
        }

        public static bool IsOfferBought(int index)
        {
            ResetDayIfNeeded();
            return MetaProgress.Data.shopBought.Contains(index);
        }

        public static bool TryBuyOffer(int index)
        {
            BlueprintOffer offer = Offers[index];
            if (IsOfferBought(index) || !MetaProgress.TrySpendCoins(offer.price, false)) return false;
            MetaProgress.Data.shopBought.Add(index);
            MetaProgress.AddBlueprints(offer.tower.id, offer.amount);
            return true;
        }

        public static bool TryExchange(int index)
        {
            if (!MetaProgress.TrySpendCrystals(Exchanges[index][0], false)) return false;
            MetaProgress.Add(Exchanges[index][1], 0);
            return true;
        }

        // Real purchases need the PluginYG2 Payments module, which is not installed yet.
        // Until then crystal packs can only be "bought" in the editor, for testing.
        public static bool CanBuyCrystals => Application.isEditor;

        public static bool BuyCrystals(int index)
        {
            if (!CanBuyCrystals) return false;
            MetaProgress.Add(0, CrystalPacks[index][0]);
            return true;
        }
    }
}
