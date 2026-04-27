using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace POPHero
{
    public static class CsvFileReader
    {
        public static string[] ReadAllLinesWithRetry(string path, int maxAttempts = 12, int delayMs = 120)
        {
            Exception lastError = null;
            for (var attempt = 1; attempt <= Math.Max(1, maxAttempts); attempt++)
            {
                try
                {
                    return ReadAllLinesShared(path);
                }
                catch (IOException ex)
                {
                    lastError = ex;
                    if (attempt >= maxAttempts)
                        break;

                    Thread.Sleep(Math.Max(1, delayMs));
                }
            }

            throw new IOException(
                $"Failed to read CSV `{path}` because the file is locked by another process. Close the spreadsheet/editor using it and try again.",
                lastError);
        }

        static string[] ReadAllLinesShared(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream, Encoding.UTF8, true);
            var lines = new List<string>();
            while (!reader.EndOfStream)
                lines.Add(reader.ReadLine() ?? string.Empty);
            return lines.ToArray();
        }
    }

    public sealed class ConfigCsvTable
    {
        public string Name;
        public readonly List<List<string>> Rows = new();
        public List<string> Header => Rows.Count > 0 ? Rows[0] : new List<string>();
        public readonly List<ConfigCsvRow> DataRows = new();

        public static ConfigCsvTable Load(string path)
        {
            var table = new ConfigCsvTable { Name = Path.GetFileName(path) };
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
                    table.DataRows.Add(new ConfigCsvRow(table, i + 1, values));
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

    public sealed class ConfigCsvRow
    {
        public ConfigCsvRow(ConfigCsvTable table, int lineNumber, List<string> values)
        {
            Table = table;
            LineNumber = lineNumber;
            Values = values;
        }

        public ConfigCsvTable Table { get; }
        public int LineNumber { get; }
        public List<string> Values { get; }

        public string Get(string field)
        {
            var index = Table.Header.FindIndex(header => string.Equals(header, field, StringComparison.OrdinalIgnoreCase));
            return index >= 0 && index < Values.Count ? Values[index].Trim() : string.Empty;
        }
    }

    public static class ConfigTableCsvParsers
    {
        public static T ParseEnum<T>(string raw, T fallback) where T : struct
        {
            return ConfigTableService.TryParseEnumKey(raw, out T value) ? value : fallback;
        }

        public static int ParseInt(string value, int fallback = 0)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
        }

        public static bool ParseBool(string value, bool fallback = false)
        {
            if (bool.TryParse(value, out var parsed))
                return parsed;

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                return intValue != 0;

            return fallback;
        }

        public static float ParseFloat(string value, float fallback = 0f)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
        }

        public static RarityWeightSet ParseRarityWeights(string raw)
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

        public static List<string> ParseTokenList(string raw, string fallback = "none")
        {
            var result = new List<string>();
            if (!string.IsNullOrWhiteSpace(raw))
            {
                var parts = raw.Split(new[] { '|', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    var token = part.Trim();
                    if (!string.IsNullOrWhiteSpace(token))
                        result.Add(token);
                }
            }

            if (result.Count == 0 && !string.IsNullOrWhiteSpace(fallback))
                result.Add(fallback.Trim());

            return result;
        }

        public static List<EncounterEnemyDef> ParseEncounterEnemies(string raw)
        {
            var result = new List<EncounterEnemyDef>();
            if (string.IsNullOrWhiteSpace(raw))
                return result;

            var parts = raw.Split(new[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var token = part.Trim();
                if (string.IsNullOrWhiteSpace(token))
                    continue;

                var fields = token.Split(new[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
                var enemyId = fields.Length > 0 ? ParseInt(fields[0]) : 0;
                var slotKey = fields.Length > 1 ? fields[1] : string.Empty;
                result.Add(new EncounterEnemyDef
                {
                    enemyId = enemyId,
                    slot = ParseEnemyEncounterSlot(slotKey, EnemyEncounterSlot.Primary)
                });
            }

            return result;
        }

        public static EnemyEncounterSlot ParseEnemyEncounterSlot(string raw, EnemyEncounterSlot fallback)
        {
            return TryParseEnemyEncounterSlot(raw, out var slot) ? slot : fallback;
        }

        public static bool TryParseEnemyEncounterSlot(string raw, out EnemyEncounterSlot slot)
        {
            slot = EnemyEncounterSlot.Primary;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var normalized = raw.Trim().Replace("-", "_").Replace(" ", "_").ToLowerInvariant();
            switch (normalized)
            {
                case "slot_front":
                case "front":
                case "primary":
                    slot = EnemyEncounterSlot.Primary;
                    return true;
                case "slot_mid":
                case "mid":
                case "middle":
                    slot = EnemyEncounterSlot.Mid;
                    return true;
                case "slot_air":
                case "air":
                case "support":
                    slot = EnemyEncounterSlot.Support;
                    return true;
                default:
                    return Enum.TryParse(raw.Trim(), true, out slot);
            }
        }

        public static bool IsBoolLiteral(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;

            if (bool.TryParse(value, out _))
                return true;

            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue) &&
                   (intValue == 0 || intValue == 1);
        }
    }
}
