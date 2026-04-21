namespace POPHero
{
    public static class EnemyIntentTextFormatter
    {
        public static string BuildStatusText(EnemyEncounterState encounter, EnemyData fallbackEnemy = null)
        {
            var enemy = encounter?.Enemy ?? fallbackEnemy;
            if (enemy == null || enemy.CurrentHp <= 0)
                return "敌人意图：-";

            if (encounter == null)
                return $"敌人意图：攻击 {enemy.AttackDamage}";

            if (encounter.DistanceStepsRemaining > 1)
                return $"敌人意图：前进 1 步（剩余 {encounter.DistanceStepsRemaining} 步）";

            if (encounter.DistanceStepsRemaining == 1)
                return $"敌人意图：贴脸并攻击 {enemy.AttackDamage}";

            return $"敌人意图：攻击 {enemy.AttackDamage}";
        }

        public static string BuildWorldText(EnemyEncounterState encounter, EnemyData fallbackEnemy = null)
        {
            var enemy = encounter?.Enemy ?? fallbackEnemy;
            if (enemy == null || enemy.CurrentHp <= 0)
                return string.Empty;

            if (encounter == null)
                return $"攻击 {enemy.AttackDamage}";

            if (encounter.DistanceStepsRemaining > 1)
                return $"前进 1 步\n剩余 {encounter.DistanceStepsRemaining} 步";

            if (encounter.DistanceStepsRemaining == 1)
                return $"贴脸攻击 {enemy.AttackDamage}";

            return $"攻击 {enemy.AttackDamage}";
        }
    }
}
