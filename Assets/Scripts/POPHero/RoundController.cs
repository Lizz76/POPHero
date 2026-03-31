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

        public void Initialize(PopHeroGame owner, Vector2 initialLaunchPosition)
        {
            game = owner;
            LaunchPosition = initialLaunchPosition;
            RoundNumber = 0;
            RoundAttackScore = 0;
            RoundShieldGain = 0;
            RoundHitCount = 0;
        }

        public void BeginRound()
        {
            RoundNumber += 1;
            RoundAttackScore = 0;
            RoundShieldGain = 0;
            RoundHitCount = 0;
            game.Player.ClearShield();
            game.EnemyPresenter?.ClearPreviewDamage();
        }

        public void AddAttack(int amount)
        {
            RoundAttackScore += Mathf.Max(0, amount);
            RoundHitCount += 1;
            game.RefreshPendingDamagePreview();
        }

        public void MultiplyAttack(float multiplier)
        {
            RoundHitCount += 1;
            if (RoundAttackScore <= 0 || multiplier <= 0f)
                return;

            RoundAttackScore = Mathf.Max(0, Mathf.RoundToInt(RoundAttackScore * multiplier));
            game.RefreshPendingDamagePreview();
        }

        public void AddShield(int amount)
        {
            RoundShieldGain += Mathf.Max(0, amount);
            RoundHitCount += 1;
            game.Player.SetShield(RoundShieldGain);
        }

        public RoundResolveResult ResolveRound(Vector2 landingPoint)
        {
            LaunchPosition = landingPoint;

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
            }

            game.EnemyPresenter?.ClearPreviewDamage();

            if (game.CurrentEnemy != null && !result.enemyDefeated && game.CurrentEnemy.AttackDamage > 0)
            {
                result.enemyCounterDamage = game.CurrentEnemy.AttackDamage;
                game.Player.ApplyDamage(result.enemyCounterDamage);
            }

            game.Player.ClearShield();
            result.playerDefeated = game.Player.IsDead;
            return result;
        }
    }
}
