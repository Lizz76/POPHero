using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    internal sealed class BoardManagerContext
    {
        public readonly PlayerBlockCollection BlockCollection = new();
        public readonly List<BoardBlock> RuntimeBlocks = new();
        public readonly List<BlockRewardOption> ActiveRewardOptions = new();
        public readonly List<BlockCardState> AllCardsCache = new();
        public PopHeroGame Game;
        public Transform BlockRoot;
        public PhysicsMaterial2D BounceMaterial;
        public int CardSerial;
        public GameObject SafeZoneMarker;
        public Rect CurrentSafeZone;

        public int ActiveCapacity => Mathf.Max(1, Game.config.blockRewards.maxActiveBlocks);
        public int ReserveCapacity => Mathf.Max(0, Game.config.blockRewards.maxReserveBlocks);

        public void RefreshAllCardsCache()
        {
            AllCardsCache.Clear();
            AllCardsCache.AddRange(BlockCollection.activeBlocks);
            AllCardsCache.AddRange(BlockCollection.reserveBlocks);
        }
    }

    internal static class BlockPresentationUtility
    {
        public static SocketTargetMask GetMaskForBlock(BoardBlockType blockType)
        {
            return blockType switch
            {
                BoardBlockType.AttackAdd => SocketTargetMask.Attack,
                BoardBlockType.Shield => SocketTargetMask.Shield,
                BoardBlockType.AttackMultiply => SocketTargetMask.Multiplier,
                _ => SocketTargetMask.Hybrid
            };
        }

        public static BlockFamily GetFamilyForType(BoardBlockType blockType)
        {
            return blockType switch
            {
                BoardBlockType.AttackAdd => BlockFamily.Strike,
                BoardBlockType.Shield => BlockFamily.Guard,
                BoardBlockType.AttackMultiply => BlockFamily.Prism,
                _ => BlockFamily.Hybrid
            };
        }

        public static string GetRarityName(BlockRarity rarity)
        {
            return rarity switch
            {
                BlockRarity.White => "白",
                BlockRarity.Blue => "蓝",
                BlockRarity.Purple => "紫",
                BlockRarity.Gold => "金",
                _ => "白"
            };
        }

        public static string GetBlockTypeName(BoardBlockType blockType)
        {
            return blockType switch
            {
                BoardBlockType.AttackAdd => "攻击",
                BoardBlockType.Shield => "防御",
                BoardBlockType.AttackMultiply => "倍率",
                _ => "混合"
            };
        }

        public static string GetRewardDescription(BoardBlockType blockType, BlockRarity rarity, float value)
        {
            return blockType switch
            {
                BoardBlockType.AttackAdd => $"{GetRarityName(rarity)}攻击方块，基础伤害 +{Mathf.RoundToInt(value)}。",
                BoardBlockType.Shield => $"{GetRarityName(rarity)}防御方块，基础护盾 +{Mathf.RoundToInt(value)}。",
                _ => $"{GetRarityName(rarity)}倍率方块，基础倍率 x{value:0.0#}。"
            };
        }

        public static string GetCardFlavor(BlockCardState cardState)
        {
            return cardState.family switch
            {
                BlockFamily.Strike => "偏重直接伤害，适合和爆发或重复命中的嵌片联动。",
                BlockFamily.Guard => "偏重单回合护盾和反击减伤，适合走转化与防守路线。",
                BlockFamily.Prism => "偏重倍率和连携，是很强的连招起点。",
                _ => "能和多种嵌片家族稳定配合。"
            };
        }

        public static Color GetRewardColor(BoardBlockType blockType, BlockRarity rarity, Color baseColor)
        {
            return rarity switch
            {
                BlockRarity.White => baseColor,
                BlockRarity.Blue => Color.Lerp(baseColor, new Color(0.32f, 0.62f, 1f, 1f), 0.45f),
                BlockRarity.Purple => Color.Lerp(baseColor, new Color(0.7f, 0.34f, 1f, 1f), 0.55f),
                BlockRarity.Gold => Color.Lerp(baseColor, new Color(1f, 0.8f, 0.24f, 1f), 0.65f),
                _ => baseColor
            };
        }

        public static void RefreshCardPresentation(BlockCardState cardState)
        {
            cardState.tags.Clear();
            cardState.cardName = $"{GetRarityName(cardState.rarity)}{GetBlockTypeName(cardState.baseBlockType)}方块";
            cardState.mainActionText = cardState.baseBlockType switch
            {
                BoardBlockType.AttackAdd => $"命中时获得 {Mathf.RoundToInt(cardState.baseValueA)} 点伤害。",
                BoardBlockType.AttackMultiply => $"命中时将当前伤害乘以 {cardState.baseValueA:0.0#}。",
                BoardBlockType.Shield => $"命中时获得 {Mathf.RoundToInt(cardState.baseValueA)} 点护盾。",
                _ => "命中时触发混合效果。"
            };

            cardState.detailLines.Clear();
            cardState.detailLines.Add(GetCardFlavor(cardState));
            foreach (var socket in cardState.sockets)
            {
                if (socket.installedSticker == null)
                    continue;

                cardState.detailLines.Add(socket.installedSticker.data.mainActionText);
                cardState.tags.Add(socket.installedSticker.data.family.ToString());
            }
        }
    }

