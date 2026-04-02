using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public class BoardManager : MonoBehaviour
    {
        BoardManagerContext context;
        BlockCollectionService collectionService;
        BlockRewardService rewardService;
        RuntimeBoardService runtimeBoardService;

        public IReadOnlyList<BoardBlock> ActiveBlocks => runtimeBoardService?.ActiveBlocks ?? System.Array.Empty<BoardBlock>();
        public IReadOnlyList<BlockCardState> ActiveCardStates => collectionService?.ActiveCardStates ?? System.Array.Empty<BlockCardState>();
        public IReadOnlyList<BlockCardState> ReserveCardStates => collectionService?.ReserveCardStates ?? System.Array.Empty<BlockCardState>();
        public IReadOnlyList<BlockCardState> AllCardStates => collectionService?.AllCardStates ?? System.Array.Empty<BlockCardState>();
        public IReadOnlyList<BlockRewardOption> ActiveRewardOptions => rewardService?.ActiveRewardOptions ?? System.Array.Empty<BlockRewardOption>();
        public Rect CurrentSafeZone => runtimeBoardService?.CurrentSafeZone ?? default;
        public int ActiveCapacity => collectionService?.ActiveCapacity ?? 1;
        public int ReserveCapacity => collectionService?.ReserveCapacity ?? 0;
        public int ActiveCardCount => collectionService?.ActiveCardCount ?? 0;
        public int ReserveCardCount => collectionService?.ReserveCardCount ?? 0;
        public int BlueprintCount => collectionService?.BlueprintCount ?? 0;
        public int VisibleBlockCount => ActiveCardCount;
        public bool IsActiveFull => collectionService?.IsActiveFull ?? false;
        public bool IsReserveFull => collectionService?.IsReserveFull ?? false;
        public bool CanAcceptRewardBlock => collectionService?.CanAcceptRewardBlock ?? false;
        public bool RewardWillGoToReserve => collectionService?.RewardWillGoToReserve ?? false;

        public void Initialize(PopHeroGame owner, Transform runtimeRoot, PhysicsMaterial2D material)
        {
            context = new BoardManagerContext
            {
                Game = owner,
                BlockRoot = runtimeRoot,
                BounceMaterial = material
            };
            collectionService = new BlockCollectionService(context);
            rewardService = new BlockRewardService(context, collectionService);
            runtimeBoardService = new RuntimeBoardService(context, collectionService);
            collectionService.Reset();
            runtimeBoardService.BuildSafeZoneMarker();
        }

        public void ShuffleBlocks(Vector2 launchPoint)
        {
            runtimeBoardService?.ShuffleBlocks(launchPoint);
        }

        public void ResetBlockProgression()
        {
        }

        public void AdvanceBlockProgression()
        {
        }

        public void GenerateRewardOptions(int defeatedEnemies, int count)
        {
            rewardService?.GenerateRewardOptions(defeatedEnemies, count);
        }

        public bool TryClaimRewardOption(int index, out BlockCardState addedCard, out bool addedToReserve, out string failReason)
        {
            addedCard = null;
            addedToReserve = false;
            failReason = string.Empty;
            return rewardService != null && rewardService.TryClaimRewardOption(index, out addedCard, out addedToReserve, out failReason);
        }

        public void ClearRewardOptions()
        {
            rewardService?.ClearRewardOptions();
        }

        public bool TryRemoveOwnedCard(string cardId, out string failReason)
        {
            failReason = string.Empty;
            if (collectionService == null || runtimeBoardService == null)
                return false;

            if (!collectionService.TryRemoveOwnedCard(cardId, out var removedCard, out var removedFromActive, out failReason))
                return false;

            if (removedFromActive)
                runtimeBoardService.RemoveRuntimeCard(removedCard);

            collectionService.EnsureAtLeastOneActive();
            runtimeBoardService.RefreshRuntimeBoardIfManageable();
            return true;
        }

        public bool TrySwapActiveAndReserve(string activeCardId, string reserveCardId, out string failReason)
        {
            failReason = string.Empty;
            if (collectionService == null || runtimeBoardService == null)
                return false;

            if (!collectionService.TrySwapActiveAndReserve(activeCardId, reserveCardId, out failReason))
                return false;

            runtimeBoardService.RefreshRuntimeBoardIfManageable();
            return true;
        }

        public bool EnsureAtLeastOneActive()
        {
            return collectionService != null && collectionService.EnsureAtLeastOneActive();
        }

        public void ApplyPreviewState(IReadOnlyCollection<BoardBlock> hitBlocks)
        {
            runtimeBoardService?.ApplyPreviewState(hitBlocks);
        }

        public void ClearPreviewState()
        {
            runtimeBoardService?.ClearPreviewState();
        }

        public int GetInstalledFamilyCount(BlockCardState card, StickerFamily family)
        {
            return runtimeBoardService?.GetInstalledFamilyCount(card, family) ?? 0;
        }

        public bool TryInstallSticker(string cardId, int socketIndex, StickerInstance sticker, out string failReason)
        {
            failReason = string.Empty;
            return runtimeBoardService != null && runtimeBoardService.TryInstallSticker(cardId, socketIndex, sticker, out failReason);
        }

        public StickerInstance RemoveSticker(string cardId, int socketIndex)
        {
            return runtimeBoardService?.RemoveSticker(cardId, socketIndex);
        }

        public void UnlockRandomSocket()
        {
            runtimeBoardService?.UnlockRandomSocket();
        }
    }
}
