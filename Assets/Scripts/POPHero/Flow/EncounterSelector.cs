using System;
using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public sealed class EncounterSelector
    {
        const int RecentNormalLimit = 2;

        readonly Func<int, int, int> randomRange;
        readonly List<string> recentNormalEncounterIds = new(RecentNormalLimit);
        readonly List<EncounterDef> candidates = new();
        readonly List<EncounterDef> filteredCandidates = new();

        public EncounterSelector(Func<int, int, int> randomRange = null)
        {
            this.randomRange = randomRange ?? ((minInclusive, maxExclusive) => UnityEngine.Random.Range(minInclusive, maxExclusive));
        }

        public IReadOnlyList<string> RecentNormalEncounterIds => recentNormalEncounterIds;

        public void ResetHistory()
        {
            recentNormalEncounterIds.Clear();
        }

        public EncounterDef Select(IReadOnlyList<EncounterDef> encounters, int act, EncounterNodeType nodeType, int floor)
        {
            candidates.Clear();
            filteredCandidates.Clear();

            if (encounters != null)
            {
                for (var index = 0; index < encounters.Count; index++)
                {
                    var encounter = encounters[index];
                    if (!IsEligible(encounter, act, nodeType, floor))
                        continue;

                    candidates.Add(encounter);
                    if (nodeType != EncounterNodeType.Normal || encounter.allowRepeat || !recentNormalEncounterIds.Contains(encounter.encounterId))
                        filteredCandidates.Add(encounter);
                }
            }

            var pool = filteredCandidates.Count > 0 ? filteredCandidates : candidates;
            if (pool.Count == 0)
            {
                var fallback = FindFallback(encounters, act, nodeType);
                Record(fallback);
                return fallback;
            }

            var selected = PickWeighted(pool);
            Record(selected);
            return selected;
        }

        static bool IsEligible(EncounterDef encounter, int act, EncounterNodeType nodeType, int floor)
        {
            if (encounter == null || string.IsNullOrWhiteSpace(encounter.encounterId) || encounter.weight <= 0 || encounter.enemies == null || encounter.enemies.Count == 0)
                return false;

            var clampedFloor = Mathf.Max(1, floor);
            return encounter.act == Mathf.Max(1, act) &&
                   encounter.nodeType == nodeType &&
                   clampedFloor >= Mathf.Max(1, encounter.minFloor) &&
                   clampedFloor <= Mathf.Max(Mathf.Max(1, encounter.minFloor), encounter.maxFloor);
        }

        static EncounterDef FindFallback(IReadOnlyList<EncounterDef> encounters, int act, EncounterNodeType nodeType)
        {
            var fallback = FindFirst(encounters, encounter => encounter.act == Mathf.Max(1, act) && encounter.nodeType == nodeType);
            if (fallback != null)
                return fallback;

            fallback = FindFirst(encounters, encounter => encounter.nodeType == nodeType);
            if (fallback != null)
                return fallback;

            fallback = FindFirst(encounters, _ => true);
            if (fallback != null)
                return fallback;

            return null;
        }

        static EncounterDef FindFirst(IReadOnlyList<EncounterDef> encounters, Func<EncounterDef, bool> predicate)
        {
            if (encounters == null)
                return null;

            for (var index = 0; index < encounters.Count; index++)
            {
                var encounter = encounters[index];
                if (encounter != null &&
                    !string.IsNullOrWhiteSpace(encounter.encounterId) &&
                    encounter.enemies != null &&
                    encounter.enemies.Count > 0 &&
                    predicate(encounter))
                {
                    return encounter;
                }
            }

            return null;
        }

        EncounterDef PickWeighted(IReadOnlyList<EncounterDef> pool)
        {
            var totalWeight = 0;
            for (var index = 0; index < pool.Count; index++)
                totalWeight += Mathf.Max(0, pool[index]?.weight ?? 0);

            if (totalWeight <= 0)
                return pool.Count > 0 ? pool[0] : null;

            var roll = randomRange(0, totalWeight);
            for (var index = 0; index < pool.Count; index++)
            {
                var weight = Mathf.Max(0, pool[index]?.weight ?? 0);
                if (roll < weight)
                    return pool[index];

                roll -= weight;
            }

            return pool[pool.Count - 1];
        }

        void Record(EncounterDef encounter)
        {
            if (encounter == null || encounter.nodeType != EncounterNodeType.Normal || string.IsNullOrWhiteSpace(encounter.encounterId))
                return;

            recentNormalEncounterIds.Add(encounter.encounterId);
            while (recentNormalEncounterIds.Count > RecentNormalLimit)
                recentNormalEncounterIds.RemoveAt(0);
        }
    }
}