internal sealed class BlockCollectionService
    {
        readonly BoardManagerContext context;

        public BlockCollectionService(BoardManagerContext boardContext)
        {
            context = boardContext;
        }

        public IReadOnlyList<BlockCardState> ActiveCardStates => context.BlockCollection.activeBlocks;
        public IReadOnlyList<BlockCardState> ReserveCardStates => context.BlockCollection.reserveBlocks;
        public IReadOnlyList<BlockCardState> AllCardStates => context.AllCardsCache;
        public int ActiveCapacity => context.ActiveCapacity;
        public int ReserveCapacity => context.ReserveCapacity;
        public int ActiveCardCount => context.BlockCollection.ActiveCount;
        public int ReserveCardCount => context.BlockCollection.ReserveCount;
        public int BlueprintCount => context.BlockCollection.GetTotalBlockCount();
        public bool IsActiveFull => context.BlockCollection.IsActiveFull(ActiveCapacity);
        public bool IsReserveFull => context.BlockCollection.IsReserveFull(ReserveCapacity);
        public bool CanAcceptRewardBlock => !IsActiveFull || !IsReserveFull;
        public bool RewardWillGoToReserve => IsActiveFull && !IsReserveFull;

        public void Reset()
        {
            context.BlockCollection.Clear();
            context.ActiveRewardOptions.Clear();
            context.AllCardsCache.Clear();
            context.CardSerial = 0;
        }

        public BlockCardState FindCard(string cardId)
        {
            return context.BlockCollection.FindCard(cardId);
        }

        public bool TryAddCard(BlockCardState card, out bool addedToReserve, out string failReason)
        {
            var result = context.BlockCollection.TryAddCard(card, ActiveCapacity, ReserveCapacity, out addedToReserve, out failReason);
            if (result)
                context.RefreshAllCardsCache();
            return result;
        }

        public bool TryRemoveOwnedCard(string cardId, out BlockCardState removedCard, out bool removedFromActive, out string failReason)
        {
            failReason = string.Empty;
            removedCard = null;
            removedFromActive = false;
            if (context.BlockCollection.GetTotalBlockCount() <= 1)
            {
                failReason = "至少要保留 1 张方块。";
                return false;
            }

            if (!context.BlockCollection.TryRemoveCard(cardId, out removedCard, out removedFromActive))
            {
                failReason = "没有找到对应的方块。";
                return false;
            }

            context.RefreshAllCardsCache();
            return true;
        }

        public bool TrySwapActiveAndReserve(string activeCardId, string reserveCardId, out string failReason)
        {
            failReason = string.Empty;
            if (!context.BlockCollection.SwapActiveAndReserve(activeCardId, reserveCardId))
            {
                failReason = "没有在上阵或仓库中找到要交换的方块。";
                return false;
            }

            context.RefreshAllCardsCache();
            return true;
        }

        public bool EnsureAtLeastOneActive()
        {
            var promoted = context.BlockCollection.EnsureAtLeastOneActive();
            if (promoted)
                context.RefreshAllCardsCache();
            return promoted;
        }
    }

