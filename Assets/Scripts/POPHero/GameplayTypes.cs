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

    public enum RoundState
    {
        Aim,
        BallFlying,
        RoundResolve,
        BlockRewardChoose,
        RewardChoose,
        Shop,
        LoadoutManage,
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
        Sticker,
        Mod,
        Growth
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
                failReason = "方块实例无效。";
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

            failReason = "上阵区和仓库区都已满。";
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
    }
}
