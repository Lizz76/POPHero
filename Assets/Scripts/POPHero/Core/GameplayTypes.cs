using System;
using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public class WallAimPoint
    {
        public string id;
        public Vector2 position;
        public ArenaSurfaceType wallSide;
        public Vector2 normal;
        public int priority;
    }

    public struct WallHitMemory
    {
        public bool hasValue;
        public ArenaSurfaceType surfaceType;
        public Vector2 hitPoint;
        public Vector2 hitNormal;
        public Collider2D collider;

        public void Set(ArenaSurfaceType surface, Vector2 point, Vector2 normal, Collider2D hitCollider)
        {
            hasValue = true;
            surfaceType = surface;
            hitPoint = point;
            hitNormal = normal;
            collider = hitCollider;
        }

        public void Clear()
        {
            hasValue = false;
            surfaceType = ArenaSurfaceType.Block;
            hitPoint = Vector2.zero;
            hitNormal = Vector2.zero;
            collider = null;
        }
    }

    public struct CornerBounceResult
    {
        public Vector2 safePoint;
        public Vector2 combinedNormal;
        public Collider2D ignoredColliderA;
        public Collider2D ignoredColliderB;
    }

    public struct EmbeddedRecoveryResult
    {
        public Vector2 safePoint;
        public Vector2 recoveryNormal;
        public Collider2D ignoredCollider;
    }

    public enum RoundState
    {
        Map,
        Aim,
        BallFlying,
        RoundResolve,
        BallRewardChoose,
        BlockRewardChoose,
        RewardChoose,
        Shop,
        BlockOperations,
        LoadoutManage,
        MapEvent,
        GameOver
    }

    public enum BoardBlockType
    {
        AttackAdd,
        AttackMultiply,
        Shield,
        Hybrid
    }

    public enum BlockRarity
    {
        White,
        Blue,
        Purple,
        Gold
    }

    public enum BallSpecialType
    {
        None,
        Lightning,
        Burst,
        Pierce
    }

    public enum InputAimMode
    {
        MobileDragConfirm,
        PCMouseAimClick
    }

    public enum BlockVisualState
    {
        Default,
        Highlight,
        Dim
    }

    public enum BlockFamily
    {
        Strike,
        Guard,
        Prism,
        Hybrid
    }

    [Flags]
    public enum SocketTargetMask
    {
        None = 0,
        Attack = 1 << 0,
        Shield = 1 << 1,
        Multiplier = 1 << 2,
        Hybrid = 1 << 3,
        Any = Attack | Shield | Multiplier | Hybrid
    }

    public enum StickerRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic
    }

    public enum StickerFamily
    {
        Forge,
        Ward,
        Prism,
        Chain,
        Ember,
        Spark,
        Thorn,
        Echo,
        Frost,
        Alloy
    }

    [Flags]
    public enum StickerTag
    {
        None = 0,
        Attack = 1 << 0,
        Shield = 1 << 1,
        Multiplier = 1 << 2,
        Token = 1 << 3,
        Chain = 1 << 4,
        Convert = 1 << 5,
        Bonus = 1 << 6
    }

    public enum StickerTriggerType
    {
        OnRoundStart,
        OnBlockHit,
        OnAttackBlockHit,
        OnShieldBlockHit,
        OnMultiplierBlockHit,
        OnRoundEnd,
        OnEnemyDamaged,
        OnEnemyKilled,
        OnBoardRefreshed
    }

    public enum StickerEffectType
    {
        Scripted
    }

    public enum ModCategory
    {
        Information,
        Economy,
        Operation,
        Growth,
        Build
    }

    public enum ShopItemKind
    {
        Sticker = 0,
        Mod = 1,
        Growth = 2,
        Block = 3
    }

    public enum ShopSlotKind
    {
        Sticker = 0,
        Mod = 1,
        Growth = 2,
        RemoveBlock = 3,
        Reroll = 4,
        Block = 5
    }

    public enum GrowthRewardType
    {
        UnlockSocket,
        IncreaseInventoryCapacity,
        IncreaseLaunchCapacity
    }

    public enum ShopEventState
    {
        Hidden,
        ShopWillAppear,
        ShopItemsGenerated,
        TryToSpendMoney,
        ShopBuySuccess,
        ShopNoMoney,
        ShopShuffle,
        ShopDisappear
    }

    public enum MapNodeKind
    {
        Battle,
        Shop,
        Workbench,
        Rest,
        Event,
        Boss
    }

    public enum MapEventActionType
    {
        GainGold,
        TakeDamageUnlockSocket,
        OpenWorkbench,
        Heal
    }

    public enum MapNodeStatus
    {
        Locked,
        Available,
        Current,
        Completed
    }

    [Serializable]
    public class SocketSlotState
    {
        public int index;
        public bool isUnlocked;
        public SocketTargetMask targetMask = SocketTargetMask.Any;
        public StickerInstance installedSticker;

        public bool HasSticker => installedSticker != null;
    }

    [Serializable]
    public class BlockCardState
    {
        public string id;
        public BoardBlockType baseBlockType;
        public BlockRarity rarity;
        public BlockFamily family;
        public readonly List<string> tags = new();
        public float baseValueA;
        public float baseValueB;
        public string cardName;
        public string mainActionText;
        public readonly List<string> detailLines = new();
        public readonly List<SocketSlotState> sockets = new();
        public int templateOrder;

        public int UnlockedSocketCount
        {
            get
            {
                var count = 0;
                foreach (var socket in sockets)
                {
                    if (socket.isUnlocked)
                        count += 1;
                }

                return count;
            }
        }

        public int InstalledStickerCount
        {
            get
            {
                var count = 0;
                foreach (var socket in sockets)
                {
                    if (socket.installedSticker != null)
                        count += 1;
                }

                return count;
            }
        }
    }

    [Serializable]
    public class PlayerBlockCollection
    {
        public readonly List<BlockCardState> activeBlocks = new();
        public readonly List<BlockCardState> reserveBlocks = new();

        public int ActiveCount => activeBlocks.Count;
        public int ReserveCount => reserveBlocks.Count;

        public void Clear()
        {
            activeBlocks.Clear();
            reserveBlocks.Clear();
        }

        public int GetTotalBlockCount()
        {
            return activeBlocks.Count + reserveBlocks.Count;
        }

        public bool CanAddToActive(int maxActiveBlocks)
        {
            return activeBlocks.Count < Mathf.Max(1, maxActiveBlocks);
        }

        public bool CanAddToReserve(int maxReserveBlocks)
        {
            return reserveBlocks.Count < Mathf.Max(0, maxReserveBlocks);
        }

        public bool IsActiveFull(int maxActiveBlocks)
        {
            return !CanAddToActive(maxActiveBlocks);
        }

        public bool IsReserveFull(int maxReserveBlocks)
        {
            return !CanAddToReserve(maxReserveBlocks);
        }

        public BlockCardState FindCard(string cardId)
        {
            var active = activeBlocks.Find(card => card.id == cardId);
            return active ?? reserveBlocks.Find(card => card.id == cardId);
        }

        public bool TryAddCard(BlockCardState card, int maxActiveBlocks, int maxReserveBlocks, out bool addedToReserve, out string failReason)
        {
            addedToReserve = false;
            failReason = string.Empty;
            if (card == null)
            {
                failReason = "Invalid block instance.";
                return false;
            }

            if (CanAddToActive(maxActiveBlocks))
            {
                activeBlocks.Add(card);
                return true;
            }

            if (CanAddToReserve(maxReserveBlocks))
            {
                reserveBlocks.Add(card);
                addedToReserve = true;
                return true;
            }

            failReason = "Active and reserve are both full.";
            return false;
        }

        public bool TryRemoveCard(string cardId, out BlockCardState removedCard, out bool removedFromActive)
        {
            removedCard = activeBlocks.Find(card => card.id == cardId);
            if (removedCard != null)
            {
                removedFromActive = true;
                activeBlocks.Remove(removedCard);
                return true;
            }

            removedCard = reserveBlocks.Find(card => card.id == cardId);
            if (removedCard != null)
            {
                removedFromActive = false;
                reserveBlocks.Remove(removedCard);
                return true;
            }

            removedFromActive = false;
            return false;
        }

        public bool TryReplaceCard(string cardId, BlockCardState replacement, out BlockCardState replacedCard, out bool replacedActive)
        {
            replacedCard = null;
            replacedActive = false;
            if (replacement == null)
                return false;

            var activeIndex = activeBlocks.FindIndex(card => card.id == cardId);
            if (activeIndex >= 0)
            {
                replacedCard = activeBlocks[activeIndex];
                activeBlocks[activeIndex] = replacement;
                replacedActive = true;
                return true;
            }

            var reserveIndex = reserveBlocks.FindIndex(card => card.id == cardId);
            if (reserveIndex >= 0)
            {
                replacedCard = reserveBlocks[reserveIndex];
                reserveBlocks[reserveIndex] = replacement;
                return true;
            }

            return false;
        }

        public bool SwapActiveAndReserve(string activeCardId, string reserveCardId)
        {
            var activeIndex = activeBlocks.FindIndex(card => card.id == activeCardId);
            var reserveIndex = reserveBlocks.FindIndex(card => card.id == reserveCardId);
            if (activeIndex < 0 || reserveIndex < 0)
                return false;

            (activeBlocks[activeIndex], reserveBlocks[reserveIndex]) = (reserveBlocks[reserveIndex], activeBlocks[activeIndex]);
            return true;
        }

        public bool EnsureAtLeastOneActive()
        {
            if (activeBlocks.Count > 0 || reserveBlocks.Count == 0)
                return false;

            activeBlocks.Add(reserveBlocks[0]);
            reserveBlocks.RemoveAt(0);
            return true;
        }
    }

    [Serializable]
    public class BlockRewardOption
    {
        public string id;
        public BoardBlockType blockType;
        public BlockRarity rarity;
        public float baseValue;
        public string displayName;
        public string desc;
        public Color color;
        public BlockFamily family;
    }

    [Serializable]
    public class GrowthRewardData
    {
        public string id;
        public string name;
        public string description;
        public GrowthRewardType rewardType;
        public int value;
        public int shopPrice;
        public int weight = 100;
    }

    [Serializable]
    public class RewardChoiceEntry
    {
        public string id;
        public string title;
        public string description;
        public ShopItemKind kind;
        public StickerData stickerData;
        public ModData modData;
        public GrowthRewardData growthData;
        public BlockRewardOption blockReward;
    }

    [Serializable]
    public class ShopItemEntry
    {
        public string id;
        public ShopItemKind kind;
        public string title;
        public string description;
        public int price;
        public bool purchased;
        public StickerData stickerData;
        public ModData modData;
        public GrowthRewardData growthData;
        public BlockRewardOption blockReward;
    }

    [Serializable]
    public sealed class BlockOperationSessionState
    {
        public string profileId;
        public RoundState returnState = RoundState.Shop;
        public int deleteUsedCount;
        public int swapUsedCount;
        public int upgradeUsedCount;
        public string lastFeedback;

        public bool IsOpen => !string.IsNullOrWhiteSpace(profileId);

        public void Reset(string nextProfileId, RoundState nextReturnState)
        {
            profileId = nextProfileId;
            returnState = nextReturnState;
            deleteUsedCount = 0;
            swapUsedCount = 0;
            upgradeUsedCount = 0;
            lastFeedback = string.Empty;
        }

        public void Clear()
        {
            profileId = string.Empty;
            returnState = RoundState.Shop;
            deleteUsedCount = 0;
            swapUsedCount = 0;
            upgradeUsedCount = 0;
            lastFeedback = string.Empty;
        }
    }

    [Serializable]
    public sealed class MapNodeState
    {
        public string id;
        public int floor;
        public MapNodeKind kind;
        public MapNodeStatus status;
        public Vector2 normalizedPosition;
        public int enemyIndex;
        public string encounterId;
        public List<string> nextNodeIds = new();

        public bool IsSelectable => status == MapNodeStatus.Available;
    }

    [Serializable]
    public sealed class MapEventChoiceState
    {
        public int index;
        public MapEventActionType actionType;
        public string title;
        public string description;
        public string buttonText;
        public int intValue;
        public float healPercent;
        public string profileId;
    }

    [Serializable]
    public class BallDefinition
    {
        public string id;
        public string displayName;
        public BlockRarity rarity;
        public string description;
        public float attackMultiplier = 1f;
        public float shieldMultiplier = 1f;
        public float multiplierMultiplier = 1f;
        public bool isInitial;
        public bool isBattleReward;
        public bool isShop;
        public BallSpecialType specialType;
        public float valueA;
        public float valueB;
        public float valueC;
        public float valueD;

        public string ShortName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(displayName))
                    return string.IsNullOrWhiteSpace(id) ? "BALL" : id;
                return displayName.Length <= 2 ? displayName : displayName.Substring(0, 2);
            }
        }
    }

    [Serializable]
    public sealed class PlayerBallInstance
    {
        public string runtimeId;
        public string definitionId;
        public BallDefinition definition;
    }

    [Serializable]
    public sealed class PlayerBallCollection
    {
        public readonly List<PlayerBallInstance> balls = new();

        public IReadOnlyList<PlayerBallInstance> Balls => balls;
        public int Count => balls.Count;

        public void Clear()
        {
            balls.Clear();
        }

        public PlayerBallInstance Add(BallDefinition definition, int serial)
        {
            if (definition == null)
                return null;

            var instance = new PlayerBallInstance
            {
                runtimeId = $"ball_{serial:000}",
                definitionId = definition.id,
                definition = definition
            };
            balls.Add(instance);
            return instance;
        }
    }

    [Serializable]
    public sealed class BallBagState
    {
        public readonly List<PlayerBallInstance> drawPile = new();
        public readonly List<PlayerBallInstance> usedPile = new();
        public PlayerBallInstance currentBall;
        public PlayerBallInstance activeRoundBall;
        public int discardsRemaining;

        public void Clear()
        {
            drawPile.Clear();
            usedPile.Clear();
            currentBall = null;
            activeRoundBall = null;
            discardsRemaining = 0;
        }
    }

    [Serializable]
    public sealed class BallRewardOption
    {
        public int index;
        public BallDefinition definition;
        public string displayName;
        public string description;
        public string rarityText;
        public Color color;
    }
}
