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

            Add(Make("impact_core", "冲击核心", "攻击方块命中时额外获得 +6 伤害。", StickerRarity.Common, SocketTargetMask.Attack, StickerFamily.Forge, StickerTriggerType.OnAttackBlockHit, 6f, detailA: "只关注当前这一次命中。", detailB: "适合高频命中的攻击方块。"));
            Add(Make("echo_mark", "回响印记", "第一次命中攻击方块会留下回响，下一次攻击方块命中时消耗回响并获得 +10 伤害。", StickerRarity.Uncommon, SocketTargetMask.Attack, StickerFamily.Echo, StickerTriggerType.OnAttackBlockHit, 10f, detailA: "先存值，再兑现。", detailB: "中长路线的收益更高。", spawnType: "echo_mark", reactionType: "consume_echo"));
            Add(Make("shatter_loop", "碎裂回路", "本回合同一张方块第二次被命中时，额外获得 +8 伤害。", StickerRarity.Uncommon, SocketTargetMask.Attack, StickerFamily.Forge, StickerTriggerType.OnAttackBlockHit, 8f, detailA: "鼓励你再次弹到同一张方块。", detailB: "会留下碎裂标签供其他嵌片利用。", spawnType: "shatter"));
            Add(Make("guard_furnace", "护炉", "防御方块命中时额外获得 +6 护盾。", StickerRarity.Common, SocketTargetMask.Shield, StickerFamily.Ward, StickerTriggerType.OnShieldBlockHit, 6f, 0.3f, detailA: "回合结束时，本回合 30% 的护盾会转化为伤害。", detailB: "适合更慢、更稳的路线。"));
            Add(Make("prism_guard", "棱镜守御", "如果本回合命中过倍率方块，护盾转伤的比例额外 +20%。", StickerRarity.Rare, SocketTargetMask.Shield, StickerFamily.Prism, StickerTriggerType.OnRoundEnd, 0.2f, detailA: "先吃倍率，再拿护盾，收益更高。", detailB: "读取的是整回合标签，而不是单次命中。"));
            Add(Make("mirror_plating", "镜面镀层", "命中过防御方块后，下一次攻击方块造成等于当前护盾一半的额外伤害。", StickerRarity.Rare, SocketTargetMask.Shield, StickerFamily.Alloy, StickerTriggerType.OnShieldBlockHit, detailA: "先防守，再进攻。", detailB: "会消耗储存的镜像充能。", spawnType: "mirror_charge"));
            Add(Make("amp_seed", "增幅种子", "倍率方块命中时生成 1 层增幅充能。", StickerRarity.Common, SocketTargetMask.Multiplier, StickerFamily.Prism, StickerTriggerType.OnMultiplierBlockHit, 1f, detailA: "后续攻击方块可以消耗这些充能。", detailB: "和爆发类嵌片搭配很好。", spawnType: "amp_charge"));
            Add(Make("amp_burst", "增幅爆发", "攻击方块每消耗 1 层增幅充能，就额外造成 +12 伤害。", StickerRarity.Uncommon, SocketTargetMask.Attack, StickerFamily.Prism, StickerTriggerType.OnAttackBlockHit, 12f, detailA: "先存倍率，再把它花在攻击上。", detailB: "会消耗所有已储存的增幅充能。", reactionType: "consume_amp"));
            Add(Make("twin_resonance", "双重共振", "连续命中两个不同的倍率方块时，第二个倍率方块额外获得 x1.4 加成。", StickerRarity.Rare, SocketTargetMask.Multiplier, StickerFamily.Prism, StickerTriggerType.OnMultiplierBlockHit, 1.4f, detailA: "需要真正连起来的路线。", detailB: "一旦顺序被打断就会重置。"));
            Add(Make("chain_ledger", "连锁账本", "每命中一种不同家族的方块就获得 1 层链；重复家族会断链。", StickerRarity.Rare, SocketTargetMask.Any, StickerFamily.Chain, StickerTriggerType.OnRoundEnd, 3f, detailA: "回合结束时，每层链额外 +3 伤害。", detailB: "奖励路线多样性。"));
            Add(Make("ember_seed", "余烬种子", "攻击方块命中时生成 1 层余烬。", StickerRarity.Common, SocketTargetMask.Attack, StickerFamily.Ember, StickerTriggerType.OnAttackBlockHit, 1f, detailA: "余烬自身不会直接产生效果。", detailB: "它会等待其他嵌片来接住它。", spawnType: "ember"));
            Add(Make("ember_catcher", "余烬捕手", "防御方块会接住所有余烬，并把每层余烬转成 4 护盾和 2 伤害。", StickerRarity.Uncommon, SocketTargetMask.Shield, StickerFamily.Ember, StickerTriggerType.OnShieldBlockHit, 4f, 2f, detailA: "把攻击状态先变成防御，再把收益转回伤害。", detailB: "会消耗所有已储存的余烬。", reactionType: "catch_ember"));
            Add(Make("thorn_rack", "棘刺架", "防御方块会生成 1 根棘刺，每根棘刺都会让敌人的反击伤害 -2。", StickerRarity.Uncommon, SocketTargetMask.Shield, StickerFamily.Thorn, StickerTriggerType.OnShieldBlockHit, 2f, detailA: "它本身并不直接提供护盾。", detailB: "所有棘刺会在回合结束时清空。", spawnType: "thorn"));
            Add(Make("spark_tape", "火花胶带", "倍率方块会生成火花；下一次防御方块把它变成护盾，下一次攻击方块把它变成伤害。", StickerRarity.Uncommon, SocketTargetMask.Any, StickerFamily.Spark, StickerTriggerType.OnBlockHit, 5f, 7f, detailA: "同一种中间状态可以转成不同收益。", detailB: "火花会保留到被消耗或回合结束。", spawnType: "spark"));
            Add(Make("same_family_latch", "同系卡榫", "若一张方块上至少装了两个同家族嵌片，则它每回合第一次触发额外 +8。", StickerRarity.Rare, SocketTargetMask.Any, StickerFamily.Alloy, StickerTriggerType.OnBlockHit, 8f, detailA: "鼓励把同系构筑压在一张卡上。", detailB: "和同系模组加成叠起来很强。"));
            Add(Make("breaker_note", "破阵音符", "如果本回合已经命中过倍率方块，之后的攻击方块每次都会额外获得 +8。", StickerRarity.Common, SocketTargetMask.Attack, StickerFamily.Prism, StickerTriggerType.OnAttackBlockHit, 8f, detailA: "先叠倍率，再拿稳定的收尾伤害。", detailB: "读取的是整回合状态。"));
            Add(Make("glass_ledger", "玻璃账本", "回合结束时，每种不同家族都会额外增加 2 点伤害。", StickerRarity.Uncommon, SocketTargetMask.Any, StickerFamily.Chain, StickerTriggerType.OnRoundEnd, 2f, detailA: "比连锁账本更稳，但上限更低。", detailB: "很适合混搭型牌组。"));
            Add(Make("frost_trace", "霜痕", "防御方块会留下霜痕；下一次倍率方块额外获得 x1.2 加成。", StickerRarity.Rare, SocketTargetMask.Shield, StickerFamily.Frost, StickerTriggerType.OnShieldBlockHit, 1.2f, detailA: "让防御路线也能反哺倍率路线。", detailB: "会消耗掉储存的霜痕。", spawnType: "frost_trace"));
            Add(Make("alloy_echo", "合金回响", "同一张方块第三次被命中时，额外获得 10 伤害和 4 护盾。", StickerRarity.Epic, SocketTargetMask.Any, StickerFamily.Alloy, StickerTriggerType.OnBlockHit, 10f, 4f, detailA: "奖励你记住那些高价值反弹点。", detailB: "只有真正再次回到同一张方块才会触发。"));
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
