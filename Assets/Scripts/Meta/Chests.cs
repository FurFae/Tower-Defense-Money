using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TowerDefense
{
    public class ChestDefinition
    {
        public readonly string title;
        public readonly int blueprints;
        public readonly int price; // crystals
        public readonly bool guaranteeRare;

        public ChestDefinition(string title, int blueprints, int price, bool guaranteeRare)
        {
            this.title = title;
            this.blueprints = blueprints;
            this.price = price;
            this.guaranteeRare = guaranteeRare;
        }
    }

    public class ChestResult
    {
        public readonly List<TowerDefinition> towers = new List<TowerDefinition>();
        public readonly List<int> amounts = new List<int>();
        public readonly List<TowerDefinition> unlocked = new List<TowerDefinition>();
        public int coins;

        public void AddBlueprint(TowerDefinition tower)
        {
            int index = towers.IndexOf(tower);
            if (index < 0)
            {
                towers.Add(tower);
                amounts.Add(1);
            }
            else
            {
                amounts[index]++;
            }
        }

        public string Describe()
        {
            StringBuilder text = new StringBuilder();
            for (int i = 0; i < towers.Count; i++)
                text.AppendLine($"{TowerCatalog.ColoredTitle(towers[i])} ×{amounts[i]}");
            foreach (TowerDefinition tower in unlocked)
                text.AppendLine($"Новая башня: {TowerCatalog.ColoredTitle(tower)}");
            text.Append("+" + RuntimeUI.CoinsText(coins));
            return text.ToString();
        }
    }

    // Chests drop tower blueprints: each blueprint rolls a rarity, then a tower of it.
    public static class Chests
    {
        public static readonly ChestDefinition Victory = new ChestDefinition("Сундук за победу", 3, 0, false);

        public static readonly ChestDefinition[] ShopChests =
        {
            new ChestDefinition("Сундук", 10, 30, false),
            new ChestDefinition("Большой сундук", 30, 80, true),
        };

        // Drop weights per rarity, out of 1000: 60%, 25%, 11%, 3.5%, 0.5%.
        private static readonly int[] Weights = { 600, 250, 110, 35, 5 };
        // Every N-th bought chest has a guaranteed rare-or-better blueprint.
        public const int GuaranteeEvery = 10;
        private const int MinCoinsPerBlueprint = 3;
        private const int MaxCoinsPerBlueprint = 6;

        public static string OddsText => "Шансы: обычная 60%, необычная 25%, редкая 11%, эпическая 3,5%, легендарная 0,5%";

        public static ChestResult Open(ChestDefinition chest, bool guaranteeRare, bool save = true)
        {
            ChestResult result = new ChestResult();
            for (int i = 0; i < chest.blueprints; i++)
            {
                Rarity rarity = RollRarity(guaranteeRare && i == 0 ? Rarity.Rare : Rarity.Common);
                TowerDefinition tower = RandomTower(rarity);
                result.AddBlueprint(tower);
                if (MetaProgress.AddBlueprints(tower.id, 1, false))
                    result.unlocked.Add(tower);
                result.coins += Random.Range(MinCoinsPerBlueprint, MaxCoinsPerBlueprint + 1);
            }
            MetaProgress.Add(result.coins, 0, false);
            if (save) MetaProgress.Save();
            return result;
        }

        public static bool TryBuy(ChestDefinition chest, out ChestResult result)
        {
            result = null;
            if (!MetaProgress.TrySpendCrystals(chest.price, false)) return false;
            int number = ++MetaProgress.Data.chestsBought;
            result = Open(chest, chest.guaranteeRare || number % GuaranteeEvery == 0);
            return true;
        }

        private static Rarity RollRarity(Rarity minimum)
        {
            int total = 0;
            for (int i = (int)minimum; i < Weights.Length; i++) total += Weights[i];

            int roll = Random.Range(0, total);
            for (int i = (int)minimum; i < Weights.Length; i++)
            {
                if (roll < Weights[i]) return (Rarity)i;
                roll -= Weights[i];
            }
            return minimum;
        }

        private static TowerDefinition RandomTower(Rarity rarity)
        {
            List<TowerDefinition> pool = new List<TowerDefinition>();
            foreach (TowerDefinition tower in TowerCatalog.Towers)
                if (tower.rarity == rarity) pool.Add(tower);
            return pool[Random.Range(0, pool.Count)];
        }
    }
}
