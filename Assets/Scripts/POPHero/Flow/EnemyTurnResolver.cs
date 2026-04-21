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
        public EnemyTurnOutcome(EnemyTurnActionType actionType, int distanceBefore, int distanceAfter, int damageDealt)
        {
            ActionType = actionType;
            DistanceBefore = Mathf.Max(0, distanceBefore);
            DistanceAfter = Mathf.Max(0, distanceAfter);
            DamageDealt = Mathf.Max(0, damageDealt);
        }

        public EnemyTurnActionType ActionType { get; }
        public int DistanceBefore { get; }
        public int DistanceAfter { get; }
        public int DamageDealt { get; }
        public bool DidAdvance => DistanceAfter < DistanceBefore;
        public bool DidAttack => ActionType == EnemyTurnActionType.Attack;

        public static EnemyTurnOutcome None(int distanceSteps = 0)
        {
            return new EnemyTurnOutcome(EnemyTurnActionType.None, distanceSteps, distanceSteps, 0);
        }
    }

    public sealed class EnemyTurnResolver
    {
        public EnemyTurnOutcome Resolve(EnemyEncounterState encounter, int counterReduction, PlayerData player)
        {
            if (encounter == null || encounter.Enemy == null || !encounter.IsAlive)
                return EnemyTurnOutcome.None();

            var distanceBefore = encounter.DistanceStepsRemaining;
            if (distanceBefore > 1)
            {
                var distanceAfterAdvance = encounter.AdvanceOneStep();
                return new EnemyTurnOutcome(EnemyTurnActionType.Advance, distanceBefore, distanceAfterAdvance, 0);
            }

            if (distanceBefore == 1)
                encounter.AdvanceOneStep();

            var damage = Mathf.Max(0, encounter.Enemy.AttackDamage - Mathf.Max(0, counterReduction));
            if (damage > 0)
                player?.ApplyDamage(damage);

            return new EnemyTurnOutcome(EnemyTurnActionType.Attack, distanceBefore, encounter.DistanceStepsRemaining, damage);
        }
    }
}
