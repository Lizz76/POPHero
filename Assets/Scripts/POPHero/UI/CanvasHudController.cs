using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace POPHero
{
    public class CanvasHudController : MonoBehaviour
    {
        PopHeroGame game;
        IHudCommandSink sink;
        Canvas canvas;

        readonly StatusPanelPresenter statusPresenter = new();
        readonly CombatPanelPresenter combatPresenter = new();
        readonly BlockManagementPresenter blockPresenter = new();
        readonly IntermissionPanelPresenter intermissionPresenter = new();

        TMP_Text statusTitle;
        TMP_Text statusState;
        TMP_Text statusAimMode;
        TMP_Text statusLevel;
        TMP_Text statusKills;
        TMP_Text statusBlocks;
        TMP_Text statusHp;
        TMP_Text statusShield;
        TMP_Text statusGold;
        TMP_Text statusInventory;
        TMP_Text statusLaunches;
        TMP_Text statusEnemy;
        TMP_Text statusEnemyHp;
        TMP_Text statusEnemyAtk;

        TMP_Text combatTitle;
        TMP_Text combatAttack;
        TMP_Text combatShield;
        TMP_Text combatHits;
        TMP_Text combatPreview;
        TMP_Text combatMessage;

        TMP_Text blockHeader;
        TMP_Text blockHint;
        TMP_Text activeTitle;
        TMP_Text reserveTitle;
        TMP_Text damageLabel;
        TMP_Text damageValue;

        TMP_Text tooltipTitle;
        TMP_Text tooltipBody;
        TMP_Text dragName;
        TMP_Text dragMask;
        TMP_Text dragHint;

        TMP_Text blockRewardTitle;
        TMP_Text blockRewardSubtitle;
        TMP_Text rewardTitle;
        TMP_Text rewardSubtitle;
        TMP_Text shopTitle;
        TMP_Text shopSubtitle;
        TMP_Text shopGold;
        TMP_Text shopFeedback;
        TMP_Text shopDeleteActive;
        TMP_Text shopDeleteReserve;
        TMP_Text shopDeleteHint;
        TMP_Text loadoutTitle;
        TMP_Text loadoutSubtitle;
        TMP_Text activeModsTitle;
        TMP_Text reserveModsTitle;
        TMP_Text gameOverTitle;
        TMP_Text gameOverMessage;

        Button toggleAimButton;
        Button shuffleButton;
        Button addGoldButton;
        Button killEnemyButton;
        Button damagePlayerButton;
        Button blockRewardSkipButton;
        Button rewardRerollButton;
        Button rewardSkipButton;
        Button shopRerollButton;
        Button shopCloseButton;
        Button loadoutCancelButton;
        Button loadoutContinueButton;
        Button gameOverRetryButton;
        Button gameOverMenuButton;

        RectTransform activeRowsRoot;
        RectTransform reserveRowsRoot;
        RectTransform damagePanel;
        RectTransform tooltipPanel;
        RectTransform dragPanel;
        RectTransform blockRewardContent;
        RectTransform rewardContent;
        RectTransform shopItemsContent;
        RectTransform shopDeleteActiveContent;
        RectTransform shopDeleteReserveContent;
        RectTransform inventoryContent;
        RectTransform activeModsContent;
        RectTransform reserveModsContent;

        GameObject blockManagementPanel;
        GameObject blockRewardModal;
        GameObject rewardModal;
        GameObject shopModal;
        GameObject loadoutModal;
        GameObject gameOverModal;

        Canvas blockManagementCanvas;
        GraphicRaycaster blockManagementRaycaster;
        bool blockManagementCanvasSettingsCaptured;
        bool blockManagementDefaultOverrideSorting;
        int blockManagementDefaultSortingOrder;
        int blockManagementDefaultSortingLayerId;

        readonly List<CanvasBlockRowView> activeRows = new();
        readonly List<CanvasBlockRowView> reserveRows = new();
        readonly List<CanvasCardView> blockRewardCards = new();
        readonly List<CanvasCardView> rewardCards = new();
        readonly List<CanvasCardView> shopCards = new();
        readonly List<CanvasListEntryView> deleteActiveEntries = new();
        readonly List<CanvasListEntryView> deleteReserveEntries = new();
        readonly List<CanvasListEntryView> inventoryEntries = new();
        readonly List<CanvasListEntryView> activeModEntries = new();
        readonly List<CanvasListEntryView> reserveModEntries = new();

        string tooltipTitleValue;
        string tooltipBodyValue;
        Color tooltipColor = Color.white;
        string selectedActiveId;
        string selectedReserveId;
        bool initialized;

        public void Initialize(PopHeroGame owner)
        {
            game = owner;
            sink = owner;
            canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
            if (canvas == null)
                Debug.LogError("[POPHero] CanvasHudController could not find a parent Canvas in Battle scene.");
            BindScene();
            ApplyRuntimeFont();
            ValidateBindings();
            BindButtons();
            initialized = true;
            gameObject.SetActive(true);
            ForceRefresh();
        }

        void ForceRefresh()
        {
            if (!initialized || game == null)
                return;

            SafeRefresh("status", RefreshStatus);
            SafeRefresh("combat", RefreshCombat);
            SafeRefresh("blocks", RefreshBlocks);
            SafeRefresh("damage", RefreshDamage);
            SafeRefresh("modals", RefreshModals);
            SafeRefresh("layers", RefreshInteractionLayers);
            SafeRefresh("drag", RefreshDragPanel);
            SafeRefresh("tooltip", RefreshTooltip);
        }

        void ApplyRuntimeFont()
        {
            var font = PrototypeVisualFactory.GetCjkTmpFontAsset();
            if (font == null)
                return;

            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text != null)
                    text.font = font;
            }
        }

        void LateUpdate()
        {
            if (!initialized || game == null)
                return;

            if (!game.CanManageBlockAssignments)
            {
                selectedActiveId = null;
                selectedReserveId = null;
            }

            SafeRefresh("status", RefreshStatus);
            SafeRefresh("combat", RefreshCombat);
            SafeRefresh("blocks", RefreshBlocks);
            SafeRefresh("damage", RefreshDamage);
            SafeRefresh("modals", RefreshModals);
            SafeRefresh("layers", RefreshInteractionLayers);
            SafeRefresh("drag", RefreshDragPanel);
            SafeRefresh("tooltip", RefreshTooltip);
        }

        void SafeRefresh(string area, Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[POPHero] Canvas HUD refresh failed in {area}: {ex.Message}");
            }
        }

        public void SetTooltip(string title, string body, Color color)
        {
            tooltipTitleValue = title ?? string.Empty;
            tooltipBodyValue = body ?? string.Empty;
            tooltipColor = color;
        }

        public void ClearTooltip()
        {
            tooltipTitleValue = string.Empty;
            tooltipBodyValue = string.Empty;
        }

        void BindScene()
        {
            statusTitle = T("HudRoot/StatusPanel/TitleText");
            statusState = T("HudRoot/StatusPanel/StateText");
            statusAimMode = T("HudRoot/StatusPanel/AimModeText");
            statusLevel = T("HudRoot/StatusPanel/LevelText");
            statusKills = T("HudRoot/StatusPanel/KillsText");
            statusBlocks = T("HudRoot/StatusPanel/BlockText");
            statusHp = T("HudRoot/StatusPanel/HpText");
            statusShield = T("HudRoot/StatusPanel/ShieldText");
            statusGold = T("HudRoot/StatusPanel/GoldText");
            statusInventory = T("HudRoot/StatusPanel/InventoryText");
            statusLaunches = T("HudRoot/StatusPanel/LaunchesText");
            statusEnemy = T("HudRoot/StatusPanel/EnemyText");
            statusEnemyHp = T("HudRoot/StatusPanel/EnemyHpText");
            statusEnemyAtk = T("HudRoot/StatusPanel/EnemyAttackText");

            combatTitle = T("HudRoot/CombatPanel/TitleText");
            combatAttack = T("HudRoot/CombatPanel/RoundAttackText");
            combatShield = T("HudRoot/CombatPanel/RoundShieldText");
            combatHits = T("HudRoot/CombatPanel/RoundHitText");
            combatPreview = T("HudRoot/CombatPanel/PreviewText");
            combatMessage = T("HudRoot/CombatPanel/MessageText");

            toggleAimButton = B("HudRoot/CombatPanel/Buttons/ToggleAimButton");
            shuffleButton = B("HudRoot/CombatPanel/Buttons/ShuffleButton");
            addGoldButton = B("HudRoot/CombatPanel/Buttons/AddGoldButton");
            killEnemyButton = B("HudRoot/CombatPanel/Buttons/KillEnemyButton");
            damagePlayerButton = B("HudRoot/CombatPanel/Buttons/DamagePlayerButton");

            blockManagementPanel = G("HudRoot/BlockManagementPanel");
            blockHeader = T("HudRoot/BlockManagementPanel/HeaderText");
            blockHint = T("HudRoot/BlockManagementPanel/HintText");
            activeTitle = T("HudRoot/BlockManagementPanel/ActiveSection/TitleText");
            reserveTitle = T("HudRoot/BlockManagementPanel/ReserveSection/TitleText");
            activeRowsRoot = R("HudRoot/BlockManagementPanel/ActiveSection/ScrollView/Viewport/Rows");
            reserveRowsRoot = R("HudRoot/BlockManagementPanel/ReserveSection/ScrollView/Viewport/Rows");

            damagePanel = R("HudRoot/DamageCounterPanel");
            damageLabel = T("HudRoot/DamageCounterPanel/LabelText");
            damageValue = T("HudRoot/DamageCounterPanel/ValueText");

            tooltipPanel = R("TooltipRoot/TooltipPanel");
            tooltipTitle = T("TooltipRoot/TooltipPanel/TitleText");
            tooltipBody = T("TooltipRoot/TooltipPanel/BodyText");
            dragPanel = R("TooltipRoot/DragStickerPanel");
            dragName = T("TooltipRoot/DragStickerPanel/NameText");
            dragMask = T("TooltipRoot/DragStickerPanel/MaskText");
            dragHint = T("TooltipRoot/DragStickerPanel/HintText");

            blockRewardModal = G("ModalRoot/BlockRewardModal");
            blockRewardTitle = T("ModalRoot/BlockRewardModal/Window/Header/TitleText");
            blockRewardSubtitle = T("ModalRoot/BlockRewardModal/Window/Header/SubtitleText");
            blockRewardContent = R("ModalRoot/BlockRewardModal/Window/Body/ScrollView/Viewport/Content");
            blockRewardSkipButton = B("ModalRoot/BlockRewardModal/Window/Footer/SkipButton");

            rewardModal = G("ModalRoot/RewardModal");
            rewardTitle = T("ModalRoot/RewardModal/Window/Header/TitleText");
            rewardSubtitle = T("ModalRoot/RewardModal/Window/Header/SubtitleText");
            rewardContent = R("ModalRoot/RewardModal/Window/Body/ScrollView/Viewport/Content");
            rewardRerollButton = B("ModalRoot/RewardModal/Window/Footer/RerollButton");
            rewardSkipButton = B("ModalRoot/RewardModal/Window/Footer/SkipButton");

            shopModal = G("ModalRoot/ShopModal");
            shopTitle = T("ModalRoot/ShopModal/Window/Header/TitleText");
            shopSubtitle = T("ModalRoot/ShopModal/Window/Header/SubtitleText");
            shopGold = T("ModalRoot/ShopModal/Window/Header/GoldText");
            shopFeedback = T("ModalRoot/ShopModal/Window/Header/FeedbackText");
            shopItemsContent = R("ModalRoot/ShopModal/Window/Body/ItemsPanel/ItemsScroll/Viewport/Content");
            shopDeleteActiveContent =
                ROptional("ModalRoot/ShopModal/Window/Body/DeletePanel/Columns/ActiveColumn/ActiveScroll/Viewport/ActiveContent") ??
                R("ModalRoot/ShopModal/Window/Body/DeletePanel/Columns/ActiveColumn/ActiveContent");
            shopDeleteReserveContent =
                ROptional("ModalRoot/ShopModal/Window/Body/DeletePanel/Columns/ReserveColumn/ReserveScroll/Viewport/ReserveContent") ??
                R("ModalRoot/ShopModal/Window/Body/DeletePanel/Columns/ReserveColumn/ReserveContent");
            shopDeleteActive = T("ModalRoot/ShopModal/Window/Body/DeletePanel/Titles/ActiveTitleText");
            shopDeleteReserve = T("ModalRoot/ShopModal/Window/Body/DeletePanel/Titles/ReserveTitleText");
            shopDeleteHint = T("ModalRoot/ShopModal/Window/Body/DeletePanel/HintText");
            shopRerollButton = B("ModalRoot/ShopModal/Window/Footer/RerollButton");
            shopCloseButton = B("ModalRoot/ShopModal/Window/Footer/CloseButton");

            loadoutModal = G("ModalRoot/LoadoutModal");
            loadoutTitle = T("ModalRoot/LoadoutModal/Window/Header/TitleText");
            loadoutSubtitle = T("ModalRoot/LoadoutModal/Window/Header/SubtitleText");
            inventoryContent = R("ModalRoot/LoadoutModal/Window/Body/Columns/InventoryPanel/ScrollView/Viewport/Content");
            activeModsTitle = T("ModalRoot/LoadoutModal/Window/Body/Columns/ModsPanel/ActiveTitleText");
            reserveModsTitle = T("ModalRoot/LoadoutModal/Window/Body/Columns/ModsPanel/ReserveTitleText");
            activeModsContent = R("ModalRoot/LoadoutModal/Window/Body/Columns/ModsPanel/ActiveContent");
            reserveModsContent = R("ModalRoot/LoadoutModal/Window/Body/Columns/ModsPanel/ReserveContent");
            loadoutCancelButton = B("ModalRoot/LoadoutModal/Window/Footer/CancelButton");
            loadoutContinueButton = B("ModalRoot/LoadoutModal/Window/Footer/ContinueButton");

            gameOverModal = G("ModalRoot/GameOverModal");
            gameOverTitle = T("ModalRoot/GameOverModal/Window/Header/TitleText");
            gameOverMessage = T("ModalRoot/GameOverModal/Window/Body/MessageText");
            gameOverRetryButton = B("ModalRoot/GameOverModal/Window/Footer/RetryButton");
            gameOverMenuButton = B("ModalRoot/GameOverModal/Window/Footer/MenuButton");
        }

        void ValidateBindings()
        {
            Validate(statusTitle, "HudRoot/StatusPanel/TitleText");
            Validate(combatTitle, "HudRoot/CombatPanel/TitleText");
            Validate(blockManagementPanel, "HudRoot/BlockManagementPanel");
            Validate(activeRowsRoot, "HudRoot/BlockManagementPanel/ActiveSection/Rows");
            Validate(reserveRowsRoot, "HudRoot/BlockManagementPanel/ReserveSection/Rows");
            Validate(blockRewardModal, "ModalRoot/BlockRewardModal");
            Validate(rewardModal, "ModalRoot/RewardModal");
            Validate(shopModal, "ModalRoot/ShopModal");
            Validate(loadoutModal, "ModalRoot/LoadoutModal");
            Validate(gameOverModal, "ModalRoot/GameOverModal");
        }

        void BindButtons()
        {
            Bind(toggleAimButton, () => Run(new HudCommand(HudCommandType.ToggleAimMode)));
            Bind(shuffleButton, () => Run(new HudCommand(HudCommandType.DebugShuffleBoard)));
            Bind(addGoldButton, () => Run(new HudCommand(HudCommandType.DebugAddGold)));
            Bind(killEnemyButton, () => Run(new HudCommand(HudCommandType.DebugKillEnemy)));
            Bind(damagePlayerButton, () => Run(new HudCommand(HudCommandType.DebugDamagePlayer)));
            Bind(blockRewardSkipButton, () => Run(new HudCommand(HudCommandType.SkipBlockReward)));
            Bind(rewardRerollButton, () => Run(new HudCommand(HudCommandType.TryRerollRewardChoices)));
            Bind(rewardSkipButton, () => Run(new HudCommand(HudCommandType.SkipRewardChoices)));
            Bind(shopRerollButton, () => Run(new HudCommand(HudCommandType.TryRerollShop)));
            Bind(shopCloseButton, () => Run(new HudCommand(HudCommandType.CloseShop)));
            Bind(loadoutCancelButton, () => Run(new HudCommand(HudCommandType.CancelStickerDrag)));
            Bind(loadoutContinueButton, () => Run(new HudCommand(HudCommandType.FinishLoadout)));
            Bind(gameOverRetryButton, () => SceneFlowService.Instance.ReloadBattle());
            Bind(gameOverMenuButton, () => SceneFlowService.Instance.LoadMainMenu());
        }

        void RefreshStatus()
        {
            var model = statusPresenter.Build(game);
            Set(statusTitle, "战斗状态");
            Set(statusState, model.StateText);
            Set(statusAimMode, model.AimModeText);
            Set(statusLevel, model.LevelText);
            Set(statusKills, model.KillsText);
            Set(statusBlocks, model.BlockText);
            Set(statusHp, model.HpText);
            Set(statusShield, model.ShieldText);
            Set(statusGold, model.GoldText);
            Set(statusInventory, model.InventoryText);
            Set(statusLaunches, model.LaunchesText);
            Set(statusEnemy, model.EnemyText);
            Set(statusEnemyHp, model.EnemyHpText);
            Set(statusEnemyAtk, model.EnemyAttackText);
        }

        void RefreshCombat()
        {
            var model = combatPresenter.Build(game);
            Set(combatTitle, "战斗信息");
            Set(combatAttack, model.RoundAttackText);
            Set(combatShield, model.RoundShieldText);
            Set(combatHits, model.RoundHitText);
            Set(combatPreview, model.PreviewText);
            Set(combatMessage, model.IntermissionText);
            SetButtonLabel(toggleAimButton, "切换瞄准");
            SetButtonLabel(shuffleButton, "重排方块");
            SetButtonLabel(addGoldButton, "金币 +25");
            SetButtonLabel(killEnemyButton, "秒杀敌人");
            SetButtonLabel(damagePlayerButton, "主角 -10 血");
        }

        void RefreshBlocks()
        {
            var model = blockPresenter.Build(game);
            var blockCollections = game?.BlockCollections;
            Set(blockHeader, model.HeaderText);
            Set(blockHint, BuildBlockHint(model.HintText));
            Set(activeTitle, $"上阵 {blockCollections?.ActiveCardCount ?? 0}/{blockCollections?.ActiveCapacity ?? 0}");
            Set(reserveTitle, $"仓库 {blockCollections?.ReserveCardCount ?? 0}/{blockCollections?.ReserveCapacity ?? 0}");

            EnsureRows(activeRows, activeRowsRoot, model.ActiveRows.Count);
            for (var index = 0; index < model.ActiveRows.Count; index++)
                ConfigureRow(activeRows[index], model.ActiveRows[index], true);

            EnsureRows(reserveRows, reserveRowsRoot, model.ReserveRows.Count);
            for (var index = 0; index < model.ReserveRows.Count; index++)
                ConfigureRow(reserveRows[index], model.ReserveRows[index], false);
        }

        void RefreshDamage()
        {
            var visible = game != null && game.State == RoundState.BallFlying;
            SetActive(damagePanel, visible);
            if (!visible)
                return;

            Set(damageLabel, "伤害");
            Set(damageValue, Mathf.Max(0, game?.RoundController?.PendingDamage ?? 0).ToString());
        }

        void RefreshModals()
        {
            var state = game.State;
            SetActive(blockRewardModal, state == RoundState.BlockRewardChoose);
            SetActive(rewardModal, state == RoundState.RewardChoose);
            SetActive(shopModal, state == RoundState.Shop);
            SetActive(loadoutModal, state == RoundState.LoadoutManage);
            SetActive(gameOverModal, state == RoundState.GameOver);

            if (state == RoundState.BlockRewardChoose)
                RefreshBlockReward();
            else
                HideExtra(blockRewardCards, 0);

            if (state == RoundState.RewardChoose)
                RefreshReward();
            else
                HideExtra(rewardCards, 0);

            if (state == RoundState.Shop)
                RefreshShop();
            else
            {
                HideExtra(shopCards, 0);
                HideExtra(deleteActiveEntries, 0);
                HideExtra(deleteReserveEntries, 0);
            }

            if (state == RoundState.LoadoutManage)
                RefreshLoadout();
            else
            {
                HideExtra(inventoryEntries, 0);
                HideExtra(activeModEntries, 0);
                HideExtra(reserveModEntries, 0);
            }

            if (state == RoundState.GameOver)
                RefreshGameOver();
        }

        void RefreshInteractionLayers()
        {
            if (game == null || blockManagementPanel == null)
                return;

            EnsureBlockManagementCanvas();
            if (blockManagementCanvas == null)
                return;

            if (game.State == RoundState.LoadoutManage)
            {
                blockManagementCanvas.overrideSorting = true;
                if (canvas != null)
                    blockManagementCanvas.sortingLayerID = canvas.sortingLayerID;
                blockManagementCanvas.sortingOrder = (canvas != null ? canvas.sortingOrder : blockManagementDefaultSortingOrder) + 20;
            }
            else if (blockManagementCanvasSettingsCaptured)
            {
                blockManagementCanvas.overrideSorting = blockManagementDefaultOverrideSorting;
                blockManagementCanvas.sortingLayerID = blockManagementDefaultSortingLayerId;
                blockManagementCanvas.sortingOrder = blockManagementDefaultSortingOrder;
            }

            if (blockManagementRaycaster != null)
                blockManagementRaycaster.enabled = true;
        }

        void RefreshDragPanel()
        {
            var dragging = game?.StickerInventory?.DraggingSticker;
            var visible = dragging != null && game != null && game.CanManageBlockAssignments;
            SetActive(dragPanel, visible);
            if (!visible)
                return;

            Set(dragName, dragging.data.name);
            Set(dragMask, $"适用：{MaskIcon(dragging.data.targetBlockType)}  {MaskText(dragging.data.targetBlockType)}");
            Set(dragHint, "点击高亮槽位安装；点击已装槽位可卸下。");
            Position(dragPanel, Input.mousePosition, 18f, 18f);
        }

        void RefreshTooltip()
        {
            var visible = !string.IsNullOrWhiteSpace(tooltipTitleValue);
            SetActive(tooltipPanel, visible);
            if (!visible)
                return;

            Set(tooltipTitle, tooltipTitleValue);
            Set(tooltipBody, tooltipBodyValue);
            if (tooltipTitle != null)
                tooltipTitle.color = tooltipColor;
            Position(tooltipPanel, Input.mousePosition, 18f, 18f);
        }

        void RefreshBlockReward()
        {
            var model = intermissionPresenter.BuildBlockReward(game);
            Set(blockRewardTitle, model.TitleText);
            Set(blockRewardSubtitle, model.SubtitleText);
            SetButtonLabel(blockRewardSkipButton, model.SkipButtonText);
            SetActive(blockRewardSkipButton, model.ShowSkipButton);

            EnsureCards(blockRewardCards, blockRewardContent, model.Cards.Count);
            for (var index = 0; index < model.Cards.Count; index++)
            {
                var card = model.Cards[index];
                var view = blockRewardCards[index];
                view.gameObject.SetActive(true);
                view.Set(card.DisplayName, card.TypeText, $"{card.RarityText}  {card.ValueText}", card.Description, card.SelectButtonText, card.AccentColor);
                view.SetInteractable(card.CanSelect);
                var capturedIndex = card.Index;
                view.SetAction(() => Run(new HudCommand(HudCommandType.TrySelectBlockReward, capturedIndex)));
            }
        }

        void RefreshReward()
        {
            var model = intermissionPresenter.BuildRewardPanel(game);
            Set(rewardTitle, model.TitleText);
            Set(rewardSubtitle, model.SubtitleText);
            SetButtonLabel(rewardRerollButton, model.RerollButtonText);
            SetButtonLabel(rewardSkipButton, model.SkipButtonText);

            EnsureCards(rewardCards, rewardContent, model.Cards.Count);
            for (var index = 0; index < model.Cards.Count; index++)
            {
                var card = model.Cards[index];
                var view = rewardCards[index];
                view.gameObject.SetActive(true);
                view.Set(card.Title, card.KindText, string.Empty, card.Description, "选择", RewardKindColor(card.KindText));
                var capturedIndex = card.Index;
                view.SetInteractable(true);
                view.SetAction(() => Run(new HudCommand(HudCommandType.TrySelectReward, capturedIndex)));
            }
        }

        void RefreshShop()
        {
            var model = intermissionPresenter.BuildShopPanel(game);
            Set(shopTitle, model.TitleText);
            Set(shopSubtitle, model.SubtitleText);
            Set(shopGold, model.GoldText);
            Set(shopFeedback, model.LastFeedbackText);
            Set(shopDeleteActive, "删除：上阵");
            Set(shopDeleteReserve, "删除：仓库");
            Set(shopDeleteHint, model.DeleteHintText);
            SetButtonLabel(shopRerollButton, model.RerollButtonText);
            SetButtonLabel(shopCloseButton, model.CloseButtonText);

            EnsureCards(shopCards, shopItemsContent, model.Items.Count);
            for (var index = 0; index < model.Items.Count; index++)
            {
                var item = model.Items[index];
                var view = shopCards[index];
                view.gameObject.SetActive(true);
                view.Set(item.Title, item.KindText, item.PriceText, item.Description, item.ButtonText, RewardKindColor(item.KindText));
                view.SetInteractable(!item.Purchased);
                var capturedIndex = item.Index;
                view.SetAction(() => Run(new HudCommand(HudCommandType.TryBuyShopItem, capturedIndex)));
            }

            RefreshRemoval(deleteActiveEntries, shopDeleteActiveContent, model.ActiveCards, model.HasRemovedBlockThisVisit);
            RefreshRemoval(deleteReserveEntries, shopDeleteReserveContent, model.ReserveCards, model.HasRemovedBlockThisVisit);
        }

        void RefreshLoadout()
        {
            var model = intermissionPresenter.BuildLoadoutPanel(game);
            Set(loadoutTitle, model.TitleText);
            Set(loadoutSubtitle, BuildLoadoutSubtitle(model.SubtitleText));
            Set(activeModsTitle, "启用模组");
            Set(reserveModsTitle, "待机模组");
            SetButtonLabel(loadoutCancelButton, model.CancelDragText);
            SetButtonLabel(loadoutContinueButton, model.ContinueButtonText);
            if (loadoutCancelButton != null)
                loadoutCancelButton.interactable = model.CanCancelDrag;

            EnsureEntries(inventoryEntries, inventoryContent, model.Inventory.Count);
            for (var index = 0; index < model.Inventory.Count; index++)
            {
                var sticker = model.Inventory[index];
                var view = inventoryEntries[index];
                view.gameObject.SetActive(true);
                view.Set(sticker.data.name, MaskIcon(sticker.data.targetBlockType), sticker.data.mainActionText, StickerColor(sticker.data.rarity), () => Run(new HudCommand(HudCommandType.BeginStickerDrag, 0, sticker.runtimeId)));
                view.SetInteractable(true);
                view.SetTooltip(sticker.data.name, InventoryStickerTooltip(sticker), StickerColor(sticker.data.rarity), this);
            }

            RefreshMods(activeModEntries, activeModsContent, model.ActiveMods, true);
            RefreshMods(reserveModEntries, reserveModsContent, model.ReserveMods, false);
        }

        void RefreshGameOver()
        {
            Set(gameOverTitle, "本局结束");
            Set(gameOverMessage, game?.GameOverMessage ?? "本局结束。");
            SetButtonLabel(gameOverRetryButton, "再来一局");
            SetButtonLabel(gameOverMenuButton, "返回主菜单");
        }

        void RefreshRemoval(List<CanvasListEntryView> views, RectTransform root, IReadOnlyList<BlockCardState> cards, bool disabled)
        {
            EnsureEntries(views, root, cards.Count);
            for (var index = 0; index < cards.Count; index++)
            {
                var card = cards[index];
                var view = views[index];
                view.gameObject.SetActive(true);
                view.Set(card.cardName, BlockIcon(card.baseBlockType), Format(card), BlockColor(card), () => Run(new HudCommand(HudCommandType.TryRemoveBlockInShop, 0, card.id)));
                view.SetInteractable(!disabled);
                view.SetTooltip(card.cardName, BlockTooltip(card), RarityColor(card.rarity), this);
            }
        }

        void RefreshMods(List<CanvasListEntryView> views, RectTransform root, IReadOnlyList<ModInstance> mods, bool active)
        {
            EnsureEntries(views, root, mods.Count);
            for (var index = 0; index < mods.Count; index++)
            {
                var mod = mods[index];
                var view = views[index];
                view.gameObject.SetActive(true);
                view.Set(mod.data.name, ModCategoryText(mod.data.category), active ? "点击停用" : "点击启用", RewardKindColor("模组"), () => Run(new HudCommand(HudCommandType.ToggleModActivation, 0, mod.runtimeId)));
                view.SetInteractable(true);
                view.SetTooltip(mod.data.name, mod.data.description, RewardKindColor("模组"), this);
            }
        }

        void ConfigureRow(CanvasBlockRowView view, BlockRowModel model, bool activeSection)
        {
            view.SetIndex(model.DisplayIndex + 1);
            view.SetSelection(activeSection
                ? selectedActiveId == model.Card?.id
                : selectedReserveId == model.Card?.id);

            if (model.Card == null)
            {
                view.SetType("-", new Color(0.24f, 0.26f, 0.3f, 0.8f), null);
                view.SetTypeTooltip("空槽位", activeSection ? "这里还没有上阵方块。" : "这里还没有仓库方块。", new Color(0.72f, 0.76f, 0.84f, 1f), this);
                view.SetStickerCount(0);
                view.SetSocketCount(0);
                return;
            }

            view.SetType(BlockIcon(model.Card.baseBlockType), BlockColor(model.Card), () => ClickBlock(model.Card, activeSection));
            view.SetTypeTooltip(model.Card.cardName, BlockTooltip(model.Card), RarityColor(model.Card.rarity), this);

            var stickers = new List<StickerInstance>();
            foreach (var socket in model.Card.sockets)
            {
                if (socket.installedSticker != null)
                    stickers.Add(socket.installedSticker);
            }

            view.SetStickerCount(stickers.Count);
            for (var index = 0; index < stickers.Count; index++)
            {
                view.SetSticker(index, StickerShort(stickers[index]), StickerColor(stickers[index].data.rarity));
                view.SetStickerTooltip(index, stickers[index].data.name, StickerTooltip(stickers[index]), StickerColor(stickers[index].data.rarity), this);
            }

            view.SetSocketCount(model.Card.sockets.Count);
            for (var index = 0; index < model.Card.sockets.Count; index++)
            {
                var socket = model.Card.sockets[index];
                var icon = socket.isUnlocked
                    ? socket.installedSticker != null ? StickerShort(socket.installedSticker) : "+"
                    : "L";
                var color = socket.isUnlocked
                    ? socket.installedSticker != null
                        ? StickerColor(socket.installedSticker.data.rarity)
                        : (CanInstall(model.Card, socket) ? new Color(0.42f, 0.86f, 0.54f, 1f) : new Color(0.32f, 0.36f, 0.44f, 1f))
                    : new Color(0.22f, 0.24f, 0.28f, 1f);
                var capturedIndex = index;
                view.SetSocket(index, icon, color, () => ClickSocket(model.Card, capturedIndex));
                view.SetSocketTooltip(index, socket.installedSticker != null ? socket.installedSticker.data.name : "槽位", SocketTooltip(model.Card, socket), color, this);
            }
        }

        bool CanInstall(BlockCardState card, SocketSlotState socket)
        {
            var dragging = game?.StickerInventory?.DraggingSticker;
            if (dragging == null || card == null || socket == null)
                return false;
            if (!socket.isUnlocked || socket.installedSticker != null || !game.CanManageBlockAssignments)
                return false;

            var cardMask = CardMask(card.baseBlockType);
            return (socket.targetMask & cardMask) != 0 && (dragging.data.targetBlockType & cardMask) != 0;
        }

        void ClickSocket(BlockCardState card, int socketIndex)
        {
            if (card == null || !game.CanManageBlockAssignments)
                return;

            if (socketIndex < 0 || socketIndex >= card.sockets.Count)
                return;

            var socket = card.sockets[socketIndex];
            if (game.StickerInventory.DraggingSticker != null)
            {
                Run(new HudCommand(HudCommandType.TryInstallDraggedSticker, socketIndex, card.id));
                return;
            }

            if (socket.installedSticker != null)
                Run(new HudCommand(HudCommandType.RemoveStickerFromCard, socketIndex, card.id));
        }

        void ClickBlock(BlockCardState card, bool activeSection)
        {
            if (card == null)
                return;

            if (!game.CanManageBlockAssignments)
            {
                SetTooltip(card.cardName, BlockTooltip(card), RarityColor(card.rarity));
                return;
            }

            if (activeSection)
            {
                if (!string.IsNullOrEmpty(selectedReserveId))
                {
                    Run(new HudCommand(HudCommandType.TrySwapActiveReserve, 0, card.id, selectedReserveId));
                    selectedActiveId = null;
                    selectedReserveId = null;
                    return;
                }

                selectedActiveId = selectedActiveId == card.id ? null : card.id;
                selectedReserveId = null;
            }
            else
            {
                if (!string.IsNullOrEmpty(selectedActiveId))
                {
                    Run(new HudCommand(HudCommandType.TrySwapActiveReserve, 0, selectedActiveId, card.id));
                    selectedActiveId = null;
                    selectedReserveId = null;
                    return;
                }

                selectedReserveId = selectedReserveId == card.id ? null : card.id;
                selectedActiveId = null;
            }
        }

        string BuildBlockHint(string hint)
        {
            return string.IsNullOrWhiteSpace(hint)
                ? "默认显示为紧凑图标。悬停可查看详情；整理阶段可交换上阵和仓库方块。"
                : hint;
        }

        string BuildLoadoutSubtitle(string subtitle)
        {
            return string.IsNullOrWhiteSpace(subtitle)
                ? "先拿起一个嵌片，再点击高亮槽位进行安装。"
                : subtitle;
        }

        string BlockTooltip(BlockCardState card)
        {
            if (card == null)
                return string.Empty;

            var builder = new StringBuilder();
            builder.AppendLine($"类型：{BlockType(card.baseBlockType)}");
            builder.AppendLine($"稀有度：{Format(card.rarity)}");
            builder.AppendLine($"基础值：{Format(card)}");
            builder.AppendLine($"已装嵌片：{card.InstalledStickerCount}/{card.UnlockedSocketCount}");
            if (!string.IsNullOrWhiteSpace(card.mainActionText))
                builder.AppendLine(card.mainActionText);
            if (card.detailLines.Count > 0)
                builder.AppendLine(string.Join("\n", card.detailLines));
            return builder.ToString().Trim();
        }

        string StickerTooltip(StickerInstance sticker)
        {
            if (sticker?.data == null)
                return string.Empty;

            var builder = new StringBuilder();
            builder.AppendLine($"稀有度：{StickerRarityText(sticker.data.rarity)}");
            builder.AppendLine($"适用类型：{MaskText(sticker.data.targetBlockType)}");
            if (!string.IsNullOrWhiteSpace(sticker.data.mainActionText))
                builder.AppendLine(sticker.data.mainActionText);
            if (sticker.data.detailLines.Count > 0)
                builder.AppendLine(string.Join("\n", sticker.data.detailLines));
            return builder.ToString().Trim();
        }

        string InventoryStickerTooltip(StickerInstance sticker) => StickerTooltip(sticker);

        string SocketTooltip(BlockCardState card, SocketSlotState socket)
        {
            if (!socket.isUnlocked)
                return "锁定槽位\n之后可通过成长奖励或模组解锁。";
            if (socket.installedSticker != null)
                return $"已装嵌片\n{StickerTooltip(socket.installedSticker)}";
            return $"空槽位\n可安装：{MaskText(socket.targetMask)}";
        }

        void Run(HudCommand command)
        {
            sink?.ExecuteHudCommand(command);
        }

        TMP_Text T(string path)
        {
            var node = transform.Find(path);
            if (node == null)
            {
                Debug.LogError($"[POPHero] CanvasHudController missing TMP node: {path}");
                return null;
            }

            var text = node.GetComponent<TMP_Text>();
            if (text == null)
                Debug.LogError($"[POPHero] CanvasHudController node has no TMP_Text: {path}");
            return text;
        }

        RectTransform R(string path)
        {
            var node = transform.Find(path);
            if (node == null)
            {
                Debug.LogError($"[POPHero] CanvasHudController missing RectTransform node: {path}");
                return null;
            }

            return node as RectTransform;
        }

        RectTransform ROptional(string path)
        {
            var node = transform.Find(path);
            return node as RectTransform;
        }

        Button B(string path)
        {
            var node = transform.Find(path);
            if (node == null)
            {
                Debug.LogError($"[POPHero] CanvasHudController missing Button node: {path}");
                return null;
            }

            var button = node.GetComponent<Button>();
            if (button == null)
                Debug.LogError($"[POPHero] CanvasHudController node has no Button: {path}");
            return button;
        }

        GameObject G(string path)
        {
            var node = transform.Find(path);
            if (node == null)
            {
                Debug.LogError($"[POPHero] CanvasHudController missing GameObject node: {path}");
                return null;
            }

            return node.gameObject;
        }

        static void Validate(UnityEngine.Object value, string path)
        {
            if (value == null)
                Debug.LogError($"[POPHero] Battle UI binding failed: {path}");
        }

        static void Set(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value ?? string.Empty;
        }

        static void SetButtonLabel(Button button, string value)
        {
            if (button == null)
                return;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = value ?? string.Empty;
        }

        static void SetActive(Component component, bool value)
        {
            if (component != null)
                component.gameObject.SetActive(value);
        }

        static void SetActive(GameObject gameObject, bool value)
        {
            if (gameObject != null)
                gameObject.SetActive(value);
        }

        void EnsureBlockManagementCanvas()
        {
            if (blockManagementPanel == null)
                return;

            if (blockManagementCanvas == null)
            {
                blockManagementCanvas = blockManagementPanel.GetComponent<Canvas>();
                if (blockManagementCanvas == null)
                {
                    blockManagementCanvas = blockManagementPanel.AddComponent<Canvas>();
                    if (canvas != null)
                        blockManagementCanvas.sortingLayerID = canvas.sortingLayerID;
                }
            }

            if (!blockManagementCanvasSettingsCaptured)
            {
                blockManagementDefaultOverrideSorting = blockManagementCanvas.overrideSorting;
                blockManagementDefaultSortingOrder = blockManagementCanvas.sortingOrder;
                blockManagementDefaultSortingLayerId = blockManagementCanvas.sortingLayerID;
                blockManagementCanvasSettingsCaptured = true;
            }

            if (blockManagementRaycaster == null)
            {
                blockManagementRaycaster = blockManagementPanel.GetComponent<GraphicRaycaster>();
                if (blockManagementRaycaster == null)
                    blockManagementRaycaster = blockManagementPanel.AddComponent<GraphicRaycaster>();
            }
        }

        static void Bind(Button button, Action action)
        {
            if (button == null)
                return;
            button.onClick.RemoveAllListeners();
            if (action != null)
                button.onClick.AddListener(() => action());
        }

        void EnsureRows(List<CanvasBlockRowView> rows, RectTransform root, int count)
        {
            while (rows.Count < count)
                rows.Add(CanvasBlockRowView.Create(root));
            HideExtra(rows, count);
        }

        void EnsureCards(List<CanvasCardView> cards, RectTransform root, int count)
        {
            while (cards.Count < count)
                cards.Add(CanvasCardView.Create(root));
            HideExtra(cards, count);
        }

        void EnsureEntries(List<CanvasListEntryView> entries, RectTransform root, int count)
        {
            while (entries.Count < count)
                entries.Add(CanvasListEntryView.Create(root));
            HideExtra(entries, count);
        }

        static void HideExtra(List<CanvasBlockRowView> rows, int usedCount)
        {
            for (var index = 0; index < rows.Count; index++)
                rows[index].gameObject.SetActive(index < usedCount);
        }

        static void HideExtra(List<CanvasCardView> cards, int usedCount)
        {
            for (var index = 0; index < cards.Count; index++)
                cards[index].gameObject.SetActive(index < usedCount);
        }

        static void HideExtra(List<CanvasListEntryView> entries, int usedCount)
        {
            for (var index = 0; index < entries.Count; index++)
                entries[index].gameObject.SetActive(index < usedCount);
        }

        void Position(RectTransform rect, Vector3 screenPosition, float offsetX, float offsetY)
        {
            if (rect == null || canvas == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                screenPosition,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out var localPoint);
            rect.anchoredPosition = localPoint + new Vector2(offsetX, offsetY);
        }

        static string BlockType(BoardBlockType type)
        {
            return type switch
            {
                BoardBlockType.AttackAdd => "攻击方块",
                BoardBlockType.AttackMultiply => "倍率方块",
                BoardBlockType.Shield => "防御方块",
                BoardBlockType.Hybrid => "混合方块",
                _ => type.ToString()
            };
        }

        static string BlockIcon(BoardBlockType type)
        {
            return type switch
            {
                BoardBlockType.AttackAdd => "ATK",
                BoardBlockType.AttackMultiply => "AMP",
                BoardBlockType.Shield => "SHD",
                BoardBlockType.Hybrid => "HYB",
                _ => "?"
            };
        }

        static string ModCategoryText(ModCategory category)
        {
            return category switch
            {
                ModCategory.Information => "信息",
                ModCategory.Economy => "经济",
                ModCategory.Operation => "操作",
                ModCategory.Growth => "成长",
                ModCategory.Build => "构筑",
                _ => category.ToString()
            };
        }

        static string StickerRarityText(StickerRarity rarity)
        {
            return rarity switch
            {
                StickerRarity.Common => "普通",
                StickerRarity.Uncommon => "精良",
                StickerRarity.Rare => "稀有",
                StickerRarity.Epic => "史诗",
                _ => rarity.ToString()
            };
        }

        static string Format(BlockRewardOption option) => option == null ? string.Empty : Format(option.blockType, option.baseValue);
        static string Format(BlockCardState card) => card == null ? string.Empty : Format(card.baseBlockType, card.baseValueA);

        static string Format(BlockRarity rarity)
        {
            return rarity switch
            {
                BlockRarity.White => "白",
                BlockRarity.Blue => "蓝",
                BlockRarity.Purple => "紫",
                BlockRarity.Gold => "金",
                _ => rarity.ToString()
            };
        }

        static string Format(BoardBlockType type, float value)
        {
            return type == BoardBlockType.AttackMultiply ? $"x{value:0.0#}" : $"+{Mathf.RoundToInt(value)}";
        }

        static string StickerShort(StickerInstance sticker)
        {
            if (sticker?.data == null)
                return "?";
            if (!string.IsNullOrWhiteSpace(sticker.data.shortTitle))
                return sticker.data.shortTitle.Length > 2 ? sticker.data.shortTitle.Substring(0, 2) : sticker.data.shortTitle;
            if (!string.IsNullOrWhiteSpace(sticker.data.name))
                return sticker.data.name.Length > 2 ? sticker.data.name.Substring(0, 2) : sticker.data.name;
            return "ST";
        }

        static string MaskText(SocketTargetMask mask)
        {
            if (mask == SocketTargetMask.Any)
                return "任意方块";
            var parts = new List<string>();
            if ((mask & SocketTargetMask.Attack) != 0) parts.Add("攻击");
            if ((mask & SocketTargetMask.Shield) != 0) parts.Add("防御");
            if ((mask & SocketTargetMask.Multiplier) != 0) parts.Add("倍率");
            if ((mask & SocketTargetMask.Hybrid) != 0) parts.Add("混合");
            return parts.Count == 0 ? "无" : string.Join(" / ", parts);
        }

        static string MaskIcon(SocketTargetMask mask)
        {
            if (mask == SocketTargetMask.Any)
                return "任意";
            var parts = new List<string>();
            if ((mask & SocketTargetMask.Attack) != 0) parts.Add("ATK");
            if ((mask & SocketTargetMask.Shield) != 0) parts.Add("SHD");
            if ((mask & SocketTargetMask.Multiplier) != 0) parts.Add("AMP");
            if ((mask & SocketTargetMask.Hybrid) != 0) parts.Add("HYB");
            return parts.Count == 0 ? "无" : string.Join("", parts);
        }

        static SocketTargetMask CardMask(BoardBlockType type)
        {
            return type switch
            {
                BoardBlockType.AttackAdd => SocketTargetMask.Attack,
                BoardBlockType.AttackMultiply => SocketTargetMask.Multiplier,
                BoardBlockType.Shield => SocketTargetMask.Shield,
                BoardBlockType.Hybrid => SocketTargetMask.Hybrid,
                _ => SocketTargetMask.None
            };
        }

        static Color RarityColor(BlockRarity rarity)
        {
            return rarity switch
            {
                BlockRarity.White => new Color(0.94f, 0.96f, 1f, 1f),
                BlockRarity.Blue => new Color(0.42f, 0.68f, 1f, 1f),
                BlockRarity.Purple => new Color(0.78f, 0.48f, 1f, 1f),
                BlockRarity.Gold => new Color(1f, 0.82f, 0.34f, 1f),
                _ => Color.white
            };
        }

        static Color StickerColor(StickerRarity rarity)
        {
            return rarity switch
            {
                StickerRarity.Common => new Color(0.84f, 0.88f, 0.94f, 1f),
                StickerRarity.Uncommon => new Color(0.38f, 0.78f, 1f, 1f),
                StickerRarity.Rare => new Color(0.86f, 0.5f, 1f, 1f),
                StickerRarity.Epic => new Color(1f, 0.84f, 0.34f, 1f),
                _ => Color.white
            };
        }

        static Color BlockColor(BlockCardState card) => card == null ? new Color(0.2f, 0.24f, 0.3f, 1f) : BlockColor(card.baseBlockType);

        static Color BlockColor(BoardBlockType type)
        {
            return type switch
            {
                BoardBlockType.AttackAdd => new Color(0.28f, 0.48f, 0.96f, 1f),
                BoardBlockType.AttackMultiply => new Color(0.82f, 0.48f, 0.98f, 1f),
                BoardBlockType.Shield => new Color(0.26f, 0.74f, 0.52f, 1f),
                BoardBlockType.Hybrid => new Color(0.94f, 0.72f, 0.28f, 1f),
                _ => Color.white
            };
        }

        static Color RewardKindColor(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Color.white;
            if (text.Contains("嵌片", StringComparison.OrdinalIgnoreCase) || text.Contains("Sticker", StringComparison.OrdinalIgnoreCase))
                return new Color(0.42f, 0.78f, 1f, 1f);
            if (text.Contains("模组", StringComparison.OrdinalIgnoreCase) || text.Contains("Mod", StringComparison.OrdinalIgnoreCase))
                return new Color(1f, 0.78f, 0.34f, 1f);
            if (text.Contains("成长", StringComparison.OrdinalIgnoreCase) || text.Contains("Growth", StringComparison.OrdinalIgnoreCase))
                return new Color(0.56f, 0.92f, 0.62f, 1f);
            return new Color(0.9f, 0.92f, 1f, 1f);
        }
    }
}
