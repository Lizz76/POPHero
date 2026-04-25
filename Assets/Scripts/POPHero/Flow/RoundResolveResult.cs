using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public struct EnemyResolveEntry
    {
        public EnemyEncounterSlot slot;
        public EnemyBehaviorType behaviorType;
        public EnemyData enemy;
        public bool wasTargeted;
        public bool wasDefeated;
        public int damageTaken;
        public int displayHpBefore;
        public int displayHpAfter;
        public int maxHp;
    }

    public struct RoundResolveResult
    {
        public Vector2 landingPoint;
        public int attackDamage;
        public int shieldGain;
        public int hitCount;
        public EnemyEncounterSlot targetSlot;
        public EnemyTurnOutcome enemyTurn;
        public int enemyCounterDamage;
        public bool enemyDefeated;
        public bool encounterCleared;
        public bool playerDefeated;
        public int enemyDisplayHpBeforeHit;
        public int enemyDisplayHpAfterHit;
        public int playerDisplayHpBeforeCounter;
        public int playerDisplayHpAfterCounter;
        public List<EnemyResolveEntry> enemyResults;
        public List<EnemyTurnOutcome> enemyTurns;

        public EnemyResolveEntry? FindEnemyResult(EnemyEncounterSlot slot)
        {
            if (enemyResults == null)
                return null;

            for (var index = 0; index < enemyResults.Count; index++)
            {
                if (enemyResults[index].slot == slot)
                    return enemyResults[index];
            }

            return null;
        }
    }
}
