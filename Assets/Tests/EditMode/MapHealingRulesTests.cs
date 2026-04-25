using NUnit.Framework;

namespace POPHero.Tests
{
    public sealed class MapHealingRulesTests
    {
        [Test]
        public void CalculateHealAmount_DefaultsToThirtyPercentRoundedUp()
        {
            Assert.AreEqual(30, MapHealingRules.CalculateHealAmount(100));
            Assert.AreEqual(31, MapHealingRules.CalculateHealAmount(101));
        }

        [Test]
        public void ApplyHeal_CapsAtMaxHp()
        {
            var player = new PlayerData(100, 80, 0, 0);

            var healed = MapHealingRules.ApplyHeal(player);

            Assert.AreEqual(20, healed);
            Assert.AreEqual(100, player.CurrentHp);
        }

        [Test]
        public void ApplyHeal_DoesNotReviveDeadPlayer()
        {
            var player = new PlayerData(100, 0, 0, 0);

            var healed = MapHealingRules.ApplyHeal(player);

            Assert.AreEqual(0, healed);
            Assert.AreEqual(0, player.CurrentHp);
        }
    }
}
