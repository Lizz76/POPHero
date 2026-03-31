using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace POPHero
{
    public class BuffManager : MonoBehaviour
    {
        public class BuffChoice
        {
            public string buffId;
            public string name;
            public string description;
            public BuffEffectType buffType;
            public float value;
        }

        readonly List<BuffChoice> buffPool = new();
        readonly List<BuffChoice> activeChoices = new();
        readonly List<BuffChoice> acquiredChoices = new();

        PopHeroGame game;

        public IReadOnlyList<BuffChoice> ActiveChoices => activeChoices;
        public IReadOnlyList<BuffChoice> AcquiredChoices => acquiredChoices;

        public void Initialize(PopHeroGame owner)
        {
            game = owner;
            RebuildPool();
            activeChoices.Clear();
            acquiredChoices.Clear();
        }

        public void GenerateChoices()
        {
            activeChoices.Clear();
            var shuffled = new List<BuffChoice>(buffPool);
            Shuffle(shuffled);
            var count = Mathf.Min(game.config.buffs.choicesPerReward, shuffled.Count);
            for (var i = 0; i < count; i++)
                activeChoices.Add(shuffled[i]);
        }

        public bool TryApplyChoice(int index)
        {
            if (index < 0 || index >= activeChoices.Count)
                return false;

            var choice = activeChoices[index];
            ApplyChoice(choice);
            acquiredChoices.Add(choice);
            activeChoices.Clear();
            return true;
        }

        public IEnumerable<string> GetAppliedBuffLines()
        {
            return acquiredChoices.Select(choice => choice.name);
        }

        void ApplyChoice(BuffChoice choice)
        {
            switch (choice.buffType)
            {
                case BuffEffectType.AttackAddAll:
                    game.BoardManager.AddAttackToAll(Mathf.RoundToInt(choice.value));
                    break;
                case BuffEffectType.ShieldAddAll:
                    game.BoardManager.AddShieldToAll(Mathf.RoundToInt(choice.value));
                    break;
                case BuffEffectType.MultiplierAddAll:
                    game.BoardManager.AddMultiplierToAll(choice.value);
                    break;
                case BuffEffectType.UpgradeRandomAttack:
                    game.BoardManager.UpgradeRandomAttackBlock(Mathf.RoundToInt(choice.value));
                    break;
                case BuffEffectType.UpgradeRandomShield:
                    game.BoardManager.UpgradeRandomShieldBlock(Mathf.RoundToInt(choice.value));
                    break;
                case BuffEffectType.AddAttackBlock:
                    game.BoardManager.AddRandomAttackBlock(Mathf.RoundToInt(choice.value));
                    break;
            }
        }

        void RebuildPool()
        {
            buffPool.Clear();
            var buffSettings = game.config.buffs;
            buffPool.Add(new BuffChoice
            {
                buffId = "atk_all",
                name = "锋刃校准",
                description = $"所有攻击加法方块 +{buffSettings.attackAddBonus}",
                buffType = BuffEffectType.AttackAddAll,
                value = buffSettings.attackAddBonus
            });
            buffPool.Add(new BuffChoice
            {
                buffId = "shield_all",
                name = "护壁加固",
                description = $"所有护盾方块 +{buffSettings.shieldBonus}",
                buffType = BuffEffectType.ShieldAddAll,
                value = buffSettings.shieldBonus
            });
            buffPool.Add(new BuffChoice
            {
                buffId = "multi_all",
                name = "回路放大",
                description = $"所有倍率方块 +{buffSettings.multiplierBonus:0.0#}",
                buffType = BuffEffectType.MultiplierAddAll,
                value = buffSettings.multiplierBonus
            });
            buffPool.Add(new BuffChoice
            {
                buffId = "upgrade_atk",
                name = "暴击芯片",
                description = $"随机 1 个攻击方块升级为 +{buffSettings.upgradedAttackValue}",
                buffType = BuffEffectType.UpgradeRandomAttack,
                value = buffSettings.upgradedAttackValue
            });
            buffPool.Add(new BuffChoice
            {
                buffId = "upgrade_shield",
                name = "壁垒芯片",
                description = $"随机 1 个护盾方块升级为 +{buffSettings.upgradedShieldValue}",
                buffType = BuffEffectType.UpgradeRandomShield,
                value = buffSettings.upgradedShieldValue
            });
            buffPool.Add(new BuffChoice
            {
                buffId = "add_block",
                name = "额外弹点",
                description = "方块池 +1，并新增 1 个攻击方块",
                buffType = BuffEffectType.AddAttackBlock,
                value = buffSettings.additionalAttackBlockValue
            });
        }

        static void Shuffle<T>(IList<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var swapIndex = Random.Range(0, i + 1);
                (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
            }
        }
    }
}