internal sealed class BlockRewardService
    {
        readonly BoardManagerContext context;
        readonly BlockCollectionService collectionService;

        public BlockRewardService(BoardManagerContext boardContext, BlockCollectionService collection)
        {
            context = boardContext;
            collectionService = collection;
        }

        public IReadOnlyList<BlockRewardOption> ActiveRewardOptions => context.ActiveRewardOptions;

        public void GenerateRewardOptions(int defeatedEnemies, int count)
        {
            context.ActiveRewardOptions.Clear();
            var optionCount = Mathf.Max(1, count);
            for (var index = 0; index < optionCount; index++)
                context.ActiveRewardOptions.Add(CreateRewardOption(defeatedEnemies, index));
        }

        public bool TryClaimRewardOption(int index, out BlockCardState addedCard, out bool addedToReserve, out string failReason)
        {
            addedCard = null;
            addedToReserve = false;
            failReason = string.Empty;
            if (index < 0 || index >= context.ActiveRewardOptions.Count)
            {
                failReason = "奖励索引无效。";
                return false;
            }

            addedCard = CreateCardState(context.ActiveRewardOptions[index]);
            if (!collectionService.TryAddCard(addedCard, out addedToReserve, out failReason))
                return false;

            context.ActiveRewardOptions.Clear();
            return true;
        }

        public void ClearRewardOptions()
        {
            context.ActiveRewardOptions.Clear();
        }

        BlockRewardOption CreateRewardOption(int defeatedEnemies, int optionIndex)
        {
            var blockType = RollBlockType();
            var rarity = RollRarity(defeatedEnemies);
            var value = GetRarityValue(blockType, rarity);
            return new BlockRewardOption
            {
                id = $"block_reward_{defeatedEnemies:00}_{optionIndex:00}",
                blockType = blockType,
                rarity = rarity,
                baseValue = value,
                displayName = $"{BlockPresentationUtility.GetRarityName(rarity)}{BlockPresentationUtility.GetBlockTypeName(blockType)}方块",
                desc = BlockPresentationUtility.GetRewardDescription(blockType, rarity, value),
                color = GetRewardColor(blockType, rarity),
                family = BlockPresentationUtility.GetFamilyForType(blockType)
            };
        }

        BlockCardState CreateCardState(BlockRewardOption option)
        {
            var state = new BlockCardState
            {
                id = $"card_{context.CardSerial++:000}",
                baseBlockType = option.blockType,
                rarity = option.rarity,
                family = option.family,
                baseValueA = option.baseValue,
                baseValueB = 0f,
                templateOrder = collectionService.BlueprintCount
            };

            for (var socketIndex = 0; socketIndex < context.Game.config.stickers.defaultSocketsPerCard; socketIndex++)
            {
                state.sockets.Add(new SocketSlotState
                {
                    index = socketIndex,
                    isUnlocked = socketIndex < context.Game.config.stickers.unlockedSocketsPerCard,
                    targetMask = socketIndex == 0 ? BlockPresentationUtility.GetMaskForBlock(option.blockType) : SocketTargetMask.Any
                });
            }

            BlockPresentationUtility.RefreshCardPresentation(state);
            return state;
        }

        BoardBlockType RollBlockType()
        {
            return Random.Range(0, 3) switch
            {
                0 => BoardBlockType.AttackAdd,
                1 => BoardBlockType.Shield,
                _ => BoardBlockType.AttackMultiply
            };
        }

        BlockRarity RollRarity(int defeatedEnemies)
        {
            var stages = context.Game.config.blockRewards.rarityOdds;
            var selectedStage = stages[0];
            foreach (var stage in stages)
            {
                if (defeatedEnemies >= stage.minimumKills)
                    selectedStage = stage;
            }

            var roll = Random.Range(0f, 100f);
            if (roll < selectedStage.white)
                return BlockRarity.White;
            roll -= selectedStage.white;
            if (roll < selectedStage.blue)
                return BlockRarity.Blue;
            roll -= selectedStage.blue;
            if (roll < selectedStage.purple)
                return BlockRarity.Purple;
            return BlockRarity.Gold;
        }

        float GetRarityValue(BoardBlockType blockType, BlockRarity rarity)
        {
            return blockType switch
            {
                BoardBlockType.AttackAdd => context.Game.config.blockRewards.attackValues.Get(rarity),
                BoardBlockType.Shield => context.Game.config.blockRewards.shieldValues.Get(rarity),
                _ => context.Game.config.blockRewards.multiplierValues.Get(rarity)
            };
        }

        Color GetRewardColor(BoardBlockType blockType, BlockRarity rarity)
        {
            return BlockPresentationUtility.GetRewardColor(blockType, rarity, GetBaseColor(blockType));
        }

        static Color GetBaseColor(BoardBlockType blockType)
        {
            return blockType switch
            {
                BoardBlockType.AttackAdd => new Color(0.35f, 0.56f, 0.95f),
                BoardBlockType.Shield => new Color(0.25f, 0.82f, 0.45f),
                BoardBlockType.AttackMultiply => new Color(0.72f, 0.42f, 1f),
                _ => Color.white
            };
        }
    }

