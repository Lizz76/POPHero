using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public readonly struct EncounterClearRewardSummary
    {
        public EncounterClearRewardSummary(int rewardGold, int rewardHeal, int defeatedEnemyCount)
        {
            RewardGold = Mathf.Max(0, rewardGold);
            RewardHeal = Mathf.Max(0, rewardHeal);
            DefeatedEnemyCount = Mathf.Max(0, defeatedEnemyCount);
        }

        public int RewardGold { get; }
        public int RewardHeal { get; }
        public int DefeatedEnemyCount { get; }
    }

    public sealed class EncounterDirector
    {
        readonly GameRuntimeContext context;
        readonly List<EnemyEncounterState> currentEnemyEncounters = new(2);

        public EncounterDirector(GameRuntimeContext context)
        {
            this.context = context;
        }

        public EnemyEncounterGroupState CurrentEnemyGroup { get; private set; }
        public EnemyEncounterState CurrentEnemyEncounter { get; private set; }
        public EnemyData CurrentEnemy => CurrentEnemyEncounter?.Enemy;
        public IReadOnlyList<EnemyEncounterState> CurrentEnemyEncounters => currentEnemyEncounters;

        PopHeroPrototypeConfig Config => context?.Config;

        public void Reset()
        {
            CurrentEnemyGroup = null;
            CurrentEnemyEncounter = null;
            currentEnemyEncounters.Clear();
        }

        public EnemyEncounterGroupState SpawnEncounter(int index)
        {
            CurrentEnemyGroup = BuildEnemyEncounterGroupForIndex(index);
            RefreshTargetSelection();
            return CurrentEnemyGroup;
        }

        public void RefreshTargetSelection()
        {
            currentEnemyEncounters.Clear();
            CurrentEnemyEncounter = null;

            if (CurrentEnemyGroup == null)
                return;

            var aliveEncounters = CurrentEnemyGroup.GetAliveEnemiesInTargetOrder();
            for (var index = 0; index < aliveEncounters.Count; index++)
                currentEnemyEncounters.Add(aliveEncounters[index]);

            CurrentEnemyEncounter = currentEnemyEncounters.Count > 0 ? currentEnemyEncounters[0] : null;
        }

        public EncounterClearRewardSummary BuildClearRewardSummary()
        {
            var totalRewardGold = 0;
            var totalRewardHeal = 0;
            var defeatedEnemyCount = 0;
            if (CurrentEnemyGroup == null)
                return new EncounterClearRewardSummary(0, 0, 0);

            var encounters = CurrentEnemyGroup.Encounters;
            for (var index = 0; index < encounters.Count; index++)
            {
                var enemy = encounters[index]?.Enemy;
                if (enemy == null)
                    continue;

                totalRewardGold += enemy.RewardGold;
                totalRewardHeal += enemy.RewardHeal;
                defeatedEnemyCount += 1;
            }

            return new EncounterClearRewardSummary(totalRewardGold, totalRewardHeal, defeatedEnemyCount);
        }

        EnemyEncounterGroupState BuildEnemyEncounterGroupForIndex(int index)
        {
            var config = Config;
            if (config?.enemies?.templates == null || config.enemies.templates.Count == 0)
                return new EnemyEncounterGroupState(null);

            var templates = config.enemies.templates;
            var clampedIndex = Mathf.Clamp(index, 0, Mathf.Max(0, templates.Count - 1));
            var template = templates[clampedIndex];
            var overflow = Mathf.Max(0, index - (templates.Count - 1));
            var primaryEncounter = BuildEncounterFromTemplate(template, overflow, EnemyEncounterSlot.Primary);
            var supportEncounter = index >= 1 ? BuildFlyingSupportEncounter(overflow) : null;
            return new EnemyEncounterGroupState(primaryEncounter, supportEncounter);
        }

        EnemyEncounterState BuildEncounterFromTemplate(EnemyTemplate template, int overflow, EnemyEncounterSlot slot)
        {
            var config = Config;
            if (template == null || config?.enemies == null)
                return null;

            var hp = template.maxHp + overflow * config.enemies.endlessHpGrowth;
            var rewardGold = template.rewardGold + overflow * config.enemies.endlessGoldGrowth;
            var rewardHeal = template.rewardHeal + overflow * config.enemies.endlessHealGrowth;
            var attackDamage = template.attackDamage + overflow * config.enemies.endlessAttackGrowth;
            var baseName = string.IsNullOrWhiteSpace(template.displayName) ? "敌人" : template.displayName;
            var name = overflow > 0 ? $"{baseName}+{overflow}" : baseName;
            var initialDistanceSteps = template.behaviorType == EnemyBehaviorType.MeleeAdvance && template.initialDistanceStepsOverride >= 0
                ? template.initialDistanceStepsOverride
                : config.enemies.defaultInitialDistanceSteps;
            if (template.behaviorType == EnemyBehaviorType.FlyingRangedOrigin)
                initialDistanceSteps = 0;

            var enemy = new EnemyData(name, hp, rewardGold, rewardHeal, attackDamage, template.color, template.behaviorType);
            return new EnemyEncounterState(enemy, initialDistanceSteps, slot);
        }

        EnemyEncounterState BuildFlyingSupportEncounter(int overflow)
        {
            var template = Config?.enemies?.flyingSupportTemplate;
            if (template == null || template.behaviorType != EnemyBehaviorType.FlyingRangedOrigin)
                return null;

            return BuildEncounterFromTemplate(template, overflow, EnemyEncounterSlot.Support);
        }
    }
}
