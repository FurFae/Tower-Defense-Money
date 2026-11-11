using UnityEngine;

namespace TowerDefense
{
    public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }

    public class TowerDefinition
    {
        // Matches the TowerData asset name, so gameplay can look its level up.
        public readonly string id;
        public readonly string title;
        public readonly Rarity rarity;
        // Campaign level whose first win grants the tower; 0 = blueprints only.
        public readonly int campaignLevel;
        // False until the tower has a TowerData asset and can be built in a level.
        public readonly bool playable;

        public TowerDefinition(string id, string title, Rarity rarity, int campaignLevel, bool playable)
        {
            this.id = id;
            this.title = title;
            this.rarity = rarity;
            this.campaignLevel = campaignLevel;
            this.playable = playable;
        }
    }

    // Every collectible tower and the per-rarity upgrade thresholds.
    public static class TowerCatalog
    {
        public const string StarterId = "Tower_ArrowTower";
        public const int MaxLevel = 8;

        public static readonly TowerDefinition[] Towers =
        {
            new TowerDefinition(StarterId, "Башня лучников", Rarity.Common, 0, true),
            new TowerDefinition("Tower_IceTower", "Ледяная башня", Rarity.Common, 0, true),
            new TowerDefinition("Tower_CannonTower", "Пушка", Rarity.Uncommon, 1, true),
            new TowerDefinition("Tower_PoisonTower", "Ядовитая башня", Rarity.Uncommon, 2, true),
            new TowerDefinition("Tower_LightningTower", "Башня молний", Rarity.Rare, 3, true),
            new TowerDefinition("Tower_Ballista", "Баллиста", Rarity.Rare, 0, true),
            new TowerDefinition("Tower_Flamethrower", "Огнемёт", Rarity.Epic, 0, true),
            new TowerDefinition("Tower_Totem", "Тотем поддержки", Rarity.Legendary, 0, false),
        };

        private static readonly string[] RarityNames = { "обычная", "необычная", "редкая", "эпическая", "легендарная" };
        private static readonly string[] RarityHex = { "#B0B8C4", "#5FD068", "#4DA3FF", "#C084FF", "#FFB020" };

        // Blueprints and coins needed to go from level N to N+1 (index N-1), per rarity.
        private static readonly int[][] Copies =
        {
            new[] { 2, 4, 6, 10, 16, 24, 36 },
            new[] { 2, 3, 5, 8, 12, 18, 26 },
            new[] { 2, 3, 4, 6, 9, 13, 18 },
            new[] { 1, 2, 3, 4, 6, 8, 12 },
            new[] { 1, 1, 2, 3, 4, 6, 8 },
        };

        private static readonly int[][] Coins =
        {
            new[] { 40, 70, 110, 160, 230, 320, 450 },
            new[] { 50, 90, 140, 200, 280, 390, 540 },
            new[] { 70, 120, 180, 260, 360, 500, 700 },
            new[] { 100, 160, 240, 340, 470, 650, 900 },
            new[] { 200, 400, 800, 1500, 2500, 4000, 6000 },
        };

        public static TowerDefinition Find(string id)
        {
            foreach (TowerDefinition tower in Towers)
                if (tower.id == id) return tower;
            return null;
        }

        public static int CopiesToUpgrade(Rarity rarity, int level) => Copies[(int)rarity][level - 1];

        public static int CoinsToUpgrade(Rarity rarity, int level) => Coins[(int)rarity][level - 1];

        public static string ColoredRarity(Rarity rarity) => $"<color={RarityHex[(int)rarity]}>{RarityNames[(int)rarity]}</color>";

        public static string ColoredTitle(TowerDefinition tower) => $"<color={RarityHex[(int)tower.rarity]}>{tower.title}</color>";

        public static Color RarityColor(Rarity rarity)
        {
            ColorUtility.TryParseHtmlString(RarityHex[(int)rarity], out Color color);
            return color;
        }
    }
}
