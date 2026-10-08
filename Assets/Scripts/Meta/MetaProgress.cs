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
        public int chestsBought;
        public string shopDay = "";
        public List<int> shopBought = new List<int>();
    }

    public class LevelResult
    {
        public int stars;
        public int coins;
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

        public static readonly UpgradeDefinition[] BaseUpgrades =
        {
            new UpgradeDefinition(BaseLivesId, "Укрепление базы", "+1 жизнь за уровень"),
            new UpgradeDefinition(BaseManaId, "Запас маны", "+10 маны на старте за уровень"),
        };

        // Coins for winning with 1, 2 or 3 stars.
        private static readonly int[] LevelRewards = { 30, 50, 80 };
        private const int FirstUpgradeCost = 100;

        public static event Action OnChanged;
        public static event Action<LevelResult> OnLevelCompleted;

        // PlayerPrefs is backed by IndexedDB in WebGL. Swap Load/Save for the
        // PluginYG2 Storage module once it is installed to get cloud saves.
        private const string SaveKey = "meta_v1";
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
            data = JsonUtility.FromJson<MetaData>(PlayerPrefs.GetString(SaveKey, "")) ?? new MetaData();
            // The starter tower is always owned.
            if (GetTowerLevel(TowerCatalog.StarterId) == 0)
                AddBlueprints(TowerCatalog.StarterId, 1, false);
        }

        public static void Save()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
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
            if (Data.coins < amount) return false;
            data.coins -= amount;
            if (save) Save();
            return true;
        }

        public static bool TrySpendCrystals(int amount, bool save = true)
        {
            if (Data.crystals < amount) return false;
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

        // 100, 200, 400, 800, 1600.
        public static int GetUpgradeCost(string upgradeId) => FirstUpgradeCost << GetLevel(upgradeId);

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
            return true;
        }

        public static int DamageBonusPercent(string towerId)
        {
            return Mathf.RoundToInt(DamagePerLevel * 100f) * Mathf.Max(0, GetTowerLevel(towerId) - 1);
        }

        public static float DamageMultiplier(string towerId) => 1f + DamageBonusPercent(towerId) / 100f;

        // ---- Campaign ----

        public static int StarsFor(int lives, int maxLives)
        {
            if (lives >= maxLives) return 3;
            return lives * 2 >= maxLives ? 2 : 1;
        }

        // Pays everything a won level gives: coins by stars, a chest, and on the
        // first win the tower the campaign grants for that level.
        public static LevelResult CompleteLevel(int levelNumber, int lives, int maxLives)
        {
            LevelResult result = new LevelResult { stars = StarsFor(lives, maxLives) };
            result.coins = LevelRewards[result.stars - 1];
            Add(result.coins, 0, false);

            if (levelNumber > data.levelsCompleted)
            {
                data.levelsCompleted = levelNumber;
                foreach (TowerDefinition tower in TowerCatalog.Towers)
                {
                    if (tower.campaignLevel != levelNumber) continue;
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
