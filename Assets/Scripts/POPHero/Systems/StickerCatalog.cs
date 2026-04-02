using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public class StickerCatalog
    {
        readonly Dictionary<string, StickerData> byId = new();
        readonly List<StickerData> allStickers = new();
        int instanceSerial;

        public StickerCatalog()
        {
            BuildCatalog();
        }

        public IReadOnlyList<StickerData> AllStickers => allStickers;

        public StickerData Get(string id)
        {
            return string.IsNullOrWhiteSpace(id) || !byId.TryGetValue(id, out var data) ? null : data;
        }

        public StickerInstance CreateInstance(string id)
        {
            var data = Get(id);
            if (data == null)
                return null;

            instanceSerial += 1;
            return new StickerInstance
            {
                runtimeId = $"sticker_{instanceSerial:0000}",
                data = data
            };
        }

        public StickerData GetRandomSticker(System.Predicate<StickerData> predicate = null)
        {
            var candidates = predicate == null ? allStickers : allStickers.FindAll(predicate);
            return candidates.Count == 0 ? null : candidates[Random.Range(0, candidates.Count)];
        }

        void BuildCatalog()
        {
            allStickers.Clear();
            byId.Clear();

            Add(Make("impact_core", "冲击核心", "命中攻击块时追加 6 点伤害。", StickerRarity.Common, SocketTargetMask.Attack, StickerFamily.Forge, StickerTriggerType.OnAttackBlockHit, 6f, detailA: "它只关心这一下。", detailB: "适合装在高频攻击块上。"));
            Add(Make("echo_mark", "回响印记", "第一次命中攻击块时埋下回响，下一次攻击块会吃掉它并追加 10 点伤害。", StickerRarity.Uncommon, SocketTargetMask.Attack, StickerFamily.Echo, StickerTriggerType.OnAttackBlockHit, 10f, detailA: "先记账，再爆发。", detailB: "更适合中长路线。", spawnType: "echo_mark", reactionType: "consume_echo"));
            Add(Make("shatter_loop", "碎裂回路", "同一块载体在本轮第二次命中时额外追加 8 点伤害。", StickerRarity.Uncommon, SocketTargetMask.Attack, StickerFamily.Forge, StickerTriggerType.OnAttackBlockHit, 8f, detailA: "鼓励你反复撞同一块。", detailB: "会留下碎裂标签供别的嵌片读取。", spawnType: "shatter"));
            Add(Make("guard_furnace", "护炉", "命中护盾块时额外获得 6 点护盾。", StickerRarity.Common, SocketTargetMask.Shield, StickerFamily.Ward, StickerTriggerType.OnShieldBlockHit, 6f, 0.3f, detailA: "回合结束时会把 30% 本轮护盾转成伤害。", detailB: "适合慢速稳扎路线。"));
            Add(Make("prism_guard", "棱镜护持", "若本轮碰过倍率块，护盾转伤比例额外提高 20%。", StickerRarity.Rare, SocketTargetMask.Shield, StickerFamily.Prism, StickerTriggerType.OnRoundEnd, 0.2f, detailA: "先拿倍率，再拿护盾会更赚。", detailB: "它读取的是整轮标签，不是单次命中。"));
            Add(Make("mirror_plating", "镜甲镀层", "命中过护盾块后，下一次攻击块会额外获得当前护盾一半的伤害。", StickerRarity.Rare, SocketTargetMask.Shield, StickerFamily.Alloy, StickerTriggerType.OnShieldBlockHit, detailA: "先守再攻。", detailB: "会消耗镜甲缓存。", spawnType: "mirror_charge"));
            Add(Make("amp_seed", "增幅种子", "命中倍率块时生成 1 层增幅标记。", StickerRarity.Common, SocketTargetMask.Multiplier, StickerFamily.Prism, StickerTriggerType.OnMultiplierBlockHit, 1f, detailA: "标记会被后续攻击块消耗。", detailB: "适合和爆发类嵌片配合。", spawnType: "amp_charge"));
            Add(Make("amp_burst", "增幅爆发", "命中攻击块时，每消耗 1 层增幅标记就额外造成 12 点伤害。", StickerRarity.Uncommon, SocketTargetMask.Attack, StickerFamily.Prism, StickerTriggerType.OnAttackBlockHit, 12f, detailA: "先囤倍率，再让攻击吃满。", detailB: "会消耗所有增幅标记。", reactionType: "consume_amp"));
            Add(Make("twin_resonance", "双棱共振", "连续命中两个不同倍率块时，第二个额外追加一次 1.4 倍放大。", StickerRarity.Rare, SocketTargetMask.Multiplier, StickerFamily.Prism, StickerTriggerType.OnMultiplierBlockHit, 1.4f, detailA: "要求线路真的串得起来。", detailB: "会在连续条件失效时重置。"));
            Add(Make("chain_ledger", "连锁账本", "每命中不同 family 的载体就积一层链，重复 family 会断链。", StickerRarity.Rare, SocketTargetMask.Any, StickerFamily.Chain, StickerTriggerType.OnRoundEnd, 3f, detailA: "回合结束时每层链额外结算 3 点伤害。", detailB: "它奖励路线多样性。"));
            Add(Make("ember_seed", "余烬种", "命中攻击块时生成 1 层余烬。", StickerRarity.Common, SocketTargetMask.Attack, StickerFamily.Ember, StickerTriggerType.OnAttackBlockHit, 1f, detailA: "余烬本身不结算。", detailB: "它等着被其他嵌片接住。", spawnType: "ember"));
            Add(Make("ember_catcher", "引火护套", "命中护盾块时会接住全部余烬，每层转成 4 点护盾与 2 点伤害。", StickerRarity.Uncommon, SocketTargetMask.Shield, StickerFamily.Ember, StickerTriggerType.OnShieldBlockHit, 4f, 2f, detailA: "它把攻击状态拐进防御，再带回输出。", detailB: "会吃掉所有余烬。", reactionType: "catch_ember"));
            Add(Make("thorn_rack", "棘刺架", "命中护盾块时生成 1 层棘刺，敌人反击时每层减伤 2 点。", StickerRarity.Uncommon, SocketTargetMask.Shield, StickerFamily.Thorn, StickerTriggerType.OnShieldBlockHit, 2f, detailA: "它不是护盾本身，而是削弱反击。", detailB: "回合结束时会一起清空。", spawnType: "thorn"));
            Add(Make("spark_tape", "火花胶带", "倍率块会生成火花；下一次护盾块把它转成护盾，下一次攻击块把它转成伤害。", StickerRarity.Uncommon, SocketTargetMask.Any, StickerFamily.Spark, StickerTriggerType.OnBlockHit, 5f, 7f, detailA: "同一种中间状态，会被不同 family 吃成不同收益。", detailB: "没有消费前会一直保留到回合结束。", spawnType: "spark"));
            Add(Make("same_family_latch", "同调卡榫", "同一卡片上若装有至少 2 个同 family 嵌片，则它每回合首次触发额外 +8。", StickerRarity.Rare, SocketTargetMask.Any, StickerFamily.Alloy, StickerTriggerType.OnBlockHit, 8f, detailA: "它鼓励你做集中式构筑。", detailB: "和同族加成模组叠起来会很猛。"));
            Add(Make("breaker_note", "破阵便签", "如果本轮已经碰过倍率块，后续攻击块每次额外 +8。", StickerRarity.Common, SocketTargetMask.Attack, StickerFamily.Prism, StickerTriggerType.OnAttackBlockHit, 8f, detailA: "先搭增幅，再吃稳定补刀。", detailB: "它读取的是整轮状态。"));
            Add(Make("glass_ledger", "玻璃账册", "回合结束时，每个不同 family 额外结算 2 点伤害。", StickerRarity.Uncommon, SocketTargetMask.Any, StickerFamily.Chain, StickerTriggerType.OnRoundEnd, 2f, detailA: "比连锁账本更稳，但上限略低。", detailB: "适合多样板面。"));
            Add(Make("frost_trace", "霜迹", "命中护盾块时生成霜迹；下一次倍率块会额外再放大一次 1.2 倍。", StickerRarity.Rare, SocketTargetMask.Shield, StickerFamily.Frost, StickerTriggerType.OnShieldBlockHit, 1.2f, detailA: "它让护盾路线能反哺倍率路线。", detailB: "会消耗霜迹。", spawnType: "frost_trace"));
            Add(Make("alloy_echo", "合金回声", "命中过的同一载体第三次再被击中时，追加 10 点伤害并获得 4 点护盾。", StickerRarity.Epic, SocketTargetMask.Any, StickerFamily.Alloy, StickerTriggerType.OnBlockHit, 10f, 4f, detailA: "它奖励你记住高价值反弹位。", detailB: "要真的反复撞到同一块才触发。"));
        }

        StickerData Make(string id, string name, string mainActionText, StickerRarity rarity, SocketTargetMask targetMask, StickerFamily family, StickerTriggerType triggerType, float valueA = 0f, float valueB = 0f, float valueC = 0f, string detailA = null, string detailB = null, string spawnType = null, string reactionType = null)
        {
            var data = new StickerData
            {
                id = id,
                name = name,
                shortTitle = name,
                mainActionText = mainActionText,
                rarity = rarity,
                targetBlockType = targetMask,
                family = family,
                triggerType = triggerType,
                valueA = valueA,
                valueB = valueB,
                valueC = valueC,
                spawnType = spawnType,
                reactionType = reactionType
            };
            if (!string.IsNullOrWhiteSpace(detailA))
                data.detailLines.Add(detailA);
            if (!string.IsNullOrWhiteSpace(detailB))
                data.detailLines.Add(detailB);
            return data;
        }

        void Add(StickerData data)
        {
            allStickers.Add(data);
            byId[data.id] = data;
        }
    }
}
