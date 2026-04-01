using UnityEngine;

namespace POPHero
{
    public class RoundController : MonoBehaviour
    {
        PopHeroGame game;

        public int RoundNumber { get; private set; }
        public int RoundAttackScore { get; private set; }
        public int RoundShieldGain { get; private set; }
        public int RoundHitCount { get; private set; }
        public Vector2 LaunchPosition { get; private set; }
        public int PendingDamage => RoundAttackScore;
        public RoundStickerState StickerState { get; } = new();
        public int ChainLength => StickerState.chainLength;
        public int UniqueFamilyCount => StickerState.uniqueFamilies.Count;

        public void Initialize(PopHeroGame owner, Vector2 initialLaunchPosition)
        {
            game = owner;
            LaunchPosition = initialLaunchPosition;
            RoundNumber = 0;
            RoundAttackScore = 0;
            RoundShieldGain = 0;
            RoundHitCount = 0;
            StickerState.Reset();
        }

        public void BeginRound()
        {
            RoundNumber += 1;
            RoundAttackScore = 0;
            RoundShieldGain = 0;
            RoundHitCount = 0;
            StickerState.Reset();
            game.Player.ClearShield();
            game.EnemyPresenter?.ClearPreviewDamage();
            game.StickerEffectRunner.HandleRoundStart();
        }

        public void ProcessBlockHit(BoardBlock block)
        {
            if (block?.CardState == null)
                return;

            RoundHitCount += 1;
            RegisterBlockHit(block.CardState);
            ApplyBaseBlockEffect(block);
            game.StickerEffectRunner.HandleBlockHit(block);
            game.RefreshPendingDamagePreview();
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

            game.StickerEffectRunner.HandleRoundEnd();

            var result = new RoundResolveResult
            {
                landingPoint = landingPoint,
                attackDamage = RoundAttackScore,
                shieldGain = RoundShieldGain,
                hitCount = RoundHitCount,
                enemyCounterDamage = 0,
                enemyDefeated = false,
                playerDefeated = false
            };

            if (game.CurrentEnemy != null && RoundAttackScore > 0)
            {
                result.enemyDefeated = game.CurrentEnemy.ApplyDamage(RoundAttackScore);
                game.EnemyPresenter.PlayHitFeedback(result.enemyDefeated);
                game.StickerEffectRunner.HandleEnemyDamaged(RoundAttackScore);
                if (result.enemyDefeated)
                    game.StickerEffectRunner.HandleEnemyKilled();
            }

            game.EnemyPresenter?.ClearPreviewDamage();

            if (game.CurrentEnemy != null && !result.enemyDefeated && game.CurrentEnemy.AttackDamage > 0)
            {
                result.enemyCounterDamage = Mathf.Max(0, game.CurrentEnemy.AttackDamage - StickerState.enemyCounterReduction);
                game.Player.ApplyDamage(result.enemyCounterDamage);
            }

            game.Player.ClearShield();
            result.playerDefeated = game.Player.IsDead;
            return result;
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

        void ApplyBaseBlockEffect(BoardBlock block)
        {
            switch (block.blockType)
            {
                case BoardBlockType.AttackAdd:
                    AddAttack(Mathf.RoundToInt(block.valueA));
                    break;
                case BoardBlockType.AttackMultiply:
                    MultiplyAttack(block.valueA);
                    break;
                case BoardBlockType.Shield:
                    AddShield(Mathf.RoundToInt(block.valueA));
                    break;
            }
        }
    }
}
