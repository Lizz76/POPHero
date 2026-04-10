using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public class StickerData
    {
        public string id;
        public string name;
        public string shortTitle;
        public string mainActionText;
        public readonly List<string> detailLines = new();
        public StickerRarity rarity;
        public SocketTargetMask targetBlockType = SocketTargetMask.Any;
        public StickerFamily family;
        public readonly List<StickerTag> tags = new();
        public StickerTriggerType triggerType;
        public StickerEffectType effectType = StickerEffectType.Scripted;
        public string spawnType;
        public string reactionType;
        public float valueA;
        public float valueB;
        public float valueC;
    }

    public class StickerInstance
    {
        public string runtimeId;
        public StickerData data;

        public string DisplayName => data?.name ?? "Sticker";
    }

    public class StickerInventory
    {
        readonly List<StickerInstance> stored = new();

        PopHeroGame game;
        StickerInstance draggingSticker;

        public IReadOnlyList<StickerInstance> Stored => stored;
        public StickerInstance DraggingSticker => draggingSticker;

        public void Initialize(PopHeroGame owner)
        {
            game = owner;
            stored.Clear();
            draggingSticker = null;
        }

        public int Capacity => game == null ? 0 : game.Player.StickerInventoryCapacity + game.ModManager.GetInventoryCapacityBonus();

        public bool TryAdd(StickerInstance instance)
        {
            if (instance == null || stored.Count >= Capacity)
                return false;

            stored.Add(instance);
            return true;
        }

        public bool BeginDrag(string runtimeId)
        {
            draggingSticker = stored.Find(sticker => sticker.runtimeId == runtimeId);
            return draggingSticker != null;
        }

        public StickerInstance TakeDraggingSticker()
        {
            if (draggingSticker == null)
                return null;

            stored.Remove(draggingSticker);
            var sticker = draggingSticker;
            draggingSticker = null;
            return sticker;
        }

        public void ReturnToInventory(StickerInstance instance)
        {
            if (instance != null && !stored.Contains(instance))
                stored.Add(instance);
        }

        public void CancelDrag()
        {
            draggingSticker = null;
        }

        public void RemoveById(string runtimeId)
        {
            if (string.IsNullOrWhiteSpace(runtimeId))
                return;

            stored.RemoveAll(sticker => sticker.runtimeId == runtimeId);
            if (draggingSticker != null && draggingSticker.runtimeId == runtimeId)
                draggingSticker = null;
        }
    }

    public class RoundStickerState
    {
        public readonly Dictionary<string, int> tokens = new();
        public readonly HashSet<string> roundTags = new();
        public readonly Dictionary<string, int> blockHitCounts = new();
        public readonly HashSet<BlockFamily> uniqueFamilies = new();
        public readonly HashSet<string> oncePerRound = new();
        public int chainLength;
        public BlockFamily? lastFamily;
        public int enemyCounterReduction;

        public void Reset()
        {
            tokens.Clear();
            roundTags.Clear();
            blockHitCounts.Clear();
            uniqueFamilies.Clear();
            oncePerRound.Clear();
            chainLength = 0;
            lastFamily = null;
            enemyCounterReduction = 0;
        }
    }
}
