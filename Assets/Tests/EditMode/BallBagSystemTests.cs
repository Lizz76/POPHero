using NUnit.Framework;

namespace POPHero.Tests
{
    public sealed class BallBagSystemTests
    {
        [Test]
        public void Initialize_CreatesStartingSixBallBagAndCurrentBall()
        {
            var manager = new BallBagManager();

            manager.Initialize(null);

            Assert.AreEqual(6, manager.PlayerBalls.Count);
            Assert.IsNotNull(manager.CurrentBall);
            Assert.AreEqual(5, manager.DrawPileCount);
            Assert.AreEqual(0, manager.UsedPileCount);
        }

        [Test]
        public void FinishRound_MovesCurrentToUsedAndDrawsNext()
        {
            var manager = new BallBagManager();
            manager.Initialize(null);
            var first = manager.CurrentBall;

            manager.BeginRound();
            manager.FinishRoundAndDrawNext();

            Assert.Contains(first, manager.State.usedPile);
            Assert.IsNotNull(manager.CurrentBall);
            Assert.AreEqual(4, manager.DrawPileCount);
            Assert.AreEqual(1, manager.UsedPileCount);
        }

        [Test]
        public void DrawPileEmpty_ShufflesUsedPileBackAndContinuesDrawing()
        {
            var manager = new BallBagManager();
            manager.Initialize(null);

            for (var i = 0; i < 6; i++)
            {
                manager.BeginRound();
                manager.FinishRoundAndDrawNext();
            }

            Assert.IsNotNull(manager.CurrentBall);
            Assert.AreEqual(5, manager.DrawPileCount);
            Assert.AreEqual(0, manager.UsedPileCount);
        }

        [Test]
        public void DiscardCurrentBall_OnlyWorksOncePerActionWindow()
        {
            var manager = new BallBagManager();
            manager.Initialize(null);
            var discarded = manager.CurrentBall;

            Assert.IsTrue(manager.TryDiscardCurrent(out _));
            Assert.Contains(discarded, manager.State.usedPile);
            Assert.IsFalse(manager.TryDiscardCurrent(out _));
            Assert.IsNotNull(manager.CurrentBall);
        }

        [Test]
        public void BallEffectCalculator_AppliesAttackDefenseAndMultiplierBiases()
        {
            var attackBall = new BallDefinition
            {
                attackMultiplier = 1.5f,
                shieldMultiplier = 0.8f,
                multiplierMultiplier = 1f
            };
            var defenseBall = new BallDefinition
            {
                attackMultiplier = 0.8f,
                shieldMultiplier = 1.7f,
                multiplierMultiplier = 1f
            };
            var multiplierBall = new BallDefinition
            {
                attackMultiplier = 0.7f,
                shieldMultiplier = 1f,
                multiplierMultiplier = 1.8f
            };

            Assert.AreEqual(15, BallEffectCalculator.ScaleAdditive(10, attackBall, BoardBlockType.AttackAdd));
            Assert.AreEqual(17, BallEffectCalculator.ScaleAdditive(10, defenseBall, BoardBlockType.Shield));
            Assert.AreEqual(1.36f, BallEffectCalculator.ScaleMultiplier(1.2f, multiplierBall), 0.001f);
        }

        [Test]
        public void BurstAndPierceParameters_UseConfiguredValues()
        {
            var burst = new BallDefinition
            {
                specialType = BallSpecialType.Burst,
                valueA = 5,
                valueB = 5,
                valueC = 2
            };
            var pierce = new BallDefinition
            {
                specialType = BallSpecialType.Pierce,
                valueA = 3,
                valueB = 0.8f
            };

            Assert.AreEqual(9, BallEffectCalculator.BurstDamage(burst, 10));
            Assert.AreEqual(3, BallEffectCalculator.PierceCount(pierce));
            Assert.AreEqual(0.8f, BallEffectCalculator.PierceYieldMultiplier(pierce), 0.001f);
        }
    }
}
