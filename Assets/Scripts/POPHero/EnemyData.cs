using UnityEngine;

namespace POPHero
{
    public class EnemyData
    {
        public string DisplayName { get; }
        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public int RewardGold { get; }
        public int RewardHeal { get; }
        public int AttackDamage { get; }
        public Color AccentColor { get; }

        public EnemyData(string displayName, int maxHp, int rewardGold, int rewardHeal, int attackDamage, Color accentColor)
        {
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? "敌人" : displayName;
            MaxHp = Mathf.Max(1, maxHp);
            CurrentHp = MaxHp;
            RewardGold = Mathf.Max(0, rewardGold);
            RewardHeal = Mathf.Max(0, rewardHeal);
            AttackDamage = Mathf.Max(0, attackDamage);
            AccentColor = accentColor;
        }

        public bool ApplyDamage(int amount)
        {
            CurrentHp = Mathf.Max(0, CurrentHp - Mathf.Max(0, amount));
            return CurrentHp <= 0;
        }
    }
}
