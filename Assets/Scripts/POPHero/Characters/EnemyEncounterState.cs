using UnityEngine;

namespace POPHero
{
    public sealed class EnemyEncounterState
    {
        public EnemyEncounterState(EnemyData enemy, int startingDistanceSteps)
        {
            Enemy = enemy;
            StartingDistanceSteps = Mathf.Max(0, startingDistanceSteps);
            DistanceStepsRemaining = StartingDistanceSteps;
        }

        public EnemyData Enemy { get; }
        public int StartingDistanceSteps { get; }
        public int DistanceStepsRemaining { get; private set; }
        public bool IsAlive => Enemy != null && Enemy.CurrentHp > 0;
        public bool IsInAttackRange => DistanceStepsRemaining <= 0;
        public bool WillAttackOnNextTurn => IsAlive && DistanceStepsRemaining <= 1;

        public int AdvanceOneStep()
        {
            if (DistanceStepsRemaining > 0)
                DistanceStepsRemaining -= 1;

            return DistanceStepsRemaining;
        }

        public void SetDistanceRemaining(int steps)
        {
            DistanceStepsRemaining = Mathf.Clamp(steps, 0, Mathf.Max(0, StartingDistanceSteps));
        }
    }
}
