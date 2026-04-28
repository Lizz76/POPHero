using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace POPHero.Tests
{
    public sealed class BlockAcquisitionFlowTests
    {
        readonly List<UnityEngine.Object> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (var index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                    UnityEngine.Object.DestroyImmediate(createdObjects[index]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void ShopBlockPurchase_AddsToReserveWhenActiveFull()
        {
            var game = CreateGame(1, 1, 20, out var board, out var tableConfig);
            tableConfig.shopSlots.Add(CreateBlockShopSlot(price: 7, rarity: BlockRarity.Blue));
            AssignTables(game, tableConfig);
            Assert.IsTrue(board.GrantStartingCard(BoardBlockType.AttackAdd, BlockRarity.White, out _, out var failReason), failReason);

            var shop = new ShopManager();
            shop.Initialize(game);
            shop.OpenShop();
            var blockIndex = FindBlockShopIndex(shop);

            Assert.IsTrue(shop.TryBuy(blockIndex));
            Assert.AreEqual(13, game.Player.Gold);
            Assert.AreEqual(1, board.ActiveCardCount);
            Assert.AreEqual(1, board.ReserveCardCount);
            Assert.AreEqual(BlockRarity.Blue, board.ReserveCardStates[0].rarity);
            Assert.IsTrue(shop.Items[blockIndex].purchased);
        }

        [Test]
        public void ShopBlockPurchase_FailsWithoutSpendingWhenAllBlockSlotsFull()
        {
            var game = CreateGame(1, 1, 20, out var board, out var tableConfig);
            tableConfig.shopSlots.Add(CreateBlockShopSlot(price: 7, rarity: BlockRarity.Purple));
            AssignTables(game, tableConfig);
            Assert.IsTrue(board.GrantStartingCard(BoardBlockType.AttackAdd, BlockRarity.White, out _, out var failReason), failReason);
            Assert.IsTrue(board.GrantStartingCard(BoardBlockType.Shield, BlockRarity.White, out _, out failReason), failReason);

            var shop = new ShopManager();
            shop.Initialize(game);
            shop.OpenShop();
            var blockIndex = FindBlockShopIndex(shop);

            Assert.IsFalse(shop.TryBuy(blockIndex));
            Assert.AreEqual(20, game.Player.Gold);
            Assert.AreEqual(1, board.ActiveCardCount);
            Assert.AreEqual(1, board.ReserveCardCount);
            Assert.IsFalse(shop.Items[blockIndex].purchased);
        }

        [Test]
        public void WorkbenchUpgrade_ReplacesCardWithNextRarityAndRespectsLimit()
        {
            var game = CreateGame(2, 1, 10, out var board, out var tableConfig);
            tableConfig.blockOperationProfiles.Add(new BlockOperationProfileDef
            {
                id = "map_workbench",
                allowSwap = true,
                allowUpgrade = true,
                upgradeCostGold = 2,
                maxUpgradeCount = 1
            });
            AssignTables(game, tableConfig);
            Assert.IsTrue(board.GrantStartingCard(BoardBlockType.AttackAdd, BlockRarity.White, out var sourceCard, out var failReason), failReason);

            var operations = new BlockOperationManager();
            operations.Initialize(game);
            Assert.IsTrue(operations.TryOpen("map_workbench", RoundState.Map, out failReason), failReason);

            Assert.IsTrue(operations.TryUpgradeBlock(sourceCard.id, out failReason), failReason);
            Assert.AreEqual(8, game.Player.Gold);
            Assert.AreEqual(1, board.ActiveCardCount);
            Assert.AreEqual(BlockRarity.Blue, board.ActiveCardStates[0].rarity);
            Assert.AreNotEqual(sourceCard.id, board.ActiveCardStates[0].id);
            Assert.AreEqual(1, operations.Session.upgradeUsedCount);

            Assert.IsFalse(operations.TryUpgradeBlock(board.ActiveCardStates[0].id, out _));
            Assert.AreEqual(8, game.Player.Gold);
            Assert.AreEqual(BlockRarity.Blue, board.ActiveCardStates[0].rarity);
        }

        [Test]
        public void BossClear_OffersBlueOrBetterBlockAndSelectionStartsNextMap()
        {
            var game = CreateGame(2, 1, 25, out var board, out var tableConfig);
            tableConfig.mapConfigs.Add(new MapConfigDef
            {
                id = "test_map",
                floorCount = 2,
                minNodesPerFloor = 1,
                maxNodesPerFloor = 1,
                bossEnemyIndex = 0
            });
            AssignTables(game, tableConfig);
            Assert.IsTrue(board.GrantStartingCard(BoardBlockType.AttackAdd, BlockRarity.White, out _, out var failReason), failReason);

            var runMap = new RunMapManager();
            runMap.Initialize(game);
            runMap.GenerateNewMap();
            SetField(game, "runMapManager", runMap);
            var bossNode = runMap.Nodes.First(node => node.kind == MapNodeKind.Boss);
            bossNode.status = MapNodeStatus.Current;
            SetPropertyOrBackingField(runMap, "CurrentNode", bossNode);

            Invoke(game, "HandleEnemyDefeatedCore");

            Assert.AreEqual(RoundState.BlockRewardChoose, game.State);
            Assert.IsTrue(game.IsBossBlockDraftPending);
            Assert.AreEqual(3, board.ActiveRewardOptions.Count);
            Assert.IsTrue(board.ActiveRewardOptions.All(option => option.rarity >= BlockRarity.Blue));
            Assert.AreEqual(0, game.ActiveBallRewardOptions.Count);

            Invoke(game, "ExecuteSelectBlockReward", 0);

            Assert.AreEqual(RoundState.Map, game.State);
            Assert.IsFalse(game.IsBossBlockDraftPending);
            Assert.AreEqual(25, game.Player.Gold);
            Assert.AreEqual(2, board.ActiveCardCount);
            Assert.IsNull(runMap.CurrentNode);
            Assert.IsFalse(runMap.Nodes.Contains(bossNode));
            Assert.IsTrue(runMap.Nodes.Any(node => node.status == MapNodeStatus.Available));
        }

        [Test]
        public void DebugBossClear_AlsoOffersBossBlockRewardAndStartsNextMap()
        {
            var game = CreateGame(2, 1, 25, out var board, out var tableConfig);
            tableConfig.mapConfigs.Add(new MapConfigDef
            {
                id = "test_map",
                floorCount = 2,
                minNodesPerFloor = 1,
                maxNodesPerFloor = 1,
                bossEnemyIndex = 0
            });
            AssignTables(game, tableConfig);
            Assert.IsTrue(board.GrantStartingCard(BoardBlockType.AttackAdd, BlockRarity.White, out _, out var failReason), failReason);

            var runMap = new RunMapManager();
            runMap.Initialize(game);
            runMap.GenerateNewMap();
            SetField(game, "runMapManager", runMap);
            SetField(game, "debugBattleReturnActive", true);
            SetField(game, "debugBattleIsBoss", true);

            Invoke(game, "HandleEnemyDefeatedCore");

            Assert.AreEqual(RoundState.BlockRewardChoose, game.State);
            Assert.IsTrue(game.IsBossBlockDraftPending);
            Assert.AreEqual(3, board.ActiveRewardOptions.Count);
            Assert.IsTrue(board.ActiveRewardOptions.All(option => option.rarity >= BlockRarity.Blue));
            Assert.AreEqual(0, game.ActiveBallRewardOptions.Count);

            var previousNodes = runMap.Nodes.ToArray();
            Invoke(game, "ExecuteSelectBlockReward", 0);

            Assert.AreEqual(RoundState.Map, game.State);
            Assert.IsFalse(game.IsBossBlockDraftPending);
            Assert.AreEqual(25, game.Player.Gold);
            Assert.AreEqual(2, board.ActiveCardCount);
            Assert.IsTrue(previousNodes.All(node => !runMap.Nodes.Contains(node)));
            Assert.IsTrue(runMap.Nodes.Any(node => node.status == MapNodeStatus.Available));
        }

        PopHeroGame CreateGame(int activeCapacity, int reserveCapacity, int startingGold, out BoardManager board, out PopHeroTableConfig tableConfig)
        {
            UnityEngine.Random.InitState(12345);
            var gameObject = Track(new GameObject("Test PopHeroGame"));
            gameObject.SetActive(false);
            var game = gameObject.AddComponent<PopHeroGame>();
            game.config = Track(PopHeroPrototypeConfig.CreateRuntimeDefault());
            game.config.blockRewards.maxActiveBlocks = activeCapacity;
            game.config.blockRewards.maxReserveBlocks = reserveCapacity;
            game.config.blockRewards.rewardChoiceCount = 3;
            game.config.stickers.defaultSocketsPerCard = 0;
            game.config.stickers.unlockedSocketsPerCard = 0;
            SetPropertyOrBackingField(game, "Player", new PlayerData(100, 100, 0, startingGold));

            tableConfig = Track(ScriptableObject.CreateInstance<PopHeroTableConfig>());
            AssignTables(game, tableConfig);

            var blockRoot = new GameObject("Blocks").transform;
            blockRoot.SetParent(gameObject.transform);
            board = gameObject.AddComponent<BoardManager>();
            board.Initialize(game, blockRoot, null);
            SetField(game, "boardManager", board);
            return game;
        }

        static ShopSlotDef CreateBlockShopSlot(int price, BlockRarity rarity)
        {
            var weights = new RarityWeightSet();
            switch (rarity)
            {
                case BlockRarity.White:
                    weights.white = 100f;
                    break;
                case BlockRarity.Blue:
                    weights.blue = 100f;
                    break;
                case BlockRarity.Purple:
                    weights.purple = 100f;
                    break;
                case BlockRarity.Gold:
                    weights.gold = 100f;
                    break;
            }

            return new ShopSlotDef
            {
                slotId = "shop_block",
                slotKind = ShopSlotKind.Block,
                count = 1,
                price = price,
                rarityWeights = weights
            };
        }

        static int FindBlockShopIndex(ShopManager shop)
        {
            for (var index = 0; index < shop.Items.Count; index++)
            {
                if (shop.Items[index].kind == ShopItemKind.Block)
                    return index;
            }

            Assert.Fail("Block shop item was not generated.");
            return -1;
        }

        void AssignTables(PopHeroGame game, PopHeroTableConfig tableConfig)
        {
            SetPropertyOrBackingField(game, "Tables", new ConfigTableService(tableConfig, game.config));
        }

        T Track<T>(T obj) where T : UnityEngine.Object
        {
            createdObjects.Add(obj);
            return obj;
        }

        static void Invoke(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Missing method {methodName}.");
            method.Invoke(target, args);
        }

        static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing field {fieldName}.");
            field.SetValue(target, value);
        }

        static void SetPropertyOrBackingField(object target, string propertyName, object value)
        {
            var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property?.SetMethod != null)
            {
                property.SetValue(target, value);
                return;
            }

            var backingField = target.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(backingField, $"Missing property or backing field {propertyName}.");
            backingField.SetValue(target, value);
        }
    }
}
