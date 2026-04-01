using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public class StickerEffectRunner
    {
        PopHeroGame game;

        public void Initialize(PopHeroGame owner)
        {
            game = owner;
        }

        public void HandleRoundStart()
        {
            DispatchAllInstalled(StickerTriggerType.OnRoundStart, null);
        }

        public void HandleBoardRefreshed()
        {
            DispatchAllInstalled(StickerTriggerType.OnBoardRefreshed, null);
        }

        public void HandleEnemyDamaged(int damage)
        {
            if (damage > 0)
                DispatchAllInstalled(StickerTriggerType.OnEnemyDamaged, null);
        }

        public void HandleEnemyKilled()
        {
            DispatchAllInstalled(StickerTriggerType.OnEnemyKilled, null);
        }

        public void HandleRoundEnd()
        {
            DispatchAllInstalled(StickerTriggerType.OnRoundEnd, null);
        }

        public void HandleBlockHit(BoardBlock block)
        {
            if (block?.CardState == null)
                return;

            DispatchForCard(block.CardState, StickerTriggerType.OnBlockHit, block);
            switch (block.blockType)
            {
                case BoardBlockType.AttackAdd:
                    DispatchForCard(block.CardState, StickerTriggerType.OnAttackBlockHit, block);
                    break;
                case BoardBlockType.AttackMultiply:
                    DispatchForCard(block.CardState, StickerTriggerType.OnMultiplierBlockHit, block);
                    break;
                case BoardBlockType.Shield:
                    DispatchForCard(block.CardState, StickerTriggerType.OnShieldBlockHit, block);
                    break;
            }
        }

        void DispatchAllInstalled(StickerTriggerType triggerType, BoardBlock block)
        {
            foreach (var card in game.BoardManager.ActiveCardStates)
                DispatchForCard(card, triggerType, block);
        }

        void DispatchForCard(BlockCardState card, StickerTriggerType triggerType, BoardBlock block)
        {
            if (card == null)
                return;

            foreach (var socket in card.sockets)
            {
                if (socket.installedSticker == null)
                    continue;

                RunSticker(socket.installedSticker, card, triggerType, block);
            }
        }

        void RunSticker(StickerInstance instance, BlockCardState card, StickerTriggerType triggerType, BoardBlock block)
        {
            if (instance?.data == null)
                return;

            var data = instance.data;
            var multiplier = game.ModManager.GetStickerPowerMultiplier(card, instance);

            switch (data.id)
            {
                case "impact_core":
                    if (triggerType == StickerTriggerType.OnAttackBlockHit)
                        game.RoundController.AddAttack(ScaleInt(data.valueA, multiplier));
                    break;
                case "echo_mark":
                    if (triggerType == StickerTriggerType.OnAttackBlockHit)
                    {
                        if (game.RoundController.ConsumeToken($"echo:{card.id}", 1) > 0)
                            game.RoundController.AddAttack(ScaleInt(data.valueA, multiplier));
                        else
                            game.RoundController.AddToken($"echo:{card.id}", 1);
                    }
                    break;
                case "shatter_loop":
                    if (triggerType == StickerTriggerType.OnAttackBlockHit && game.RoundController.GetBlockHitCount(card.id) >= 2)
                    {
                        game.RoundController.AddToken($"shatter:{card.id}", 1);
                        game.RoundController.AddAttack(ScaleInt(data.valueA, multiplier));
                    }
                    break;
                case "guard_furnace":
                    if (triggerType == StickerTriggerType.OnShieldBlockHit)
                        game.RoundController.AddShield(ScaleInt(data.valueA, multiplier));
                    else if (triggerType == StickerTriggerType.OnRoundEnd)
                        game.RoundController.AddAttack(Mathf.RoundToInt(game.RoundController.RoundShieldGain * data.valueB * multiplier));
                    break;
                case "prism_guard":
                    if (triggerType == StickerTriggerType.OnRoundEnd && game.RoundController.HasRoundTag("touched_multiplier"))
                        game.RoundController.AddAttack(Mathf.RoundToInt(game.RoundController.RoundShieldGain * data.valueA * multiplier));
                    break;
                case "mirror_plating":
                    if (triggerType == StickerTriggerType.OnShieldBlockHit)
                        game.RoundController.AddToken($"mirror:{card.id}", Mathf.Max(1, Mathf.RoundToInt(game.Player.CurrentShield * 0.5f)));
                    else if (triggerType == StickerTriggerType.OnAttackBlockHit)
                    {
                        var mirrorBonus = game.RoundController.ConsumeToken($"mirror:{card.id}", 99);
                        if (mirrorBonus > 0)
                            game.RoundController.AddAttack(ScaleInt(mirrorBonus, multiplier));
                    }
                    break;
                case "amp_seed":
                    if (triggerType == StickerTriggerType.OnMultiplierBlockHit)
                        game.RoundController.AddToken("amp_charge", Mathf.RoundToInt(data.valueA));
                    break;
                case "amp_burst":
                    if (triggerType == StickerTriggerType.OnAttackBlockHit)
                    {
                        var consumed = game.RoundController.ConsumeToken("amp_charge", 99);
                        if (consumed > 0)
                            game.RoundController.AddAttack(ScaleInt(consumed * data.valueA, multiplier));
                    }
                    break;
                case "twin_resonance":
                    if (triggerType == StickerTriggerType.OnMultiplierBlockHit)
                    {
                        var currentHash = card.id.GetHashCode();
                        var lastHash = game.RoundController.GetTokenCount("last_multiplier_card");
                        if (lastHash != 0 && lastHash != currentHash)
                            game.RoundController.MultiplyAttack(data.valueA);
                        game.RoundController.SetToken("last_multiplier_card", currentHash);
                    }
                    break;
                case "chain_ledger":
                    if (triggerType == StickerTriggerType.OnRoundEnd && game.RoundController.ChainLength > 0)
                        game.RoundController.AddAttack(ScaleInt(game.RoundController.ChainLength * data.valueA, multiplier));
                    break;
                case "ember_seed":
                    if (triggerType == StickerTriggerType.OnAttackBlockHit)
                        game.RoundController.AddToken("ember", 1);
                    break;
                case "ember_catcher":
                    if (triggerType == StickerTriggerType.OnShieldBlockHit)
                    {
                        var embers = game.RoundController.ConsumeToken("ember", 99);
                        if (embers > 0)
                        {
                            game.RoundController.AddShield(ScaleInt(embers * data.valueA, multiplier));
                            game.RoundController.AddAttack(ScaleInt(embers * data.valueB, multiplier));
                        }
                    }
                    break;
                case "thorn_rack":
                    if (triggerType == StickerTriggerType.OnShieldBlockHit)
                        game.RoundController.AddEnemyCounterReduction(ScaleInt(data.valueA, multiplier));
                    break;
                case "spark_tape":
                    if (triggerType == StickerTriggerType.OnMultiplierBlockHit)
                        game.RoundController.AddToken("spark", 1);
                    else if (triggerType == StickerTriggerType.OnShieldBlockHit)
                    {
                        var sparks = game.RoundController.ConsumeToken("spark", 99);
                        if (sparks > 0)
                            game.RoundController.AddShield(ScaleInt(sparks * data.valueA, multiplier));
                    }
                    else if (triggerType == StickerTriggerType.OnAttackBlockHit)
                    {
                        var sparks = game.RoundController.ConsumeToken("spark", 99);
                        if (sparks > 0)
                            game.RoundController.AddAttack(ScaleInt(sparks * data.valueB, multiplier));
                    }
                    break;
                case "same_family_latch":
                    if (triggerType == StickerTriggerType.OnBlockHit &&
                        game.RoundController.RegisterOncePerRound($"same_family:{card.id}") &&
                        game.BoardManager.GetInstalledFamilyCount(card, data.family) >= 2)
                    {
                        game.RoundController.AddAttack(ScaleInt(data.valueA, multiplier));
                    }
                    break;
                case "breaker_note":
                    if (triggerType == StickerTriggerType.OnAttackBlockHit && game.RoundController.HasRoundTag("touched_multiplier"))
                        game.RoundController.AddAttack(ScaleInt(data.valueA, multiplier));
                    break;
                case "glass_ledger":
                    if (triggerType == StickerTriggerType.OnRoundEnd && game.RoundController.UniqueFamilyCount > 0)
                        game.RoundController.AddAttack(ScaleInt(game.RoundController.UniqueFamilyCount * data.valueA, multiplier));
                    break;
                case "frost_trace":
                    if (triggerType == StickerTriggerType.OnShieldBlockHit)
                        game.RoundController.AddToken("frost_trace", 1);
                    else if (triggerType == StickerTriggerType.OnMultiplierBlockHit && game.RoundController.ConsumeToken("frost_trace", 1) > 0)
                        game.RoundController.MultiplyAttack(data.valueA);
                    break;
                case "alloy_echo":
                    if (triggerType == StickerTriggerType.OnBlockHit && game.RoundController.GetBlockHitCount(card.id) >= 3)
                    {
                        game.RoundController.AddAttack(ScaleInt(data.valueA, multiplier));
                        game.RoundController.AddShield(ScaleInt(data.valueB, multiplier));
                    }
                    break;
            }
        }

        static int ScaleInt(float value, float multiplier)
        {
            return Mathf.Max(0, Mathf.RoundToInt(value * multiplier));
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
                activeChoices.Add(new RewardChoiceEntry { id = $"reward_sticker_{guaranteedSticker.id}", title = guaranteedSticker.name, description = guaranteedSticker.mainActionText, kind = ShopItemKind.Sticker, stickerData = guaranteedSticker });

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
            growthPool.Add(new GrowthRewardData { id = "growth_inventory", name = "扩容盒", description = "嵌片库存上限 +1。", rewardType = GrowthRewardType.IncreaseInventoryCapacity, value = 1 });
            growthPool.Add(new GrowthRewardData { id = "growth_launch", name = "备用弹珠", description = "每只敌人的可发射数 +1。", rewardType = GrowthRewardType.IncreaseLaunchCapacity, value = 1 });
        }
    }
}
