using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public class BoardManager : MonoBehaviour
    {
        readonly PlayerBlockCollection blockCollection = new();
        readonly List<BoardBlock> runtimeBlocks = new();
        readonly List<BlockRewardOption> activeRewardOptions = new();
        readonly List<BlockCardState> allCardsCache = new();

        PopHeroGame game;
        Transform blockRoot;
        PhysicsMaterial2D bounceMaterial;
        int cardSerial;
        GameObject safeZoneMarker;
        Rect currentSafeZone;

        public IReadOnlyList<BoardBlock> ActiveBlocks => runtimeBlocks;
        public IReadOnlyList<BlockCardState> ActiveCardStates => blockCollection.activeBlocks;
        public IReadOnlyList<BlockCardState> ReserveCardStates => blockCollection.reserveBlocks;
        public IReadOnlyList<BlockCardState> AllCardStates => allCardsCache;
        public IReadOnlyList<BlockRewardOption> ActiveRewardOptions => activeRewardOptions;
        public Rect CurrentSafeZone => currentSafeZone;
        public int ActiveCapacity => Mathf.Max(1, game.config.blockRewards.maxActiveBlocks);
        public int ReserveCapacity => Mathf.Max(0, game.config.blockRewards.maxReserveBlocks);
        public int ActiveCardCount => blockCollection.ActiveCount;
        public int ReserveCardCount => blockCollection.ReserveCount;
        public int BlueprintCount => blockCollection.GetTotalBlockCount();
        public int VisibleBlockCount => blockCollection.ActiveCount;
        public bool IsActiveFull => blockCollection.IsActiveFull(ActiveCapacity);
        public bool IsReserveFull => blockCollection.IsReserveFull(ReserveCapacity);
        public bool CanAcceptRewardBlock => !IsActiveFull || !IsReserveFull;
        public bool RewardWillGoToReserve => IsActiveFull && !IsReserveFull;

        public void Initialize(PopHeroGame owner, Transform runtimeRoot, PhysicsMaterial2D material)
        {
            game = owner;
            blockRoot = runtimeRoot;
            bounceMaterial = material;
            blockCollection.Clear();
            activeRewardOptions.Clear();
            allCardsCache.Clear();
            cardSerial = 0;
            BuildSafeZoneMarker();
        }

        public void ShuffleBlocks(Vector2 launchPoint)
        {
            EnsureAtLeastOneActive();
            ClearRuntimeBlocks();
            currentSafeZone = BuildSafeZone(launchPoint);
            UpdateSafeZoneMarker();
            if (blockCollection.activeBlocks.Count == 0)
            {
                ClearPreviewState();
                return;
            }

            var positions = BuildCandidatePositions(currentSafeZone);
            Shuffle(positions);
            var count = Mathf.Min(positions.Count, blockCollection.activeBlocks.Count);
            for (var index = 0; index < count; index++)
                CreateRuntimeBlock(blockCollection.activeBlocks[index], positions[index]);

            ClearPreviewState();
            game.StickerEffectRunner.HandleBoardRefreshed();
        }

        public void ResetBlockProgression()
        {
        }

        public void AdvanceBlockProgression()
        {
        }

        public void GenerateRewardOptions(int defeatedEnemies, int count)
        {
            activeRewardOptions.Clear();
            var optionCount = Mathf.Max(1, count);
            for (var index = 0; index < optionCount; index++)
                activeRewardOptions.Add(CreateRewardOption(defeatedEnemies, index));
        }

        public bool TryClaimRewardOption(int index, out BlockCardState addedCard, out bool addedToReserve, out string failReason)
        {
            addedCard = null;
            addedToReserve = false;
            failReason = string.Empty;
            if (index < 0 || index >= activeRewardOptions.Count)
            {
                failReason = "奖励索引无效。";
                return false;
            }

            addedCard = CreateCardState(activeRewardOptions[index]);
            if (!blockCollection.TryAddCard(addedCard, ActiveCapacity, ReserveCapacity, out addedToReserve, out failReason))
                return false;

            RefreshAllCardsCache();
            activeRewardOptions.Clear();
            return true;
        }

        public void ClearRewardOptions()
        {
            activeRewardOptions.Clear();
        }

        public bool TryRemoveOwnedCard(string cardId, out string failReason)
        {
            failReason = string.Empty;
            if (blockCollection.GetTotalBlockCount() <= 1)
            {
                failReason = "至少要保留 1 张方块。";
                return false;
            }

            if (!blockCollection.TryRemoveCard(cardId, out var removedCard, out var removedFromActive))
            {
                failReason = "没找到这张方块。";
                return false;
            }

            if (removedFromActive)
            {
                var runtimeBlock = runtimeBlocks.Find(block => block != null && block.CardState == removedCard);
                if (runtimeBlock != null)
                {
                    runtimeBlocks.Remove(runtimeBlock);
                    Destroy(runtimeBlock.gameObject);
                }
            }

            EnsureAtLeastOneActive();
            RefreshAllCardsCache();
            RefreshRuntimeBoardIfManageable();
            return true;
        }

        public bool TrySwapActiveAndReserve(string activeCardId, string reserveCardId, out string failReason)
        {
            failReason = string.Empty;
            if (!blockCollection.SwapActiveAndReserve(activeCardId, reserveCardId))
            {
                failReason = "上阵区或仓库区中没有找到对应方块。";
                return false;
            }

            RefreshAllCardsCache();
            RefreshRuntimeBoardIfManageable();
            return true;
        }

        public bool EnsureAtLeastOneActive()
        {
            var promoted = blockCollection.EnsureAtLeastOneActive();
            if (promoted)
                RefreshAllCardsCache();
            return promoted;
        }

        public void ApplyPreviewState(IReadOnlyCollection<BoardBlock> hitBlocks)
        {
            if (runtimeBlocks.Count == 0)
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

            foreach (var block in runtimeBlocks)
            {
                if (block != null)
                    block.SetVisualState(highlightedIds.Contains(block.GetInstanceID()) ? BlockVisualState.Highlight : BlockVisualState.Dim);
            }
        }

        public void ClearPreviewState()
        {
            foreach (var block in runtimeBlocks)
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
            var card = blockCollection.FindCard(cardId);
            if (card == null || sticker?.data == null)
            {
                failReason = "目标卡片或嵌片无效。";
                return false;
            }

            if (socketIndex < 0 || socketIndex >= card.sockets.Count)
            {
                failReason = "槽位不存在。";
                return false;
            }

            var socket = card.sockets[socketIndex];
            if (!socket.isUnlocked)
            {
                failReason = "这个槽位还没解锁。";
                return false;
            }

            if (socket.installedSticker != null)
            {
                failReason = "这个槽位已经装了嵌片。";
                return false;
            }

            var requiredMask = GetMaskForBlock(card.baseBlockType);
            if ((socket.targetMask & requiredMask) == 0 || (sticker.data.targetBlockType & requiredMask) == 0)
            {
                failReason = "这张载体和这枚嵌片不匹配。";
                return false;
            }

            socket.installedSticker = sticker;
            RefreshCardPresentation(card);
            SyncRuntimeBlock(card);
            return true;
        }

        public StickerInstance RemoveSticker(string cardId, int socketIndex)
        {
            var card = blockCollection.FindCard(cardId);
            if (card == null || socketIndex < 0 || socketIndex >= card.sockets.Count)
                return null;

            var socket = card.sockets[socketIndex];
            var sticker = socket.installedSticker;
            socket.installedSticker = null;
            RefreshCardPresentation(card);
            SyncRuntimeBlock(card);
            return sticker;
        }

        public void UnlockRandomSocket()
        {
            var candidates = allCardsCache.FindAll(card => card.sockets.Exists(socket => !socket.isUnlocked) || card.sockets.Count < game.config.stickers.maxSocketsPerCard);
            if (candidates.Count == 0)
                return;

            var card = candidates[Random.Range(0, candidates.Count)];
            var lockedSocket = card.sockets.Find(socket => !socket.isUnlocked);
            if (lockedSocket != null)
            {
                lockedSocket.isUnlocked = true;
                RefreshCardPresentation(card);
                return;
            }

            if (card.sockets.Count >= game.config.stickers.maxSocketsPerCard)
                return;

            card.sockets.Add(new SocketSlotState
            {
                index = card.sockets.Count,
                isUnlocked = true,
                targetMask = SocketTargetMask.Any
            });
            RefreshCardPresentation(card);
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
                displayName = $"{GetRarityName(rarity)}{GetBlockTypeName(blockType)}方块",
                desc = GetRewardDescription(blockType, rarity, value),
                color = GetRewardColor(blockType, rarity),
                family = GetFamilyForType(blockType)
            };
        }

        BlockCardState CreateCardState(BlockRewardOption option)
        {
            var state = new BlockCardState
            {
                id = $"card_{cardSerial++:000}",
                baseBlockType = option.blockType,
                rarity = option.rarity,
                family = option.family,
                baseValueA = option.baseValue,
                baseValueB = 0f,
                templateOrder = BlueprintCount
            };

            for (var socketIndex = 0; socketIndex < game.config.stickers.defaultSocketsPerCard; socketIndex++)
            {
                state.sockets.Add(new SocketSlotState
                {
                    index = socketIndex,
                    isUnlocked = socketIndex < game.config.stickers.unlockedSocketsPerCard,
                    targetMask = socketIndex == 0 ? GetMaskForBlock(option.blockType) : SocketTargetMask.Any
                });
            }

            RefreshCardPresentation(state);
            return state;
        }

        BoardBlockType RollBlockType()
        {
            var roll = Random.Range(0, 3);
            return roll switch
            {
                0 => BoardBlockType.AttackAdd,
                1 => BoardBlockType.Shield,
                _ => BoardBlockType.AttackMultiply
            };
        }

        BlockRarity RollRarity(int defeatedEnemies)
        {
            var stages = game.config.blockRewards.rarityOdds;
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
                BoardBlockType.AttackAdd => game.config.blockRewards.attackValues.Get(rarity),
                BoardBlockType.Shield => game.config.blockRewards.shieldValues.Get(rarity),
                _ => game.config.blockRewards.multiplierValues.Get(rarity)
            };
        }

        string GetRewardDescription(BoardBlockType blockType, BlockRarity rarity, float value)
        {
            return blockType switch
            {
                BoardBlockType.AttackAdd => $"{GetRarityName(rarity)}攻击方块，基础伤害 +{Mathf.RoundToInt(value)}。",
                BoardBlockType.Shield => $"{GetRarityName(rarity)}防御方块，基础护盾 +{Mathf.RoundToInt(value)}。",
                _ => $"{GetRarityName(rarity)}倍率方块，基础倍率 x{value:0.0#}。"
            };
        }

        string GetRarityName(BlockRarity rarity)
        {
            return rarity switch
            {
                BlockRarity.White => "白色",
                BlockRarity.Blue => "蓝色",
                BlockRarity.Purple => "紫色",
                BlockRarity.Gold => "金色",
                _ => "白色"
            };
        }

        string GetBlockTypeName(BoardBlockType blockType)
        {
            return blockType switch
            {
                BoardBlockType.AttackAdd => "攻击",
                BoardBlockType.Shield => "防御",
                BoardBlockType.AttackMultiply => "倍率",
                _ => "混合"
            };
        }

        BlockFamily GetFamilyForType(BoardBlockType blockType)
        {
            return blockType switch
            {
                BoardBlockType.AttackAdd => BlockFamily.Strike,
                BoardBlockType.Shield => BlockFamily.Guard,
                BoardBlockType.AttackMultiply => BlockFamily.Prism,
                _ => BlockFamily.Hybrid
            };
        }

        Color GetRewardColor(BoardBlockType blockType, BlockRarity rarity)
        {
            var baseColor = GetColor(blockType);
            return rarity switch
            {
                BlockRarity.White => baseColor,
                BlockRarity.Blue => Color.Lerp(baseColor, new Color(0.32f, 0.62f, 1f, 1f), 0.45f),
                BlockRarity.Purple => Color.Lerp(baseColor, new Color(0.7f, 0.34f, 1f, 1f), 0.55f),
                BlockRarity.Gold => Color.Lerp(baseColor, new Color(1f, 0.8f, 0.24f, 1f), 0.65f),
                _ => baseColor
            };
        }

        void CreateRuntimeBlock(BlockCardState cardState, Vector2 position)
        {
            var go = new GameObject(cardState.id);
            go.transform.SetParent(blockRoot, false);

            BoardBlock block = cardState.baseBlockType switch
            {
                BoardBlockType.AttackAdd => go.AddComponent<AttackAddBlock>(),
                BoardBlockType.AttackMultiply => go.AddComponent<AttackMultiplyBlock>(),
                _ => go.AddComponent<ShieldBlock>()
            };

            block.Initialize(
                game,
                cardState,
                position,
                game.config.board.blockSize,
                GetRandomRotation(),
                game.config.board.keepLabelUpright,
                GetRewardColor(cardState.baseBlockType, cardState.rarity),
                bounceMaterial);
            runtimeBlocks.Add(block);
        }

        void SyncRuntimeBlock(BlockCardState cardState)
        {
            foreach (var block in runtimeBlocks)
            {
                if (block != null && block.CardState == cardState)
                    block.RefreshFromCard();
            }
        }

        void RefreshCardPresentation(BlockCardState cardState)
        {
            cardState.tags.Clear();
            cardState.cardName = $"{GetRarityName(cardState.rarity)}{GetBlockTypeName(cardState.baseBlockType)}方块";
            cardState.mainActionText = cardState.baseBlockType switch
            {
                BoardBlockType.AttackAdd => $"命中时增加 {Mathf.RoundToInt(cardState.baseValueA)} 点伤害。",
                BoardBlockType.AttackMultiply => $"命中时把当前伤害乘以 {cardState.baseValueA:0.0#}。",
                BoardBlockType.Shield => $"命中时增加 {Mathf.RoundToInt(cardState.baseValueA)} 点护盾。",
                _ => "命中时触发复合规则。"
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

        string GetCardFlavor(BlockCardState cardState)
        {
            return cardState.family switch
            {
                BlockFamily.Strike => "偏向直接输出，适合承接爆发与重复命中类嵌片。",
                BlockFamily.Guard => "偏向单回合护盾与反击削减，适合转化与防御构筑。",
                BlockFamily.Prism => "偏向倍率与中继，适合作为连段启动器。",
                _ => "可以与多种 family 组合。"
            };
        }

        Rect BuildSafeZone(Vector2 launchPoint)
        {
            var board = game.BoardRect;
            var width = game.config.board.launchSafeWidth;
            var height = game.config.board.launchSafeHeight;
            var minX = Mathf.Clamp(launchPoint.x - width * 0.5f, board.xMin, board.xMax - width);
            return new Rect(minX, board.yMin + 0.2f, width, height);
        }

        List<Vector2> BuildCandidatePositions(Rect safeZone)
        {
            var positions = new List<Vector2>();
            var board = game.BoardRect;
            var size = game.config.board.blockSize;
            var halfSize = size * 0.5f;
            var minX = board.xMin + game.config.board.sidePadding + halfSize.x;
            var maxX = board.xMax - game.config.board.sidePadding - halfSize.x;
            var minY = board.yMin + game.config.board.bottomPadding + halfSize.y;
            var maxY = board.yMax - game.config.board.topPadding - halfSize.y;

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

        void BuildSafeZoneMarker()
        {
            safeZoneMarker = PrototypeVisualFactory.CreateSpriteObject("LaunchSafeZone", transform, PrototypeVisualFactory.SquareSprite, game.config.arena.safeZoneColor, 4, Vector2.one);
            safeZoneMarker.SetActive(game.config.debug.showSpawnSafeZone);
        }

        void UpdateSafeZoneMarker()
        {
            if (safeZoneMarker == null)
                return;

            safeZoneMarker.SetActive(game.config.debug.showSpawnSafeZone);
            safeZoneMarker.transform.position = currentSafeZone.center;
            safeZoneMarker.transform.localScale = new Vector3(currentSafeZone.width, currentSafeZone.height, 1f);
        }

        void ClearRuntimeBlocks()
        {
            foreach (var block in runtimeBlocks)
            {
                if (block != null)
                    Destroy(block.gameObject);
            }

            runtimeBlocks.Clear();
        }

        void RefreshAllCardsCache()
        {
            allCardsCache.Clear();
            allCardsCache.AddRange(blockCollection.activeBlocks);
            allCardsCache.AddRange(blockCollection.reserveBlocks);
        }

        void RefreshRuntimeBoardIfManageable()
        {
            if (game == null)
                return;

            if (game.State == RoundState.Shop || game.State == RoundState.LoadoutManage)
                ShuffleBlocks(game.CurrentLaunchPoint);
        }

        Color GetColor(BoardBlockType type)
        {
            return type switch
            {
                BoardBlockType.AttackAdd => game.config.board.attackAddColor,
                BoardBlockType.AttackMultiply => game.config.board.attackMultiplyColor,
                _ => game.config.board.shieldColor
            };
        }

        SocketTargetMask GetMaskForBlock(BoardBlockType type)
        {
            return type switch
            {
                BoardBlockType.AttackAdd => SocketTargetMask.Attack,
                BoardBlockType.AttackMultiply => SocketTargetMask.Multiplier,
                BoardBlockType.Shield => SocketTargetMask.Shield,
                _ => SocketTargetMask.Hybrid
            };
        }

        static void Shuffle<T>(IList<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var swapIndex = Random.Range(0, i + 1);
                (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
            }
        }

        float GetRandomRotation()
        {
            var min = game.config.board.minRotationAngle;
            var max = game.config.board.maxRotationAngle;
            var step = Mathf.Max(0.001f, game.config.board.rotationStep);
            if (Mathf.Approximately(min, max))
                return min;

            var randomAngle = Random.Range(min, max);
            return Mathf.Round(randomAngle / step) * step;
        }
    }
}
