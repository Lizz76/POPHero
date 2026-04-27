using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace POPHero.Tests
{
    public sealed class EnemyEncounterTests
    {
        [Test]
        public void MeleeEnemy_AdvancesTwice_ThenAttacksAtMeleeRange()
        {
            var enemy = new EnemyData("Test Melee", 30, 0, 0, 7, Color.red);
            var encounter = new EnemyEncounterState(enemy, 3, EnemyEncounterSlot.Primary);
            var resolver = new EnemyTurnResolver();
            var player = new PlayerData(100, 100, 0, 0);

            var first = resolver.ResolveGroup(new[] { encounter }, 0, player)[0];
            var second = resolver.ResolveGroup(new[] { encounter }, 0, player)[0];
            var third = resolver.ResolveGroup(new[] { encounter }, 0, player)[0];

            Assert.AreEqual(EnemyTurnActionType.Advance, first.ActionType);
            Assert.AreEqual(3, first.DistanceBefore);
            Assert.AreEqual(2, first.DistanceAfter);
            Assert.AreEqual(0, first.DamageDealt);

            Assert.AreEqual(EnemyTurnActionType.Advance, second.ActionType);
            Assert.AreEqual(2, second.DistanceBefore);
            Assert.AreEqual(1, second.DistanceAfter);
            Assert.AreEqual(0, second.DamageDealt);

            Assert.AreEqual(EnemyTurnActionType.Attack, third.ActionType);
            Assert.AreEqual(1, third.DistanceBefore);
            Assert.AreEqual(0, third.DistanceAfter);
            Assert.AreEqual(7, third.DamageDealt);
            Assert.AreEqual(93, player.CurrentHp);
        }

        [Test]
        public void FlyingEnemy_AttacksFromOriginWithoutApproachDistance()
        {
            var enemy = new EnemyData("Test Flyer", 20, 0, 0, 5, Color.cyan, EnemyBehaviorType.FlyingRangedOrigin, 3901, "bird", new[] { "none" });
            var encounter = new EnemyEncounterState(enemy, 3, EnemyEncounterSlot.Support);
            var resolver = new EnemyTurnResolver();
            var player = new PlayerData(100, 100, 0, 0);

            var turn = resolver.ResolveGroup(new[] { encounter }, 0, player)[0];

            Assert.AreEqual(EnemyTurnActionType.Attack, turn.ActionType);
            Assert.AreEqual(EnemyBehaviorType.FlyingRangedOrigin, turn.BehaviorType);
            Assert.AreEqual(0, turn.DistanceBefore);
            Assert.AreEqual(0, turn.DistanceAfter);
            Assert.AreEqual(5, turn.DamageDealt);
            Assert.IsTrue(turn.IsRangedAttack);
            Assert.AreEqual("bird", enemy.PrefabKey);
            CollectionAssert.Contains(enemy.AbilityIds, "none");
        }

        [Test]
        public void EnemyTurnResolver_DoesNotDependOnPrefabKey()
        {
            var defaultEnemy = new EnemyData("Default Body", 20, 0, 0, 5, Color.red, EnemyBehaviorType.MeleeAdvance, 3001, "default");
            var birdBodyEnemy = new EnemyData("Bird Body", 20, 0, 0, 5, Color.cyan, EnemyBehaviorType.MeleeAdvance, 3001, "bird");
            var defaultEncounter = new EnemyEncounterState(defaultEnemy, 0, EnemyEncounterSlot.Primary);
            var birdBodyEncounter = new EnemyEncounterState(birdBodyEnemy, 0, EnemyEncounterSlot.Primary);
            var resolver = new EnemyTurnResolver();
            var defaultPlayer = new PlayerData(100, 100, 0, 0);
            var birdBodyPlayer = new PlayerData(100, 100, 0, 0);

            var defaultTurn = resolver.ResolveGroup(new[] { defaultEncounter }, 0, defaultPlayer)[0];
            var birdBodyTurn = resolver.ResolveGroup(new[] { birdBodyEncounter }, 0, birdBodyPlayer)[0];

            Assert.AreEqual(defaultTurn.ActionType, birdBodyTurn.ActionType);
            Assert.AreEqual(defaultTurn.DamageDealt, birdBodyTurn.DamageDealt);
            Assert.AreEqual(defaultPlayer.CurrentHp, birdBodyPlayer.CurrentHp);
        }

        [Test]
        public void EnemyGroup_TargetsPrimaryBeforeSupport_ThenFallsBackToSupport()
        {
            var primary = new EnemyEncounterState(new EnemyData("Primary", 10, 0, 0, 4, Color.red), 3, EnemyEncounterSlot.Primary);
            var support = new EnemyEncounterState(new EnemyData("Support", 10, 0, 0, 3, Color.cyan, EnemyBehaviorType.FlyingRangedOrigin), 0, EnemyEncounterSlot.Support);
            var group = new EnemyEncounterGroupState(primary, support);

            Assert.AreSame(primary, group.GetPrimaryTarget());
            primary.Enemy.ApplyDamage(99);

            Assert.AreSame(support, group.GetPrimaryTarget());
            Assert.IsNull(group.GetSecondaryTarget());
            Assert.IsFalse(group.AllDefeated);
        }

        [Test]
        public void EnemyTurnResolver_UsesSharedCounterReductionAcrossEnemies()
        {
            var melee = new EnemyEncounterState(new EnemyData("Melee", 10, 0, 0, 8, Color.red), 0, EnemyEncounterSlot.Primary);
            var flyer = new EnemyEncounterState(new EnemyData("Flyer", 10, 0, 0, 6, Color.cyan, EnemyBehaviorType.FlyingRangedOrigin), 0, EnemyEncounterSlot.Support);
            var resolver = new EnemyTurnResolver();
            var player = new PlayerData(100, 100, 0, 0);

            var turns = resolver.ResolveGroup(new List<EnemyEncounterState> { melee, flyer }, 10, player);

            Assert.AreEqual(2, turns.Count);
            Assert.AreEqual(0, turns[0].DamageDealt);
            Assert.AreEqual(4, turns[1].DamageDealt);
            Assert.AreEqual(96, player.CurrentHp);
        }

        [Test]
        public void EncounterDirector_LegacyIndexSpawn_DoesNotAutoAddFlyingSupport()
        {
            var config = PopHeroPrototypeConfig.CreateRuntimeDefault();
            var director = new EncounterDirector(new GameRuntimeContext { Config = config });

            var first = director.SpawnEncounter(0);
            Assert.IsNotNull(first.GetEncounter(EnemyEncounterSlot.Primary));
            Assert.IsNull(first.GetEncounter(EnemyEncounterSlot.Support));
            Assert.AreEqual(1, director.CurrentEnemyEncounters.Count);

            var second = director.SpawnEncounter(1);
            Assert.IsNotNull(second.GetEncounter(EnemyEncounterSlot.Primary));
            Assert.IsNull(second.GetEncounter(EnemyEncounterSlot.Support));
            Assert.AreEqual(1, director.CurrentEnemyEncounters.Count);

            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void EncounterDirector_EncounterIdSpawnsFixedGroundAndBird()
        {
            Assert.IsTrue(ConfigTableCsvRuntimeLoader.TryLoadFromProjectCsv(out var tables, out _, out var error), error);
            var config = PopHeroPrototypeConfig.CreateRuntimeDefault();
            try
            {
                var service = new ConfigTableService(tables, config);
                service.ApplyToPrototypeConfig(config);
                var director = new EncounterDirector(new GameRuntimeContext { Config = config, Tables = service });

                var first = director.SpawnEncounter("act1_mid_ground_bird");
                var second = director.SpawnEncounter("act1_mid_ground_bird");

                AssertFixedGroundBirdEncounter(first);
                AssertFixedGroundBirdEncounter(second);
                Assert.AreEqual("act1_mid_ground_bird", director.CurrentEncounterId);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
                if (tables != null)
                    UnityEngine.Object.DestroyImmediate(tables);
            }
        }

        static void AssertFixedGroundBirdEncounter(EnemyEncounterGroupState group)
        {
            var primary = group.GetEncounter(EnemyEncounterSlot.Primary);
            var support = group.GetEncounter(EnemyEncounterSlot.Support);

            Assert.IsNotNull(primary);
            Assert.IsNotNull(support);
            Assert.AreEqual(3001, primary.Enemy.EnemyId);
            Assert.AreEqual(3901, support.Enemy.EnemyId);
            Assert.AreEqual(EnemyBehaviorType.MeleeAdvance, primary.BehaviorType);
            Assert.AreEqual(EnemyBehaviorType.FlyingRangedOrigin, support.BehaviorType);
            Assert.AreEqual(0, support.DistanceStepsRemaining);
            Assert.AreEqual("bird", support.Enemy.PrefabKey);
        }
    }
}
