using System;
using System.Collections.Generic;
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

    public readonly struct StickerEffectContext
    {
        public StickerEffectContext(PopHeroGame game, StickerInstance instance, BlockCardState card, StickerTriggerType triggerType, BoardBlock block, float multiplier)
        {
            Game = game;
            Instance = instance;
            Card = card;
            TriggerType = triggerType;
            Block = block;
            Multiplier = multiplier;
        }

        public PopHeroGame Game { get; }
        public StickerInstance Instance { get; }
        public StickerData Data => Instance?.data;
        public BlockCardState Card { get; }
        public StickerTriggerType TriggerType { get; }
        public BoardBlock Block { get; }
        public float Multiplier { get; }

        public int ScaleInt(float value)
        {
            return Mathf.Max(0, Mathf.RoundToInt(value * Multiplier));
        }
    }

    public interface IStickerEffectHandler
    {
        string EffectId { get; }
        void Execute(StickerEffectContext context);
    }

    sealed class DelegateStickerEffectHandler : IStickerEffectHandler
    {
        readonly Action<StickerEffectContext> execute;

        public DelegateStickerEffectHandler(string effectId, Action<StickerEffectContext> execute)
        {
            EffectId = effectId;
            this.execute = execute;
        }

        public string EffectId { get; }
        public void Execute(StickerEffectContext context) => execute?.Invoke(context);
    }

    public sealed class StickerEffectExecutor
    {
        readonly Dictionary<string, IStickerEffectHandler> handlers = new(StringComparer.OrdinalIgnoreCase);
        readonly PopHeroGame game;

        public StickerEffectExecutor(PopHeroGame owner)
        {
            game = owner;
            RegisterDefaultHandlers();
        }

        public void RegisterHandler(IStickerEffectHandler handler)
        {
            if (handler == null || string.IsNullOrWhiteSpace(handler.EffectId))
                return;

            handlers[handler.EffectId.Trim()] = handler;
        }

        public void Execute(StickerInstance instance, BlockCardState card, StickerTriggerType triggerType, BoardBlock block)
        {
            if (instance?.data == null)
                return;

            var data = instance.data;
            var multiplier = game.ModManager.GetStickerPowerMultiplier(card, instance);
            if (!handlers.TryGetValue(data.id, out var handler))
                return;

            handler.Execute(new StickerEffectContext(game, instance, card, triggerType, block, multiplier));
        }

        void RegisterDefaultHandlers()
        {
            Register("impact_core", context =>
            {
                if (context.TriggerType == StickerTriggerType.OnAttackBlockHit)
                    context.Game.RoundController.AddAttack(context.ScaleInt(context.Data.valueA));
            });
            Register("echo_mark", context =>
            {
                if (context.TriggerType != StickerTriggerType.OnAttackBlockHit || context.Card == null)
                    return;

                if (context.Game.RoundController.ConsumeToken($"echo:{context.Card.id}", 1) > 0)
                    context.Game.RoundController.AddAttack(context.ScaleInt(context.Data.valueA));
                else
                    context.Game.RoundController.AddToken($"echo:{context.Card.id}", 1);
            });
            Register("shatter_loop", context =>
            {
                if (context.TriggerType == StickerTriggerType.OnAttackBlockHit && context.Card != null && context.Game.RoundController.GetBlockHitCount(context.Card.id) >= 2)
                {
                    context.Game.RoundController.AddToken($"shatter:{context.Card.id}", 1);
                    context.Game.RoundController.AddAttack(context.ScaleInt(context.Data.valueA));
                }
            });
            Register("guard_furnace", context =>
            {
                if (context.TriggerType == StickerTriggerType.OnShieldBlockHit)
                    context.Game.RoundController.AddShield(context.ScaleInt(context.Data.valueA));
                else if (context.TriggerType == StickerTriggerType.OnRoundEnd)
                    context.Game.RoundController.AddAttack(Mathf.RoundToInt(context.Game.RoundController.RoundShieldGain * context.Data.valueB * context.Multiplier));
            });
            Register("prism_guard", context =>
            {
                if (context.TriggerType == StickerTriggerType.OnRoundEnd && context.Game.RoundController.HasRoundTag("touched_multiplier"))
                    context.Game.RoundController.AddAttack(Mathf.RoundToInt(context.Game.RoundController.RoundShieldGain * context.Data.valueA * context.Multiplier));
            });
            Register("mirror_plating", context =>
            {
                if (context.Card == null)
                    return;

                if (context.TriggerType == StickerTriggerType.OnShieldBlockHit)
                    context.Game.RoundController.AddToken($"mirror:{context.Card.id}", Mathf.Max(1, Mathf.RoundToInt(context.Game.Player.CurrentShield * 0.5f)));
                else if (context.TriggerType == StickerTriggerType.OnAttackBlockHit)
                {
                    var mirrorBonus = context.Game.RoundController.ConsumeToken($"mirror:{context.Card.id}", 99);
                    if (mirrorBonus > 0)
                        context.Game.RoundController.AddAttack(context.ScaleInt(mirrorBonus));
                }
            });
            Register("amp_seed", context =>
            {
                if (context.TriggerType == StickerTriggerType.OnMultiplierBlockHit)
                    context.Game.RoundController.AddToken("amp_charge", Mathf.RoundToInt(context.Data.valueA));
            });
            Register("amp_burst", context =>
            {
                if (context.TriggerType != StickerTriggerType.OnAttackBlockHit)
                    return;

                var consumed = context.Game.RoundController.ConsumeToken("amp_charge", 99);
                if (consumed > 0)
                    context.Game.RoundController.AddAttack(context.ScaleInt(consumed * context.Data.valueA));
            });
            Register("twin_resonance", context =>
            {
                if (context.TriggerType != StickerTriggerType.OnMultiplierBlockHit || context.Card == null)
                    return;

                var currentHash = context.Card.id.GetHashCode();
                var lastHash = context.Game.RoundController.GetTokenCount("last_multiplier_card");
                if (lastHash != 0 && lastHash != currentHash)
                    context.Game.RoundController.MultiplyAttack(context.Data.valueA);
                context.Game.RoundController.SetToken("last_multiplier_card", currentHash);
            });
            Register("chain_ledger", context =>
            {
                if (context.TriggerType == StickerTriggerType.OnRoundEnd && context.Game.RoundController.ChainLength > 0)
                    context.Game.RoundController.AddAttack(context.ScaleInt(context.Game.RoundController.ChainLength * context.Data.valueA));
            });
            Register("ember_seed", context =>
            {
                if (context.TriggerType == StickerTriggerType.OnAttackBlockHit)
                    context.Game.RoundController.AddToken("ember", 1);
            });
            Register("ember_catcher", context =>
            {
                if (context.TriggerType != StickerTriggerType.OnShieldBlockHit)
                    return;

                var embers = context.Game.RoundController.ConsumeToken("ember", 99);
                if (embers > 0)
                {
                    context.Game.RoundController.AddShield(context.ScaleInt(embers * context.Data.valueA));
                    context.Game.RoundController.AddAttack(context.ScaleInt(embers * context.Data.valueB));
                }
            });
            Register("thorn_rack", context =>
            {
                if (context.TriggerType == StickerTriggerType.OnShieldBlockHit)
                    context.Game.RoundController.AddEnemyCounterReduction(context.ScaleInt(context.Data.valueA));
            });
            Register("spark_tape", context =>
            {
                if (context.TriggerType == StickerTriggerType.OnMultiplierBlockHit)
                {
                    context.Game.RoundController.AddToken("spark", 1);
                }
                else if (context.TriggerType == StickerTriggerType.OnShieldBlockHit)
                {
                    var sparks = context.Game.RoundController.ConsumeToken("spark", 99);
                    if (sparks > 0)
                        context.Game.RoundController.AddShield(context.ScaleInt(sparks * context.Data.valueA));
                }
                else if (context.TriggerType == StickerTriggerType.OnAttackBlockHit)
                {
                    var sparks = context.Game.RoundController.ConsumeToken("spark", 99);
                    if (sparks > 0)
                        context.Game.RoundController.AddAttack(context.ScaleInt(sparks * context.Data.valueB));
                }
            });
            Register("same_family_latch", context =>
            {
                if (context.TriggerType == StickerTriggerType.OnBlockHit &&
                    context.Card != null &&
                    context.Game.RoundController.RegisterOncePerRound($"same_family:{context.Card.id}") &&
                    context.Game.BoardManager.GetInstalledFamilyCount(context.Card, context.Data.family) >= 2)
                {
                    context.Game.RoundController.AddAttack(context.ScaleInt(context.Data.valueA));
                }
            });
            Register("breaker_note", context =>
            {
                if (context.TriggerType == StickerTriggerType.OnAttackBlockHit && context.Game.RoundController.HasRoundTag("touched_multiplier"))
                    context.Game.RoundController.AddAttack(context.ScaleInt(context.Data.valueA));
            });
            Register("glass_ledger", context =>
            {
                if (context.TriggerType == StickerTriggerType.OnRoundEnd && context.Game.RoundController.UniqueFamilyCount > 0)
                    context.Game.RoundController.AddAttack(context.ScaleInt(context.Game.RoundController.UniqueFamilyCount * context.Data.valueA));
            });
            Register("frost_trace", context =>
            {
                if (context.TriggerType == StickerTriggerType.OnShieldBlockHit)
                    context.Game.RoundController.AddToken("frost_trace", 1);
                else if (context.TriggerType == StickerTriggerType.OnMultiplierBlockHit && context.Game.RoundController.ConsumeToken("frost_trace", 1) > 0)
                    context.Game.RoundController.MultiplyAttack(context.Data.valueA);
            });
            Register("alloy_echo", context =>
            {
                if (context.TriggerType == StickerTriggerType.OnBlockHit && context.Card != null && context.Game.RoundController.GetBlockHitCount(context.Card.id) >= 3)
                {
                    context.Game.RoundController.AddAttack(context.ScaleInt(context.Data.valueA));
                    context.Game.RoundController.AddShield(context.ScaleInt(context.Data.valueB));
                }
            });
        }

        void Register(string effectId, Action<StickerEffectContext> execute)
        {
            RegisterHandler(new DelegateStickerEffectHandler(effectId, execute));
        }
    }
}
