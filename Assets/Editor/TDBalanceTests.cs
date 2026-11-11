using System.Collections.Generic;
using NUnit.Framework;
using TowerDefense;
using UnityEditor;
using UnityEngine;

namespace TowerDefense.EditorTools
{
    // Economy and effect sanity checks (roadmap item 11). Run via Test Runner, EditMode tab.
    public class TDBalanceTests
    {
        private static readonly Dictionary<string, TowerData> Towers = new Dictionary<string, TowerData>();

        [OneTimeSetUp]
        public void LoadTowers()
        {
            foreach (TowerDefinition definition in TowerCatalog.Towers)
            {
                TowerData data = AssetDatabase.LoadAssetAtPath<TowerData>(
                    $"Assets/Generated/Data/{definition.id}.asset");
                if (data != null) Towers[definition.id] = data;
            }
        }

        [Test]
        public void Catalog_TowersHaveUniqueIds()
        {
            HashSet<string> ids = new HashSet<string>();
            foreach (TowerDefinition tower in TowerCatalog.Towers)
                Assert.IsTrue(ids.Add(tower.id), $"Duplicate tower id {tower.id}");
        }

        [Test]
        public void Economy_AllTowersHavePositiveCostAndDamage()
        {
            foreach (TowerDefinition definition in TowerCatalog.Towers)
            {
                if (!definition.playable) continue;
                TowerData data = Load(definition.id);
                Assert.Greater(data.cost, 0, definition.id);
                Assert.Greater(data.damage, 0f, definition.id);
                Assert.Greater(data.range, 0f, definition.id);
                Assert.Greater(data.fireRate, 0f, definition.id);
            }
        }

        // DPS на единицу маны должен быть в разумном коридоре: дешёвые башни эффективнее
        // в сыром ДПС, дорогие — за счёт эффектов (пробитие, цепь, поджог).
        [Test]
        public void Economy_DpsPerManaWithinBand()
        {
            foreach (TowerDefinition definition in TowerCatalog.Towers)
            {
                if (!definition.playable) continue;
                TowerData data = Load(definition.id);
                float dpsPerMana = (data.damage * data.fireRate + data.damageOverTimePerSecond + data.burnDamagePerSecond) / data.cost;
                Assert.GreaterOrEqual(dpsPerMana, 0.05f, $"{definition.id} too weak per mana (базовый урон; эффекты сверху)");
                Assert.LessOrEqual(dpsPerMana, 0.6f, $"{definition.id} too strong per mana");
            }
        }

        [Test]
        public void Economy_StarterIsCheapest()
        {
            TowerData starter = Load(TowerCatalog.StarterId);
            foreach (TowerDefinition definition in TowerCatalog.Towers)
            {
                if (!definition.playable) continue;
                Assert.LessOrEqual(starter.cost, Load(definition.id).cost,
                    $"Starter must stay the cheapest tower ({definition.id})");
            }
        }

        [Test]
        public void Effects_PoisonHasDurationAndDps()
        {
            TowerData poison = Load("Tower_PoisonTower");
            Assert.Greater(poison.damageOverTimePerSecond, 0f);
            Assert.Greater(poison.damageOverTimeDuration, 0f);
            Assert.IsTrue(poison.preferUnpoisonedTarget, "Poison tower should prefer fresh targets");
        }

        [Test]
        public void Effects_SlowIsAControlNotAKiller()
        {
            TowerData ice = Load("Tower_IceTower");
            Assert.Greater(ice.slowDuration, 0f);
            Assert.Less(ice.slowMultiplier, 1f);
            Assert.Less(ice.damage * ice.fireRate, 15f, "Ice tower DPS must stay low: control, not killer");
        }

        [Test]
        public void Effects_SplashAndChainConfigured()
        {
            TowerData cannon = Load("Tower_CannonTower");
            Assert.Greater(cannon.splashRadius, 0f);
            TowerData lightning = Load("Tower_LightningTower");
            Assert.Greater(lightning.chainCount, 0);
            Assert.Greater(lightning.chainRadius, 0f);
            TowerData ballista = Load("Tower_Ballista");
            Assert.Greater(ballista.pierceCount, 0);
        }

