using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public enum EnemyTurnActionType
    {
        None,
        Advance,
        Attack
    }

    public readonly struct EnemyTurnOutcome
    {
        public EnemyTurnOutcome(EnemyEncounterSlot slot, EnemyBehaviorType behaviorType, EnemyTurnActionType actionType, int distanceBefore, int distanceAfter, int damageDealt, int playerHpAfterAction)
        {
            Slot = slot;
            BehaviorType = behaviorType;
            ActionType = actionType;
            DistanceBefore = Mathf.Max(0, distanceBefore);
            DistanceAfter = Mathf.Max(0, distanceAfter);
            DamageDealt = Mathf.Max(0, damageDealt);
            PlayerHpAfterAction = Mathf.Max(0, playerHpAfterAction);
        }

        public EnemyEncounterSlot Slot { get; }
        public EnemyBehaviorType BehaviorType { get; }
        public EnemyTurnActionType ActionType { get; }
        public int DistanceBefore { get; }
        public int DistanceAfter { get; }
        public int DamageDealt { get; }
        public int PlayerHpAfterAction { get; }
        public bool DidAdvance => DistanceAfter < DistanceBefore;
        public bool DidAttack => ActionType == EnemyTurnActionType.Attack;
        public bool IsRangedAttack => BehaviorType == EnemyBehaviorType.FlyingRangedOrigin && DidAttack;

        public static EnemyTurnOutcome None(EnemyEncounterSlot slot = EnemyEncounterSlot.Primary, EnemyBehaviorType behaviorType = EnemyBehaviorType.MeleeAdvance, int distanceSteps = 0, int playerHpAfterAction = 0)
        {
            return new EnemyTurnOutcome(slot, behaviorType, EnemyTurnActionType.None, distanceSteps, distanceSteps, 0, playerHpAfterAction);
        }
    }

    public sealed class EnemyTurnResolver
    {
        public List<EnemyTurnOutcome> ResolveGroup(IReadOnlyList<EnemyEncounterState> encounters, int counterReduction, PlayerData player)
        {
            var results = new List<EnemyTurnOutcome>(encounters?.Count ?? 0);
            if (encounters == null)
                return results;

            var sharedCounterReduction = Mathf.Max(0, counterReduction);
            for (var index = 0; index < encounters.Count; index++)
            {
                var encounter = encounters[index];
                if (encounter == null || encounter.Enemy == null || !encounter.IsAlive)
                    continue;

                results.Add(ResolveSingle(encounter, ref sharedCounterReduction, player));
            }

            return results;
        }

        EnemyTurnOutcome ResolveSingle(EnemyEncounterState encounter, ref int sharedCounterReduction, PlayerData player)
        {
            if (encounter == null || encounter.Enemy == null || !encounter.IsAlive)
                return EnemyTurnOutcome.None(playerHpAfterAction: player != null ? player.CurrentHp : 0);

            var behaviorType = encounter.BehaviorType;
            var distanceBefore = encounter.DistanceStepsRemaining;
            if (behaviorType == EnemyBehaviorType.MeleeAdvance && distanceBefore > 1)
            {
                var distanceAfterAdvance = encounter.AdvanceOneStep();
                return new EnemyTurnOutcome(encounter.Slot, behaviorType, EnemyTurnActionType.Advance, distanceBefore, distanceAfterAdvance, 0, player != null ? player.CurrentHp : 0);
            }

            if (behaviorType == EnemyBehaviorType.MeleeAdvance && distanceBefore == 1)
                encounter.AdvanceOneStep();

            var damage = ResolveIncomingDamage(encounter.Enemy.AttackDamage, ref sharedCounterReduction);
            if (damage > 0)
                player?.ApplyDamage(damage);

            return new EnemyTurnOutcome(
                encounter.Slot,
                behaviorType,
                EnemyTurnActionType.Attack,
                distanceBefore,
                encounter.DistanceStepsRemaining,
                damage,
                player != null ? player.CurrentHp : 0);
        }

        int ResolveIncomingDamage(int baseDamage, ref int sharedCounterReduction)
        {
            var clampedDamage = Mathf.Max(0, baseDamage);
            if (clampedDamage <= 0)
                return 0;

            var absorbed = Mathf.Min(sharedCounterReduction, clampedDamage);
            sharedCounterReduction = Mathf.Max(0, sharedCounterReduction - absorbed);
            return Mathf.Max(0, clampedDamage - absorbed);
        }
    }
}
