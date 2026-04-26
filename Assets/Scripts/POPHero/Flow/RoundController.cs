using UnityEngine;

namespace POPHero
{
    public class RoundController : MonoBehaviour
    {
        PopHeroGame game;
        EnemyTurnResolver enemyTurnResolver;

        public int RoundNumber { get; private set; }
        public int RoundAttackScore { get; private set; }
        public int RoundShieldGain { get; private set; }
        public int RoundHitCount { get; private set; }
        public Vector2 LaunchPosition { get; private set; }
        public int PendingDamage => RoundAttackScore;
        public RoundStickerState StickerState { get; } = new();
        public int ChainLength => StickerState.chainLength;
        public int UniqueFamilyCount => StickerState.uniqueFamilies.Count;
        readonly System.Collections.Generic.HashSet<BoardBlock> lightningTriggeredBlocks = new();
        BallDefinition activeBall;
        int lightningTriggerCount;

        public void Initialize(PopHeroGame owner, Vector2 initialLaunchPosition)
        {
            game = owner;
            LaunchPosition = initialLaunchPosition;
            RoundNumber = 0;
            RoundAttackScore = 0;
            RoundShieldGain = 0;
            RoundHitCount = 0;
            StickerState.Reset();
            lightningTriggeredBlocks.Clear();
            lightningTriggerCount = 0;
            activeBall = null;
            enemyTurnResolver = new EnemyTurnResolver();
        }

        public void BeginRound()
        {
            RoundNumber += 1;
            RoundAttackScore = 0;
            RoundShieldGain = 0;
            RoundHitCount = 0;
            StickerState.Reset();
            lightningTriggeredBlocks.Clear();
            lightningTriggerCount = 0;
            activeBall = game.CurrentActionBall;
            game.Player.ClearShield();
            game.CombatEventHub?.Publish(new CombatEventPayload(StickerTriggerType.OnRoundStart));
        }

        public void ProcessBlockHit(BoardBlock block, float effectMultiplier = 1f)
        {
            ProcessBlockHitInternal(block, effectMultiplier, true);
        }

        void ProcessBlockHitInternal(BoardBlock block, float effectMultiplier, bool allowLightning)
        {
            if (block?.CardState == null)
                return;

            RoundHitCount += 1;
            RegisterBlockHit(block.CardState);
            ApplyBaseBlockEffect(block, effectMultiplier);
            game.CombatEventHub?.Publish(new CombatEventPayload(StickerTriggerType.OnBlockHit, block));
            switch (block.blockType)
            {
                case BoardBlockType.AttackAdd:
                    game.CombatEventHub?.Publish(new CombatEventPayload(StickerTriggerType.OnAttackBlockHit, block));
                    break;
                case BoardBlockType.AttackMultiply:
                    game.CombatEventHub?.Publish(new CombatEventPayload(StickerTriggerType.OnMultiplierBlockHit, block));
                    break;
                case BoardBlockType.Shield:
                    game.CombatEventHub?.Publish(new CombatEventPayload(StickerTriggerType.OnShieldBlockHit, block));
                    break;
            }
            game.RefreshPendingDamagePreview();

            if (allowLightning)
                TryTriggerLightning(block);
        }

        public void AddAttack(int amount)
        {
            RoundAttackScore += Mathf.Max(0, amount);
            game.RefreshPendingDamagePreview();
        }

        public void MultiplyAttack(float multiplier)
        {
            if (RoundAttackScore <= 0 || multiplier <= 0f)
                return;

            RoundAttackScore = Mathf.Max(0, Mathf.RoundToInt(RoundAttackScore * multiplier));
            game.RefreshPendingDamagePreview();
        }

        public void AddShield(int amount)
        {
            RoundShieldGain += Mathf.Max(0, amount);
            game.Player.SetShield(RoundShieldGain);
        }

        public void AddToken(string tokenId, int amount)
        {
            if (string.IsNullOrWhiteSpace(tokenId) || amount == 0)
                return;

            StickerState.tokens[tokenId] = GetTokenCount(tokenId) + amount;
        }

        public void SetToken(string tokenId, int amount)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return;

            StickerState.tokens[tokenId] = amount;
        }

        public int GetTokenCount(string tokenId)
        {
            return string.IsNullOrWhiteSpace(tokenId) || !StickerState.tokens.TryGetValue(tokenId, out var value) ? 0 : value;
        }

        public int ConsumeToken(string tokenId, int amount)
        {
            var current = GetTokenCount(tokenId);
            if (current <= 0)
                return 0;

            var consumed = Mathf.Clamp(amount, 0, current);
            var remaining = current - consumed;
            if (remaining <= 0)
                StickerState.tokens.Remove(tokenId);
            else
                StickerState.tokens[tokenId] = remaining;

            return consumed;
        }

