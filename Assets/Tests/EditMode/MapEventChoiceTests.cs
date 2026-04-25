using NUnit.Framework;

namespace POPHero.Tests
{
    public sealed class MapEventChoiceTests
    {
        [Test]
        public void CreateDefaultEventChoices_ProvidesReusableRouteActions()
        {
            var choices = RunMapManager.CreateDefaultEventChoices();

            Assert.AreEqual(4, choices.Count);
            Assert.AreEqual(MapEventActionType.GainGold, choices[0].actionType);
            Assert.AreEqual(12, choices[0].intValue);
            Assert.AreEqual(MapEventActionType.TakeDamageUnlockSocket, choices[1].actionType);
            Assert.AreEqual(10, choices[1].intValue);
            Assert.AreEqual(MapEventActionType.OpenWorkbench, choices[2].actionType);
            Assert.AreEqual("map_workbench", choices[2].profileId);
            Assert.AreEqual(MapEventActionType.Heal, choices[3].actionType);
            Assert.AreEqual(MapHealingRules.DefaultHealPercent, choices[3].healPercent);
        }
    }
}
