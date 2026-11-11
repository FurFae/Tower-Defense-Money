using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    public class UpgradeDefinition
    {
        public readonly string id;
        public readonly string title;
        public readonly string effect;

        public UpgradeDefinition(string id, string title, string effect)
        {
            this.id = id;
            this.title = title;
            this.effect = effect;
        }
    }

    [Serializable]
    public class MetaData
    {
        public int coins;
        public int crystals;
        public List<string> upgradeIds = new List<string>();
        public List<int> upgradeLevels = new List<int>();
        // Tower collection: level 0 = locked, copies = spare blueprints.
        public List<string> towerIds = new List<string>();
        public List<int> towerLevels = new List<int>();
        public List<int> towerCopies = new List<int>();
        public int levelsCompleted;
        // Best stars per level, index = level number - 1.
        public List<int> levelStars = new List<int>();
        // Towers taken into battle: only these are offered in the build menu.
        public List<string> deck = new List<string>();
        public int chestsBought;
        public string shopDay = "";
        public List<int> shopBought = new List<int>();
        // Save format version; migrations run in MetaProgress.Load.
        public int version = MetaProgress.SaveVersion;
    }

    public class LevelResult
    {
        public int stars;
        public int coins;
        public int leftoverMana;
        public int manaCoins;
        public TowerDefinition campaignTower;
        public ChestResult chest;
    }

    // Persistent economy: coins (earned by playing, spent on upgrades and blueprints),
    // crystals (premium), the tower collection and campaign progress.
    // Mana is the in-level resource and lives in GameManager.
    public static class MetaProgress
    {
        public const string BaseLivesId = "base_lives";
        public const string BaseManaId = "base_mana";
        public const int MaxUpgradeLevel = 5;
        public const float DamagePerLevel = 0.1f;
        public const int ManaPerLevel = 10;
        public const int DeckSize = 4;
        // Mana left at the end of a won level turns into coins at this rate.
        public const int ManaPerCoin = 10;
        // Bump when MetaData changes shape; Load() migrates older saves step by step.
        public const int SaveVersion = 2;

        public static readonly UpgradeDefinition[] BaseUpgrades =
        {
            new UpgradeDefinition(BaseLivesId, "Укрепление базы", "+1 жизнь за уровень"),
            new UpgradeDefinition(BaseManaId, "Запас маны", "+10 маны на старте за уровень"),
        };

        // Coins for winning with 1, 2 or 3 stars.
        private static readonly int[] LevelRewards = { 30, 50, 80 };
        private const int FirstUpgradeCost = 80;

        public static event Action OnChanged;
        public static event Action<LevelResult> OnLevelCompleted;

        private static MetaData data;

        public static MetaData Data
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        private static void Load()
        {
            data = JsonUtility.FromJson<MetaData>(SaveStorage.Get(SaveStorage.MetaKey)) ?? new MetaData();
            Migrate(data);
            // The starter tower is always owned.
            if (GetTowerLevel(TowerCatalog.StarterId) == 0)
                AddBlueprints(TowerCatalog.StarterId, 1, false);
            // Grant newly introduced campaign unlocks to existing players as well.
            foreach (TowerDefinition tower in TowerCatalog.Towers)
                if (tower.playable && tower.campaignLevel > 0 && tower.campaignLevel <= data.levelsCompleted
                    && GetTowerLevel(tower.id) == 0)
                    AddBlueprints(tower.id, 1, false);
            // Saves from before the deck existed take every tower they own into battle.
            if (data.deck.Count == 0)
                foreach (TowerDefinition tower in TowerCatalog.Towers)
                    if (GetTowerLevel(tower.id) > 0) TryAddToDeck(tower.id);
        }

        // Forward-compatible save migrations: each step bumps the version by one.
        private static void Migrate(MetaData loaded)
        {
            while (loaded.version < SaveVersion)
            {
                switch (loaded.version)
                {
                    case 0:
                    case 1:
                        // v1 -> v2: nothing to convert yet; new fields have defaults.
                        break;
                }
                loaded.version++;
            }
        }

        public static void Save()
        {
            SaveStorage.Set(SaveStorage.MetaKey, JsonUtility.ToJson(Data));
            OnChanged?.Invoke();
        }

        // Drops the cached state, e.g. when the cloud save has arrived.
        public static void Reload()
        {
            data = null;
            OnChanged?.Invoke();
        }

        public static int Coins => Data.coins;
        public static int Crystals => Data.crystals;
        public static int LevelsCompleted => Data.levelsCompleted;

        public static void Add(int coins, int crystals, bool save = true)
        {
            Data.coins += coins;
            data.crystals += crystals;
            if (save) Save();
        }

        public static bool TrySpendCoins(int amount, bool save = true)
        {
            if (amount < 0 || Data.coins < amount) return false;
            data.coins -= amount;
            if (save) Save();
            return true;
        }

        public static bool TrySpendCrystals(int amount, bool save = true)
        {
            if (amount < 0 || Data.crystals < amount) return false;
            data.crystals -= amount;
            if (save) Save();
            return true;
        }

        // ---- Base upgrades ----

        public static int GetLevel(string upgradeId)
        {
            int index = Data.upgradeIds.IndexOf(upgradeId);
            return index < 0 ? 0 : Data.upgradeLevels[index];
        }

        public static bool IsMaxed(string upgradeId) => GetLevel(upgradeId) >= MaxUpgradeLevel;

        // 80, 140, 200, 260, 320 coins.
        public static int GetUpgradeCost(string upgradeId) => FirstUpgradeCost + 60 * GetLevel(upgradeId);

        public static bool TryUpgrade(string upgradeId)
        {
            if (IsMaxed(upgradeId) || !TrySpendCoins(GetUpgradeCost(upgradeId), false)) return false;

            int index = data.upgradeIds.IndexOf(upgradeId);
            if (index < 0)
            {
                data.upgradeIds.Add(upgradeId);
                data.upgradeLevels.Add(1);
            }
            else
            {
                data.upgradeLevels[index]++;
            }
            Save();
            return true;
        }

        // ---- Tower collection ----

        private static int TowerIndex(string towerId, bool create)
        {
            // Not Data: Load() calls this before the starter tower is in place.
            int index = data.towerIds.IndexOf(towerId);
            if (index < 0 && create)
            {
                data.towerIds.Add(towerId);
                data.towerLevels.Add(0);
                data.towerCopies.Add(0);
                index = data.towerIds.Count - 1;
            }
            return index;
        }

        public static int GetTowerLevel(string towerId)
        {
            if (data == null) Load();
            int index = TowerIndex(towerId, false);
            return index < 0 ? 0 : data.towerLevels[index];
        }

        public static int GetTowerCopies(string towerId)
        {
            if (data == null) Load();
            int index = TowerIndex(towerId, false);
            return index < 0 ? 0 : data.towerCopies[index];
        }

        // Towers outside the catalog are not collectible and are always available.
        public static bool IsUnlocked(string towerId)
        {
            return TowerCatalog.Find(towerId) == null || GetTowerLevel(towerId) > 0;
        }

        // The first blueprint unlocks the tower. Returns true if it was unlocked by this call.
        public static bool AddBlueprints(string towerId, int amount, bool save = true)
        {
            if (data == null) Load();
            int index = TowerIndex(towerId, true);
            bool unlocked = false;
            if (data.towerLevels[index] == 0 && amount > 0)
            {
                data.towerLevels[index] = 1;
                amount--;
                unlocked = true;
            }
            data.towerCopies[index] += amount;
            if (unlocked) TryAddToDeck(towerId);
            if (save) Save();
            return unlocked;
        }

        public static bool IsTowerMaxed(string towerId) => GetTowerLevel(towerId) >= TowerCatalog.MaxLevel;

        public static int CopiesNeeded(string towerId)
        {
            return TowerCatalog.CopiesToUpgrade(TowerCatalog.Find(towerId).rarity, GetTowerLevel(towerId));
        }

        public static int TowerUpgradeCost(string towerId)
        {
            return TowerCatalog.CoinsToUpgrade(TowerCatalog.Find(towerId).rarity, GetTowerLevel(towerId));
        }

        public static bool CanUpgradeTower(string towerId)
        {
            return GetTowerLevel(towerId) > 0 && !IsTowerMaxed(towerId)
                && GetTowerCopies(towerId) >= CopiesNeeded(towerId) && Coins >= TowerUpgradeCost(towerId);
        }

        public static bool TryUpgradeTower(string towerId)
        {
            if (!CanUpgradeTower(towerId)) return false;
            int index = TowerIndex(towerId, false);
            data.towerCopies[index] -= CopiesNeeded(towerId);
            data.coins -= TowerUpgradeCost(towerId);
            data.towerLevels[index]++;
            Save();
            RetentionSystem.Report(RetentionStat.TowerUpgrades);
            return true;
        }

        public static int DamageBonusPercent(string towerId)
        {
            return Mathf.RoundToInt(DamagePerLevel * 100f) * Mathf.Max(0, GetTowerLevel(towerId) - 1);
        }

        public static float DamageMultiplier(string towerId) => 1f + DamageBonusPercent(towerId) / 100f;

        // ---- Deck ----

        public static int DeckCount => Data.deck.Count;

        // Towers outside the catalog are always offered.
        public static bool IsInDeck(string towerId)
        {
            return TowerCatalog.Find(towerId) == null || Data.deck.Contains(towerId);
        }

        public static bool CanAddToDeck(string towerId)
        {
            TowerDefinition tower = TowerCatalog.Find(towerId);
            return tower != null && tower.playable && GetTowerLevel(towerId) > 0
                && !data.deck.Contains(towerId) && data.deck.Count < DeckSize;
        }

        private static bool TryAddToDeck(string towerId)
        {
            if (!CanAddToDeck(towerId)) return false;
            data.deck.Add(towerId);
            return true;
        }

        // At least one tower always stays in the deck.
        public static void ToggleDeck(string towerId)
        {
            if (Data.deck.Contains(towerId))
            {
                if (data.deck.Count > 1) data.deck.Remove(towerId);
            }
            else if (!TryAddToDeck(towerId))
            {
                return;
            }
            Save();
        }

        // ---- Campaign ----

        public static int GetLevelStars(int levelNumber)
        {
            List<int> stars = Data.levelStars;
            return levelNumber >= 1 && levelNumber <= stars.Count ? stars[levelNumber - 1] : 0;
        }

        public static int StarsFor(int lives, int maxLives)
        {
            if (lives >= maxLives) return 3;
            return lives * 2 >= maxLives ? 2 : 1;
        }

        // Pays everything a won level gives: coins by stars, a chest, and on the
        // first win the tower the campaign grants for that level.
        public static LevelResult CompleteLevel(int levelNumber, int lives, int maxLives, int leftoverMana)
        {
            LevelResult result = new LevelResult { stars = StarsFor(lives, maxLives) };
            result.coins = Mathf.RoundToInt((LevelRewards[result.stars - 1] + 40 * (levelNumber - 1)) * Levels.RewardMultiplier);
            if (GetLevelStars(levelNumber) == 0) result.coins += 100 * levelNumber;
            result.leftoverMana = leftoverMana;
            result.manaCoins = Mathf.Clamp(leftoverMana / ManaPerCoin, 0, 30);
            Add(result.coins + result.manaCoins, 0, false);

            while (data.levelStars.Count < levelNumber) data.levelStars.Add(0);
            data.levelStars[levelNumber - 1] = Mathf.Max(data.levelStars[levelNumber - 1], result.stars);

            if (levelNumber > data.levelsCompleted)
            {
                data.levelsCompleted = levelNumber;
                foreach (TowerDefinition tower in TowerCatalog.Towers)
                {
                    if (tower.campaignLevel != levelNumber || !tower.playable) continue;
                    AddBlueprints(tower.id, 1, false);
                    result.campaignTower = tower;
                }
            }

            result.chest = Chests.Open(Chests.Victory, false, false);
            Save();
            OnLevelCompleted?.Invoke(result);
            return result;
        }
    }
}
