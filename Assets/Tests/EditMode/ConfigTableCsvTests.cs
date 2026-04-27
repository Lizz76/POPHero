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
                var birdEncounter = tables.encounters.Find(encounter => encounter.encounterId == "act1_mid_ground_bird");
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
    }
}