        [Test]
        public void Targeting_GroundSplashCannotHitFlying()
        {
            Assert.IsFalse(Load("Tower_CannonTower").canHitFlying);
            Assert.IsFalse(Load("Tower_Flamethrower").canHitFlying);
            Assert.IsTrue(Load("Tower_ArrowTower").canHitFlying);
            Assert.IsTrue(Load("Tower_ArrowTower").projectileIsArrow);
            Assert.IsTrue(Load("Tower_Ballista").projectileIsArrow);
        }

        [Test]
        public void Economy_UpgradeTablesCoverAllLevels()
        {
            foreach (Rarity rarity in System.Enum.GetValues(typeof(Rarity)))
            {
                for (int level = 1; level < TowerCatalog.MaxLevel; level++)
                {
                    Assert.Greater(TowerCatalog.CopiesToUpgrade(rarity, level), 0, $"{rarity} L{level}");
                    Assert.Greater(TowerCatalog.CoinsToUpgrade(rarity, level), 0, $"{rarity} L{level}");
                }
            }
        }

        [Test]
        public void Economy_PlayableCampaignRewardsAreUniquePerLevel()
        {
            var levels = new HashSet<int>();
            foreach (var tower in TowerCatalog.Towers)
                if (tower.campaignLevel > 0)
                {
                    Assert.IsTrue(tower.playable);
                    Assert.IsTrue(levels.Add(tower.campaignLevel));
                }
        }

        [Test]
        public void Economy_UpgradeCostsGrowWithoutExponentialGrind()
        {
            foreach (Rarity rarity in new[] { Rarity.Common, Rarity.Uncommon, Rarity.Rare, Rarity.Epic })
            {
                int previous = 0;
                for (int level = 1; level < TowerCatalog.MaxLevel; level++)
                {
                    int cost = TowerCatalog.CoinsToUpgrade(rarity, level);
                    Assert.Greater(cost, previous);
                    Assert.LessOrEqual(cost, 900);
                    previous = cost;
                }
            }
        }

        [Test]
        public void Economy_ChestWeightsOnlyIncludePlayableRewards()
        {
            var field = typeof(Chests).GetField("Weights", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var weights = (int[])field.GetValue(null);
            int total = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                total += weights[i];
                if (weights[i] == 0) continue;
                Assert.IsTrue(System.Array.Exists(TowerCatalog.Towers, t => t.playable && (int)t.rarity == i));
            }
            Assert.AreEqual(1000, total);
        }
        [Test]
        public void Projectile_InitializeResetsLifetimeForReuse()
        {
            var go = new GameObject("ProjectileTest");
            try
            {
                var projectile = go.AddComponent<Projectile>();
                var lifetime = typeof(Projectile).GetField("lifetime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                lifetime.SetValue(projectile, 4.99f);
                projectile.Initialize(null, 6f, 8f);
                Assert.AreEqual(0f, (float)lifetime.GetValue(projectile));
                lifetime.SetValue(projectile, 5f);
                projectile.Initialize(null, 6f, 8f);
                Assert.AreEqual(0f, (float)lifetime.GetValue(projectile));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Campaign_DifficultyDoesNotTripleBetweenAdjacentMaps()
        {
            for (int i = 1; i < Levels.All.Length; i++)
            {
                var before = Levels.All[i - 1];
                var after = Levels.All[i];
                float pressure = after.healthMultiplier * after.countMultiplier /
                    (before.healthMultiplier * before.countMultiplier);
                Assert.LessOrEqual(pressure, 1.5f);
            }
        }
        private static TowerData Load(string id)
        {
            if (Towers.TryGetValue(id, out TowerData data) && data != null) return data;
            Assert.Fail($"TowerData not found for {id}");
            return null;
        }
    }
}
