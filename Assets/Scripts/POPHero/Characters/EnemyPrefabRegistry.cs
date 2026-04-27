using System;
using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    [CreateAssetMenu(fileName = ResourceName, menuName = "POPHero/Enemy Prefab Registry")]
    public sealed class EnemyPrefabRegistry : ScriptableObject
    {
        public const string ResourceName = "EnemyPrefabRegistry";
        public const string DefaultPrefabKey = "default";

        [Serializable]
        public sealed class Entry
        {
            public string key = DefaultPrefabKey;
            public GameObject prefab;
        }

        [SerializeField] GameObject defaultPrefab;
        [SerializeField] List<Entry> entries = new();

        public GameObject DefaultPrefab
        {
            get => defaultPrefab;
            set => defaultPrefab = value;
        }

        public List<Entry> Entries => entries;

        public GameObject ResolvePrefab(string prefabKey, bool logWarning = true)
        {
            var normalizedKey = NormalizeKey(prefabKey);
            if (!string.Equals(normalizedKey, DefaultPrefabKey, StringComparison.OrdinalIgnoreCase))
            {
                for (var index = 0; index < entries.Count; index++)
                {
                    var entry = entries[index];
                    if (entry == null || !string.Equals(NormalizeKey(entry.key), normalizedKey, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (entry.prefab != null)
                        return entry.prefab;

                    if (logWarning)
                        Debug.LogWarning($"[POPHero] Enemy prefabKey `{normalizedKey}` is registered without a prefab. Falling back to `{DefaultPrefabKey}`.");
                    return defaultPrefab;
                }

                if (logWarning)
                    Debug.LogWarning($"[POPHero] Enemy prefabKey `{normalizedKey}` was not found. Falling back to `{DefaultPrefabKey}`.");
            }

            if (defaultPrefab == null && logWarning)
                Debug.LogWarning("[POPHero] Enemy prefab registry default prefab is missing.");

            return defaultPrefab;
        }

        public static string NormalizeKey(string prefabKey)
        {
            return string.IsNullOrWhiteSpace(prefabKey) ? DefaultPrefabKey : prefabKey.Trim();
        }
    }
}
