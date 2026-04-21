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
            "blockOperation.csv"
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

        static Dictionary<string, CsvTable> LoadTables(string sourceFolder)
        {
            var tables = new Dictionary<string, CsvTable>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in Directory.GetFiles(sourceFolder, "*.csv"))
            {
                var table = CsvTable.Load(path);
                tables[table.Name] = table;
            }

            return tables;
        }

        static void FillTableConfig(PopHeroTableConfig asset, IReadOnlyDictionary<string, CsvTable> tables)
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
        }

        static IEnumerable<CsvRow> GetRows(IReadOnlyDictionary<string, CsvTable> tables, string tableName)
        {
            return tables.TryGetValue(tableName, out var table) ? table.DataRows : Array.Empty<CsvRow>();
        }

        static T ParseEnum<T>(string raw, T fallback) where T : struct
        {
            return ConfigTableService.TryParseEnumKey(raw, out T value) ? value : fallback;
        }

        static int ParseInt(string value, int fallback = 0)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
        }

        static bool ParseBool(string value, bool fallback = false)
        {
            if (bool.TryParse(value, out var parsed))
                return parsed;

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                return intValue != 0;

            return fallback;
        }

        static float ParseFloat(string value, float fallback = 0f)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
        }

        static RarityWeightSet ParseRarityWeights(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new RarityWeightSet();

            var parts = raw.Split(new[] { '|', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            return new RarityWeightSet
            {
                white = parts.Length > 0 ? ParseFloat(parts[0]) : 0f,
                blue = parts.Length > 1 ? ParseFloat(parts[1]) : 0f,
                purple = parts.Length > 2 ? ParseFloat(parts[2]) : 0f,
                gold = parts.Length > 3 ? ParseFloat(parts[3]) : 0f
            };
        }

        sealed class CsvTable
        {
            public string Name;
            public readonly List<List<string>> Rows = new();
            public List<string> Header => Rows.Count > 0 ? Rows[0] : new List<string>();
            public readonly List<CsvRow> DataRows = new();

            public static CsvTable Load(string path)
            {
                var table = new CsvTable { Name = Path.GetFileName(path) };
                var lines = CsvFileReader.ReadAllLinesWithRetry(path);
                for (var i = 0; i < lines.Length; i++)
                {
                    var values = ParseCsvLine(lines[i]);
                    if (i == 0 && values.Count > 0)
                        values[0] = values[0].TrimStart('\uFEFF');
                    table.Rows.Add(values);
                }

                if (table.Rows.Count >= 5)
                {
                    for (var i = 5; i < table.Rows.Count; i++)
                    {
                        var values = table.Rows[i];
                        if (values.Count == 0 || values.All(string.IsNullOrWhiteSpace))
                            continue;
                        table.DataRows.Add(new CsvRow(table, i + 1, values));
                    }
                }

                return table;
            }

            static List<string> ParseCsvLine(string line)
            {
                var values = new List<string>();
                var current = new StringBuilder();
                var inQuotes = false;
                for (var i = 0; i < line.Length; i++)
                {
                    var ch = line[i];
                    if (ch == '"')
                    {
                        if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = !inQuotes;
                        }
                    }
                    else if (ch == ',' && !inQuotes)
                    {
                        values.Add(current.ToString());
                        current.Clear();
                    }
                    else
                    {
                        current.Append(ch);
                    }
                }

                values.Add(current.ToString());
                return values;
            }
        }

        sealed class CsvRow
        {
            readonly CsvTable table;
            readonly List<string> values;

            public CsvRow(CsvTable table, int lineNumber, List<string> values)
            {
                this.table = table;
                LineNumber = lineNumber;
                this.values = values;
            }

            public int LineNumber { get; }

            public string Get(string field)
            {
                var index = table.Header.FindIndex(header => string.Equals(header, field, StringComparison.OrdinalIgnoreCase));
                return index >= 0 && index < values.Count ? values[index].Trim() : string.Empty;
            }
        }
    }
}
