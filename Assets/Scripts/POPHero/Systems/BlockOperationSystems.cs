using UnityEngine;

namespace POPHero
{
    public sealed class BlockOperationManager : IBlockOperationService
    {
        PopHeroGame game;
        readonly BlockOperationSessionState session = new();
        BlockOperationProfileDef currentProfile;

        public bool IsOpen => session.IsOpen && currentProfile != null;
        public BlockOperationProfileDef CurrentProfile => currentProfile;
        public BlockOperationSessionState Session => session;

        public void Initialize(PopHeroGame owner)
        {
            game = owner;
            Close();
        }

        public bool TryOpen(string profileId, RoundState returnState, out string failReason)
        {
            failReason = string.Empty;
            if (game?.Tables == null)
            {
                failReason = "方块操作配置不可用。";
                return false;
            }

            if (!game.Tables.TryGetBlockOperationProfile(profileId, out var profile) || profile == null)
            {
                failReason = $"找不到方块操作配置：{profileId}";
                return false;
            }

            currentProfile = profile;
            session.Reset(profile.id, returnState);
            session.lastFeedback = profile.hintText ?? string.Empty;
            return true;
        }

        public void Close()
        {
            currentProfile = null;
            session.Clear();
        }

        public bool TryRemoveBlock(string cardId, out string failReason)
        {
            failReason = string.Empty;
            if (!IsOpen)
            {
                failReason = "当前没有打开方块操作。";
                return false;
            }

            if (!currentProfile.allowDelete)
            {
                failReason = "当前规则不允许删除方块。";
                return false;
            }

            if (!CanUse(session.deleteUsedCount, currentProfile.maxDeleteCount))
            {
                failReason = BuildLimitReachedMessage("删除", currentProfile.maxDeleteCount);
                session.lastFeedback = failReason;
                return false;
            }

            var cost = Mathf.Max(0, currentProfile.deleteCostGold);
            if (game.Player.Gold < cost)
            {
                failReason = $"金币不足，删除需要 {cost} 金币。";
                session.lastFeedback = failReason;
                return false;
            }

            if (!game.BoardManager.TryRemoveOwnedCard(cardId, out failReason))
            {
                session.lastFeedback = failReason;
                return false;
            }

            if (cost > 0)
                game.Player.SpendGold(cost);

            session.deleteUsedCount += 1;
            session.lastFeedback = cost > 0
                ? $"已删除方块，消耗 {cost} 金币。"
                : "已删除方块。";
            return true;
        }

        public bool TryUpgradeBlock(string cardId, out string failReason)
        {
            failReason = string.Empty;
            if (!IsOpen)
            {
                failReason = "当前没有打开方块操作。";
                return false;
            }

            if (!currentProfile.allowUpgrade)
            {
                failReason = "当前规则不允许升级方块。";
                return false;
            }

            if (!CanUse(session.upgradeUsedCount, currentProfile.maxUpgradeCount))
            {
                failReason = BuildLimitReachedMessage("升级", currentProfile.maxUpgradeCount);
                session.lastFeedback = failReason;
                return false;
            }

            var cost = Mathf.Max(0, currentProfile.upgradeCostGold);
            if (game.Player.Gold < cost)
            {
                failReason = $"金币不足，升级需要 {cost} 金币。";
                session.lastFeedback = failReason;
                return false;
            }

            if (!game.BoardManager.TryUpgradeOwnedCard(cardId, out var upgradedCard, out failReason))
            {
                session.lastFeedback = failReason;
                return false;
            }

            if (cost > 0)
                game.Player.SpendGold(cost);

            session.upgradeUsedCount += 1;
            var cardName = upgradedCard != null && !string.IsNullOrWhiteSpace(upgradedCard.cardName)
                ? upgradedCard.cardName
                : "新方块";
            session.lastFeedback = cost > 0
                ? $"已置换升级为 {cardName}，消耗 {cost} 金币。"
                : $"已置换升级为 {cardName}。";
            return true;
        }

        public bool TrySwapActiveReserve(string activeCardId, string reserveCardId, out string failReason)
        {
            failReason = string.Empty;
            if (!IsOpen)
            {
                failReason = "当前没有打开方块操作。";
                return false;
            }

            if (!currentProfile.allowSwap)
            {
                failReason = "当前规则不允许替换上阵方块。";
                return false;
            }

            if (!CanUse(session.swapUsedCount, currentProfile.maxSwapCount))
            {
                failReason = BuildLimitReachedMessage("替换", currentProfile.maxSwapCount);
                session.lastFeedback = failReason;
                return false;
            }

            var cost = Mathf.Max(0, currentProfile.swapCostGold);
            if (game.Player.Gold < cost)
            {
                failReason = $"金币不足，替换需要 {cost} 金币。";
                session.lastFeedback = failReason;
                return false;
            }

            if (!game.BoardManager.TrySwapActiveAndReserve(activeCardId, reserveCardId, out failReason))
            {
                session.lastFeedback = failReason;
                return false;
            }

            if (cost > 0)
                game.Player.SpendGold(cost);

            session.swapUsedCount += 1;
            session.lastFeedback = cost > 0
                ? $"已完成方块替换，消耗 {cost} 金币。"
                : "已完成方块替换。";
            return true;
        }

        static bool CanUse(int usedCount, int maxCount)
        {
            return maxCount < 0 || usedCount < maxCount;
        }

        static string BuildLimitReachedMessage(string actionName, int maxCount)
        {
            return maxCount < 0
                ? $"{actionName}次数已用尽。"
                : $"本次进入最多可{actionName} {maxCount} 次，次数已用完。";
        }
    }
}
