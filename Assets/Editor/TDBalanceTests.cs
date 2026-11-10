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
                float dpsPerMana = data.damage * data.fireRate / data.cost;
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

        private static TowerData Load(string id)
        {
            if (Towers.TryGetValue(id, out TowerData data) && data != null) return data;
            Assert.Fail($"TowerData not found for {id}");
            return null;
        }
    }
}
