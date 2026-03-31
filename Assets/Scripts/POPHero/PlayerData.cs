using UnityEngine;

namespace POPHero
{
    public class PlayerData
    {
        public const int MaxLevelCap = 10;

        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public int CurrentShield { get; private set; }
        public int Gold { get; private set; }
        public int Level { get; private set; }
        public int KillsTowardNextLevel { get; private set; }
        public int KillsRequiredForNextLevel => IsMaxLevel ? 0 : GetKillsRequiredForLevelUp(Level);
        public int AvailableBlockCount => 1 + Level;
        public bool IsMaxLevel => Level >= MaxLevelCap;
        public bool IsDead => CurrentHp <= 0;

        public PlayerData(int maxHp, int currentHp, int currentShield, int gold)
        {
            Reset(maxHp, currentHp, currentShield, gold);
        }

        public void Reset(int maxHp, int currentHp, int currentShield, int gold)
        {
            MaxHp = Mathf.Max(1, maxHp);
            CurrentHp = Mathf.Clamp(currentHp, 0, MaxHp);
            CurrentShield = Mathf.Max(0, currentShield);
            Gold = Mathf.Max(0, gold);
            Level = 0;
            KillsTowardNextLevel = 0;
        }

        public void SetShield(int amount)
        {
            CurrentShield = Mathf.Max(0, amount);
        }

        public void AddShield(int amount)
        {
            CurrentShield = Mathf.Max(0, CurrentShield + Mathf.Max(0, amount));
        }

        public void ClearShield()
        {
            CurrentShield = 0;
        }

        public void AddGold(int amount)
        {
            Gold = Mathf.Max(0, Gold + Mathf.Max(0, amount));
        }

        public void Heal(int amount)
        {
            CurrentHp = Mathf.Clamp(CurrentHp + Mathf.Max(0, amount), 0, MaxHp);
        }

        public void RestoreToFullHealth()
        {
            CurrentHp = MaxHp;
        }

        public void ApplyDamage(int damage)
        {
            var remaining = Mathf.Max(0, damage);
            if (remaining <= 0)
                return;

            if (CurrentShield > 0)
            {
                var absorbed = Mathf.Min(CurrentShield, remaining);
                CurrentShield -= absorbed;
                remaining -= absorbed;
            }

            if (remaining > 0)
                CurrentHp = Mathf.Max(0, CurrentHp - remaining);
        }

        public bool RegisterKillAndTryLevelUp()
        {
            if (IsMaxLevel)
                return false;

            KillsTowardNextLevel += 1;
            if (KillsTowardNextLevel < KillsRequiredForNextLevel)
                return false;

            KillsTowardNextLevel = 0;
            Level = Mathf.Min(MaxLevelCap, Level + 1);
            return true;
        }

        public static int GetKillsRequiredForLevelUp(int level)
        {
            return Mathf.Min(Mathf.Max(0, level) + 1, 6);
        }
    }
}