internal sealed class RuntimeBoardService
    {
        readonly BoardManagerContext context;
        readonly BlockCollectionService collectionService;

        public RuntimeBoardService(BoardManagerContext boardContext, BlockCollectionService collection)
        {
            context = boardContext;
            collectionService = collection;
        }

        public IReadOnlyList<BoardBlock> ActiveBlocks => context.RuntimeBlocks;
        public Rect CurrentSafeZone => context.CurrentSafeZone;

        public void BuildSafeZoneMarker()
        {
            context.SafeZoneMarker = PrototypeVisualFactory.CreateSpriteObject("LaunchSafeZone", context.Game.transform, PrototypeVisualFactory.SquareSprite, context.Game.config.arena.safeZoneColor, 4, Vector2.one);
            context.SafeZoneMarker.SetActive(context.Game.config.debug.showSpawnSafeZone);
        }

        public void ShuffleBlocks(Vector2 launchPoint)
        {
            collectionService.EnsureAtLeastOneActive();
            ClearRuntimeBlocks();
            context.CurrentSafeZone = BuildSafeZone(launchPoint);
            UpdateSafeZoneMarker();
            if (context.BlockCollection.activeBlocks.Count == 0)
            {
                ClearPreviewState();
                return;
            }

            var positions = BuildCandidatePositions(context.CurrentSafeZone);
            Shuffle(positions);
            var count = Mathf.Min(positions.Count, context.BlockCollection.activeBlocks.Count);
            for (var index = 0; index < count; index++)
                CreateRuntimeBlock(context.BlockCollection.activeBlocks[index], positions[index]);

            ClearPreviewState();
            context.Game.CombatEventHub?.Publish(new CombatEventPayload(StickerTriggerType.OnBoardRefreshed));
        }

        public void ApplyPreviewState(IReadOnlyCollection<BoardBlock> hitBlocks)
        {
            if (context.RuntimeBlocks.Count == 0)
                return;

            if (hitBlocks == null)
            {
                ClearPreviewState();
                return;
            }

            var highlightedIds = new HashSet<int>();
            foreach (var block in hitBlocks)
            {
                if (block != null)
                    highlightedIds.Add(block.GetInstanceID());
            }

            foreach (var block in context.RuntimeBlocks)
            {
                if (block != null)
                    block.SetVisualState(highlightedIds.Contains(block.GetInstanceID()) ? BlockVisualState.Highlight : BlockVisualState.Dim);
            }
        }

        public void ClearPreviewState()
        {
            foreach (var block in context.RuntimeBlocks)
            {
                if (block != null)
                    block.SetVisualState(BlockVisualState.Default);
            }
        }

        public int GetInstalledFamilyCount(BlockCardState card, StickerFamily family)
        {
            if (card == null)
                return 0;

            var count = 0;
            foreach (var socket in card.sockets)
            {
                if (socket.installedSticker?.data?.family == family)
                    count += 1;
            }

            return count;
        }

        public bool TryInstallSticker(string cardId, int socketIndex, StickerInstance sticker, out string failReason)
        {
            failReason = string.Empty;
            var card = collectionService.FindCard(cardId);
            if (card == null || sticker?.data == null)
            {
                failReason = "Invalid card or sticker target.";
                return false;
            }

            if (socketIndex < 0 || socketIndex >= card.sockets.Count)
            {
                failReason = "Socket does not exist.";
                return false;
            }

            var socket = card.sockets[socketIndex];
            if (!socket.isUnlocked)
            {
                failReason = "This socket is still locked.";
                return false;
            }

            if (socket.installedSticker != null)
            {
                failReason = "This socket already has a sticker.";
                return false;
            }

            var requiredMask = BlockPresentationUtility.GetMaskForBlock(card.baseBlockType);
            if ((socket.targetMask & requiredMask) == 0 || (sticker.data.targetBlockType & requiredMask) == 0)
            {
                failReason = "This card cannot use that sticker.";
                return false;
            }

            socket.installedSticker = sticker;
            BlockPresentationUtility.RefreshCardPresentation(card);
            SyncRuntimeBlock(card);
            return true;
        }

        public StickerInstance RemoveSticker(string cardId, int socketIndex)
        {
            var card = collectionService.FindCard(cardId);
            if (card == null || socketIndex < 0 || socketIndex >= card.sockets.Count)
                return null;

            var socket = card.sockets[socketIndex];
            var sticker = socket.installedSticker;
            socket.installedSticker = null;
            BlockPresentationUtility.RefreshCardPresentation(card);
            SyncRuntimeBlock(card);
            return sticker;
        }

        public void UnlockRandomSocket()
        {
            var candidates = new List<BlockCardState>();
            foreach (var card in context.AllCardsCache)
            {
                if (card.sockets.Exists(socket => !socket.isUnlocked) || card.sockets.Count < context.Game.config.stickers.maxSocketsPerCard)
                    candidates.Add(card);
            }

            if (candidates.Count == 0)
                return;

            var cardToUnlock = candidates[Random.Range(0, candidates.Count)];
            var lockedSocket = cardToUnlock.sockets.Find(socket => !socket.isUnlocked);
            if (lockedSocket != null)
            {
                lockedSocket.isUnlocked = true;
                BlockPresentationUtility.RefreshCardPresentation(cardToUnlock);
                SyncRuntimeBlock(cardToUnlock);
                return;
            }

            if (cardToUnlock.sockets.Count >= context.Game.config.stickers.maxSocketsPerCard)
                return;

            cardToUnlock.sockets.Add(new SocketSlotState
            {
                index = cardToUnlock.sockets.Count,
                isUnlocked = true,
                targetMask = SocketTargetMask.Any
            });
            BlockPresentationUtility.RefreshCardPresentation(cardToUnlock);
            SyncRuntimeBlock(cardToUnlock);
        }

        public void RemoveRuntimeCard(BlockCardState removedCard)
        {
            var runtimeBlock = context.RuntimeBlocks.Find(block => block != null && block.CardState == removedCard);
            if (runtimeBlock != null)
            {
                context.RuntimeBlocks.Remove(runtimeBlock);
                Object.Destroy(runtimeBlock.gameObject);
            }
        }

        public void RefreshRuntimeBoardIfManageable()
        {
            if (!context.Game.CanManageBlockAssignments)
                return;

            ShuffleBlocks(context.Game.CurrentLaunchPoint);
        }

        void SyncRuntimeBlock(BlockCardState cardState)
        {
            foreach (var block in context.RuntimeBlocks)
            {
                if (block != null && block.CardState == cardState)
                    block.RefreshFromCard();
            }
        }

        Rect BuildSafeZone(Vector2 launchPoint)
        {
            var board = context.Game.BoardRect;
            var width = context.Game.config.board.launchSafeWidth;
            var height = context.Game.config.board.launchSafeHeight;
            var minX = Mathf.Clamp(launchPoint.x - width * 0.5f, board.xMin, board.xMax - width);
            return new Rect(minX, board.yMin + 0.2f, width, height);
        }

        List<Vector2> BuildCandidatePositions(Rect safeZone)
        {
            var positions = new List<Vector2>();
            var board = context.Game.BoardRect;
            var size = context.Game.config.board.blockSize;
            var halfSize = size * 0.5f;
            var minX = board.xMin + context.Game.config.board.sidePadding + halfSize.x;
            var maxX = board.xMax - context.Game.config.board.sidePadding - halfSize.x;
            var minY = board.yMin + context.Game.config.board.bottomPadding + halfSize.y;
            var maxY = board.yMax - context.Game.config.board.topPadding - halfSize.y;

            var spacingX = size.x + 0.45f;
            var spacingY = size.y + 0.5f;
            var columns = Mathf.Max(4, Mathf.FloorToInt((maxX - minX) / spacingX) + 1);
            var rows = Mathf.Max(4, Mathf.FloorToInt((maxY - minY) / spacingY) + 1);

            for (var y = 0; y < rows; y++)
            {
                var tY = rows == 1 ? 0.5f : y / (rows - 1f);
                var posY = Mathf.Lerp(minY, maxY, tY);
                for (var x = 0; x < columns; x++)
                {
                    var tX = columns == 1 ? 0.5f : x / (columns - 1f);
                    var posX = Mathf.Lerp(minX, maxX, tX);
                    var point = new Vector2(posX, posY);
                    if (point.x > safeZone.xMin - halfSize.x &&
                        point.x < safeZone.xMax + halfSize.x &&
                        point.y > safeZone.yMin - halfSize.y &&
                        point.y < safeZone.yMax + halfSize.y)
                        continue;

                    positions.Add(point);
                }
            }

            return positions;
        }

        void CreateRuntimeBlock(BlockCardState cardState, Vector2 position)
        {
            var go = new GameObject(cardState.id);
            go.transform.SetParent(context.BlockRoot, false);

            BoardBlock block = cardState.baseBlockType switch
            {
                BoardBlockType.AttackAdd => go.AddComponent<AttackAddBlock>(),
                BoardBlockType.AttackMultiply => go.AddComponent<AttackMultiplyBlock>(),
                _ => go.AddComponent<ShieldBlock>()
            };

            block.Initialize(
                context.Game,
                cardState,
                position,
                context.Game.config.board.blockSize,
                GetRandomRotation(),
                context.Game.config.board.keepLabelUpright,
                GetRewardColor(cardState.baseBlockType, cardState.rarity),
                context.BounceMaterial);
            context.RuntimeBlocks.Add(block);
        }

        void UpdateSafeZoneMarker()
        {
            if (context.SafeZoneMarker == null)
                return;

            context.SafeZoneMarker.SetActive(context.Game.config.debug.showSpawnSafeZone);
            context.SafeZoneMarker.transform.position = context.CurrentSafeZone.center;
            context.SafeZoneMarker.transform.localScale = new Vector3(context.CurrentSafeZone.width, context.CurrentSafeZone.height, 1f);
        }

        void ClearRuntimeBlocks()
        {
            foreach (var block in context.RuntimeBlocks)
            {
                if (block != null)
                    Object.Destroy(block.gameObject);
            }

            context.RuntimeBlocks.Clear();
        }

        void Shuffle<T>(IList<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        Color GetRewardColor(BoardBlockType blockType, BlockRarity rarity)
        {
            return BlockPresentationUtility.GetRewardColor(blockType, rarity, GetBaseColor(blockType));
        }

        static Color GetBaseColor(BoardBlockType blockType)
        {
            return blockType switch
            {
                BoardBlockType.AttackAdd => new Color(0.35f, 0.56f, 0.95f),
                BoardBlockType.Shield => new Color(0.25f, 0.82f, 0.45f),
                BoardBlockType.AttackMultiply => new Color(0.72f, 0.42f, 1f),
                _ => Color.white
            };
        }

        float GetRandomRotation()
        {
            var min = context.Game.config.board.minRotationAngle;
            var max = context.Game.config.board.maxRotationAngle;
            var step = Mathf.Max(0.001f, context.Game.config.board.rotationStep);
            if (Mathf.Approximately(min, max))
                return min;

            var randomAngle = Random.Range(min, max);
            return Mathf.Round(randomAngle / step) * step;
        }
    }
}