        public void AddRoundTag(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag))
                StickerState.roundTags.Add(tag);
        }

        public bool HasRoundTag(string tag)
        {
            return !string.IsNullOrWhiteSpace(tag) && StickerState.roundTags.Contains(tag);
        }

        public int GetBlockHitCount(string blockId)
        {
            return string.IsNullOrWhiteSpace(blockId) || !StickerState.blockHitCounts.TryGetValue(blockId, out var count) ? 0 : count;
        }

        public void AddEnemyCounterReduction(int amount)
        {
            StickerState.enemyCounterReduction = Mathf.Max(0, StickerState.enemyCounterReduction + Mathf.Max(0, amount));
        }

        public bool RegisterOncePerRound(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || StickerState.oncePerRound.Contains(key))
                return false;

            StickerState.oncePerRound.Add(key);
            return true;
        }

        public RoundResolveResult ResolveRound(Vector2 landingPoint)
        {
            LaunchPosition = landingPoint;

            game.CombatEventHub?.Publish(new CombatEventPayload(StickerTriggerType.OnRoundEnd));

            var targetEncounter = game.CurrentEnemyEncounter;
            var enemyGroup = game.CurrentEnemyGroup;
            var targetSlot = targetEncounter != null ? targetEncounter.Slot : EnemyEncounterSlot.Primary;
            var targetDistance = targetEncounter != null ? targetEncounter.DistanceStepsRemaining : 0;
            var targetBehavior = targetEncounter != null ? targetEncounter.BehaviorType : EnemyBehaviorType.MeleeAdvance;
            var playerHpBeforeCounter = game.Player != null ? game.Player.CurrentHp : 0;

            var result = new RoundResolveResult
            {
                landingPoint = landingPoint,
                attackDamage = RoundAttackScore,
                shieldGain = RoundShieldGain,
                hitCount = RoundHitCount,
                targetSlot = targetSlot,
                enemyTurn = EnemyTurnOutcome.None(targetSlot, targetBehavior, targetDistance, playerHpBeforeCounter),
                enemyCounterDamage = 0,
                enemyDefeated = false,
                encounterCleared = false,
                playerDefeated = false,
                playerDisplayHpBeforeCounter = playerHpBeforeCounter,
                playerDisplayHpAfterCounter = playerHpBeforeCounter,
                enemyResults = BuildEnemyResolveEntries(enemyGroup),
                enemyTurns = new System.Collections.Generic.List<EnemyTurnOutcome>(2)
            };

            if (targetEncounter != null)
            {
                result.enemyDisplayHpBeforeHit = targetEncounter.Enemy != null ? targetEncounter.Enemy.CurrentHp : 0;
                result.enemyDisplayHpAfterHit = result.enemyDisplayHpBeforeHit;
            }

            var burstDamage = BallEffectCalculator.BurstDamage(activeBall, RoundHitCount);
            if (burstDamage > 0)
                RoundAttackScore += burstDamage;
            result.attackDamage = RoundAttackScore;

            if (targetEncounter != null && targetEncounter.Enemy != null && RoundAttackScore > 0)
            {
                result.enemyDefeated = targetEncounter.Enemy.ApplyDamage(RoundAttackScore);
                result.enemyDisplayHpAfterHit = targetEncounter.Enemy.CurrentHp;
                UpdateEnemyResolveEntry(result.enemyResults, targetEncounter, true, result.enemyDefeated, RoundAttackScore);
                game.CombatEventHub?.Publish(new CombatEventPayload(StickerTriggerType.OnEnemyDamaged, damage: RoundAttackScore));
                if (result.enemyDefeated)
                    game.CombatEventHub?.Publish(new CombatEventPayload(StickerTriggerType.OnEnemyKilled));
            }
            else if (targetEncounter != null)
            {
                UpdateEnemyResolveEntry(result.enemyResults, targetEncounter, false, false, 0);
            }

            if (enemyGroup != null && !enemyGroup.AllDefeated)
            {
                result.enemyTurns = enemyTurnResolver.ResolveGroup(enemyGroup.GetAliveEnemiesInTargetOrder(), StickerState.enemyCounterReduction, game.Player);
                if (result.enemyTurns.Count > 0)
                    result.enemyTurn = result.enemyTurns[0];

                for (var index = 0; index < result.enemyTurns.Count; index++)
                    result.enemyCounterDamage += result.enemyTurns[index].DamageDealt;
            }

            result.encounterCleared = enemyGroup == null || enemyGroup.AllDefeated;
            game.RefreshEnemyTargetSelection();
            game.Player.ClearShield();
            result.playerDefeated = game.Player.IsDead;
            result.playerDisplayHpAfterCounter = game.Player != null ? game.Player.CurrentHp : 0;
            return result;
        }

        static System.Collections.Generic.List<EnemyResolveEntry> BuildEnemyResolveEntries(EnemyEncounterGroupState enemyGroup)
        {
            var entries = new System.Collections.Generic.List<EnemyResolveEntry>(enemyGroup?.Encounters.Count ?? 0);
            if (enemyGroup == null)
                return entries;

            var encounters = enemyGroup.Encounters;
            for (var index = 0; index < encounters.Count; index++)
            {
                var encounter = encounters[index];
                if (encounter == null || encounter.Enemy == null)
                    continue;

                entries.Add(new EnemyResolveEntry
                {
                    slot = encounter.Slot,
                    behaviorType = encounter.BehaviorType,
                    enemy = encounter.Enemy,
                    wasTargeted = false,
                    wasDefeated = !encounter.IsAlive,
                    damageTaken = 0,
                    displayHpBefore = encounter.Enemy.CurrentHp,
                    displayHpAfter = encounter.Enemy.CurrentHp,
                    maxHp = encounter.Enemy.MaxHp
                });
            }

            return entries;
        }

        static void UpdateEnemyResolveEntry(System.Collections.Generic.List<EnemyResolveEntry> entries, EnemyEncounterState encounter, bool wasTargeted, bool wasDefeated, int damageTaken)
        {
            if (entries == null || encounter == null || encounter.Enemy == null)
                return;

            for (var index = 0; index < entries.Count; index++)
            {
                if (entries[index].slot != encounter.Slot)
                    continue;

                var entry = entries[index];
                entry.wasTargeted = wasTargeted;
                entry.wasDefeated = wasDefeated;
                entry.damageTaken = Mathf.Max(0, damageTaken);
                entry.displayHpAfter = encounter.Enemy.CurrentHp;
                entry.maxHp = encounter.Enemy.MaxHp;
                entries[index] = entry;
                return;
            }
        }

        void RegisterBlockHit(BlockCardState card)
        {
            StickerState.blockHitCounts[card.id] = GetBlockHitCount(card.id) + 1;
            if (card.baseBlockType == BoardBlockType.AttackMultiply)
                AddRoundTag("touched_multiplier");

            if (StickerState.lastFamily == null)
            {
                StickerState.lastFamily = card.family;
                StickerState.uniqueFamilies.Add(card.family);
                StickerState.chainLength = StickerState.uniqueFamilies.Count;
                return;
            }

            if (StickerState.lastFamily == card.family)
            {
                StickerState.chainLength = 0;
                StickerState.uniqueFamilies.Clear();
                StickerState.uniqueFamilies.Add(card.family);
            }
            else
            {
                StickerState.uniqueFamilies.Add(card.family);
                StickerState.chainLength = StickerState.uniqueFamilies.Count;
            }

            StickerState.lastFamily = card.family;
        }

        void ApplyBaseBlockEffect(BoardBlock block, float effectMultiplier)
        {
            var ball = activeBall;
            switch (block.blockType)
            {
                case BoardBlockType.AttackAdd:
                    AddAttack(BallEffectCalculator.ScaleAdditive(block.valueA, ball, block.blockType, effectMultiplier));
                    break;
                case BoardBlockType.AttackMultiply:
                    MultiplyAttack(BallEffectCalculator.ScaleMultiplier(block.valueA, ball, effectMultiplier));
                    break;
                case BoardBlockType.Shield:
                    AddShield(BallEffectCalculator.ScaleAdditive(block.valueA, ball, block.blockType, effectMultiplier));
                    break;
            }
        }

        void TryTriggerLightning(BoardBlock source)
        {
            if (activeBall == null || activeBall.specialType != BallSpecialType.Lightning || source == null)
                return;

            var maxTriggers = Mathf.Max(0, Mathf.RoundToInt(activeBall.valueD <= 0f ? 3f : activeBall.valueD));
            if (lightningTriggerCount >= maxTriggers)
                return;

            var chance = Mathf.Clamp01(activeBall.valueA <= 0f ? 0.3f : activeBall.valueA);
            if (Random.value > chance)
                return;

            var target = FindLightningTarget(source);
            if (target == null)
                return;

            lightningTriggerCount += 1;
            lightningTriggeredBlocks.Add(target);
            ProcessBlockHitInternal(target, activeBall.valueC <= 0f ? 0.5f : activeBall.valueC, false);
            target.PlayHitFeedback();
        }

        BoardBlock FindLightningTarget(BoardBlock source)
        {
            var blocks = game.RuntimeBoard?.ActiveBlocks;
            if (blocks == null)
                return null;

            var range = activeBall.valueB <= 0f ? 1f : activeBall.valueB;
            var blockSize = game.config != null ? Mathf.Max(game.config.board.blockSize.x, game.config.board.blockSize.y) : 1f;
            var rangeWorld = range <= 3f ? range * blockSize * 1.5f : range;
            var sourcePosition = source.transform.position;
            BoardBlock best = null;
            var bestDistance = float.MaxValue;
            for (var index = 0; index < blocks.Count; index++)
            {
                var candidate = blocks[index];
                if (candidate == null || candidate == source || lightningTriggeredBlocks.Contains(candidate))
                    continue;

                var distance = Vector2.Distance(sourcePosition, candidate.transform.position);
                if (distance > rangeWorld || distance >= bestDistance)
                    continue;

                best = candidate;
                bestDistance = distance;
            }

            return best;
        }
    }
}
