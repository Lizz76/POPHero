using System;
using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    [CreateAssetMenu(fileName = "POPHeroTableConfig", menuName = "POPHero/Table Config")]
    public sealed class PopHeroTableConfig : ScriptableObject
    {
        public List<TableGlobalConfigEntry> globalConfig = new();
        public List<BlockTypeDef> blockTypes = new();
        public List<BlockRarityDef> blockRarities = new();
        public List<BlockRewardStageDef> blockRewardStages = new();
        public List<EnemyDef> enemies = new();
        public List<StickerDef> stickers = new();
        public List<StickerTokenDef> stickerTokens = new();
        public List<ModDef> mods = new();
        public List<GrowthRewardDef> growthRewards = new();
        public List<ShopSlotDef> shopSlots = new();
        public List<BlockOperationProfileDef> blockOperationProfiles = new();

        public bool HasGameplayTables =>
            blockTypes.Count > 0 ||
            blockRarities.Count > 0 ||
            blockRewardStages.Count > 0 ||
            enemies.Count > 0 ||
            stickers.Count > 0 ||
            mods.Count > 0 ||
            growthRewards.Count > 0 ||
            shopSlots.Count > 0 ||
            blockOperationProfiles.Count > 0;
    }

    [Serializable]
    public sealed class TableGlobalConfigEntry
    {
        public string key;
        public string value;
        public string type;
        public string description;
    }

    [Serializable]
    public sealed class BlockTypeDef
    {
        public string id;
        public BoardBlockType blockType;
        public string blockTypeKey;
        public string nameCN;
        public BlockFamily family;
        public string description;
    }

    [Serializable]
    public sealed class BlockRarityDef
    {
        public string id;
        public BoardBlockType blockType;
        public BlockRarity rarity;
        public string rarityName;
        public float baseValueA;
        public float baseValueB;
    }

    [Serializable]
    public sealed class BlockRewardStageDef
    {
        public int id;
        public int killThreshold;
        public float whiteWeight;
        public float blueWeight;
        public float purpleWeight;
        public float goldWeight;
    }

    [Serializable]
    public sealed class EnemyDef
    {
        public int id;
        public string displayName;
        public int maxHp;
        public int attackDamage;
        public int rewardGold;
        public int rewardHeal;
        public int initialDistanceSteps = -1;
        public Color color = Color.white;
        public int spawnWeight = 100;
    }

    [Serializable]
    public sealed class StickerDef
    {
        public int configId;
        public string effectKey;
        public string name;
        public string description;
        public StickerRarity rarity;
        public StickerFamily family;
        public StickerTriggerType trigger;
        public SocketTargetMask targetMask = SocketTargetMask.Any;
        public float valueA;
        public float valueB;
        public float valueC;
        public string spawnType;
        public string reactionType;
        public string detailA;
        public string detailB;
    }

    [Serializable]
    public sealed class StickerTokenDef
    {
        public int id;
        public string tokenKey;
        public string nameCN;
        public bool stackable;
        public int maxStack;
        public int decayType;
        public float effectPerStack;
    }

    [Serializable]
    public sealed class ModDef
    {
        public string id;
        public string name;
        public string description;
        public ModCategory category;
        public float valueA;
        public float valueB;
        public float valueC;
        public string effectKey;
    }

    [Serializable]
    public sealed class GrowthRewardDef
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
    public sealed class ShopSlotDef
    {
        public string slotId;
        public ShopSlotKind slotKind;
        public string itemPool;
        public int count = 1;
        public int price;
        public RarityWeightSet rarityWeights = new();
        public int weight = 100;
    }

    [Serializable]
    public sealed class BlockOperationProfileDef
    {
        public string id;
        public string title;
        public string subtitle;
        public string hintText;
        public string activeColumnTitle;
        public string reserveColumnTitle;
        public string openButtonText;
        public string closeButtonText;
        public bool allowDelete;
        public int deleteCostGold;
        public int maxDeleteCount = -1;
        public bool allowSwap;
        public int swapCostGold;
        public int maxSwapCount = -1;
    }

    [Serializable]
    public sealed class RarityWeightSet
    {
        public float white;
        public float blue;
        public float purple;
        public float gold;

        public bool HasAnyWeight => white > 0f || blue > 0f || purple > 0f || gold > 0f;

        public BlockRarity Roll()
        {
            var total = Mathf.Max(0f, white) + Mathf.Max(0f, blue) + Mathf.Max(0f, purple) + Mathf.Max(0f, gold);
            if (total <= 0f)
                return BlockRarity.White;

            var roll = UnityEngine.Random.Range(0f, total);
            if (roll < white)
                return BlockRarity.White;
            roll -= white;
            if (roll < blue)
                return BlockRarity.Blue;
            roll -= blue;
            if (roll < purple)
                return BlockRarity.Purple;
            return BlockRarity.Gold;
        }
    }
}
