using System;
using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public sealed class TopStatusBarModel
    {
        public string HpText;
        public string GoldText;
        public string ProgressCountText;
        public string RunTimerText;
    }

    public sealed class StatusPanelModel
    {
        public string StateText;
        public string AimModeText;
        public string LevelText;
        public string KillsText;
        public string BlockText;
        public string HpText;
        public string ShieldText;
        public string GoldText;
        public string InventoryText;
        public string LaunchesText;
        public string BallBagText;
        public string EnemyText;
        public string EnemyHpText;
        public string EnemyAttackText;
    }

    public sealed class CombatPanelModel
    {
        public string RoundAttackText;
        public string RoundShieldText;
        public string RoundHitText;
        public string PreviewText;
        public string IntermissionText;
        public string CurrentBallText;
        public string CurrentBallDescription;
        public string DiscardButtonText;
        public bool CanDiscardCurrentBall;
    }

    public sealed class BlockRowModel
    {
        public int DisplayIndex;
        public bool IsActiveSection;
        public BlockCardState Card;
    }

    public sealed class BlockManagementPanelModel
    {
        public string HeaderText;
        public string HintText;
        public IReadOnlyList<BlockRowModel> ActiveRows;
        public IReadOnlyList<BlockRowModel> ReserveRows;
    }

    public sealed class BlockOperationsPanelModel
    {
        public string TitleText;
        public string SubtitleText;
        public string HintText;
        public string FeedbackText;
        public string ActiveColumnTitle;
        public string ReserveColumnTitle;
        public string DeleteStatusText;
        public string SwapStatusText;
        public string CloseButtonText;
        public IReadOnlyList<BlockCardState> ActiveCards;
        public IReadOnlyList<BlockCardState> ReserveCards;
        public bool AllowDelete;
        public bool AllowSwap;
    }

    public sealed class BlockRewardCardModel
    {
        public int Index;
        public string DisplayName;
        public string TypeText;
        public string RarityText;
        public string ValueText;
        public string Description;
        public Color AccentColor;
        public bool CanSelect;
        public string SelectButtonText;
    }

    public sealed class BlockRewardPanelModel
    {
        public string TitleText;
        public string SubtitleText;
        public IReadOnlyList<BlockRewardCardModel> Cards;
        public bool ShowSkipButton;
        public string SkipButtonText;
    }

    public sealed class BallRewardCardModel
    {
        public int Index;
        public string DisplayName;
        public string RarityText;
        public string Description;
        public Color AccentColor;
    }

    public sealed class BallRewardPanelModel
    {
        public string TitleText;
        public string SubtitleText;
        public IReadOnlyList<BallRewardCardModel> Cards;
    }

    public sealed class RewardCardModel
    {
        public int Index;
        public string Title;
        public string Description;
        public string KindText;
    }

    public sealed class RewardPanelModel
    {
        public string TitleText;
        public string SubtitleText;
        public IReadOnlyList<RewardCardModel> Cards;
        public string RerollButtonText;
        public string SkipButtonText;
    }

    public sealed class ShopItemCardModel
    {
        public int Index;
        public string Title;
        public string Description;
        public string KindText;
        public string PriceText;
        public bool Purchased;
        public string ButtonText;
    }

    public sealed class ShopPanelModel
    {
        public string TitleText;
        public string SubtitleText;
        public IReadOnlyList<ShopItemCardModel> Items;
        public string LastFeedbackText;
        public string GoldText;
        public string RerollCostText;
        public string BlockOperationsButtonText;
        public string RerollButtonText;
        public string CloseButtonText;
    }

    public sealed class MapNodeCardModel
    {
        public string NodeId;
        public int Floor;
        public MapNodeKind Kind;
        public MapNodeStatus Status;
        public Vector2 NormalizedPosition;
        public IReadOnlyList<string> NextNodeIds;
        public string Title;
        public string KindText;
        public string StatusText;
        public string Description;
        public string ButtonText;
        public bool CanSelect;
        public Color AccentColor;
    }

    public sealed class MapPanelModel
    {
        public string TitleText;
        public string SubtitleText;
        public string FeedbackText;
        public string ConnectionsText;
        public IReadOnlyList<MapNodeCardModel> Nodes;
    }

    public sealed class MapEventOptionCardModel
    {
        public int Index;
        public string Title;
        public string Description;
        public string ButtonText;
    }

    public sealed class MapEventPanelModel
    {
        public string TitleText;
        public string SubtitleText;
        public IReadOnlyList<MapEventOptionCardModel> Options;
    }

    public sealed class LoadoutPanelModel
    {
        public string TitleText;
        public string SubtitleText;
        public IReadOnlyList<StickerInstance> Inventory;
        public IReadOnlyList<ModInstance> ActiveMods;
        public IReadOnlyList<ModInstance> ReserveMods;
        public bool CanCancelDrag;
        public string CancelDragText;
        public string ContinueButtonText;
    }

    public sealed class TopStatusBarPresenter
    {
        public TopStatusBarModel Build(IGameReadModel game)
        {
            var player = game?.Player;
            return new TopStatusBarModel
            {
                HpText = player != null ? $"{player.CurrentHp}/{player.MaxHp}" : "--/--",
                GoldText = player != null ? player.Gold.ToString() : "0",
                ProgressCountText = player != null ? player.TotalKills.ToString() : "0",
                RunTimerText = FormatRunTime(game?.RunElapsedSeconds ?? 0f)
            };
        }

        static string FormatRunTime(float elapsedSeconds)
        {
            var clampedSeconds = Mathf.Max(0, Mathf.FloorToInt(elapsedSeconds));
            var time = TimeSpan.FromSeconds(clampedSeconds);
            return time.TotalHours >= 1d
                ? $"{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00}"
                : $"{time.Minutes:00}:{time.Seconds:00}";
        }
    }

    public sealed class StatusPanelPresenter
    {
        public StatusPanelModel Build(IGameReadModel game)
        {
            var player = game?.Player;
            var enemy = game?.CurrentEnemy;
            var encounterState = game?.CurrentEnemyEncounter;
            var encounters = game?.CurrentEnemyEncounters;
            var secondaryEncounter = GetSecondaryEncounter(encounters, encounterState);
            var hasPlayer = player != null;
            var stickerInventory = game?.StickerInventory;
            var mods = game?.Mods;
            var blocks = game?.BlockCollections;
            var inventoryCount = stickerInventory?.Stored?.Count ?? 0;
            var inventoryCapacity = hasPlayer
                ? player.StickerInventoryCapacity + (mods?.GetInventoryCapacityBonus() ?? 0)
                : 0;
            var activeCount = blocks?.ActiveCardCount ?? 0;
            var activeCapacity = blocks?.ActiveCapacity ?? 0;
            var reserveCount = blocks?.ReserveCardCount ?? 0;
            var reserveCapacity = blocks?.ReserveCapacity ?? 0;
            var state = game != null ? GetStateText(game.State) : "--";
            var aimMode = game?.AimModeDisplayText ?? "--";
            var launches = game != null ? $"抽取 {game.BallDrawPileCount} / 已用 {game.BallUsedPileCount}" : "抽取 -- / 已用 --";
            var encounter = game != null ? game.EncounterIndex.ToString() : "--";

            var model = new StatusPanelModel
            {
                StateText = $"状态：{state}",
                AimModeText = $"瞄准模式：{aimMode}",
                LevelText = hasPlayer ? $"等级：{player.Level}" : "等级：-",
                KillsText = hasPlayer
                    ? $"击杀进度：{player.KillsTowardNextLevel} / {(player.IsMaxLevel ? "已满" : player.KillsRequiredForNextLevel.ToString())}"
                    : "击杀进度：- / --",
                BlockText = $"方块组：上阵 {activeCount}/{activeCapacity}  背包 {reserveCount}/{reserveCapacity}",
                HpText = hasPlayer ? $"生命：{player.CurrentHp}/{player.MaxHp}" : "生命：-/--",
                ShieldText = hasPlayer ? $"护盾：{player.CurrentShield}" : "护盾：-",
                GoldText = hasPlayer ? $"金币：{player.Gold}" : "金币：-",
                InventoryText = $"嵌片库存：{inventoryCount}/{inventoryCapacity}",
                LaunchesText = $"弹球袋：{launches}",
                BallBagText = $"当前弹球：{game?.CurrentBallName ?? "--"}",
                EnemyText = $"当前目标 #{encounter}：{enemy?.DisplayName ?? "--"}",
                EnemyHpText = enemy != null ? $"目标生命：{enemy.CurrentHp}/{enemy.MaxHp}" : "目标生命：-/--",
                EnemyAttackText = enemy != null ? $"目标攻击：{enemy.AttackDamage}" : "目标攻击：-"
            };

            model.EnemyAttackText = EnemyIntentTextFormatter.BuildStatusText(encounterState, enemy);
            if (secondaryEncounter != null && secondaryEncounter.Enemy != null)
            {
                model.EnemyHpText += $"\n副敌生命：{secondaryEncounter.Enemy.CurrentHp}/{secondaryEncounter.Enemy.MaxHp}";
                model.EnemyAttackText += $"\n副敌意图：{EnemyIntentTextFormatter.BuildWorldText(secondaryEncounter, secondaryEncounter.Enemy).Replace("\n", "，")}";
            }

            return model;
        }

        static EnemyEncounterState GetSecondaryEncounter(IReadOnlyList<EnemyEncounterState> encounters, EnemyEncounterState currentEncounter)
        {
            if (encounters == null)
                return null;

            for (var index = 0; index < encounters.Count; index++)
            {
                var encounter = encounters[index];
                if (encounter != null && encounter != currentEncounter && encounter.IsAlive)
                    return encounter;
            }

            return null;
        }

        static string GetStateText(RoundState state)
        {
            return state switch
            {
                RoundState.Aim => "瞄准",
                RoundState.BallFlying => "飞行中",
                RoundState.RoundResolve => "结算中",
                RoundState.BlockRewardChoose => "选方块",
                RoundState.RewardChoose => "选奖励",
                RoundState.Shop => "商店",
                RoundState.BlockOperations => "方块操作",
                RoundState.LoadoutManage => "背包",
                RoundState.Map => "地图",
                RoundState.MapEvent => "路线事件",
                RoundState.GameOver => "结束",
                _ => state.ToString()
            };
        }
    }

    public sealed class CombatPanelPresenter
    {
        public CombatPanelModel Build(IGameReadModel game)
        {
            var round = game?.RoundController;
            var mods = game?.Mods;
            var previewEnabled = mods?.ShowHitCounter() ?? false;
            return new CombatPanelModel
            {
                RoundAttackText = $"本轮伤害：{round?.RoundAttackScore ?? 0}",
                RoundShieldText = $"本轮护盾：{round?.RoundShieldGain ?? 0}",
                RoundHitText = $"命中次数：{round?.RoundHitCount ?? 0}",
                CurrentBallText = $"当前弹球：{game?.CurrentBallName ?? "--"}  抽取 {game?.BallDrawPileCount ?? 0} / 已用 {game?.BallUsedPileCount ?? 0}",
                CurrentBallDescription = game?.CurrentBallDescription ?? string.Empty,
                DiscardButtonText = game?.CanDiscardCurrentBall == true ? "弃掉当前弹球" : "本行动不能弃球",
                CanDiscardCurrentBall = game?.CanDiscardCurrentBall == true,
                PreviewText = previewEnabled && game != null && game.State == RoundState.Aim
                    ? $"锁定路线预览：总命中 {game.PreviewHitCount}，攻击 {game.PreviewAttackBlockCount}，防御 {game.PreviewShieldBlockCount}，倍率 {game.PreviewMultiplierBlockCount}"
                    : string.Empty,
                IntermissionText = game?.IntermissionMessage ?? string.Empty
            };
        }
    }

    public sealed class BlockManagementPresenter
    {
        readonly List<BlockRowModel> activeRows = new();
        readonly List<BlockRowModel> reserveRows = new();

        public BlockManagementPanelModel Build(IGameReadModel game)
        {
            activeRows.Clear();
            reserveRows.Clear();
            var blocks = game?.BlockCollections;
            var activeCapacity = blocks?.ActiveCapacity ?? 0;
            var reserveCapacity = blocks?.ReserveCapacity ?? 0;
            var activeStates = blocks?.ActiveCardStates;
            var reserveStates = blocks?.ReserveCardStates;

            for (var index = 0; index < activeCapacity; index++)
            {
                activeRows.Add(new BlockRowModel
                {
                    DisplayIndex = index,
                    IsActiveSection = true,
                    Card = activeStates != null && index < activeStates.Count ? activeStates[index] : null
                });
            }

            for (var index = 0; index < reserveCapacity; index++)
            {
                reserveRows.Add(new BlockRowModel
                {
                    DisplayIndex = index,
                    IsActiveSection = false,
                    Card = reserveStates != null && index < reserveStates.Count ? reserveStates[index] : null
                });
            }

            return new BlockManagementPanelModel
            {
                HeaderText = "方块管理",
                HintText = "右侧只显示上阵方块。悬停可查看方块、嵌片和槽位详情。",
                ActiveRows = activeRows,
                ReserveRows = reserveRows
            };
        }
    }

    public sealed class IntermissionPanelPresenter
    {
        readonly List<BallRewardCardModel> ballRewardCards = new();
        readonly List<BlockRewardCardModel> blockRewardCards = new();
        readonly List<RewardCardModel> rewardCards = new();
        readonly List<ShopItemCardModel> shopItemCards = new();
        readonly List<MapNodeCardModel> mapNodeCards = new();
        readonly List<MapEventOptionCardModel> mapEventOptionCards = new();
        readonly BlockOperationsPanelModel blockOperationsPanel = new();

        public BallRewardPanelModel BuildBallReward(IGameReadModel game)
        {
            ballRewardCards.Clear();
            var options = game?.ActiveBallRewardOptions ?? Array.Empty<BallRewardOption>();
            for (var index = 0; index < options.Count; index++)
            {
                var option = options[index];
                ballRewardCards.Add(new BallRewardCardModel
                {
                    Index = option.index,
                    DisplayName = option.displayName,
                    RarityText = $"稀有度：{option.rarityText}",
                    Description = option.description,
                    AccentColor = option.color
                });
            }

            return new BallRewardPanelModel
            {
                TitleText = "选择新弹球",
                SubtitleText = "将一颗弹球加入你的弹球袋，之后的战斗会从袋中抽取。",
                Cards = ballRewardCards
            };
        }

        public MapPanelModel BuildMapPanel(IGameReadModel game)
        {
            mapNodeCards.Clear();
            var map = game?.RunMap;
            var nodes = map?.Nodes ?? Array.Empty<MapNodeState>();
            for (var index = 0; index < nodes.Count; index++)
            {
                var node = nodes[index];
                mapNodeCards.Add(new MapNodeCardModel
                {
                    NodeId = node.id,
                    Floor = node.floor,
                    Kind = node.kind,
                    Status = node.status,
                    NormalizedPosition = node.normalizedPosition,
                    NextNodeIds = node.nextNodeIds,
                    Title = $"{node.floor + 1}-{index + 1} {RunMapManager.GetNodeKindName(node.kind)}",
                    KindText = $"类型：{RunMapManager.GetNodeKindName(node.kind)}",
                    StatusText = $"状态：{GetMapNodeStatusText(node.status)}",
                    Description = BuildMapNodeDescription(node),
                    ButtonText = node.IsSelectable ? "进入" : GetMapNodeStatusText(node.status),
                    CanSelect = node.IsSelectable,
                    AccentColor = GetMapNodeColor(node.kind, node.status)
                });
            }

            return new MapPanelModel
            {
                TitleText = "路线地图",
                SubtitleText = "选择一个亮起的节点继续前进。战斗奖励结束后会回到地图。",
                FeedbackText = map?.LastFeedback ?? string.Empty,
                ConnectionsText = BuildConnectionsText(nodes),
                Nodes = mapNodeCards
            };
        }

        public MapEventPanelModel BuildMapEventPanel(IGameReadModel game)
        {
            mapEventOptionCards.Clear();
            var choices = game?.RunMap?.CurrentEventChoices ?? Array.Empty<MapEventChoiceState>();
            for (var index = 0; index < choices.Count; index++)
            {
                var choice = choices[index];
                mapEventOptionCards.Add(new MapEventOptionCardModel
                {
                    Index = choice.index,
                    Title = choice.title,
                    Description = choice.description,
                    ButtonText = choice.buttonText
                });
            }

            return new MapEventPanelModel
            {
                TitleText = "路线事件",
                SubtitleText = "选择一种处理方式，然后继续路线。",
                Options = mapEventOptionCards
            };
        }

        public BlockRewardPanelModel BuildBlockReward(IGameReadModel game)
        {
            blockRewardCards.Clear();
            var blockRewards = game?.BlockRewards;
            var blockCollections = game?.BlockCollections;
            var options = blockRewards?.ActiveRewardOptions ?? Array.Empty<BlockRewardOption>();
            for (var index = 0; index < options.Count; index++)
            {
                var option = options[index];
                blockRewardCards.Add(new BlockRewardCardModel
                {
                    Index = index,
                    DisplayName = option.displayName,
                    TypeText = $"类型：{GetBlockTypeText(option.blockType)}",
                    RarityText = $"稀有度：{GetRarityText(option.rarity)}",
                    ValueText = $"数值：{FormatBlockValue(option)}",
                    Description = option.desc,
                    AccentColor = GetRarityColor(option.rarity),
                    CanSelect = game == null || game.IsInitialBlockDraftPending || (blockCollections?.CanAcceptRewardBlock ?? false),
                    SelectButtonText = blockCollections?.RewardWillGoToReserve == true && game != null && !game.IsInitialBlockDraftPending
                        ? "放入背包"
                        : "加入方块组"
                });
            }

            var subtitle = game != null && game.IsInitialBlockDraftPending
                ? "在第一场战斗开始前，先选择你的起始方块。"
                : blockCollections?.CanAcceptRewardBlock == false
                    ? "上阵和方块背包都已满，请先腾出空间。"
                    : blockCollections?.RewardWillGoToReserve == true
                        ? "上阵已满，选中的方块会进入方块背包。"
                        : "上阵还有空位，选中的方块会直接加入上阵。";

            return new BlockRewardPanelModel
            {
                TitleText = game != null && game.IsInitialBlockDraftPending ? "选择起始方块" : "选择新方块",
                SubtitleText = subtitle,
                Cards = blockRewardCards,
                ShowSkipButton = game != null && !game.IsInitialBlockDraftPending,
                SkipButtonText = "跳过本次方块"
            };
        }

        public RewardPanelModel BuildRewardPanel(IGameReadModel game)
        {
            rewardCards.Clear();
            var choices = game?.RewardChoiceController?.ActiveChoices;
            if (choices != null)
            {
                for (var index = 0; index < choices.Count; index++)
                {
                    rewardCards.Add(new RewardCardModel
                    {
                        Index = index,
                        Title = choices[index].title,
                        Description = choices[index].description,
                        KindText = $"类别：{GetRewardKindText(choices[index].kind)}"
                    });
                }
            }

            return new RewardPanelModel
            {
                TitleText = "选择奖励",
                SubtitleText = "从嵌片、模组或成长奖励中选一个，然后进入商店。",
                Cards = rewardCards,
                RerollButtonText = $"刷新奖励（-{game?.Config?.shop?.stickerRerollMoney ?? 0}）",
                SkipButtonText = $"跳过并得金币（+{game?.Config?.shop?.stickerSkipMoney ?? 0}）"
            };
        }

        public ShopPanelModel BuildShopPanel(IGameReadModel game)
        {
            shopItemCards.Clear();
            var shops = game?.Shops;
            var mods = game?.Mods;
            var player = game?.Player;
            var config = game?.Config;
            var blockOperationsButtonText = "方块操作";
            var items = shops?.Items ?? Array.Empty<ShopItemEntry>();
            for (var index = 0; index < items.Count; index++)
            {
                shopItemCards.Add(new ShopItemCardModel
                {
                    Index = index,
                    Title = items[index].title,
                    Description = items[index].description,
                    KindText = $"类别：{GetRewardKindText(items[index].kind)}",
                    PriceText = $"价格：{items[index].price}",
                    Purchased = items[index].purchased,
                    ButtonText = items[index].purchased ? "已购买" : "购买"
                });
            }

            if (game is PopHeroGame popHeroGame &&
                popHeroGame.Tables != null &&
                popHeroGame.Tables.TryGetBlockOperationProfile(config?.shop?.blockOperationProfileId, out var profile) &&
                !string.IsNullOrWhiteSpace(profile?.openButtonText))
            {
                blockOperationsButtonText = profile.openButtonText;
            }

            var rerollCost = Mathf.Max(1, (config?.shop?.shopRerollMoney ?? 1) - (mods?.GetShopRerollDiscount() ?? 0));
            return new ShopPanelModel
            {
                TitleText = "商店",
                SubtitleText = "购买嵌片、模组和成长项。需要调整方块构筑时，可进入独立的方块操作面板。",
                Items = shopItemCards,
                LastFeedbackText = shops?.LastFeedback ?? string.Empty,
                GoldText = $"金币：{player?.Gold ?? 0}",
                RerollCostText = $"刷新费用：{rerollCost}",
                BlockOperationsButtonText = blockOperationsButtonText,
                RerollButtonText = $"刷新商店（-{rerollCost}）",
                CloseButtonText = "离开商店"
            };
        }

        public BlockOperationsPanelModel BuildBlockOperationsPanel(IGameReadModel game)
        {
            var profile = game?.BlockOperations?.CurrentProfile;
            var session = game?.BlockOperations?.Session;
            var blocks = game?.BlockCollections;

            blockOperationsPanel.TitleText = string.IsNullOrWhiteSpace(profile?.title) ? "方块操作" : profile.title;
            blockOperationsPanel.SubtitleText = profile?.subtitle ?? string.Empty;
            blockOperationsPanel.HintText = profile?.hintText ?? string.Empty;
            blockOperationsPanel.FeedbackText = session?.lastFeedback ?? string.Empty;
            blockOperationsPanel.ActiveColumnTitle = string.IsNullOrWhiteSpace(profile?.activeColumnTitle) ? "上阵方块" : profile.activeColumnTitle;
            blockOperationsPanel.ReserveColumnTitle = string.IsNullOrWhiteSpace(profile?.reserveColumnTitle) ? "背包方块" : profile.reserveColumnTitle;
            blockOperationsPanel.DeleteStatusText = BuildOperationStatusText("删除", profile?.allowDelete ?? false, profile?.deleteCostGold ?? 0, profile?.maxDeleteCount ?? -1, session?.deleteUsedCount ?? 0);
            blockOperationsPanel.SwapStatusText = BuildOperationStatusText("替换", profile?.allowSwap ?? false, profile?.swapCostGold ?? 0, profile?.maxSwapCount ?? -1, session?.swapUsedCount ?? 0);
            blockOperationsPanel.CloseButtonText = string.IsNullOrWhiteSpace(profile?.closeButtonText) ? "关闭" : profile.closeButtonText;
            blockOperationsPanel.ActiveCards = blocks?.ActiveCardStates ?? Array.Empty<BlockCardState>();
            blockOperationsPanel.ReserveCards = blocks?.ReserveCardStates ?? Array.Empty<BlockCardState>();
            blockOperationsPanel.AllowDelete = profile?.allowDelete ?? false;
            blockOperationsPanel.AllowSwap = profile?.allowSwap ?? false;
            return blockOperationsPanel;
        }

        public LoadoutPanelModel BuildLoadoutPanel(IGameReadModel game)
        {
            var inventory = game?.StickerInventory;
            var mods = game?.Mods;
            return new LoadoutPanelModel
            {
                TitleText = "背包",
                SubtitleText = "从背包里拖拽嵌片，然后点击右侧高亮槽位进行安装。",
                Inventory = inventory?.Stored ?? Array.Empty<StickerInstance>(),
                ActiveMods = mods?.ActiveMods ?? Array.Empty<ModInstance>(),
                ReserveMods = mods?.ReserveMods ?? Array.Empty<ModInstance>(),
                CanCancelDrag = inventory?.DraggingSticker != null,
                CancelDragText = "取消拖拽",
                ContinueButtonText = "继续"
            };
        }

        static string BuildOperationStatusText(string label, bool allowed, int costGold, int maxCount, int usedCount)
        {
            if (!allowed)
                return $"{label}：已禁用";

            var remaining = maxCount < 0 ? "无限" : Mathf.Max(0, maxCount - usedCount).ToString();
            return $"{label}：{costGold} 金币 / 剩余 {remaining}";
        }

        static string GetBlockTypeText(BoardBlockType blockType)
        {
            return blockType switch
            {
                BoardBlockType.AttackAdd => "攻击",
                BoardBlockType.Shield => "防御",
                BoardBlockType.AttackMultiply => "倍率",
                _ => "混合"
            };
        }

        static string GetRarityText(BlockRarity rarity)
        {
            return rarity switch
            {
                BlockRarity.White => "白",
                BlockRarity.Blue => "蓝",
                BlockRarity.Purple => "紫",
                BlockRarity.Gold => "金",
                _ => "白"
            };
        }

        static string GetRewardKindText(ShopItemKind kind)
        {
            return kind switch
            {
                ShopItemKind.Sticker => "嵌片",
                ShopItemKind.Mod => "模组",
                ShopItemKind.Growth => "成长",
                _ => kind.ToString()
            };
        }

        static string BuildMapNodeDescription(MapNodeState node)
        {
            if (node == null)
                return string.Empty;

            var nextText = node.nextNodeIds.Count > 0
                ? $"连向 {node.nextNodeIds.Count} 个后续节点。"
                : "路线终点。";
            return node.kind switch
            {
                MapNodeKind.Rest => $"恢复 30% 最大生命。{nextText}",
                MapNodeKind.Battle => $"进入一场普通战斗。{nextText}",
                MapNodeKind.Shop => $"打开商店，随后进入背包整理。{nextText}",
                MapNodeKind.Workbench => $"免费进行一次方块操作。{nextText}",
                MapNodeKind.Event => $"触发一个轻量路线事件。{nextText}",
                MapNodeKind.Boss => "最终 Boss。击败后完成本条路线。",
                _ => nextText
            };
        }

        static string BuildConnectionsText(IReadOnlyList<MapNodeState> nodes)
        {
            if (nodes == null || nodes.Count == 0)
                return "暂无路线。";

            var text = new System.Text.StringBuilder();
            for (var index = 0; index < nodes.Count; index++)
            {
                var node = nodes[index];
                if (node.nextNodeIds.Count == 0)
                    continue;

                text.Append($"{node.floor + 1}层 {RunMapManager.GetNodeKindName(node.kind)} -> ");
                for (var nextIndex = 0; nextIndex < node.nextNodeIds.Count; nextIndex++)
                {
                    if (nextIndex > 0)
                        text.Append(" / ");
                    text.Append(FindNodeLabel(nodes, node.nextNodeIds[nextIndex]));
                }
                text.AppendLine();
            }

            return text.Length > 0 ? text.ToString() : "暂无后续连线。";
        }

        static string FindNodeLabel(IReadOnlyList<MapNodeState> nodes, string nodeId)
        {
            for (var index = 0; index < nodes.Count; index++)
            {
                if (nodes[index].id == nodeId)
                    return $"{nodes[index].floor + 1}层 {RunMapManager.GetNodeKindName(nodes[index].kind)}";
            }

            return nodeId;
        }

        static string GetMapNodeStatusText(MapNodeStatus status)
        {
            return status switch
            {
                MapNodeStatus.Locked => "未解锁",
                MapNodeStatus.Available => "可进入",
                MapNodeStatus.Current => "处理中",
                MapNodeStatus.Completed => "已完成",
                _ => status.ToString()
            };
        }

        static Color GetMapNodeColor(MapNodeKind kind, MapNodeStatus status)
        {
            if (status == MapNodeStatus.Completed)
                return new Color(0.38f, 0.74f, 0.48f, 1f);
            if (status == MapNodeStatus.Locked)
                return new Color(0.42f, 0.46f, 0.54f, 1f);

            return kind switch
            {
                MapNodeKind.Battle => new Color(0.9f, 0.35f, 0.28f, 1f),
                MapNodeKind.Shop => new Color(0.92f, 0.72f, 0.28f, 1f),
                MapNodeKind.Workbench => new Color(0.36f, 0.62f, 0.95f, 1f),
                MapNodeKind.Rest => new Color(0.32f, 0.78f, 0.58f, 1f),
                MapNodeKind.Event => new Color(0.66f, 0.48f, 0.92f, 1f),
                MapNodeKind.Boss => new Color(1f, 0.22f, 0.32f, 1f),
                _ => Color.white
            };
        }

        static string FormatBlockValue(BlockRewardOption option)
        {
            return option.blockType == BoardBlockType.AttackMultiply
                ? $"x{option.baseValue:0.0#}"
                : $"+{Mathf.RoundToInt(option.baseValue)}";
        }

        static Color GetRarityColor(BlockRarity rarity)
        {
            return rarity switch
            {
                BlockRarity.White => Color.white,
                BlockRarity.Blue => new Color(0.42f, 0.72f, 1f),
                BlockRarity.Purple => new Color(0.78f, 0.46f, 1f),
                BlockRarity.Gold => new Color(1f, 0.82f, 0.34f),
                _ => Color.white
            };
        }
    }
}
