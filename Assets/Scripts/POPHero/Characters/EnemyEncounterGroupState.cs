using System.Collections.Generic;

namespace POPHero
{
    public sealed class EnemyEncounterGroupState
    {
        readonly List<EnemyEncounterState> encounters = new(3);
        readonly List<EnemyEncounterState> aliveBuffer = new(3);

        public EnemyEncounterGroupState(EnemyEncounterState primaryEncounter, EnemyEncounterState supportEncounter = null)
            : this(primaryEncounter, null, supportEncounter)
        {
        }

        public EnemyEncounterGroupState(EnemyEncounterState primaryEncounter, EnemyEncounterState midEncounter, EnemyEncounterState supportEncounter)
        {
            if (primaryEncounter != null)
                encounters.Add(primaryEncounter);

            if (midEncounter != null)
                encounters.Add(midEncounter);

            if (supportEncounter != null)
                encounters.Add(supportEncounter);
        }

        public EnemyEncounterGroupState(IEnumerable<EnemyEncounterState> encounterStates)
        {
            if (encounterStates == null)
                return;

            foreach (var encounter in encounterStates)
            {
                if (encounter != null)
                    encounters.Add(encounter);
            }
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

            var mid = GetEncounter(EnemyEncounterSlot.Mid);
            if (mid != null && mid.IsAlive)
                return mid;

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

            var mid = GetEncounter(EnemyEncounterSlot.Mid);
            if (mid != null && mid.IsAlive)
                aliveBuffer.Add(mid);

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
