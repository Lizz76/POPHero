using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace POPHero
{
    public sealed class ConfigTableService
    {
        readonly PopHeroTableConfig tableConfig;
        readonly PopHeroPrototypeConfig legacyConfig;
        readonly Dictionary<string, string> globals = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<BoardBlockType, BlockTypeDef> blockTypes = new();
        readonly Dictionary<string, BlockRarityDef> blockRarities = new();
        readonly Dictionary<string, StickerDef> stickersByEffectKey = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, ModDef> modsByEffectKey = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, BlockOperationProfileDef> blockOperationProfilesById = new(StringComparer.OrdinalIgnoreCase);

        public ConfigTableService(PopHeroTableConfig tables, PopHeroPrototypeConfig legacy)
        {
            tableConfig = tables;
            legacyConfig = legacy;
            RebuildIndexes();
        }

        public PopHeroTableConfig Raw => tableConfig;
        public bool HasTables => tableConfig != null && tableConfig.HasGameplayTables;
        public IReadOnlyList<StickerDef> StickerDefs => tableConfig != null ? tableConfig.stickers : Array.Empty<StickerDef>();
        public IReadOnlyList<ModDef> ModDefs => tableConfig != null ? tableConfig.mods : Array.Empty<ModDef>();
        public IReadOnlyList<GrowthRewardDef> GrowthRewardDefs => tableConfig != null ? tableConfig.growthRewards : Array.Empty<GrowthRewardDef>();
        public IReadOnlyList<ShopSlotDef> ShopSlots => tableConfig != null ? tableConfig.shopSlots : Array.Empty<ShopSlotDef>();
        public IReadOnlyList<EnemyDef> EnemyDefs => tableConfig != null ? tableConfig.enemies : Array.Empty<EnemyDef>();
        public IReadOnlyList<BlockOperationProfileDef> BlockOperationProfiles => tableConfig != null ? tableConfig.blockOperationProfiles : Array.Empty<BlockOperationProfileDef>();

        void RebuildIndexes()
        {
            globals.Clear();
            blockTypes.Clear();
            blockRarities.Clear();
            stickersByEffectKey.Clear();
            modsByEffectKey.Clear();
            blockOperationProfilesById.Clear();

            if (tableConfig == null)
                return;

            foreach (var entry in tableConfig.globalConfig)
            {
                if (!string.IsNullOrWhiteSpace(entry.key))
                    globals[entry.key.Trim()] = entry.value;
            }

            foreach (var blockType in tableConfig.blockTypes)
                blockTypes[blockType.blockType] = blockType;

            foreach (var rarity in tableConfig.blockRarities)
                blockRarities[BuildBlockRarityKey(rarity.blockType, rarity.rarity)] = rarity;

            foreach (var sticker in tableConfig.stickers)
            {
                if (!string.IsNullOrWhiteSpace(sticker.effectKey))
                    stickersByEffectKey[sticker.effectKey.Trim()] = sticker;
            }

            foreach (var mod in tableConfig.mods)
            {
                var key = string.IsNullOrWhiteSpace(mod.effectKey) ? mod.id : mod.effectKey;
                if (!string.IsNullOrWhiteSpace(key))
                    modsByEffectKey[key.Trim()] = mod;
            }

            foreach (var profile in tableConfig.blockOperationProfiles)
            {
                if (!string.IsNullOrWhiteSpace(profile.id))
                    blockOperationProfilesById[profile.id.Trim()] = profile;
            }
        }

        public void ApplyToPrototypeConfig(PopHeroPrototypeConfig config)
        {
            if (config == null || tableConfig == null)
                return;

            config.player.maxHp = GetInt("playerMaxHp", config.player.maxHp);
            config.player.currentHp = GetInt("playerStartHp", config.player.currentHp);
            config.player.startShield = GetInt("playerStartShield", config.player.startShield);
            config.player.startGold = GetInt("playerStartGold", config.player.startGold);

            config.ball.radius = GetFloat("ballRadius", config.ball.radius);
            config.ball.speed = GetFloat("ballSpeed", config.ball.speed);
            config.ball.accelerationPerBounce = GetFloat("ballAccelPerBounce", config.ball.accelerationPerBounce);
            config.ball.maxSpeed = GetFloat("ballMaxSpeed", config.ball.maxSpeed);
            config.ball.maxFlightDuration = GetFloat("ballMaxFlightDuration", config.ball.maxFlightDuration);
            config.ball.maxCollisionStepsPerFixedUpdate = GetInt("ballMaxCollisionStepsPerFixedUpdate", config.ball.maxCollisionStepsPerFixedUpdate);

            config.blockRewards.maxActiveBlocks = GetInt("maxActiveBlocks", config.blockRewards.maxActiveBlocks);
            config.blockRewards.maxReserveBlocks = GetInt("maxReserveBlocks", config.blockRewards.maxReserveBlocks);
            config.blockRewards.initialChoiceCount = GetInt("initialBlockChoiceCount", config.blockRewards.initialChoiceCount);
            config.blockRewards.rewardChoiceCount = GetInt("blockRewardChoiceCount", config.blockRewards.rewardChoiceCount);

            config.intermission.rewardChoiceCount = GetInt("rewardChoiceCount", config.intermission.rewardChoiceCount);
            config.stickers.baseInventoryCapacity = GetInt("stickerInventoryCapacity", config.stickers.baseInventoryCapacity);
            config.stickers.defaultSocketsPerCard = GetInt("defaultSocketsPerCard", config.stickers.defaultSocketsPerCard);
            config.stickers.maxSocketsPerCard = GetInt("maxSocketsPerCard", config.stickers.maxSocketsPerCard);
            config.mods.modUseCap = GetInt("modUseCap", config.mods.modUseCap);

            config.shop.stickerSkipMoney = GetInt("stickerSkipMoney", config.shop.stickerSkipMoney);
            config.shop.stickerRerollMoney = GetInt("stickerRerollMoney", config.shop.stickerRerollMoney);
            config.shop.shopRerollMoney = GetInt("shopRerollMoney", config.shop.shopRerollMoney);
            config.shop.blockRemovalCost = GetInt("blockRemovalCost", config.shop.blockRemovalCost);
            config.shop.blockOperationProfileId = GetString("shopBlockOperationProfileId", config.shop.blockOperationProfileId);

            config.intermission.shopStickerSlots = GetInt("shopStickerSlots", config.intermission.shopStickerSlots);
            config.intermission.shopModSlots = GetInt("shopModSlots", config.intermission.shopModSlots);
            config.intermission.shopGrowthSlots = GetInt("shopGrowthSlots", config.intermission.shopGrowthSlots);

            config.enemies.maxLaunchesPerEnemy = GetInt("maxLaunchesPerEnemy", config.enemies.maxLaunchesPerEnemy);
            config.enemies.endlessHpGrowth = GetInt("endlessHpGrowth", config.enemies.endlessHpGrowth);
            config.enemies.endlessGoldGrowth = GetInt("endlessGoldGrowth", config.enemies.endlessGoldGrowth);
            config.enemies.endlessHealGrowth = GetInt("endlessHealGrowth", config.enemies.endlessHealGrowth);
            config.enemies.endlessAttackGrowth = GetInt("endlessAttackGrowth", config.enemies.endlessAttackGrowth);

            config.board.attackAddCount = GetInt("attackAddCount", config.board.attackAddCount);
            config.board.attackMultiplyCount = GetInt("attackMultiplyCount", config.board.attackMultiplyCount);
            config.board.shieldCount = GetInt("shieldCount", config.board.shieldCount);
            config.board.blockSize = new Vector2(
                GetFloat("boardBlockWidth", config.board.blockSize.x),
                GetFloat("boardBlockHeight", config.board.blockSize.y));

            ApplyBlockRewardStages(config);
            ApplyBlockRewardValues(config);
            ApplyEnemies(config);
        }

        void ApplyBlockRewardStages(PopHeroPrototypeConfig config)
        {
            if (tableConfig.blockRewardStages.Count == 0)
                return;

            config.blockRewards.rarityOdds.Clear();
            foreach (var stage in tableConfig.blockRewardStages)
            {
                config.blockRewards.rarityOdds.Add(new RarityOddsStage
                {
                    minimumKills = stage.killThreshold,
                    white = stage.whiteWeight,
                    blue = stage.blueWeight,
                    purple = stage.purpleWeight,
                    gold = stage.goldWeight
                });
            }
        }

        void ApplyBlockRewardValues(PopHeroPrototypeConfig config)
        {
            SetRarityValueTable(config.blockRewards.attackValues, BoardBlockType.AttackAdd);
            SetRarityValueTable(config.blockRewards.shieldValues, BoardBlockType.Shield);
            SetRarityValueTable(config.blockRewards.multiplierValues, BoardBlockType.AttackMultiply);
        }

        void SetRarityValueTable(RarityValueTable valueTable, BoardBlockType blockType)
        {
            if (valueTable == null)
                return;

            if (TryGetBlockRarity(blockType, BlockRarity.White, out var white))
                valueTable.white = white.baseValueA;
            if (TryGetBlockRarity(blockType, BlockRarity.Blue, out var blue))
                valueTable.blue = blue.baseValueA;
            if (TryGetBlockRarity(blockType, BlockRarity.Purple, out var purple))
                valueTable.purple = purple.baseValueA;
            if (TryGetBlockRarity(blockType, BlockRarity.Gold, out var gold))
                valueTable.gold = gold.baseValueA;
        }

        void ApplyEnemies(PopHeroPrototypeConfig config)
        {
            if (tableConfig.enemies.Count == 0)
                return;

            config.enemies.templates.Clear();
            foreach (var enemy in tableConfig.enemies)
            {
                config.enemies.templates.Add(new EnemyTemplate
                {
                    displayName = enemy.displayName,
                    maxHp = enemy.maxHp,
                    attackDamage = enemy.attackDamage,
                    rewardGold = enemy.rewardGold,
                    rewardHeal = enemy.rewardHeal,
                    color = enemy.color
                });
            }
        }

        public int GetInt(string key, int fallback)
        {
            return globals.TryGetValue(key, out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
        }

        public float GetFloat(string key, float fallback)
        {
            return globals.TryGetValue(key, out var value) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
        }

        public string GetString(string key, string fallback = "")
        {
            return globals.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value.Trim()
                : fallback;
        }

        public bool TryGetBlockType(BoardBlockType type, out BlockTypeDef definition)
        {
            return blockTypes.TryGetValue(type, out definition);
        }

        public bool TryGetBlockRarity(BoardBlockType type, BlockRarity rarity, out BlockRarityDef definition)
        {
            return blockRarities.TryGetValue(BuildBlockRarityKey(type, rarity), out definition);
        }

        public BlockRarity RollRarity(int defeatedEnemies)
        {
            if (tableConfig == null || tableConfig.blockRewardStages.Count == 0)
                return RollLegacyRarity(defeatedEnemies);

            var selected = tableConfig.blockRewardStages[0];
            foreach (var stage in tableConfig.blockRewardStages)
            {
                if (defeatedEnemies >= stage.killThreshold)
                    selected = stage;
            }

            return new RarityWeightSet
            {
                white = selected.whiteWeight,
                blue = selected.blueWeight,
                purple = selected.purpleWeight,
                gold = selected.goldWeight
            }.Roll();
        }

        BlockRarity RollLegacyRarity(int defeatedEnemies)
        {
            var stages = legacyConfig.blockRewards.rarityOdds;
            var selectedStage = stages[0];
            foreach (var stage in stages)
            {
                if (defeatedEnemies >= stage.minimumKills)
                    selectedStage = stage;
            }

            return new RarityWeightSet
            {
                white = selectedStage.white,
                blue = selectedStage.blue,
                purple = selectedStage.purple,
                gold = selectedStage.gold
            }.Roll();
        }

        public BoardBlockType RollBlockType()
        {
            if (tableConfig == null || tableConfig.blockTypes.Count == 0)
                return UnityEngine.Random.Range(0, 3) switch
                {
                    0 => BoardBlockType.AttackAdd,
                    1 => BoardBlockType.Shield,
                    _ => BoardBlockType.AttackMultiply
                };

            var candidates = tableConfig.blockTypes.FindAll(def => def.blockType != BoardBlockType.Hybrid);
            if (candidates.Count == 0)
                candidates = tableConfig.blockTypes;
            return candidates[UnityEngine.Random.Range(0, candidates.Count)].blockType;
        }

        public StickerDef GetStickerDef(string effectKey)
        {
            return !string.IsNullOrWhiteSpace(effectKey) && stickersByEffectKey.TryGetValue(effectKey, out var sticker) ? sticker : null;
        }

        public ModDef GetModDef(string effectKey)
        {
            return !string.IsNullOrWhiteSpace(effectKey) && modsByEffectKey.TryGetValue(effectKey, out var mod) ? mod : null;
        }

        public bool TryGetBlockOperationProfile(string profileId, out BlockOperationProfileDef definition)
        {
            definition = null;
            return !string.IsNullOrWhiteSpace(profileId) && blockOperationProfilesById.TryGetValue(profileId.Trim(), out definition);
        }

        static string BuildBlockRarityKey(BoardBlockType blockType, BlockRarity rarity)
        {
            return $"{blockType}:{rarity}";
        }

        public static bool TryParseEnumKey<T>(string key, out T value) where T : struct
        {
            value = default;
            return !string.IsNullOrWhiteSpace(key) && Enum.TryParse(key.Trim(), true, out value);
        }

        public static bool TryParseSocketMask(string raw, out SocketTargetMask mask)
        {
            mask = SocketTargetMask.None;
            if (string.IsNullOrWhiteSpace(raw))
            {
                mask = SocketTargetMask.Any;
                return true;
            }

            if (string.Equals(raw.Trim(), "Any", StringComparison.OrdinalIgnoreCase))
            {
                mask = SocketTargetMask.Any;
                return true;
            }

            var parts = raw.Split(new[] { '|', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (!TryParseEnumKey(part, out SocketTargetMask parsed) || parsed == SocketTargetMask.None)
                    return false;
                mask |= parsed;
            }

            return mask != SocketTargetMask.None;
        }

        public static Color ParseColorHex(string raw, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return fallback;

            var normalized = raw.Trim();
            if (!normalized.StartsWith("#", StringComparison.Ordinal))
                normalized = "#" + normalized;
            return ColorUtility.TryParseHtmlString(normalized, out var color) ? color : fallback;
        }
    }
}
