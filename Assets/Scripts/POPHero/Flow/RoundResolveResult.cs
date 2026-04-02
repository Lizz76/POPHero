using UnityEngine;

namespace POPHero
{
    public struct RoundResolveResult
    {
        public Vector2 landingPoint;
        public int attackDamage;
        public int shieldGain;
        public int hitCount;
        public int enemyCounterDamage;
        public bool enemyDefeated;
        public bool playerDefeated;
        public int enemyDisplayHpBeforeHit;
        public int enemyDisplayHpAfterHit;
        public int playerDisplayHpBeforeCounter;
        public int playerDisplayHpAfterCounter;
    }
}
