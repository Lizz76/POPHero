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
            return new StatusPanelModel
            {
                StateText = $"状态：{GetStateText(game.State)}",
                AimModeText = $"瞄准：{game.AimModeDisplayText}",
                LevelText = $"等级：{game.Player.Level}",
                KillsText = $"击杀进度：{game.Player.KillsTowardNextLevel} / {(game.Player.IsMaxLevel ? "满级" : game.Player.KillsRequiredForNextLevel.ToString())}",
                BlockText = $"方块：上阵 {game.BlockCollections.ActiveCardCount}/{game.BlockCollections.ActiveCapacity}  仓库 {game.BlockCollections.ReserveCardCount}/{game.BlockCollections.ReserveCapacity}",
                HpText = $"生命：{game.Player.CurrentHp}/{game.Player.MaxHp}",
                ShieldText = $"护盾：{game.Player.CurrentShield}",
                GoldText = $"金币：{game.Player.Gold}",
                InventoryText = $"嵌片库存：{game.StickerInventory.Stored.Count}/{game.Player.StickerInventoryCapacity + game.Mods.GetInventoryCapacityBonus()}",
                LaunchesText = $"可发射数：{game.RemainingLaunchesForEnemy}/{game.MaxLaunchesPerEnemy}",
                EnemyText = $"敌人 #{game.EncounterIndex}：{game.CurrentEnemy?.DisplayName ?? "--"}",
                EnemyHpText = $"敌人生命：{game.CurrentEnemy?.CurrentHp ?? 0}/{game.CurrentEnemy?.MaxHp ?? 0}",
                EnemyAttackText = $"敌人攻击：{game.CurrentEnemy?.AttackDamage ?? 0}"
            };
        }

        static string GetStateText(RoundState state)
        {
            return state switch
            {
                RoundState.Aim => "瞄准",
                RoundState.BallFlying => "弹射中",
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
            return new CombatPanelModel
            {
                RoundAttackText = $"本轮伤害：{game.RoundController.RoundAttackScore}",
                RoundShieldText = $"本轮护盾：{game.RoundController.RoundShieldGain}",
                RoundHitText = $"命中次数：{game.RoundController.RoundHitCount}",
                PreviewText = game.Mods.ShowHitCounter() && game.State == RoundState.Aim
                    ? $"锁定路线预览：总命中 {game.PreviewHitCount}，攻击 {game.PreviewAttackBlockCount}，防御 {game.PreviewShieldBlockCount}，倍率 {game.PreviewMultiplierBlockCount}"
                    : string.Empty,
                IntermissionText = game.IntermissionMessage
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

            for (var index = 0; index < game.BlockCollections.ActiveCapacity; index++)
            {
                activeRows.Add(new BlockRowModel
                {
                    DisplayIndex = index,
                    IsActiveSection = true,
                    Card = index < game.BlockCollections.ActiveCardStates.Count ? game.BlockCollections.ActiveCardStates[index] : null
                });
            }

            for (var index = 0; index < game.BlockCollections.ReserveCapacity; index++)
            {
                reserveRows.Add(new BlockRowModel
                {
                    DisplayIndex = index,
                    IsActiveSection = false,
                    Card = index < game.BlockCollections.ReserveCardStates.Count ? game.BlockCollections.ReserveCardStates[index] : null
                });
            }

            return new BlockManagementPanelModel
            {
                HeaderText = "方块管理",
                HintText = game.CanManageBlockAssignments
                    ? "先点仓库方块图标，再点上阵方块图标即可互换。默认只显示图标，悬停时查看详情。"
                    : "默认只显示紧凑图标。把鼠标放到方块、嵌片或槽位图标上可查看 tooltip。",
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
            var options = game.BlockRewards.ActiveRewardOptions;
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
                    CanSelect = game.IsInitialBlockDraftPending || game.BlockCollections.CanAcceptRewardBlock,
                    SelectButtonText = game.BlockCollections.RewardWillGoToReserve && !game.IsInitialBlockDraftPending ? "放入仓库" : "加入方块组"
                });
            }

            var subtitle = game.IsInitialBlockDraftPending
                ? "先选 1 张方块加入上阵区，然后才开始第一只怪的战斗。"
                : !game.BlockCollections.CanAcceptRewardBlock
                    ? "上阵区与仓库区都已满，本次只能跳过。先在后续中场里删除或替换方块。"
                    : game.BlockCollections.RewardWillGoToReserve
                        ? "上阵区已满，这次拿到的新方块会进入仓库。"
                        : "上阵区未满，选中的方块会直接加入上阵区。";

            return new BlockRewardPanelModel
            {
                TitleText = game.IsInitialBlockDraftPending ? "初始方块三选一" : "新增方块三选一",
                SubtitleText = subtitle,
                Cards = blockRewardCards,
                ShowSkipButton = !game.IsInitialBlockDraftPending,
                SkipButtonText = "跳过，不添加方块"
            };
        }

        public RewardPanelModel BuildRewardPanel(IGameReadModel game)
        {
            rewardCards.Clear();
            var choices = game.RewardChoiceController.ActiveChoices;
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

            return new RewardPanelModel
            {
                TitleText = "奖励阶段：选择一项",
                SubtitleText = "拿到新的嵌片、模组或成长项，然后进入商店。",
                Cards = rewardCards,
                RerollButtonText = $"刷新奖励 (-{game.Config.shop.stickerRerollMoney})",
                SkipButtonText = $"跳过并拿金币 (+{game.Config.shop.stickerSkipMoney})"
            };
        }

        public ShopPanelModel BuildShopPanel(IGameReadModel game)
        {
            shopItemCards.Clear();
            var items = game.Shops.Items;
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

            return new ShopPanelModel
            {
                TitleText = "商店",
                SubtitleText = "可以购买嵌片、模组和成长项，也可以刷新商店；每次进店还可以删除 1 张方块。",
                Items = shopItemCards,
                ActiveCards = game.BlockCollections.ActiveCardStates,
                ReserveCards = game.BlockCollections.ReserveCardStates,
                DeleteHintText = game.Shops.HasRemovedBlockThisVisit
                    ? "本次商店已经删除过 1 张方块。"
                    : $"可花费 {game.Config.shop.blockRemovalCost} 金币删除 1 张方块，至少保留 1 张。",
                LastFeedbackText = game.Shops.LastFeedback,
                GoldText = $"金币：{game.Player.Gold}",
                RerollCostText = $"刷新费用：{Mathf.Max(1, game.Config.shop.shopRerollMoney - game.Mods.GetShopRerollDiscount())}",
                HasRemovedBlockThisVisit = game.Shops.HasRemovedBlockThisVisit,
                RerollButtonText = $"刷新商店 (-{Mathf.Max(1, game.Config.shop.shopRerollMoney - game.Mods.GetShopRerollDiscount())})",
                CloseButtonText = "离开商店"
            };
        }

        public LoadoutPanelModel BuildLoadoutPanel(IGameReadModel game)
        {
            return new LoadoutPanelModel
            {
                TitleText = "中场整理",
                SubtitleText = "点击库存中的嵌片把它附着到鼠标上，再点击右侧方块卡片上的合法槽位安装。",
                Inventory = game.StickerInventory.Stored,
                ActiveMods = game.Mods.ActiveMods,
                ReserveMods = game.Mods.ReserveMods,
                CanCancelDrag = game.StickerInventory.DraggingSticker != null,
                CancelDragText = "取消当前拖拽嵌片",
                ContinueButtonText = "继续战斗"
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
