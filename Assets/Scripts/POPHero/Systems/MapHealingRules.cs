using UnityEngine;

namespace POPHero
{
    public static class MapHealingRules
    {
        public const float DefaultHealPercent = 0.3f;

        public static int CalculateHealAmount(int maxHp, float healPercent = DefaultHealPercent)
        {
            var safeMaxHp = Mathf.Max(1, maxHp);
            var safePercent = healPercent > 0f ? healPercent : DefaultHealPercent;
            return Mathf.Max(1, Mathf.CeilToInt(safeMaxHp * safePercent - 0.0001f));
        }

        public static int ApplyHeal(PlayerData player, float healPercent = DefaultHealPercent)
        {
            if (player == null || player.IsDead)
                return 0;

            var before = player.CurrentHp;
            player.Heal(CalculateHealAmount(player.MaxHp, healPercent));
            return Mathf.Max(0, player.CurrentHp - before);
        }
    }
}
