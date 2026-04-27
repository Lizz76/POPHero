using System;
using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public enum EnemyBehaviorType
    {
        MeleeAdvance,
        FlyingRangedOrigin
    }

    public enum EnemyEncounterSlot
    {
        Primary,
        Mid,
        Support
    }

    public class EnemyData
    {
        public int EnemyId { get; }
        public string DisplayName { get; }
        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public int RewardGold { get; }
        public int RewardHeal { get; }
        public int AttackDamage { get; }
        public Color AccentColor { get; }
        public EnemyBehaviorType BehaviorType { get; }
        public string PrefabKey { get; }
        public IReadOnlyList<string> AbilityIds { get; }

        public EnemyData(string displayName, int maxHp, int rewardGold, int rewardHeal, int attackDamage, Color accentColor, EnemyBehaviorType behaviorType = EnemyBehaviorType.MeleeAdvance, int enemyId = 0, string prefabKey = null, IEnumerable<string> abilityIds = null)
        {
            EnemyId = Mathf.Max(0, enemyId);
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Enemy" : displayName;
            MaxHp = Mathf.Max(1, maxHp);
            CurrentHp = MaxHp;
            RewardGold = Mathf.Max(0, rewardGold);
            RewardHeal = Mathf.Max(0, rewardHeal);
            AttackDamage = Mathf.Max(0, attackDamage);
            AccentColor = accentColor;
            BehaviorType = behaviorType;
            PrefabKey = EnemyPrefabRegistry.NormalizeKey(prefabKey);
            AbilityIds = BuildAbilityIds(abilityIds);
        }

        public bool ApplyDamage(int amount)
        {
            CurrentHp = Mathf.Max(0, CurrentHp - Mathf.Max(0, amount));
            return CurrentHp <= 0;
        }

        static IReadOnlyList<string> BuildAbilityIds(IEnumerable<string> abilityIds)
        {
            if (abilityIds == null)
                return Array.AsReadOnly(new[] { "none" });

            var result = new List<string>();
            foreach (var abilityId in abilityIds)
            {
                if (!string.IsNullOrWhiteSpace(abilityId))
                    result.Add(abilityId.Trim());
            }

            if (result.Count == 0)
                result.Add("none");

            return result.AsReadOnly();
        }
    }
}
