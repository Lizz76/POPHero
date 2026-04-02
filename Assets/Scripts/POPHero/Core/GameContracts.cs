using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public interface IGameReadModel
    {
        RoundState State { get; }
        PlayerData Player { get; }
        EnemyData CurrentEnemy { get; }
        int EncounterIndex { get; }
        int RemainingLaunchesForEnemy { get; }
        int MaxLaunchesPerEnemy { get; }
        string IntermissionMessage { get; }
        bool CanManageBlockAssignments { get; }
        bool IsInitialBlockDraftPending { get; }
        string AimModeDisplayText { get; }
        int PreviewHitCount { get; }
        int PreviewAttackBlockCount { get; }
        int PreviewShieldBlockCount { get; }
        int PreviewMultiplierBlockCount { get; }
        IBlockCollectionService BlockCollections { get; }
        IBlockRewardService BlockRewards { get; }
        StickerInventory StickerInventory { get; }
        IModService Mods { get; }
        RoundController RoundController { get; }
        RewardChoiceController RewardChoiceController { get; }
        IShopService Shops { get; }
        PopHeroPrototypeConfig Config { get; }
        string GameOverMessage { get; }
    }

    public interface IAimService
    {
        AimLockContext Context { get; }
        void Initialize(PopHeroGame owner, TrajectoryPredictor predictor);
        void Reset();
        bool BeginInput(Vector2 cursorWorld);
        void EndInput();
        bool UpdateLockedAim(Vector2 cursorWorld, bool forceAccept);
    }

    public interface IBounceStepSolver
    {
        bool TryCastStep(Vector2 origin, Vector2 direction, float maxDistance, Collider2D ignoredCollider, Collider2D secondaryIgnoredCollider, out TrajectoryCastStep step);
        bool TryResolveCornerBounce(WallHitMemory previousWallHit, TrajectoryCastStep step, out CornerBounceResult result);
    }

    public interface ICombatEventListener
    {
        void OnCombatEvent(CombatEventPayload payload);
    }

    public readonly struct CombatEventPayload
    {
        public CombatEventPayload(StickerTriggerType triggerType, BoardBlock block = null, int damage = 0)
        {
            TriggerType = triggerType;
            Block = block;
            Damage = damage;
        }

        public StickerTriggerType TriggerType { get; }
        public BoardBlock Block { get; }
        public int Damage { get; }
    }

    public interface ICombatEventHub
    {
        void Subscribe(ICombatEventListener listener);
        void Unsubscribe(ICombatEventListener listener);
        void Publish(CombatEventPayload payload);
    }

    public sealed class CombatEventHub : ICombatEventHub
    {
        readonly List<ICombatEventListener> listeners = new();

        public void Subscribe(ICombatEventListener listener)
        {
            if (listener != null && !listeners.Contains(listener))
                listeners.Add(listener);
        }

        public void Unsubscribe(ICombatEventListener listener)
        {
            if (listener != null)
                listeners.Remove(listener);
        }

        public void Publish(CombatEventPayload payload)
        {
            for (var index = 0; index < listeners.Count; index++)
                listeners[index]?.OnCombatEvent(payload);
        }
    }

    public enum HudCommandType
    {
        None,
        ToggleAimMode,
        DebugShuffleBoard,
        DebugAddGold,
        DebugKillEnemy,
        DebugDamagePlayer,
        TrySelectBlockReward,
        SkipBlockReward,
        TrySelectReward,
        TryRerollRewardChoices,
        SkipRewardChoices,
        TryBuyShopItem,
        TryRerollShop,
        CloseShop,
        FinishLoadout,
        BeginStickerDrag,
        CancelStickerDrag,
        ToggleModActivation,
        TryRemoveBlockInShop,
        TrySwapActiveReserve,
        TryInstallDraggedSticker,
        RemoveStickerFromCard
    }

    public readonly struct HudCommand
    {
        public HudCommand(HudCommandType type, int intValue = 0, string primaryId = null, string secondaryId = null)
        {
            Type = type;
            IntValue = intValue;
            PrimaryId = primaryId;
            SecondaryId = secondaryId;
        }

        public HudCommandType Type { get; }
        public int IntValue { get; }
        public string PrimaryId { get; }
        public string SecondaryId { get; }
    }

    public interface IHudCommandSink
    {
        void ExecuteHudCommand(HudCommand command);
    }

    public interface IBlockCollectionService
    {
        IReadOnlyList<BlockCardState> ActiveCardStates { get; }
        IReadOnlyList<BlockCardState> ReserveCardStates { get; }
        IReadOnlyList<BlockCardState> AllCardStates { get; }
        int ActiveCapacity { get; }
        int ReserveCapacity { get; }
        int ActiveCardCount { get; }
        int ReserveCardCount { get; }
        int BlueprintCount { get; }
        bool IsActiveFull { get; }
        bool IsReserveFull { get; }
        bool CanAcceptRewardBlock { get; }
        bool RewardWillGoToReserve { get; }
        bool TryRemoveOwnedCard(string cardId, out string failReason);
        bool TrySwapActiveAndReserve(string activeCardId, string reserveCardId, out string failReason);
        bool EnsureAtLeastOneActive();
    }

    public interface IBlockRewardService
    {
        IReadOnlyList<BlockRewardOption> ActiveRewardOptions { get; }
        void GenerateRewardOptions(int defeatedEnemies, int count);
        bool TryClaimRewardOption(int index, out BlockCardState addedCard, out bool addedToReserve, out string failReason);
        void ClearRewardOptions();
    }

    public interface IRuntimeBoardService
    {
        IReadOnlyList<BoardBlock> ActiveBlocks { get; }
        Rect CurrentSafeZone { get; }
        void ShuffleBlocks(Vector2 launchPoint);
        void ApplyPreviewState(IReadOnlyCollection<BoardBlock> hitBlocks);
        void ClearPreviewState();
        int GetInstalledFamilyCount(BlockCardState card, StickerFamily family);
        bool TryInstallSticker(string cardId, int socketIndex, StickerInstance sticker, out string failReason);
        StickerInstance RemoveSticker(string cardId, int socketIndex);
        void UnlockRandomSocket();
    }

    public interface IModService
    {
        IReadOnlyList<ModInstance> ActiveMods { get; }
        IReadOnlyList<ModInstance> ReserveMods { get; }
        int GetInventoryCapacityBonus();
        bool ShowHitCounter();
        int GetShopRerollDiscount();
    }

    public interface IShopService
    {
        IReadOnlyList<ShopItemEntry> Items { get; }
        string LastFeedback { get; }
        bool HasRemovedBlockThisVisit { get; }
    }
}
