using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace POPHero
{
    public static class ConfigTableCsvRuntimeLoader
    {
        static readonly string[] RequiredTables =
        {
            "globalConfig.csv",
            "blockType.csv",
            "blockRarity.csv",
            "blockRewardStage.csv",
            "enemy.csv",
            "sticker.csv",
            "stickerToken.csv",
            "mod.csv",
            "growthReward.csv",
            "shop.csv",
            "blockOperation.csv",
            "mapConfig.csv"
        };

        public static bool TryLoadFromProjectCsv(out PopHeroTableConfig tableConfig, out string sourceFolder, out string error)
        {
            tableConfig = null;
            sourceFolder = Path.Combine(Application.dataPath, "ConfigTables");
            error = string.Empty;

            if (!Directory.Exists(sourceFolder))
            {
                error = $"CSV folder not found: {sourceFolder}";
                return false;
            }

            try
            {
                var tables = LoadTables(sourceFolder);
                var missing = RequiredTables.Where(required => !tables.ContainsKey(required)).ToArray();
                if (missing.Length > 0)
                {
                    error = $"Missing CSV tables: {string.Join(", ", missing)}";
                    return false;
                }

                tableConfig = ScriptableObject.CreateInstance<PopHeroTableConfig>();
                tableConfig.name = "POPHeroTableConfig (Runtime CSV)";
                tableConfig.hideFlags = HideFlags.DontSave;
                FillTableConfig(tableConfig, tables);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                if (tableConfig != null)
                    UnityEngine.Object.Destroy(tableConfig);
                tableConfig = null;
                return false;
            }
        }

        static Dictionary<string, ConfigCsvTable> LoadTables(string sourceFolder)
        {
            var tables = new Dictionary<string, ConfigCsvTable>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in Directory.GetFiles(sourceFolder, "*.csv"))
            {
                var table = ConfigCsvTable.Load(path);
                tables[table.Name] = table;
            }

            return tables;
        }

        static void FillTableConfig(PopHeroTableConfig asset, IReadOnlyDictionary<string, ConfigCsvTable> tables)
        {
            asset.globalConfig.Clear();
            asset.blockTypes.Clear();
            asset.blockRarities.Clear();
            asset.blockRewardStages.Clear();
            asset.enemies.Clear();
            asset.stickers.Clear();
            asset.stickerTokens.Clear();
            asset.mods.Clear();
            asset.growthRewards.Clear();
            asset.shopSlots.Clear();
            asset.blockOperationProfiles.Clear();
            asset.mapConfigs.Clear();
            asset.balls.Clear();

            foreach (var row in GetRows(tables, "globalConfig.csv"))
            {
                asset.globalConfig.Add(new TableGlobalConfigEntry
                {
                    key = row.Get("key"),
                    value = row.Get("value"),
                    type = row.Get("type"),
                    description = row.Get("des")
                });
            }

            foreach (var row in GetRows(tables, "blockType.csv"))
            {
                asset.blockTypes.Add(new BlockTypeDef
                {
                    id = row.Get("id"),
                    blockTypeKey = row.Get("blockTypeKey"),
                    blockType = ParseEnum(row.Get("blockTypeKey"), BoardBlockType.AttackAdd),
                    nameCN = row.Get("nameCN"),
                    family = ParseEnum(row.Get("familyKey"), BlockFamily.Strike),
                    description = row.Get("des")
                });
            }

            foreach (var row in GetRows(tables, "blockRarity.csv"))
            {
                asset.blockRarities.Add(new BlockRarityDef
                {
                    id = row.Get("id"),
                    blockType = ParseEnum(row.Get("blockTypeKey"), BoardBlockType.AttackAdd),
                    rarity = ParseEnum(row.Get("rarityKey"), BlockRarity.White),
                    rarityName = row.Get("rarityName"),
                    baseValueA = ParseFloat(row.Get("baseValueA")),
                    baseValueB = ParseFloat(row.Get("baseValueB"))
                });
            }

            foreach (var row in GetRows(tables, "blockRewardStage.csv"))
            {
                asset.blockRewardStages.Add(new BlockRewardStageDef
                {
                    id = ParseInt(row.Get("id")),
                    killThreshold = ParseInt(row.Get("killThreshold")),
                    whiteWeight = ParseFloat(row.Get("whiteWeight")),
                    blueWeight = ParseFloat(row.Get("blueWeight")),
                    purpleWeight = ParseFloat(row.Get("purpleWeight")),
                    goldWeight = ParseFloat(row.Get("goldWeight"))
                });
            }

            foreach (var row in GetRows(tables, "enemy.csv"))
            {
                asset.enemies.Add(new EnemyDef
                {
                    id = ParseInt(row.Get("id")),
                    displayName = row.Get("displayName"),
                    maxHp = ParseInt(row.Get("maxHp")),
                    attackDamage = ParseInt(row.Get("attackDamage")),
                    rewardGold = ParseInt(row.Get("rewardGold")),
                    rewardHeal = ParseInt(row.Get("rewardHeal")),
                    behaviorType = ParseEnum(row.Get("behaviorType"), EnemyBehaviorType.MeleeAdvance),
                    initialDistanceSteps = ParseInt(row.Get("initialDistanceSteps"), -1),
                    color = ConfigTableService.ParseColorHex(row.Get("colorHex"), Color.white),
                    spawnWeight = ParseInt(row.Get("spawnWeight"), 100)
                });
            }

            foreach (var row in GetRows(tables, "sticker.csv"))
            {
                ConfigTableService.TryParseSocketMask(row.Get("targetMask"), out var mask);
                asset.stickers.Add(new StickerDef
                {
                    configId = ParseInt(row.Get("configId")),
                    effectKey = row.Get("effectKey"),
                    name = row.Get("name"),
                    description = row.Get("des"),
                    rarity = ParseEnum(row.Get("rarityKey"), StickerRarity.Common),
                    family = ParseEnum(row.Get("familyKey"), StickerFamily.Forge),
                    trigger = ParseEnum(row.Get("triggerKey"), StickerTriggerType.OnBlockHit),
                    targetMask = mask,
                    valueA = ParseFloat(row.Get("valueA")),
                    valueB = ParseFloat(row.Get("valueB")),
                    valueC = ParseFloat(row.Get("valueC")),
                    spawnType = row.Get("spawnType"),
                    reactionType = row.Get("reactionType"),
                    detailA = row.Get("detailA"),
                    detailB = row.Get("detailB")
                });
            }

            foreach (var row in GetRows(tables, "stickerToken.csv"))
            {
                asset.stickerTokens.Add(new StickerTokenDef
                {
                    id = ParseInt(row.Get("id")),
                    tokenKey = row.Get("tokenKey"),
                    nameCN = row.Get("nameCN"),
                    stackable = ParseBool(row.Get("stackable")),
                    maxStack = ParseInt(row.Get("maxStack")),
                    decayType = ParseInt(row.Get("decayType")),
                    effectPerStack = ParseFloat(row.Get("effectPerStack"))
                });
            }

            foreach (var row in GetRows(tables, "mod.csv"))
            {
                asset.mods.Add(new ModDef
                {
                    id = row.Get("id"),
                    name = row.Get("name"),
                    description = row.Get("des"),
                    category = ParseEnum(row.Get("categoryKey"), ModCategory.Information),
                    valueA = ParseFloat(row.Get("valueA")),
                    valueB = ParseFloat(row.Get("valueB")),
                    valueC = ParseFloat(row.Get("valueC")),
                    effectKey = row.Get("effectKey")
                });
            }

            foreach (var row in GetRows(tables, "growthReward.csv"))
            {
                asset.growthRewards.Add(new GrowthRewardDef
                {
                    id = row.Get("id"),
                    name = row.Get("name"),
                    description = row.Get("des"),
                    rewardType = ParseEnum(row.Get("rewardTypeKey"), GrowthRewardType.UnlockSocket),
                    value = ParseInt(row.Get("value")),
                    shopPrice = ParseInt(row.Get("shopPrice")),
                    weight = ParseInt(row.Get("weight"), 100)
                });
            }

            foreach (var row in GetRows(tables, "shop.csv"))
            {
                asset.shopSlots.Add(new ShopSlotDef
                {
                    slotId = row.Get("slotId"),
                    slotKind = ParseEnum(row.Get("slotKind"), ShopSlotKind.Sticker),
                    itemPool = row.Get("itemPool"),
                    count = ParseInt(row.Get("count"), 1),
                    price = ParseInt(row.Get("price")),
                    rarityWeights = ParseRarityWeights(row.Get("rarityWeights")),
                    weight = ParseInt(row.Get("weight"), 100)
                });
            }

            foreach (var row in GetRows(tables, "blockOperation.csv"))
            {
                asset.blockOperationProfiles.Add(new BlockOperationProfileDef
                {
                    id = row.Get("id"),
                    title = row.Get("title"),
                    subtitle = row.Get("subtitle"),
                    hintText = row.Get("hintText"),
                    activeColumnTitle = row.Get("activeColumnTitle"),
                    reserveColumnTitle = row.Get("reserveColumnTitle"),
                    openButtonText = row.Get("openButtonText"),
                    closeButtonText = row.Get("closeButtonText"),
                    allowDelete = ParseBool(row.Get("allowDelete")),
                    deleteCostGold = ParseInt(row.Get("deleteCostGold")),
                    maxDeleteCount = ParseInt(row.Get("maxDeleteCount"), -1),
                    allowSwap = ParseBool(row.Get("allowSwap")),
                    swapCostGold = ParseInt(row.Get("swapCostGold")),
                    maxSwapCount = ParseInt(row.Get("maxSwapCount"), -1)
                });
            }

            foreach (var row in GetRows(tables, "mapConfig.csv"))
            {
                asset.mapConfigs.Add(new MapConfigDef
                {
                    id = row.Get("id"),
                    floorCount = ParseInt(row.Get("floorCount"), 7),
                    minNodesPerFloor = ParseInt(row.Get("minNodesPerFloor"), 2),
                    maxNodesPerFloor = ParseInt(row.Get("maxNodesPerFloor"), 3),
                    extraConnectionChance = ParseFloat(row.Get("extraConnectionChance"), 0.35f),
                    battleWeight = ParseInt(row.Get("battleWeight"), 70),
                    shopWeight = ParseInt(row.Get("shopWeight"), 12),
                    workbenchWeight = ParseInt(row.Get("workbenchWeight"), 8),
                    restWeight = ParseInt(row.Get("restWeight"), 10),
                    eventWeight = ParseInt(row.Get("eventWeight"), 10),
                    bossEnemyIndex = ParseInt(row.Get("bossEnemyIndex"), -1)
                });
            }

            foreach (var row in GetRows(tables, "ball.csv"))
            {
                asset.balls.Add(new BallDefinition
                {
                    id = row.Get("id"),
                    displayName = row.Get("name"),
                    rarity = ParseEnum(row.Get("rarityKey"), BlockRarity.White),
                    description = row.Get("description"),
                    attackMultiplier = ParseFloat(row.Get("attackMultiplier"), 1f),
                    shieldMultiplier = ParseFloat(row.Get("shieldMultiplier"), 1f),
                    multiplierMultiplier = ParseFloat(row.Get("multiplierMultiplier"), 1f),
                    isInitial = ParseBool(row.Get("isInitial")),
                    isBattleReward = ParseBool(row.Get("isBattleReward")),
                    isShop = ParseBool(row.Get("isShop")),
                    specialType = ParseEnum(row.Get("specialType"), BallSpecialType.None),
                    valueA = ParseFloat(row.Get("valueA")),
                    valueB = ParseFloat(row.Get("valueB")),
                    valueC = ParseFloat(row.Get("valueC")),
                    valueD = ParseFloat(row.Get("valueD"))
                });
            }
        }

        static IEnumerable<ConfigCsvRow> GetRows(IReadOnlyDictionary<string, ConfigCsvTable> tables, string tableName)
        {
            return tables.TryGetValue(tableName, out var table) ? table.DataRows : Array.Empty<ConfigCsvRow>();
        }

        static T ParseEnum<T>(string raw, T fallback) where T : struct
        {
            return ConfigTableCsvParsers.ParseEnum(raw, fallback);
        }

        static int ParseInt(string value, int fallback = 0)
        {
            return ConfigTableCsvParsers.ParseInt(value, fallback);
        }

        static bool ParseBool(string value, bool fallback = false)
        {
            return ConfigTableCsvParsers.ParseBool(value, fallback);
        }

        static float ParseFloat(string value, float fallback = 0f)
        {
            return ConfigTableCsvParsers.ParseFloat(value, fallback);
        }

        static RarityWeightSet ParseRarityWeights(string raw)
        {
            return ConfigTableCsvParsers.ParseRarityWeights(raw);
        }
    }
}
