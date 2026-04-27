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
        readonly List<EnemyEncounterState> currentEnemyEncounters = new(3);

        public EncounterDirector(GameRuntimeContext context)
        {
            this.context = context;
        }

        public EnemyEncounterGroupState CurrentEnemyGroup { get; private set; }
        public EnemyEncounterState CurrentEnemyEncounter { get; private set; }
        public EnemyData CurrentEnemy => CurrentEnemyEncounter?.Enemy;
        public IReadOnlyList<EnemyEncounterState> CurrentEnemyEncounters => currentEnemyEncounters;
        public string CurrentEncounterId { get; private set; }

        PopHeroPrototypeConfig Config => context?.Config;

        public void Reset()
        {
            CurrentEnemyGroup = null;
            CurrentEnemyEncounter = null;
            CurrentEncounterId = string.Empty;
            currentEnemyEncounters.Clear();
        }

        public EnemyEncounterGroupState SpawnEncounter(int index)
        {
            CurrentEnemyGroup = BuildEnemyEncounterGroupForIndex(index);
            CurrentEncounterId = string.Empty;
            RefreshTargetSelection();
            return CurrentEnemyGroup;
        }

        public EnemyEncounterGroupState SpawnEncounter(string encounterId)
        {
            CurrentEnemyGroup = BuildEnemyEncounterGroupForId(encounterId);
            CurrentEncounterId = encounterId ?? string.Empty;
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
            return new EnemyEncounterGroupState(primaryEncounter);
        }

        EnemyEncounterGroupState BuildEnemyEncounterGroupForId(string encounterId)
        {
            var encounter = FindEncounter(encounterId) ?? FindFirstEncounter();
            if (encounter == null || encounter.enemies == null || encounter.enemies.Count == 0)
                return BuildEnemyEncounterGroupForIndex(0);

            var built = new List<EnemyEncounterState>(encounter.enemies.Count);
            for (var index = 0; index < encounter.enemies.Count; index++)
            {
                var entry = encounter.enemies[index];
                var template = FindEnemyTemplate(entry.enemyId);
                var state = BuildEncounterFromTemplate(template, 0, entry.slot);
                if (state != null)
                    built.Add(state);
            }

            return built.Count > 0 ? new EnemyEncounterGroupState(built) : BuildEnemyEncounterGroupForIndex(0);
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

            var enemy = new EnemyData(name, hp, rewardGold, rewardHeal, attackDamage, template.color, template.behaviorType, template.enemyId, template.prefabKey, template.abilityIds);
            return new EnemyEncounterState(enemy, initialDistanceSteps, slot);
        }

        EncounterDef FindEncounter(string encounterId)
        {
            if (string.IsNullOrWhiteSpace(encounterId))
                return null;

            var encounters = context?.Tables?.EncounterDefs;
            if (encounters == null)
                return null;

            for (var index = 0; index < encounters.Count; index++)
            {
                var encounter = encounters[index];
                if (encounter != null && string.Equals(encounter.encounterId, encounterId, System.StringComparison.OrdinalIgnoreCase))
                    return encounter;
            }

            return null;
        }

        EncounterDef FindFirstEncounter()
        {
            var encounters = context?.Tables?.EncounterDefs;
            if (encounters == null)
                return null;

            for (var index = 0; index < encounters.Count; index++)
            {
                if (encounters[index] != null)
                    return encounters[index];
            }

            return null;
        }

        EnemyTemplate FindEnemyTemplate(int enemyId)
        {
            var templates = Config?.enemies?.templates;
            if (templates == null || templates.Count == 0)
                return null;

            for (var index = 0; index < templates.Count; index++)
            {
                var template = templates[index];
                if (template != null && template.enemyId == enemyId)
                    return template;
            }

            return templates[0];
        }
    }
}
