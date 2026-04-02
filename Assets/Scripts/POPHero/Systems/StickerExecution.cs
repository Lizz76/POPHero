using UnityEngine;

namespace POPHero
{
    public sealed class StickerTriggerDispatcher
    {
        readonly PopHeroGame game;
        readonly StickerEffectExecutor effectExecutor;

        public StickerTriggerDispatcher(PopHeroGame owner, StickerEffectExecutor executor)
        {
            game = owner;
            effectExecutor = executor;
        }

        public void DispatchAllInstalled(StickerTriggerType triggerType, BoardBlock block)
        {
            foreach (var card in game.BlockCollections.ActiveCardStates)
                DispatchForCard(card, triggerType, block);
        }

        public void DispatchForCard(BlockCardState card, StickerTriggerType triggerType, BoardBlock block)
        {
            if (card == null)
                return;

            foreach (var socket in card.sockets)
            {
                if (socket.installedSticker == null)
                    continue;

                effectExecutor.Execute(socket.installedSticker, card, triggerType, block);
            }
        }
    }

    public sealed class StickerEffectExecutor
    {
        readonly PopHeroGame game;

        public StickerEffectExecutor(PopHeroGame owner)
        {
            game = owner;
        }

        public void Execute(StickerInstance instance, BlockCardState card, StickerTriggerType triggerType, BoardBlock block)
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
}
