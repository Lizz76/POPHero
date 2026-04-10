using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
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
        public IReadOnlyList<BlockCardState> ActiveCards;
        public IReadOnlyList<BlockCardState> ReserveCards;
        public string DeleteHintText;
        public string LastFeedbackText;
        public string GoldText;
        public string RerollCostText;
        public bool HasRemovedBlockThisVisit;
        public string RerollButtonText;
        public string CloseButtonText;
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

    public sealed class StatusPanelPresenter
    {
        public StatusPanelModel Build(IGameReadModel game)
        {
            var player = game?.Player;
            var enemy = game?.CurrentEnemy;
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
            var launches = game != null ? $"{game.RemainingLaunchesForEnemy}/{game.MaxLaunchesPerEnemy}" : "--/--";
            var encounter = game != null ? game.EncounterIndex.ToString() : "--";

            return new StatusPanelModel
            {
                StateText = $"状态：{state}",
                AimModeText = $"瞄准模式：{aimMode}",
                LevelText = hasPlayer ? $"等级：{player.Level}" : "等级：-",
                KillsText = hasPlayer
                    ? $"击杀进度：{player.KillsTowardNextLevel} / {(player.IsMaxLevel ? "已满" : player.KillsRequiredForNextLevel.ToString())}"
                    : "击杀进度：- / --",
                BlockText = $"方块组：上阵 {activeCount}/{activeCapacity}  仓库 {reserveCount}/{reserveCapacity}",
                HpText = hasPlayer ? $"生命：{player.CurrentHp}/{player.MaxHp}" : "生命：-/--",
                ShieldText = hasPlayer ? $"护盾：{player.CurrentShield}" : "护盾：-",
                GoldText = hasPlayer ? $"金币：{player.Gold}" : "金币：-",
                InventoryText = $"嵌片库存：{inventoryCount}/{inventoryCapacity}",
                LaunchesText = $"发射次数：{launches}",
                EnemyText = $"敌人 #{encounter}：{enemy?.DisplayName ?? "--"}",
                EnemyHpText = enemy != null ? $"敌人生命：{enemy.CurrentHp}/{enemy.MaxHp}" : "敌人生命：-/--",
                EnemyAttackText = enemy != null ? $"敌人攻击：{enemy.AttackDamage}" : "敌人攻击：-"
            };
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
                RoundState.LoadoutManage => "整理",
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
                HintText = game != null && game.CanManageBlockAssignments
                    ? "先点仓库方块，再点上阵方块即可交换。悬停图标可查看详情。"
                    : "默认显示为紧凑图标。悬停可查看方块、嵌片和槽位详情。",
                ActiveRows = activeRows,
                ReserveRows = reserveRows
            };
        }
    }

    public sealed class IntermissionPanelPresenter
    {
        readonly List<BlockRewardCardModel> blockRewardCards = new();
        readonly List<RewardCardModel> rewardCards = new();
        readonly List<ShopItemCardModel> shopItemCards = new();

        public BlockRewardPanelModel BuildBlockReward(IGameReadModel game)
        {
            blockRewardCards.Clear();
            var blockRewards = game?.BlockRewards;
            var blockCollections = game?.BlockCollections;
            var options = blockRewards?.ActiveRewardOptions ?? System.Array.Empty<BlockRewardOption>();
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
                    SelectButtonText = blockCollections?.RewardWillGoToReserve == true && game != null && !game.IsInitialBlockDraftPending ? "放入仓库" : "加入方块组"
                });
            }

            var subtitle = game != null && game.IsInitialBlockDraftPending
                ? "在第一场战斗开始前，先选择你的起始方块。"
                : blockCollections?.CanAcceptRewardBlock == false
                    ? "上阵和仓库都已满。请先跳过这次奖励，之后再腾出空间。"
                    : blockCollections?.RewardWillGoToReserve == true
                        ? "上阵已满，选中的方块将自动进入仓库。"
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
            var blocks = game?.BlockCollections;
            var mods = game?.Mods;
            var player = game?.Player;
            var config = game?.Config;
            var items = shops?.Items ?? System.Array.Empty<ShopItemEntry>();
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

            var rerollCost = Mathf.Max(1, (config?.shop?.shopRerollMoney ?? 1) - (mods?.GetShopRerollDiscount() ?? 0));
            return new ShopPanelModel
            {
                TitleText = "商店",
                SubtitleText = "购买嵌片、模组和成长项。每次进店还能刷新商品，并删除一张方块。",
                Items = shopItemCards,
                ActiveCards = blocks?.ActiveCardStates ?? System.Array.Empty<BlockCardState>(),
                ReserveCards = blocks?.ReserveCardStates ?? System.Array.Empty<BlockCardState>(),
                DeleteHintText = shops?.HasRemovedBlockThisVisit == true
                    ? "本次进店已经删除过一张方块。"
                    : $"花费 {config?.shop?.blockRemovalCost ?? 0} 金币可删除一张方块。至少要保留一张上阵方块。",
                LastFeedbackText = shops?.LastFeedback ?? string.Empty,
                GoldText = $"金币：{player?.Gold ?? 0}",
                RerollCostText = $"刷新费用：{rerollCost}",
                HasRemovedBlockThisVisit = shops?.HasRemovedBlockThisVisit ?? false,
                RerollButtonText = $"刷新商店（-{rerollCost}）",
                CloseButtonText = "离开商店"
            };
        }

        public LoadoutPanelModel BuildLoadoutPanel(IGameReadModel game)
        {
            var inventory = game?.StickerInventory;
            var mods = game?.Mods;
            return new LoadoutPanelModel
            {
                TitleText = "整理配置",
                SubtitleText = "从库存拿起嵌片，然后点击右侧可用槽位进行安装。",
                Inventory = inventory?.Stored ?? System.Array.Empty<StickerInstance>(),
                ActiveMods = mods?.ActiveMods ?? System.Array.Empty<ModInstance>(),
                ReserveMods = mods?.ReserveMods ?? System.Array.Empty<ModInstance>(),
                CanCancelDrag = inventory?.DraggingSticker != null,
                CancelDragText = "取消拖拽",
                ContinueButtonText = "继续"
            };
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
