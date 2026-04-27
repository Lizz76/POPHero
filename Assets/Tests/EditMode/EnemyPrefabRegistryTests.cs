using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace POPHero.Tests
{
    public sealed class EnemyPrefabRegistryTests
    {
        [Test]
        public void RuntimeRegistry_MapsBirdPrefabAsset()
        {
            var registry = Resources.Load<EnemyPrefabRegistry>(EnemyPrefabRegistry.ResourceName);

            Assert.IsNotNull(registry);
            var birdPrefab = registry.ResolvePrefab("bird", false);
            Assert.IsNotNull(birdPrefab);
            Assert.AreEqual("BirdEnemy", birdPrefab.name);
        }

        [Test]
        public void Registry_MapsBirdKeyAndFallsBackWithWarning()
        {
            var registry = ScriptableObject.CreateInstance<EnemyPrefabRegistry>();
            var defaultPrefab = new GameObject("DefaultEnemyPrefab");
            var birdPrefab = new GameObject("BirdEnemyPrefab");
            try
            {
                registry.DefaultPrefab = defaultPrefab;
                registry.Entries.Add(new EnemyPrefabRegistry.Entry { key = "bird", prefab = birdPrefab });

                Assert.AreSame(birdPrefab, registry.ResolvePrefab("bird"));

                LogAssert.Expect(LogType.Warning, new Regex("Enemy prefabKey `missing_key` was not found"));
                Assert.AreSame(defaultPrefab, registry.ResolvePrefab("missing_key"));
            }
            finally
            {
                Object.DestroyImmediate(defaultPrefab);
                Object.DestroyImmediate(birdPrefab);
                Object.DestroyImmediate(registry);
            }
        }
    }
}
