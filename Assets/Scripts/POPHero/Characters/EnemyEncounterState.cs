using UnityEngine;

namespace POPHero
{
    public sealed class EnemyEncounterState
    {
        public EnemyEncounterState(EnemyData enemy, int startingDistanceSteps, EnemyEncounterSlot slot)
        {
            Enemy = enemy;
            Slot = slot;
            StartingDistanceSteps = UsesApproachDistance ? Mathf.Max(0, startingDistanceSteps) : 0;
            DistanceStepsRemaining = StartingDistanceSteps;
        }

        public EnemyData Enemy { get; }
        public EnemyEncounterSlot Slot { get; }
        public int StartingDistanceSteps { get; }
        public int DistanceStepsRemaining { get; private set; }
        public EnemyBehaviorType BehaviorType => Enemy?.BehaviorType ?? EnemyBehaviorType.MeleeAdvance;
        public bool UsesApproachDistance => BehaviorType == EnemyBehaviorType.MeleeAdvance;
        public bool IsAlive => Enemy != null && Enemy.CurrentHp > 0;
        public bool IsInAttackRange => !UsesApproachDistance || DistanceStepsRemaining <= 0;
        public bool WillAttackOnNextTurn => IsAlive && (!UsesApproachDistance || DistanceStepsRemaining <= 1);

        public int AdvanceOneStep()
        {
            if (UsesApproachDistance && DistanceStepsRemaining > 0)
                DistanceStepsRemaining -= 1;

            return DistanceStepsRemaining;
        }

        public void SetDistanceRemaining(int steps)
        {
            DistanceStepsRemaining = UsesApproachDistance
                ? Mathf.Clamp(steps, 0, Mathf.Max(0, StartingDistanceSteps))
                : 0;
        }
    }
}
