using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace POPHero.Tests
{
    public sealed class ConfigTableCsvTests
    {
        [Test]
        public void RuntimeCsvLoader_ReadsEnemyAndGlobalConfigTables()
        {
            Assert.IsTrue(ConfigTableCsvRuntimeLoader.TryLoadFromProjectCsv(out var tables, out _, out var error), error);
            try
            {
                Assert.IsNotNull(tables);
                Assert.IsTrue(tables.enemies.Count >= 2);
                Assert.IsTrue(tables.globalConfig.Exists(entry => entry.key == "endlessHpGrowth" && entry.value == "45"));
                Assert.IsTrue(tables.globalConfig.Exists(entry => entry.key == "endlessAttackGrowth" && entry.value == "2"));
                Assert.IsTrue(tables.enemies.Exists(enemy => enemy.behaviorType == EnemyBehaviorType.FlyingRangedOrigin));
                var bird = tables.enemies.Find(enemy => enemy.prefabKey == "bird");
                Assert.IsNotNull(bird);
                Assert.AreEqual(EnemyBehaviorType.FlyingRangedOrigin, bird.behaviorType);
                CollectionAssert.Contains(bird.abilityIds, "none");
                var birdEncounter = tables.encounters.Find(encounter => encounter.encounterId == "act1_early_03");
                Assert.IsNotNull(birdEncounter);
                Assert.AreEqual(EncounterNodeType.Normal, birdEncounter.nodeType);
                Assert.AreEqual(2, birdEncounter.enemies.Count);
                Assert.AreEqual(3001, birdEncounter.enemies[0].enemyId);
                Assert.AreEqual(EnemyEncounterSlot.Primary, birdEncounter.enemies[0].slot);
                Assert.AreEqual(3901, birdEncounter.enemies[1].enemyId);
                Assert.AreEqual(EnemyEncounterSlot.Support, birdEncounter.enemies[1].slot);
                Assert.IsTrue(tables.mapConfigs.Count > 0);
                Assert.AreEqual(10, tables.mapConfigs[0].restWeight);
            }
            finally
            {
                if (tables != null)
                    UnityEngine.Object.DestroyImmediate(tables);
            }
        }

        [Test]
        public void RuntimeCsvLoader_ReadsAct1EncounterPoolV1()
        {
            Assert.IsTrue(ConfigTableCsvRuntimeLoader.TryLoadFromProjectCsv(out var tables, out _, out var error), error);
            try
            {
                var act1Normal = tables.encounters
                    .Where(encounter => encounter.act == 1 && encounter.nodeType == EncounterNodeType.Normal)
                    .ToList();
                var act1Boss = tables.encounters
                    .Where(encounter => encounter.act == 1 && encounter.nodeType == EncounterNodeType.Boss)
                    .ToList();

                Assert.AreEqual(8, act1Normal.Count);
                Assert.AreEqual(1, act1Boss.Count);
                AssertEncounter(tables, "act1_early_01", 1, 2, 100, "3001:Primary");
                AssertEncounter(tables, "act1_early_02", 1, 3, 80, "3001:Primary", "3001:Mid");
                AssertEncounter(tables, "act1_early_03", 2, 3, 70, "3001:Primary", "3901:Support");
                AssertEncounter(tables, "act1_mid_01", 3, 5, 90, "3001:Primary", "3002:Mid");
                AssertEncounter(tables, "act1_mid_02", 3, 6, 90, "3002:Primary", "3901:Support");
                AssertEncounter(tables, "act1_mid_03", 4, 6, 70, "3003:Primary");
                AssertEncounter(tables, "act1_late_01", 5, 7, 90, "3003:Primary", "3901:Support");
                AssertEncounter(tables, "act1_late_02", 6, 7, 70, "3002:Primary", "3001:Mid", "3901:Support");

                var birdEncounters = act1Normal.Where(encounter => encounter.enemies.Any(enemy => enemy.enemyId == 3901)).ToList();
                Assert.AreEqual(4, birdEncounters.Count);
                Assert.IsTrue(birdEncounters.All(encounter =>
                    encounter.enemies.Any(enemy => enemy.enemyId == 3901 && enemy.slot == EnemyEncounterSlot.Support)));
            }
            finally
            {
                if (tables != null)
                    Object.DestroyImmediate(tables);
            }
        }

        [Test]
        public void SharedCsvParsers_ParseRarityWeightsAndBoolConsistently()
        {
            var weights = ConfigTableCsvParsers.ParseRarityWeights("50|25|15|10");

            Assert.AreEqual(50f, weights.white);
            Assert.AreEqual(25f, weights.blue);
            Assert.AreEqual(15f, weights.purple);
            Assert.AreEqual(10f, weights.gold);
            Assert.IsTrue(ConfigTableCsvParsers.ParseBool("1"));
            Assert.IsFalse(ConfigTableCsvParsers.ParseBool("0", true));
        }

        [Test]
        public void SharedCsvParsers_ParseEnemyBehaviorAliases()
        {
            Assert.IsTrue(ConfigTableService.TryParseEnumKey("ground_melee", out EnemyBehaviorType ground));
            Assert.AreEqual(EnemyBehaviorType.MeleeAdvance, ground);

            Assert.IsTrue(ConfigTableService.TryParseEnumKey("flying_ranged", out EnemyBehaviorType flying));
            Assert.AreEqual(EnemyBehaviorType.FlyingRangedOrigin, flying);
        }

        [Test]
        public void SharedCsvParsers_ParseEncounterEnemySlots()
        {
            var enemies = ConfigTableCsvParsers.ParseEncounterEnemies("3001:slot_front|3901:slot_air|3002:slot_mid");

            Assert.AreEqual(3, enemies.Count);
            Assert.AreEqual(3001, enemies[0].enemyId);
            Assert.AreEqual(EnemyEncounterSlot.Primary, enemies[0].slot);
            Assert.AreEqual(3901, enemies[1].enemyId);
            Assert.AreEqual(EnemyEncounterSlot.Support, enemies[1].slot);
            Assert.AreEqual(3002, enemies[2].enemyId);
            Assert.AreEqual(EnemyEncounterSlot.Mid, enemies[2].slot);
        }

        [Test]
        public void RuntimeCsvLoader_ReadsBlockAcquisitionTables()
        {
            Assert.IsTrue(ConfigTableCsvRuntimeLoader.TryLoadFromProjectCsv(out var tables, out _, out var error), error);
            try
            {
                var blockSlot = tables.shopSlots.Find(slot => slot.slotId == "shop_block");
                Assert.IsNotNull(blockSlot);
                Assert.AreEqual(ShopSlotKind.Block, blockSlot.slotKind);
                Assert.AreEqual(1, blockSlot.count);
                Assert.AreEqual(12, blockSlot.price);
                Assert.IsTrue(blockSlot.rarityWeights.HasAnyWeight);

                var workbench = tables.blockOperationProfiles.Find(profile => profile.id == "map_workbench");
                Assert.IsNotNull(workbench);
                Assert.IsFalse(workbench.allowDelete);
                Assert.IsTrue(workbench.allowSwap);
                Assert.IsTrue(workbench.allowUpgrade);
                Assert.AreEqual(0, workbench.upgradeCostGold);
                Assert.AreEqual(1, workbench.maxUpgradeCount);
            }
            finally
            {
                if (tables != null)
                    Object.DestroyImmediate(tables);
            }
        }

        [Test]
        public void SharedCsvParsers_ParseBlockShopSlotKind()
        {
            Assert.IsTrue(ConfigTableService.TryParseEnumKey("Block", out ShopSlotKind block));
            Assert.AreEqual(ShopSlotKind.Block, block);
        }

        [Test]
        public void PlayerBlockCollection_TryReplaceCard_PreservesActiveAndReservePositions()
        {
            var collection = new PlayerBlockCollection();
            var active = new BlockCardState { id = "active", rarity = BlockRarity.White };
            var reserve = new BlockCardState { id = "reserve", rarity = BlockRarity.Blue };
            collection.activeBlocks.Add(active);
            collection.reserveBlocks.Add(reserve);

            var upgradedActive = new BlockCardState { id = "upgraded_active", rarity = BlockRarity.Blue };
            Assert.IsTrue(collection.TryReplaceCard("active", upgradedActive, out var replacedActive, out var replacedWasActive));
            Assert.IsTrue(replacedWasActive);
            Assert.AreSame(active, replacedActive);
            Assert.AreSame(upgradedActive, collection.activeBlocks[0]);
            Assert.AreSame(reserve, collection.reserveBlocks[0]);

            var upgradedReserve = new BlockCardState { id = "upgraded_reserve", rarity = BlockRarity.Purple };
            Assert.IsTrue(collection.TryReplaceCard("reserve", upgradedReserve, out var replacedReserve, out var replacedWasReserveActive));
            Assert.IsFalse(replacedWasReserveActive);
            Assert.AreSame(reserve, replacedReserve);
            Assert.AreSame(upgradedActive, collection.activeBlocks[0]);
            Assert.AreSame(upgradedReserve, collection.reserveBlocks[0]);
        }

        [Test]
        public void MapConfigRestWeight_MissingColumnFallsBackToDefault()
        {
            var table = new ConfigCsvTable { Name = "mapConfig.csv" };
            table.Rows.Add(new System.Collections.Generic.List<string>
            {
                "id",
                "floorCount",
                "minNodesPerFloor",
                "maxNodesPerFloor",
                "extraConnectionChance",
                "battleWeight",
                "shopWeight",
                "workbenchWeight",
                "eventWeight",
                "bossEnemyIndex"
            });
            var row = new ConfigCsvRow(table, 6, new System.Collections.Generic.List<string>
            {
                "default",
                "7",
                "2",
                "3",
                "0.35",
                "70",
                "12",
                "8",
                "10",
                "4"
            });

            Assert.AreEqual(10, ConfigTableCsvParsers.ParseInt(row.Get("restWeight"), 10));
        }

        static void AssertEncounter(PopHeroTableConfig tables, string id, int minFloor, int maxFloor, int weight, params string[] enemies)
        {
            var encounter = tables.encounters.Find(row => row.encounterId == id);
            Assert.IsNotNull(encounter, id);
            Assert.AreEqual(1, encounter.act, id);
            Assert.AreEqual(EncounterNodeType.Normal, encounter.nodeType, id);
            Assert.AreEqual(minFloor, encounter.minFloor, id);
            Assert.AreEqual(maxFloor, encounter.maxFloor, id);
            Assert.AreEqual(weight, encounter.weight, id);
            Assert.IsFalse(encounter.allowRepeat, id);
            Assert.AreEqual(enemies.Length, encounter.enemies.Count, id);

            for (var index = 0; index < enemies.Length; index++)
            {
                var expected = enemies[index].Split(':');
                Assert.AreEqual(int.Parse(expected[0]), encounter.enemies[index].enemyId, id);
                Assert.AreEqual(expected[1], encounter.enemies[index].slot.ToString(), id);
            }
        }
    }
}
