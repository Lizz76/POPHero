using System;
using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public interface IGamePhaseState
    {
        RoundState Id { get; }
        bool AllowsInput { get; }
        void Enter(RoundState previous);
        void Exit(RoundState next);
    }

    sealed class SimpleGamePhaseState : IGamePhaseState
    {
        readonly Action<RoundState> onEnter;
        readonly Action<RoundState> onExit;

        public SimpleGamePhaseState(RoundState id, bool allowsInput, Action<RoundState> onEnter = null, Action<RoundState> onExit = null)
        {
            Id = id;
            AllowsInput = allowsInput;
            this.onEnter = onEnter;
            this.onExit = onExit;
        }

        public RoundState Id { get; }
        public bool AllowsInput { get; }

        public void Enter(RoundState previous)
        {
            onEnter?.Invoke(previous);
        }

        public void Exit(RoundState next)
        {
            onExit?.Invoke(next);
        }
    }

    public sealed class GamePhaseStateMachine
    {
        readonly Dictionary<RoundState, IGamePhaseState> phases = new();

        public IGamePhaseState Current { get; private set; }

        public void Register(IGamePhaseState phase)
        {
            if (phase != null)
                phases[phase.Id] = phase;
        }

        public void Change(RoundState newState)
        {
            var previous = Current?.Id ?? newState;
            Current?.Exit(newState);
            if (!phases.TryGetValue(newState, out var next))
                return;

            Current = next;
            Current.Enter(previous);
        }
    }

    public sealed class BlockCollectionServiceFacade : IBlockCollectionService
    {
        readonly BoardManager boardManager;

        public BlockCollectionServiceFacade(BoardManager manager)
        {
            boardManager = manager;
        }

        public IReadOnlyList<BlockCardState> ActiveCardStates => boardManager.ActiveCardStates;
        public IReadOnlyList<BlockCardState> ReserveCardStates => boardManager.ReserveCardStates;
        public IReadOnlyList<BlockCardState> AllCardStates => boardManager.AllCardStates;
        public int ActiveCapacity => boardManager.ActiveCapacity;
        public int ReserveCapacity => boardManager.ReserveCapacity;
        public int ActiveCardCount => boardManager.ActiveCardCount;
        public int ReserveCardCount => boardManager.ReserveCardCount;
        public int BlueprintCount => boardManager.BlueprintCount;
        public bool IsActiveFull => boardManager.IsActiveFull;
        public bool IsReserveFull => boardManager.IsReserveFull;
        public bool CanAcceptRewardBlock => boardManager.CanAcceptRewardBlock;
        public bool RewardWillGoToReserve => boardManager.RewardWillGoToReserve;
        public bool TryRemoveOwnedCard(string cardId, out string failReason) => boardManager.TryRemoveOwnedCard(cardId, out failReason);
        public bool TrySwapActiveAndReserve(string activeCardId, string reserveCardId, out string failReason) => boardManager.TrySwapActiveAndReserve(activeCardId, reserveCardId, out failReason);
        public bool EnsureAtLeastOneActive() => boardManager.EnsureAtLeastOneActive();
    }

    public sealed class BlockRewardServiceFacade : IBlockRewardService
    {
        readonly BoardManager boardManager;

        public BlockRewardServiceFacade(BoardManager manager)
        {
            boardManager = manager;
        }

        public IReadOnlyList<BlockRewardOption> ActiveRewardOptions => boardManager.ActiveRewardOptions;
        public void GenerateRewardOptions(int defeatedEnemies, int count) => boardManager.GenerateRewardOptions(defeatedEnemies, count);
        public bool TryClaimRewardOption(int index, out BlockCardState addedCard, out bool addedToReserve, out string failReason) => boardManager.TryClaimRewardOption(index, out addedCard, out addedToReserve, out failReason);
        public void ClearRewardOptions() => boardManager.ClearRewardOptions();
    }

    public sealed class RuntimeBoardServiceFacade : IRuntimeBoardService
    {
        readonly BoardManager boardManager;

        public RuntimeBoardServiceFacade(BoardManager manager)
        {
            boardManager = manager;
        }

        public IReadOnlyList<BoardBlock> ActiveBlocks => boardManager.ActiveBlocks;
        public Rect CurrentSafeZone => boardManager.CurrentSafeZone;
        public void ShuffleBlocks(Vector2 launchPoint) => boardManager.ShuffleBlocks(launchPoint);
        public void ApplyPreviewState(IReadOnlyCollection<BoardBlock> hitBlocks) => boardManager.ApplyPreviewState(hitBlocks);
        public void ClearPreviewState() => boardManager.ClearPreviewState();
        public int GetInstalledFamilyCount(BlockCardState card, StickerFamily family) => boardManager.GetInstalledFamilyCount(card, family);
        public bool TryInstallSticker(string cardId, int socketIndex, StickerInstance sticker, out string failReason) => boardManager.TryInstallSticker(cardId, socketIndex, sticker, out failReason);
        public StickerInstance RemoveSticker(string cardId, int socketIndex) => boardManager.RemoveSticker(cardId, socketIndex);
        public void UnlockRandomSocket() => boardManager.UnlockRandomSocket();
    }

    public sealed class ModServiceFacade : IModService
    {
        readonly ModManager modManager;

        public ModServiceFacade(ModManager manager)
        {
            modManager = manager;
        }

        public IReadOnlyList<ModInstance> ActiveMods => modManager.ActiveMods;
        public IReadOnlyList<ModInstance> ReserveMods => modManager.ReserveMods;
        public int GetInventoryCapacityBonus() => modManager.GetInventoryCapacityBonus();
        public bool ShowHitCounter() => modManager.ShowHitCounter();
        public int GetShopRerollDiscount() => modManager.GetShopRerollDiscount();
    }

    public sealed class ShopServiceFacade : IShopService
    {
        readonly ShopManager shopManager;

        public ShopServiceFacade(ShopManager manager)
        {
            shopManager = manager;
        }

        public IReadOnlyList<ShopItemEntry> Items => shopManager.Items;
        public string LastFeedback => shopManager.LastFeedback;
        public bool HasRemovedBlockThisVisit => shopManager.HasRemovedBlockThisVisit;
    }

    public sealed class GameSessionController
    {
        readonly PopHeroGame game;

        public GameSessionController(PopHeroGame owner)
        {
            game = owner;
        }

        public void StartSession()
        {
            game.StartPrototypeCore();
        }

        public void HandleEnemyDefeated()
        {
            game.HandleEnemyDefeatedCore();
        }
    }

    public sealed class BattleFlowController
    {
        readonly PopHeroGame game;

        public BattleFlowController(PopHeroGame owner)
        {
            game = owner;
        }

        public void TryLaunchBall(Vector2 direction, TrajectoryPreviewResult preview = null)
        {
            game.TryLaunchBallCore(direction, preview);
        }

        public void OnBallReturned(Vector2 landingPoint)
        {
            game.OnBallReturnedCore(landingPoint);
        }
    }

    public sealed class IntermissionFlowController
    {
        readonly PopHeroGame game;

        public IntermissionFlowController(PopHeroGame owner)
        {
            game = owner;
        }

        public void ProcessPendingAction()
        {
            game.ProcessPendingIntermissionActionCore();
        }
    }
}
