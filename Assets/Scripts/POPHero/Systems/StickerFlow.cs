using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public class StickerEffectRunner : ICombatEventListener
    {
        StickerTriggerDispatcher triggerDispatcher;

        public void Initialize(PopHeroGame owner)
        {
            triggerDispatcher = new StickerTriggerDispatcher(owner, new StickerEffectExecutor(owner));
            owner.CombatEventHub?.Subscribe(this);
        }

        public void OnCombatEvent(CombatEventPayload payload)
        {
            switch (payload.TriggerType)
            {
                case StickerTriggerType.OnRoundStart:
                case StickerTriggerType.OnBoardRefreshed:
                case StickerTriggerType.OnEnemyKilled:
                case StickerTriggerType.OnRoundEnd:
                    triggerDispatcher.DispatchAllInstalled(payload.TriggerType, payload.Block);
                    break;
                case StickerTriggerType.OnEnemyDamaged:
                    if (payload.Damage > 0)
                        triggerDispatcher.DispatchAllInstalled(payload.TriggerType, payload.Block);
                    break;
                case StickerTriggerType.OnBlockHit:
                    HandleBlockHit(payload.Block);
                    break;
                case StickerTriggerType.OnAttackBlockHit:
                case StickerTriggerType.OnShieldBlockHit:
                case StickerTriggerType.OnMultiplierBlockHit:
                    if (payload.Block?.CardState != null)
                        triggerDispatcher.DispatchForCard(payload.Block.CardState, payload.TriggerType, payload.Block);
                    break;
            }
        }

        public void HandleBlockHit(BoardBlock block)
        {
            if (block?.CardState == null)
                return;

            triggerDispatcher.DispatchForCard(block.CardState, StickerTriggerType.OnBlockHit, block);
            switch (block.blockType)
            {
                case BoardBlockType.AttackAdd:
                    triggerDispatcher.DispatchForCard(block.CardState, StickerTriggerType.OnAttackBlockHit, block);
                    break;
                case BoardBlockType.AttackMultiply:
                    triggerDispatcher.DispatchForCard(block.CardState, StickerTriggerType.OnMultiplierBlockHit, block);
                    break;
                case BoardBlockType.Shield:
                    triggerDispatcher.DispatchForCard(block.CardState, StickerTriggerType.OnShieldBlockHit, block);
                    break;
            }
        }
    }

    public class RewardChoiceController
    {
        readonly List<RewardChoiceEntry> activeChoices = new();
        readonly List<GrowthRewardData> growthPool = new();

        PopHeroGame game;

        public IReadOnlyList<RewardChoiceEntry> ActiveChoices => activeChoices;
        public string LastStatusMessage { get; private set; } = string.Empty;

        public void Initialize(PopHeroGame owner)
        {
            game = owner;
            activeChoices.Clear();
            growthPool.Clear();
            BuildGrowthPool();
        }

        public void GenerateChoices()
        {
            activeChoices.Clear();
            var choiceCount = game.ModManager.GetRewardChoiceCount();
            if (game.StickerCatalog.GetRandomSticker() is { } guaranteedSticker)
            {
                activeChoices.Add(new RewardChoiceEntry
                {
                    id = $"reward_sticker_{guaranteedSticker.id}",
                    title = guaranteedSticker.name,
                    description = guaranteedSticker.mainActionText,
                    kind = ShopItemKind.Sticker,
                    stickerData = guaranteedSticker
                });
            }

            while (activeChoices.Count < choiceCount)
            {
                var roll = Random.Range(0, 3);
                RewardChoiceEntry candidate = roll switch
                {
                    0 => CreateStickerChoice(),
                    1 => CreateModChoice(),
                    _ => CreateGrowthChoice()
                };

                if (candidate == null)
                    break;

                activeChoices.Add(candidate);
            }

            LastStatusMessage = string.Empty;
        }

        public bool TrySelectChoice(int index)
        {
            if (index < 0 || index >= activeChoices.Count)
                return false;

            ApplyChoice(activeChoices[index]);
            activeChoices.Clear();
            LastStatusMessage = string.Empty;
            return true;
        }

        public bool TryRerollChoices()
        {
            var cost = game.config.shop.stickerRerollMoney;
            if (game.Player.Gold < cost)
            {
                LastStatusMessage = "金币不足，无法刷新奖励。";
                return false;
            }

            game.Player.SpendGold(cost);
            GenerateChoices();
            LastStatusMessage = "奖励已刷新。";
            return true;
        }

        public void SkipChoices()
        {
            game.Player.AddGold(game.config.shop.stickerSkipMoney);
            activeChoices.Clear();
            LastStatusMessage = "你跳过了奖励，改拿一笔金币。";
        }

        RewardChoiceEntry CreateStickerChoice()
        {
            var data = game.StickerCatalog.GetRandomSticker();
            return data == null ? null : new RewardChoiceEntry
            {
                id = $"reward_sticker_{data.id}",
                title = data.name,
                description = data.mainActionText,
                kind = ShopItemKind.Sticker,
                stickerData = data
            };
        }

        RewardChoiceEntry CreateModChoice()
        {
            var data = game.ModManager.GetRandomUnownedMod();
            return data == null ? null : new RewardChoiceEntry
            {
                id = $"reward_mod_{data.id}",
                title = data.name,
                description = data.description,
                kind = ShopItemKind.Mod,
                modData = data
            };
        }

        RewardChoiceEntry CreateGrowthChoice()
        {
            if (growthPool.Count == 0)
                return null;

            var data = growthPool[Random.Range(0, growthPool.Count)];
            return new RewardChoiceEntry
            {
                id = $"reward_growth_{data.id}",
                title = data.name,
                description = data.description,
                kind = ShopItemKind.Growth,
                growthData = data
            };
        }

        void ApplyChoice(RewardChoiceEntry entry)
        {
            switch (entry.kind)
            {
                case ShopItemKind.Sticker:
                    var instance = game.StickerCatalog.CreateInstance(entry.stickerData.id);
                    if (instance != null && !game.StickerInventory.TryAdd(instance))
                    {
                        game.Player.IncreaseInventoryCapacity(1);
                        game.StickerInventory.TryAdd(instance);
                    }
                    break;
                case ShopItemKind.Mod:
                    game.ModManager.AcquireMod(entry.modData.id);
                    break;
                case ShopItemKind.Growth:
                    game.ApplyGrowthReward(entry.growthData);
                    break;
            }
        }

        void BuildGrowthPool()
        {
            growthPool.Add(new GrowthRewardData { id = "growth_socket", name = "扩展槽位", description = "随机一张载体卡立即解锁 1 个额外槽位。", rewardType = GrowthRewardType.UnlockSocket, value = 1 });
            growthPool.Add(new GrowthRewardData { id = "growth_inventory", name = "扩容库存", description = "嵌片库存上限 +1。", rewardType = GrowthRewardType.IncreaseInventoryCapacity, value = 1 });
            growthPool.Add(new GrowthRewardData { id = "growth_launch", name = "备用弹珠", description = "每只敌人的可发射数 +1。", rewardType = GrowthRewardType.IncreaseLaunchCapacity, value = 1 });
        }
    }
}
