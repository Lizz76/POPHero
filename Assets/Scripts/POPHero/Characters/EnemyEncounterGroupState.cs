using System.Collections.Generic;

namespace POPHero
{
    public sealed class EnemyEncounterGroupState
    {
        readonly List<EnemyEncounterState> encounters = new(2);
        readonly List<EnemyEncounterState> aliveBuffer = new(2);

        public EnemyEncounterGroupState(EnemyEncounterState primaryEncounter, EnemyEncounterState supportEncounter = null)
        {
            if (primaryEncounter != null)
                encounters.Add(primaryEncounter);

            if (supportEncounter != null)
                encounters.Add(supportEncounter);
        }

        public IReadOnlyList<EnemyEncounterState> Encounters => encounters;
        public bool AllDefeated => GetPrimaryTarget() == null;

        public EnemyEncounterState GetEncounter(EnemyEncounterSlot slot)
        {
            for (var index = 0; index < encounters.Count; index++)
            {
                var encounter = encounters[index];
                if (encounter != null && encounter.Slot == slot)
                    return encounter;
            }

            return null;
        }

        public EnemyEncounterState GetPrimaryTarget()
        {
            var primary = GetEncounter(EnemyEncounterSlot.Primary);
            if (primary != null && primary.IsAlive)
                return primary;

            var support = GetEncounter(EnemyEncounterSlot.Support);
            if (support != null && support.IsAlive)
                return support;

            for (var index = 0; index < encounters.Count; index++)
            {
                var encounter = encounters[index];
                if (encounter != null && encounter.IsAlive)
                    return encounter;
            }

            return null;
        }

        public EnemyEncounterState GetSecondaryTarget()
        {
            var primary = GetPrimaryTarget();
            if (primary == null)
                return null;

            for (var index = 0; index < encounters.Count; index++)
            {
                var encounter = encounters[index];
                if (encounter != null && encounter.IsAlive && encounter != primary)
                    return encounter;
            }

            return null;
        }

        public IReadOnlyList<EnemyEncounterState> GetAliveEnemiesInTargetOrder()
        {
            aliveBuffer.Clear();

            var primary = GetEncounter(EnemyEncounterSlot.Primary);
            if (primary != null && primary.IsAlive)
                aliveBuffer.Add(primary);

            var support = GetEncounter(EnemyEncounterSlot.Support);
            if (support != null && support.IsAlive)
                aliveBuffer.Add(support);

            for (var index = 0; index < encounters.Count; index++)
            {
                var encounter = encounters[index];
                if (encounter != null && encounter.IsAlive && !aliveBuffer.Contains(encounter))
                    aliveBuffer.Add(encounter);
            }

            return aliveBuffer;
        }
    }
}
