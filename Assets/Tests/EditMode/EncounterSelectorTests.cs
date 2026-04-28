using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace POPHero.Tests
{
    public sealed class EncounterSelectorTests
    {
        [Test]
        public void Select_FiltersByAct()
        {
            var selector = new EncounterSelector((_, _) => 0);
            var encounters = new List<EncounterDef>
            {
                MakeEncounter("act1", 1, EncounterNodeType.Normal, 1, 7),
                MakeEncounter("act2", 2, EncounterNodeType.Normal, 1, 7)
            };

            var selected = selector.Select(encounters, 2, EncounterNodeType.Normal, 1);

            Assert.AreEqual("act2", selected.encounterId);
        }

        [Test]
        public void Select_FiltersByNodeType()
        {
            var selector = new EncounterSelector((_, _) => 0);
            var encounters = new List<EncounterDef>
            {
                MakeEncounter("normal", 1, EncounterNodeType.Normal, 1, 7),
                MakeEncounter("boss", 1, EncounterNodeType.Boss, 1, 7)
            };

            var selected = selector.Select(encounters, 1, EncounterNodeType.Boss, 7);

            Assert.AreEqual("boss", selected.encounterId);
        }

        [Test]
        public void Select_FiltersByFloorRange()
        {
            var selector = new EncounterSelector((_, _) => 0);
            var encounters = new List<EncounterDef>
            {
                MakeEncounter("early", 1, EncounterNodeType.Normal, 1, 3),
                MakeEncounter("late", 1, EncounterNodeType.Normal, 6, 7)
            };

            var selected = selector.Select(encounters, 1, EncounterNodeType.Normal, 7);

            Assert.AreEqual("late", selected.encounterId);
        }

        [Test]
        public void Select_ExcludesRecentNormalEncountersWhenAlternativesExist()
        {
            var selector = new EncounterSelector((_, _) => 0);
            var encounters = new List<EncounterDef>
            {
                MakeEncounter("first", 1, EncounterNodeType.Normal, 1, 7),
                MakeEncounter("second", 1, EncounterNodeType.Normal, 1, 7)
            };

            var first = selector.Select(encounters, 1, EncounterNodeType.Normal, 1);
            var second = selector.Select(encounters, 1, EncounterNodeType.Normal, 1);

            Assert.AreEqual("first", first.encounterId);
            Assert.AreEqual("second", second.encounterId);
        }

        [Test]
        public void Select_AllowsRecentRepeatWhenItWouldEmptyCandidates()
        {
            var selector = new EncounterSelector((_, _) => 0);
            var encounters = new List<EncounterDef>
            {
                MakeEncounter("only", 1, EncounterNodeType.Normal, 1, 7)
            };

            selector.Select(encounters, 1, EncounterNodeType.Normal, 1);
            var repeated = selector.Select(encounters, 1, EncounterNodeType.Normal, 1);

            Assert.AreEqual("only", repeated.encounterId);
        }

        [Test]
        public void Select_FallsBackToSafeEncounterWhenNoRowsAreEligible()
        {
            var selector = new EncounterSelector((_, _) => 0);
            var encounters = new List<EncounterDef>
            {
                MakeEncounter("safe", 1, EncounterNodeType.Normal, 6, 7)
            };

            var fallback = selector.Select(encounters, 1, EncounterNodeType.Normal, 1);

            Assert.AreEqual("safe", fallback.encounterId);
        }

        [Test]
        public void Select_UsesWeightedRollDeterministically()
        {
            var selector = new EncounterSelector((_, _) => 10);
            var encounters = new List<EncounterDef>
            {
                MakeEncounter("low", 1, EncounterNodeType.Normal, 1, 7, 10),
                MakeEncounter("high", 1, EncounterNodeType.Normal, 1, 7, 90)
            };

            var selected = selector.Select(encounters, 1, EncounterNodeType.Normal, 1);

            Assert.AreEqual("high", selected.encounterId);
        }

        [Test]
        public void Select_WithConfiguredAct1Pool_FiltersEarlyAndLateFloors()
        {
            Assert.IsTrue(ConfigTableCsvRuntimeLoader.TryLoadFromProjectCsv(out var tables, out _, out var error), error);
            try
            {
                var floorOneSelector = new EncounterSelector((_, _) => 0);
                var floorOne = floorOneSelector.Select(tables.encounters, 1, EncounterNodeType.Normal, 1);
                Assert.IsNotNull(floorOne);
                Assert.IsFalse(floorOne.encounterId.Contains("late"));
                Assert.IsTrue(floorOne.maxFloor <= 3);

                var floorSixSelector = new EncounterSelector((_, _) => 160);
                var floorSix = floorSixSelector.Select(tables.encounters, 1, EncounterNodeType.Normal, 6);
                Assert.IsNotNull(floorSix);
                Assert.AreEqual("act1_late_01", floorSix.encounterId);
                Assert.IsTrue(floorSix.enemies.Any(enemy => enemy.enemyId == 3901 && enemy.slot == EnemyEncounterSlot.Support));
            }
            finally
            {
                if (tables != null)
                    UnityEngine.Object.DestroyImmediate(tables);
            }
        }

        static EncounterDef MakeEncounter(string id, int act, EncounterNodeType nodeType, int minFloor, int maxFloor, int weight = 100)
        {
            return new EncounterDef
            {
                encounterId = id,
                act = act,
                nodeType = nodeType,
                minFloor = minFloor,
                maxFloor = maxFloor,
                weight = weight,
                allowRepeat = false,
                enemies = new List<EncounterEnemyDef>
                {
                    new EncounterEnemyDef
                    {
                        enemyId = 3001,
                        slot = EnemyEncounterSlot.Primary
                    }
                }
            };
        }
    }
}
