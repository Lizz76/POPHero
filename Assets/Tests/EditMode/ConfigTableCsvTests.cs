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
