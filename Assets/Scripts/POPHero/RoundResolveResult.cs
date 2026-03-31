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
    }
}
