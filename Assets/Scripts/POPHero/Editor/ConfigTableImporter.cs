using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace POPHero
{
    public sealed class ConfigTableImporter : AssetPostprocessor
    {
        const string ConfigFolder = "Assets/ConfigTables";
        const string GlobalConfigFallbackPath = "Assets/globalConfig.csv";
        const string RuntimeAssetPath = "Assets/Resources/POPHeroTableConfig.asset";
        static bool rebuildQueued;

        [MenuItem("POPHero/Config/Rebuild Tables")]
        public static void RebuildTablesMenu()
        {
            RebuildTables(true);
        }

        public static void RebuildTablesCli()
        {
            try
            {
                var result = LoadAndValidateTables();
                LogValidationResult(result, true);
                if (result.HasErrors)
                {
                    EditorApplication.Exit(1);
                    return;
                }

                Directory.CreateDirectory("Assets/Resources");
                var asset = AssetDatabase.LoadAssetAtPath<PopHeroTableConfig>(RuntimeAssetPath);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<PopHeroTableConfig>();
                    AssetDatabase.CreateAsset(asset, RuntimeAssetPath);
                }

                FillAsset(asset, result);
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(RuntimeAssetPath);
                Debug.Log($"[POPHero Config] Rebuilt runtime table asset: {RuntimeAssetPath}");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("POPHero/Config/Validate Tables")]
        public static void ValidateTablesMenu()
        {
            var result = LoadAndValidateTables();
            LogValidationResult(result, true);
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (rebuildQueued)
                return;

            var touchedConfig = importedAssets.Concat(deletedAssets).Concat(movedAssets).Concat(movedFromAssetPaths)
                .Any(path => path.StartsWith(ConfigFolder + "/", StringComparison.OrdinalIgnoreCase) && path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));
            if (!touchedConfig)
                return;

            rebuildQueued = true;
            EditorApplication.delayCall += () =>
            {
                rebuildQueued = false;
                RebuildTables(false);
            };
        }

        static void RebuildTables(bool verbose)
        {
            var result = LoadAndValidateTables();
            LogValidationResult(result, verbose);
            if (result.HasErrors)
                return;

            Directory.CreateDirectory("Assets/Resources");
            var asset = AssetDatabase.LoadAssetAtPath<PopHeroTableConfig>(RuntimeAssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<PopHeroTableConfig>();
                AssetDatabase.CreateAsset(asset, RuntimeAssetPath);
            }

            FillAsset(asset, result);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(RuntimeAssetPath);
            if (verbose)
                Debug.Log($"[POPHero Config] Rebuilt runtime table asset: {RuntimeAssetPath}");
        }

        static TableImportResult LoadAndValidateTables()
        {
            var result = new TableImportResult();
            if (!Directory.Exists(ConfigFolder))
            {
                result.AddError("ConfigTables folder is missing.");
                return result;
            }

            foreach (var path in Directory.GetFiles(ConfigFolder, "*.csv"))
            {
                var table = ConfigCsvTable.Load(path);
                result.Tables[Path.GetFileName(path)] = table;
                ValidateBasicShape(table, result);
            }

            if (!result.Tables.ContainsKey("globalConfig.csv") && File.Exists(GlobalConfigFallbackPath))
            {
                var table = ConfigCsvTable.Load(GlobalConfigFallbackPath);
                result.Tables["globalConfig.csv"] = table;
                ValidateBasicShape(table, result);
            }

            ValidateRequiredTables(result);
            ValidateDomain(result);
            return result;
        }

        static void FillAsset(PopHeroTableConfig asset, TableImportResult result)
        {
            asset.globalConfig.Clear();
            asset.blockTypes.Clear();
            asset.blockRarities.Clear();
            asset.blockRewardStages.Clear();
            asset.enemies.Clear();
            asset.encounters.Clear();
            asset.stickers.Clear();
            asset.stickerTokens.Clear();
            asset.mods.Clear();
            asset.growthRewards.Clear();
            asset.shopSlots.Clear();
            asset.blockOperationProfiles.Clear();
            asset.mapConfigs.Clear();

            foreach (var row in result.GetRows("globalConfig.csv"))
            {
                asset.globalConfig.Add(new TableGlobalConfigEntry
                {
                    key = row.Get("key"),
                    value = row.Get("value"),
                    type = row.Get("type"),
                    description = row.Get("des")
                });
            }

            foreach (var row in result.GetRows("blockType.csv"))
            {
                asset.blockTypes.Add(new BlockTypeDef
                {
                    id = row.Get("id"),
                    blockTypeKey = row.Get("blockTypeKey"),
                    blockType = ParseEnum(row, "blockTypeKey", BoardBlockType.AttackAdd),
                    nameCN = row.Get("nameCN"),
                    family = ParseEnum(row, "familyKey", BlockFamily.Strike),
                    description = row.Get("des")
                });
            }

            foreach (var row in result.GetRows("blockRarity.csv"))
            {
                asset.blockRarities.Add(new BlockRarityDef
                {
                    id = row.Get("id"),
                    blockType = ParseEnum(row, "blockTypeKey", BoardBlockType.AttackAdd),
                    rarity = ParseEnum(row, "rarityKey", BlockRarity.White),
                    rarityName = row.Get("rarityName"),
                    baseValueA = ParseFloat(row.Get("baseValueA")),
                    baseValueB = ParseFloat(row.Get("baseValueB"))
                });
            }

            foreach (var row in result.GetRows("blockRewardStage.csv"))
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

            foreach (var row in result.GetRows("enemy.csv"))
            {
                asset.enemies.Add(new EnemyDef
                {
                    id = ParseInt(row.Get("id")),
                    displayName = row.Get("displayName"),
                    maxHp = ParseInt(row.Get("maxHp")),
                    attackDamage = ParseInt(row.Get("attackDamage")),
                    rewardGold = ParseInt(row.Get("rewardGold")),
                    rewardHeal = ParseInt(row.Get("rewardHeal")),
                    behaviorType = ParseEnum(row, "behaviorType", EnemyBehaviorType.MeleeAdvance),
                    initialDistanceSteps = ParseInt(row.Get("initialDistanceSteps"), -1),
                    color = ConfigTableService.ParseColorHex(row.Get("colorHex"), Color.white),
                    spawnWeight = ParseInt(row.Get("spawnWeight"), 100),
                    prefabKey = row.Get("prefabKey"),
                    abilityIds = ParseTokenList(row.Get("abilityIds"))
                });
            }

            foreach (var row in result.GetRows("encounter.csv"))
            {
                asset.encounters.Add(new EncounterDef
                {
                    encounterId = row.Get("encounterId"),
                    act = ParseInt(row.Get("act"), 1),
                    nodeType = ParseEnum(row, "nodeType", EncounterNodeType.Normal),
                    minFloor = ParseInt(row.Get("minFloor"), 1),
                    maxFloor = ParseInt(row.Get("maxFloor"), 99),
                    weight = ParseInt(row.Get("weight"), 100),
                    allowRepeat = ParseBool(row.Get("allowRepeat")),
                    enemies = ParseEncounterEnemies(row.Get("enemySlots"))
                });
            }

            foreach (var row in result.GetRows("sticker.csv"))
            {
                ConfigTableService.TryParseSocketMask(row.Get("targetMask"), out var mask);
                asset.stickers.Add(new StickerDef
                {
                    configId = ParseInt(row.Get("configId")),
                    effectKey = row.Get("effectKey"),
                    name = row.Get("name"),
                    description = row.Get("des"),
                    rarity = ParseEnum(row, "rarityKey", StickerRarity.Common),
                    family = ParseEnum(row, "familyKey", StickerFamily.Forge),
                    trigger = ParseEnum(row, "triggerKey", StickerTriggerType.OnBlockHit),
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

            foreach (var row in result.GetRows("stickerToken.csv"))
            {
                asset.stickerTokens.Add(new StickerTokenDef
                {
                    id = ParseInt(row.Get("id")),
                    tokenKey = row.Get("tokenKey"),
                    nameCN = row.Get("nameCN"),
                    stackable = ParseInt(row.Get("stackable")) != 0,
                    maxStack = ParseInt(row.Get("maxStack")),
                    decayType = ParseInt(row.Get("decayType")),
                    effectPerStack = ParseFloat(row.Get("effectPerStack"))
                });
            }

            foreach (var row in result.GetRows("mod.csv"))
            {
                asset.mods.Add(new ModDef
                {
                    id = row.Get("id"),
                    name = row.Get("name"),
                    description = row.Get("des"),
                    category = ParseEnum(row, "categoryKey", ModCategory.Information),
                    valueA = ParseFloat(row.Get("valueA")),
                    valueB = ParseFloat(row.Get("valueB")),
                    valueC = ParseFloat(row.Get("valueC")),
                    effectKey = row.Get("effectKey")
                });
            }

            foreach (var row in result.GetRows("growthReward.csv"))
            {
                asset.growthRewards.Add(new GrowthRewardDef
                {
                    id = row.Get("id"),
                    name = row.Get("name"),
                    description = row.Get("des"),
                    rewardType = ParseEnum(row, "rewardTypeKey", GrowthRewardType.UnlockSocket),
                    value = ParseInt(row.Get("value")),
                    shopPrice = ParseInt(row.Get("shopPrice")),
                    weight = ParseInt(row.Get("weight"), 100)
                });
            }

            foreach (var row in result.GetRows("shop.csv"))
            {
                asset.shopSlots.Add(new ShopSlotDef
                {
                    slotId = row.Get("slotId"),
                    slotKind = ParseEnum(row, "slotKind", ShopSlotKind.Sticker),
                    itemPool = row.Get("itemPool"),
                    count = ParseInt(row.Get("count"), 1),
                    price = ParseInt(row.Get("price")),
                    rarityWeights = ParseRarityWeights(row.Get("rarityWeights")),
                    weight = ParseInt(row.Get("weight"), 100)
                });
            }

            foreach (var row in result.GetRows("blockOperation.csv"))
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

            foreach (var row in result.GetRows("mapConfig.csv"))
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
        }

        static void ValidateRequiredTables(TableImportResult result)
        {
            foreach (var required in new[]
                     {
                         "globalConfig.csv", "blockType.csv", "blockRarity.csv", "blockRewardStage.csv",
                        "enemy.csv", "encounter.csv", "sticker.csv", "stickerToken.csv", "mod.csv", "growthReward.csv", "shop.csv", "blockOperation.csv", "mapConfig.csv"
                     })
            {
                if (!result.Tables.ContainsKey(required))
                    result.AddError($"{required} is missing.");
            }
        }

        static void ValidateBasicShape(ConfigCsvTable table, TableImportResult result)
        {
            if (table.Rows.Count < 5)
            {
                result.AddError($"{table.Name}: must contain 5 header rows.");
                return;
            }

            var width = table.Header.Count;
            for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
            {
                if (table.Rows[rowIndex].Count != width)
                    result.AddError($"{table.Name}: row {rowIndex + 1} has {table.Rows[rowIndex].Count} columns, expected {width}.");
            }

            var primaryKeys = new HashSet<string>();
            foreach (var row in table.DataRows)
            {
                var key = row.Values.Count > 0 ? row.Values[0] : string.Empty;
                if (string.IsNullOrWhiteSpace(key))
                    result.AddError($"{table.Name}: row {row.LineNumber} has empty primary key.");
                else if (!primaryKeys.Add(key))
                    result.AddError($"{table.Name}: duplicate primary key `{key}` at row {row.LineNumber}.");
            }
        }

        static void ValidateDomain(TableImportResult result)
        {
            ValidateEnums(result);
            ValidateBlockReferences(result);
            ValidateRewardStages(result);
            ValidateStickerReferences(result);
            ValidateShop(result);
            ValidateBlockOperations(result);
            ValidateEncounterTemplates(result);
            ValidateMapConfig(result);
        }

        static void ValidateEnums(TableImportResult result)
        {
            foreach (var row in result.GetRows("blockType.csv"))
            {
                RequireEnum<BoardBlockType>(result, row, "blockTypeKey");
                RequireEnum<BlockFamily>(result, row, "familyKey");
            }

            foreach (var row in result.GetRows("blockRarity.csv"))
            {
                RequireEnum<BoardBlockType>(result, row, "blockTypeKey");
                RequireEnum<BlockRarity>(result, row, "rarityKey");
            }

            foreach (var row in result.GetRows("sticker.csv"))
            {
                RequireEnum<StickerRarity>(result, row, "rarityKey");
                RequireEnum<StickerFamily>(result, row, "familyKey");
                RequireEnum<StickerTriggerType>(result, row, "triggerKey");
                if (!ConfigTableService.TryParseSocketMask(row.Get("targetMask"), out _))
                    result.AddError($"{row.Table.Name}: row {row.LineNumber} targetMask `{row.Get("targetMask")}` is invalid.");
            }

            foreach (var row in result.GetRows("mod.csv"))
                RequireEnum<ModCategory>(result, row, "categoryKey");

            foreach (var row in result.GetRows("growthReward.csv"))
                RequireEnum<GrowthRewardType>(result, row, "rewardTypeKey");

            foreach (var row in result.GetRows("shop.csv"))
                RequireEnum<ShopSlotKind>(result, row, "slotKind");

            foreach (var row in result.GetRows("encounter.csv"))
                RequireEnum<EncounterNodeType>(result, row, "nodeType");
        }

        static void ValidateBlockReferences(TableImportResult result)
        {
            var blockTypes = result.GetRows("blockType.csv").Select(row => row.Get("blockTypeKey")).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var row in result.GetRows("blockRarity.csv"))
            {
                if (!blockTypes.Contains(row.Get("blockTypeKey")))
                    result.AddError($"{row.Table.Name}: row {row.LineNumber} references missing blockTypeKey `{row.Get("blockTypeKey")}`.");
            }
        }

        static void ValidateRewardStages(TableImportResult result)
        {
            var lastThreshold = -1;
            foreach (var row in result.GetRows("blockRewardStage.csv"))
            {
                var total = ParseFloat(row.Get("whiteWeight")) + ParseFloat(row.Get("blueWeight")) + ParseFloat(row.Get("purpleWeight")) + ParseFloat(row.Get("goldWeight"));
                if (!Mathf.Approximately(total, 100f))
                    result.AddError($"{row.Table.Name}: row {row.LineNumber} rarity weights total {total}, expected 100.");

                var threshold = ParseInt(row.Get("killThreshold"));
                if (threshold <= lastThreshold)
                    result.AddError($"{row.Table.Name}: row {row.LineNumber} killThreshold must be ascending.");
                lastThreshold = threshold;
            }
        }

        static void ValidateStickerReferences(TableImportResult result)
        {
            var knownEffects = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "impact_core", "echo_mark", "shatter_loop", "guard_furnace", "prism_guard",
                "mirror_plating", "amp_seed", "amp_burst", "twin_resonance", "chain_ledger",
                "ember_seed", "ember_catcher", "thorn_rack", "spark_tape", "same_family_latch",
                "breaker_note", "glass_ledger", "frost_trace", "alloy_echo"
            };
            var tokens = result.GetRows("stickerToken.csv").Select(row => row.Get("tokenKey")).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var row in result.GetRows("sticker.csv"))
            {
                var effectKey = row.Get("effectKey");
                if (!knownEffects.Contains(effectKey))
                    result.AddError($"{row.Table.Name}: row {row.LineNumber} effectKey `{effectKey}` has no code executor.");

                ValidateTokenLikeField(result, row, "spawnType", tokens);
                ValidateTokenLikeField(result, row, "reactionType", tokens);
            }
        }

        static void ValidateTokenLikeField(TableImportResult result, ConfigCsvRow row, string field, HashSet<string> tokens)
        {
            var value = row.Get(field);
            if (string.IsNullOrWhiteSpace(value))
                return;

            if (tokens.Contains(value) || value.StartsWith("consume_", StringComparison.OrdinalIgnoreCase) || value.StartsWith("catch_", StringComparison.OrdinalIgnoreCase))
                return;

            result.AddWarning($"{row.Table.Name}: row {row.LineNumber} {field} `{value}` is not a tokenKey or known reaction prefix.");
        }

        static void ValidateShop(TableImportResult result)
        {
            foreach (var row in result.GetRows("shop.csv"))
            {
                var kind = ParseEnum(row, "slotKind", ShopSlotKind.Sticker);
                if ((kind == ShopSlotKind.Sticker || kind == ShopSlotKind.Mod || kind == ShopSlotKind.Growth) && ParseInt(row.Get("count")) <= 0)
                    result.AddError($"{row.Table.Name}: row {row.LineNumber} count must be > 0 for {kind} slots.");

                var rarityWeights = row.Get("rarityWeights");
                if (!string.IsNullOrWhiteSpace(rarityWeights))
                {
                    var parsed = ParseRarityWeights(rarityWeights);
                    var total = parsed.white + parsed.blue + parsed.purple + parsed.gold;
                    if (!Mathf.Approximately(total, 100f))
                        result.AddError($"{row.Table.Name}: row {row.LineNumber} rarityWeights total {total}, expected 100.");
                }

                if (kind == ShopSlotKind.RemoveBlock)
                    result.AddWarning($"{row.Table.Name}: row {row.LineNumber} uses legacy RemoveBlock slot. Runtime block deletion now comes from blockOperation.csv and shopBlockOperationProfileId.");
            }
        }

        static void ValidateBlockOperations(TableImportResult result)
        {
            foreach (var row in result.GetRows("blockOperation.csv"))
            {
                ValidateBool(result, row, "allowDelete");
                ValidateBool(result, row, "allowSwap");
                ValidateNonNegative(result, row, "deleteCostGold");
                ValidateNonNegative(result, row, "swapCostGold");
                ValidateLimit(result, row, "maxDeleteCount");
                ValidateLimit(result, row, "maxSwapCount");
            }
        }

        static void ValidateEncounterTemplates(TableImportResult result)
        {
            var enemyIds = result.GetRows("enemy.csv")
                .Select(row => ParseInt(row.Get("id"), -1))
                .ToHashSet();

            foreach (var row in result.GetRows("encounter.csv"))
            {
                var minFloor = ParseInt(row.Get("minFloor"));
                var maxFloor = ParseInt(row.Get("maxFloor"));
                if (minFloor <= 0 || maxFloor <= 0 || minFloor > maxFloor)
                    result.AddError($"{row.Table.Name}: row {row.LineNumber} floor range must be positive and min <= max.");

                if (ParseInt(row.Get("weight")) <= 0)
                    result.AddError($"{row.Table.Name}: row {row.LineNumber} weight must be > 0.");

                ValidateBool(result, row, "allowRepeat");
                ValidateEncounterEnemySlots(result, row, enemyIds);
            }
        }

        static void ValidateEncounterEnemySlots(TableImportResult result, ConfigCsvRow row, HashSet<int> enemyIds)
        {
            var raw = row.Get("enemySlots");
            if (string.IsNullOrWhiteSpace(raw))
            {
                result.AddError($"{row.Table.Name}: row {row.LineNumber} enemySlots must contain at least one enemy.");
                return;
            }

            var seenSlots = new HashSet<EnemyEncounterSlot>();
            var parts = raw.Split(new[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var fields = part.Trim().Split(new[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length < 2)
                {
                    result.AddError($"{row.Table.Name}: row {row.LineNumber} enemy slot `{part}` must use enemyId:slotKey.");
                    continue;
                }

                var enemyId = ParseInt(fields[0], -1);
                if (!enemyIds.Contains(enemyId))
                    result.AddError($"{row.Table.Name}: row {row.LineNumber} references missing enemy id `{fields[0]}`.");

                if (!ConfigTableCsvParsers.TryParseEnemyEncounterSlot(fields[1], out var slot))
                {
                    result.AddError($"{row.Table.Name}: row {row.LineNumber} slot `{fields[1]}` is invalid.");
                    continue;
                }

                if (!seenSlots.Add(slot))
                    result.AddError($"{row.Table.Name}: row {row.LineNumber} uses slot `{fields[1]}` more than once.");
            }
        }

        static void ValidateMapConfig(TableImportResult result)
        {
            var enemyCount = result.GetRows("enemy.csv").Count();
            foreach (var row in result.GetRows("mapConfig.csv"))
            {
                var floorCount = ParseInt(row.Get("floorCount"));
                var minNodes = ParseInt(row.Get("minNodesPerFloor"));
                var maxNodes = ParseInt(row.Get("maxNodesPerFloor"));
                if (floorCount < 2)
                    result.AddError($"{row.Table.Name}: row {row.LineNumber} floorCount must be >= 2.");
                if (minNodes <= 0 || maxNodes <= 0 || minNodes > maxNodes)
                    result.AddError($"{row.Table.Name}: row {row.LineNumber} node count range must be positive and min <= max.");

                var totalWeight =
                    Mathf.Max(0, ParseInt(row.Get("battleWeight"))) +
                    Mathf.Max(0, ParseInt(row.Get("shopWeight"))) +
                    Mathf.Max(0, ParseInt(row.Get("workbenchWeight"))) +
                    Mathf.Max(0, ParseInt(row.Get("restWeight"), 10)) +
                    Mathf.Max(0, ParseInt(row.Get("eventWeight")));
                if (totalWeight <= 0)
                    result.AddError($"{row.Table.Name}: row {row.LineNumber} node weights must not all be 0.");

                var bossEnemyIndex = ParseInt(row.Get("bossEnemyIndex"), -1);
                if (bossEnemyIndex < -1)
                    result.AddError($"{row.Table.Name}: row {row.LineNumber} bossEnemyIndex must be -1 or >= 0.");
                if (bossEnemyIndex >= enemyCount)
                    result.AddError($"{row.Table.Name}: row {row.LineNumber} bossEnemyIndex {bossEnemyIndex} is outside enemy table range.");
            }
        }

        static void RequireEnum<T>(TableImportResult result, ConfigCsvRow row, string field) where T : struct
        {
            if (!ConfigTableService.TryParseEnumKey(row.Get(field), out T _))
                result.AddError($"{row.Table.Name}: row {row.LineNumber} {field} `{row.Get(field)}` is not a valid {typeof(T).Name}.");
        }

        static void LogValidationResult(TableImportResult result, bool verbose)
        {
            foreach (var warning in result.Warnings)
                Debug.LogWarning("[POPHero Config] " + warning);
            foreach (var error in result.Errors)
                Debug.LogError("[POPHero Config] " + error);

            if (verbose && !result.HasErrors)
                Debug.Log($"[POPHero Config] Validation passed. Tables: {result.Tables.Count}");
        }

        static T ParseEnum<T>(ConfigCsvRow row, string field, T fallback) where T : struct
        {
            return ConfigTableCsvParsers.ParseEnum(row.Get(field), fallback);
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

        static List<string> ParseTokenList(string raw)
        {
            return ConfigTableCsvParsers.ParseTokenList(raw);
        }

        static List<EncounterEnemyDef> ParseEncounterEnemies(string raw)
        {
            return ConfigTableCsvParsers.ParseEncounterEnemies(raw);
        }

        static void ValidateBool(TableImportResult result, ConfigCsvRow row, string field)
        {
            if (string.IsNullOrWhiteSpace(row.Get(field)))
                return;

            if (ConfigTableCsvParsers.IsBoolLiteral(row.Get(field)))
                return;

            result.AddError($"{row.Table.Name}: row {row.LineNumber} {field} `{row.Get(field)}` is not a valid bool.");
        }

        static void ValidateNonNegative(TableImportResult result, ConfigCsvRow row, string field)
        {
            if (!int.TryParse(row.Get(field), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value < 0)
                result.AddError($"{row.Table.Name}: row {row.LineNumber} {field} must be >= 0.");
        }

        static void ValidateLimit(TableImportResult result, ConfigCsvRow row, string field)
        {
            if (!int.TryParse(row.Get(field), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value < -1)
                result.AddError($"{row.Table.Name}: row {row.LineNumber} {field} must be -1 or >= 0.");
        }

        sealed class TableImportResult
        {
            public readonly Dictionary<string, ConfigCsvTable> Tables = new(StringComparer.OrdinalIgnoreCase);
            public readonly List<string> Errors = new();
            public readonly List<string> Warnings = new();
            public bool HasErrors => Errors.Count > 0;
            public void AddError(string message) => Errors.Add(message);
            public void AddWarning(string message) => Warnings.Add(message);

            public IEnumerable<ConfigCsvRow> GetRows(string tableName)
            {
                return Tables.TryGetValue(tableName, out var table) ? table.DataRows : Array.Empty<ConfigCsvRow>();
            }
        }
    }
}
