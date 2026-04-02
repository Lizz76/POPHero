using UnityEngine;

namespace POPHero
{
    interface IAimInputStrategy
    {
        void Tick(PlayerLauncher launcher);
    }

    sealed class PcAimInputStrategy : IAimInputStrategy
    {
        public void Tick(PlayerLauncher launcher)
        {
            launcher.TickPcAimInput();
        }
    }

    sealed class MobileAimInputStrategy : IAimInputStrategy
    {
        public void Tick(PlayerLauncher launcher)
        {
            launcher.TickMobileAimInput();
        }
    }
}
