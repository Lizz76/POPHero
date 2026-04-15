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

        readonly TopStatusBarPresenter topStatusPresenter = new();
        readonly StatusPanelPresenter statusPresenter = new();
        readonly CombatPanelPresenter combatPresenter = new();
        readonly BlockManagementPresenter blockPresenter = new();
        readonly IntermissionPanelPresenter intermissionPresenter = new();

        GameObject statusPanelObject;
        RectTransform topStatusBar;
        Image topAvatarIcon;
        TMP_Text topAvatarFallback;
        Image topHpIcon;
        TMP_Text topHpIconFallback;
        TMP_Text topHpValue;
        Image topGoldIcon;
        TMP_Text topGoldIconFallback;
        TMP_Text topGoldValue;
        Image topProgressIcon;
        TMP_Text topProgressIconFallback;
        TMP_Text topProgressValue;
        Image topTimerIcon;
        TMP_Text topTimerIconFallback;
        TMP_Text topTimerValue;
        Image topSettingsIcon;
        TMP_Text topSettingsFallback;

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
        Image dragBackground;
        Image dragIcon;
        TMP_Text dragFallbackLabel;
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
        TMP_Text inventoryTitle;
        TMP_Text activeModsTitle;
        TMP_Text reserveModsTitle;
        TMP_Text gameOverTitle;
        TMP_Text gameOverMessage;
        TMP_Text settingsTitle;
        TMP_Text settingsHint;

        Button toggleAimButton;
        Button settingsButton;
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
        Button settingsResumeButton;
        Button settingsMenuButton;
        Button settingsQuitButton;

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
        GameObject settingsModal;

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
        readonly List<CanvasStickerCellView> inventoryStickerCells = new();
        readonly List<CanvasListEntryView> activeModEntries = new();
        readonly List<CanvasListEntryView> reserveModEntries = new();

        GameObject combatPanelObject;
        string tooltipTitleValue;
        string tooltipBodyValue;
        Color tooltipColor = Color.white;
        string passiveTooltipTitleValue;
        string passiveTooltipBodyValue;
        Color passiveTooltipColor = Color.white;
        string selectedActiveId;
        string selectedReserveId;
        bool gmPanelOpen;
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
            SafeRefresh("topbar", RefreshTopStatusBar);
            SafeRefresh("combat", RefreshCombat);
            SafeRefresh("blocks", RefreshBlocks);
            SafeRefresh("damage", RefreshDamage);
            SafeRefresh("modals", RefreshModals);
            SafeRefresh("layers", RefreshInteractionLayers);
            SafeRefresh("drag", RefreshDragPanel);
            SafeRefresh("tooltip", RefreshTooltip);
        }

        public void RefreshNow()
        {
            ForceRefresh();
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

            HandleGmHotkey();

            if (!game.CanManageBlockAssignments)
            {
                selectedActiveId = null;
                selectedReserveId = null;
            }

            SafeRefresh("status", RefreshStatus);
            SafeRefresh("topbar", RefreshTopStatusBar);
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
            if (game?.StickerInventory?.DraggingSticker != null)
                return;

            tooltipTitleValue = title ?? string.Empty;
            tooltipBodyValue = body ?? string.Empty;
            tooltipColor = color;
        }

        public void ClearTooltip()
        {
            tooltipTitleValue = string.Empty;
            tooltipBodyValue = string.Empty;
        }

        public void SetPassiveTooltip(string title, string body, Color color)
        {
            passiveTooltipTitleValue = title ?? string.Empty;
            passiveTooltipBodyValue = body ?? string.Empty;
            passiveTooltipColor = color;
        }

        public void ClearPassiveTooltip()
        {
            passiveTooltipTitleValue = string.Empty;
            passiveTooltipBodyValue = string.Empty;
        }

        void BindScene()
        {
            statusPanelObject = GOptional("HudRoot/StatusPanel");
            topStatusBar = ROptional("HudRoot/TopStatusBar");
            topAvatarIcon = ImageOptional("HudRoot/TopStatusBar/AvatarSlot/Icon");
            topAvatarFallback = TOptional("HudRoot/TopStatusBar/AvatarSlot/Icon/FallbackLabel");
            topHpIcon = ImageOptional("HudRoot/TopStatusBar/HpSlot/Icon");
            topHpIconFallback = TOptional("HudRoot/TopStatusBar/HpSlot/Icon/FallbackLabel");
            topHpValue = TOptional("HudRoot/TopStatusBar/HpSlot/ValueText");
            topGoldIcon = ImageOptional("HudRoot/TopStatusBar/GoldSlot/Icon");
            topGoldIconFallback = TOptional("HudRoot/TopStatusBar/GoldSlot/Icon/FallbackLabel");
            topGoldValue = TOptional("HudRoot/TopStatusBar/GoldSlot/ValueText");
            topProgressIcon = ImageOptional("HudRoot/TopStatusBar/ProgressSlot/Icon");
            topProgressIconFallback = TOptional("HudRoot/TopStatusBar/ProgressSlot/Icon/FallbackLabel");
            topProgressValue = TOptional("HudRoot/TopStatusBar/ProgressSlot/ValueText");
            topTimerIcon = ImageOptional("HudRoot/TopStatusBar/TimerSlot/Icon");
            topTimerIconFallback = TOptional("HudRoot/TopStatusBar/TimerSlot/Icon/FallbackLabel");
            topTimerValue = TOptional("HudRoot/TopStatusBar/TimerSlot/ValueText");
            topSettingsIcon = ImageOptional("HudRoot/TopStatusBar/SettingsButton/Icon");
            topSettingsFallback = TOptional("HudRoot/TopStatusBar/SettingsButton/Icon/FallbackLabel");

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
            settingsButton = BOptional("HudRoot/TopStatusBar/SettingsButton") ?? B("HudRoot/TopRightControls/SettingsButton");

            combatPanelObject = GOptional("HudRoot/CombatPanel");
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
            blockHeader = TOptional("HudRoot/BlockManagementPanel/HeaderText");
            blockHint = TOptional("HudRoot/BlockManagementPanel/HintText");
            activeTitle = TOptional("HudRoot/BlockManagementPanel/ActiveSection/TitleText");
            reserveTitle = TOptional("HudRoot/BlockManagementPanel/ReserveSection/TitleText");
            activeRowsRoot = R("HudRoot/BlockManagementPanel/ActiveSection/ScrollView/Viewport/Rows");
            reserveRowsRoot = R("HudRoot/BlockManagementPanel/ReserveSection/ScrollView/Viewport/Rows");

            damagePanel = R("HudRoot/DamageCounterPanel");
            damageLabel = T("HudRoot/DamageCounterPanel/LabelText");
            damageValue = T("HudRoot/DamageCounterPanel/ValueText");

            tooltipPanel = R("TooltipRoot/TooltipPanel");
            tooltipTitle = T("TooltipRoot/TooltipPanel/TitleText");
            tooltipBody = T("TooltipRoot/TooltipPanel/BodyText");
            dragPanel = R("TooltipRoot/DragStickerPanel");
            dragBackground = dragPanel != null ? dragPanel.GetComponent<Image>() : null;
            dragIcon = ImageOptional("TooltipRoot/DragStickerPanel/Icon");
            dragFallbackLabel = TOptional("TooltipRoot/DragStickerPanel/FallbackLabel");
            dragName = TOptional("TooltipRoot/DragStickerPanel/NameText");
            dragMask = TOptional("TooltipRoot/DragStickerPanel/MaskText");
            dragHint = TOptional("TooltipRoot/DragStickerPanel/HintText");
            EnsureDragGhostVisuals();
            EnsureFloatingPanelIgnoresRaycasts(dragPanel);

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
            inventoryTitle = T("ModalRoot/LoadoutModal/Window/Body/Columns/InventoryPanel/InventoryTitleText");
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

            settingsModal = G("ModalRoot/SettingsModal");
            settingsTitle = T("ModalRoot/SettingsModal/Window/Header/TitleText");
            settingsHint = T("ModalRoot/SettingsModal/Window/Body/HintText");
            settingsResumeButton = B("ModalRoot/SettingsModal/Window/Footer/ResumeButton");
            settingsMenuButton = B("ModalRoot/SettingsModal/Window/Footer/MenuButton");
            settingsQuitButton = B("ModalRoot/SettingsModal/Window/Footer/QuitButton");
        }

        void ValidateBindings()
        {
            Validate(statusTitle, "HudRoot/StatusPanel/TitleText");
            Validate(topStatusBar, "HudRoot/TopStatusBar");
            Validate(topHpValue, "HudRoot/TopStatusBar/HpSlot/ValueText");
            Validate(topGoldValue, "HudRoot/TopStatusBar/GoldSlot/ValueText");
            Validate(topProgressValue, "HudRoot/TopStatusBar/ProgressSlot/ValueText");
            Validate(topTimerValue, "HudRoot/TopStatusBar/TimerSlot/ValueText");
            Validate(combatTitle, "HudRoot/CombatPanel/TitleText");
            Validate(blockManagementPanel, "HudRoot/BlockManagementPanel");
            Validate(activeRowsRoot, "HudRoot/BlockManagementPanel/ActiveSection/Rows");
            Validate(reserveRowsRoot, "HudRoot/BlockManagementPanel/ReserveSection/Rows");
            Validate(blockRewardModal, "ModalRoot/BlockRewardModal");
            Validate(rewardModal, "ModalRoot/RewardModal");
            Validate(shopModal, "ModalRoot/ShopModal");
            Validate(loadoutModal, "ModalRoot/LoadoutModal");
            Validate(gameOverModal, "ModalRoot/GameOverModal");
            Validate(settingsButton, "HudRoot/TopStatusBar/SettingsButton");
            Validate(settingsModal, "ModalRoot/SettingsModal");
        }

        void BindButtons()
        {
            Bind(settingsButton, () => Run(new HudCommand(HudCommandType.OpenSettings)));
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
            Bind(settingsResumeButton, () => Run(new HudCommand(HudCommandType.CloseSettings)));
            Bind(settingsMenuButton, () => Run(new HudCommand(HudCommandType.BackToMenu)));
            Bind(settingsQuitButton, () => Run(new HudCommand(HudCommandType.QuitGame)));
        }

        void RefreshStatus()
        {
            SetActive(statusPanelObject, false);
            var model = statusPresenter.Build(game);
            Set(statusTitle, "战斗状态");
            Set(statusState, string.IsNullOrEmpty(model.StateText) ? model.StateText : model.StateText.Replace("整理", "背包"));
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

        void RefreshTopStatusBar()
        {
            if (topStatusBar == null)
                return;

            SetActive(topStatusBar, true);
            var model = topStatusPresenter.Build(game);
            var visuals = game?.Config?.hud?.topStatusBar;

            ApplyTopStatusIcon(topAvatarIcon, topAvatarFallback, visuals?.playerAvatarSprite, "角", new Color(0.92f, 0.96f, 1f, 1f));
            ApplyTopStatusIcon(topHpIcon, topHpIconFallback, visuals?.heartIconSprite, "心", new Color(1f, 0.45f, 0.45f, 1f));
            ApplyTopStatusIcon(topGoldIcon, topGoldIconFallback, visuals?.goldIconSprite, "金", new Color(1f, 0.82f, 0.32f, 1f));
            ApplyTopStatusIcon(topProgressIcon, topProgressIconFallback, visuals?.progressIconSprite, "层", new Color(0.78f, 0.88f, 1f, 1f));
            ApplyTopStatusIcon(topTimerIcon, topTimerIconFallback, visuals?.timerIconSprite, "时", new Color(0.96f, 0.9f, 0.66f, 1f));
            ApplyTopStatusIcon(topSettingsIcon, topSettingsFallback, visuals?.settingsIconSprite, "设", new Color(0.9f, 0.95f, 1f, 1f));

            Set(topHpValue, model.HpText);
            Set(topGoldValue, model.GoldText);
            Set(topProgressValue, model.ProgressCountText);
            Set(topTimerValue, model.RunTimerText);

            if (topSettingsIcon == null && topSettingsFallback == null)
                SetButtonLabel(settingsButton, "设置");
        }

        void RefreshCombat()
        {
            SetActive(combatPanelObject, gmPanelOpen);
            var model = combatPresenter.Build(game);
            Set(combatTitle, "GM 调试 / 战斗信息");
            Set(combatAttack, model.RoundAttackText);
            Set(combatShield, model.RoundShieldText);
            Set(combatHits, model.RoundHitText);
            Set(combatPreview, model.PreviewText);
            var message = string.IsNullOrWhiteSpace(model.IntermissionText)
                ? "长按 D 关闭，或按 Esc 关闭。"
                : $"{model.IntermissionText}\n长按 D 关闭，或按 Esc 关闭。";
            Set(combatMessage, message);
            SetButtonLabel(toggleAimButton, "切换瞄准");
            SetButtonLabel(shuffleButton, "重排方块");
            SetButtonLabel(addGoldButton, "金币 +25");
            SetButtonLabel(killEnemyButton, "秒杀敌人");
            SetButtonLabel(damagePlayerButton, "主角 -10 血");
        }

        void HandleGmHotkey()
        {
            if (!Application.isPlaying)
                return;

            if (game != null && game.IsSettingsOpen)
                return;

            if (gmPanelOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                gmPanelOpen = false;
                ClearTooltip();
                ClearPassiveTooltip();
                return;
            }

            if (!Input.GetKeyDown(KeyCode.D))
                return;

            gmPanelOpen = !gmPanelOpen;
            ClearTooltip();
            ClearPassiveTooltip();
        }

        void RefreshBlocks()
        {
            var model = blockPresenter.Build(game);
            var blockCollections = game?.BlockCollections;
            var blockCellPrefab = game?.Config?.board?.blockCellViewPrefab;
            Set(blockHeader, model.HeaderText);
            Set(blockHint, BuildBlockHint(model.HintText));
            Set(activeTitle, $"上阵 {blockCollections?.ActiveCardCount ?? 0}/{blockCollections?.ActiveCapacity ?? 0}");
            Set(reserveTitle, $"仓库 {blockCollections?.ReserveCardCount ?? 0}/{blockCollections?.ReserveCapacity ?? 0}");

            EnsureRows(activeRows, activeRowsRoot, model.ActiveRows.Count, blockCellPrefab);
            for (var index = 0; index < model.ActiveRows.Count; index++)
                ConfigureRow(activeRows[index], model.ActiveRows[index], true);

            EnsureRows(reserveRows, reserveRowsRoot, model.ReserveRows.Count, blockCellPrefab);
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
            SetActive(settingsModal, game != null && game.IsSettingsOpen);

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
                HideExtra(inventoryStickerCells, 0);
                HideExtra(activeModEntries, 0);
                HideExtra(reserveModEntries, 0);
            }

            if (state == RoundState.GameOver)
                RefreshGameOver();

            if (game != null && game.IsSettingsOpen)
                RefreshSettings();
        }

        void RefreshInteractionLayers()
        {
            if (game == null || blockManagementPanel == null)
                return;

            EnsureBlockManagementCanvas();
            if (blockManagementCanvas == null)
                return;

            if (game.IsSettingsOpen)
            {
                if (blockManagementCanvasSettingsCaptured)
                {
                    blockManagementCanvas.overrideSorting = blockManagementDefaultOverrideSorting;
                    blockManagementCanvas.sortingLayerID = blockManagementDefaultSortingLayerId;
                    blockManagementCanvas.sortingOrder = blockManagementDefaultSortingOrder;
                }

                if (blockManagementRaycaster != null)
                    blockManagementRaycaster.enabled = false;
                return;
            }

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
            if (game != null && game.IsSettingsOpen)
                visible = false;
            SetActive(dragPanel, visible);
            if (!visible)
                return;

            ConfigureDragGhost(dragging);
            PositionFloatingPanel(dragPanel, Input.mousePosition);
        }

        void RefreshTooltip()
        {
            var title = !string.IsNullOrWhiteSpace(tooltipTitleValue) ? tooltipTitleValue : passiveTooltipTitleValue;
            var body = !string.IsNullOrWhiteSpace(tooltipTitleValue) ? tooltipBodyValue : passiveTooltipBodyValue;
            var color = !string.IsNullOrWhiteSpace(tooltipTitleValue) ? tooltipColor : passiveTooltipColor;
            var visible = !string.IsNullOrWhiteSpace(title);
            if (game?.StickerInventory?.DraggingSticker != null)
                visible = false;
            if (game != null && game.IsSettingsOpen)
                visible = false;
            SetActive(tooltipPanel, visible);
            if (!visible)
                return;

            Set(tooltipTitle, title);
            Set(tooltipBody, body);
            if (tooltipTitle != null)
                tooltipTitle.color = color;
            PositionFloatingPanel(tooltipPanel, Input.mousePosition);
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
            Set(inventoryTitle, "嵌片背包");
            Set(activeModsTitle, "启用模组");
            Set(reserveModsTitle, "待机模组");
            SetButtonLabel(loadoutCancelButton, model.CancelDragText);
            SetButtonLabel(loadoutContinueButton, model.ContinueButtonText);
            if (loadoutCancelButton != null)
                loadoutCancelButton.interactable = model.CanCancelDrag;

            EnsureStickerCells(inventoryStickerCells, inventoryContent, model.Inventory.Count);
            var draggingId = game?.StickerInventory?.DraggingSticker?.runtimeId;
            for (var index = 0; index < model.Inventory.Count; index++)
            {
                var sticker = model.Inventory[index];
                var view = inventoryStickerCells[index];
                view.gameObject.SetActive(true);
                var runtimeId = sticker.runtimeId;
                view.Set(StickerShort(sticker), sticker.data.iconSprite, StickerColor(sticker.data.rarity), null);
                view.SetInteractable(true);
                view.SetDraggingVisual(runtimeId == draggingId);
                view.SetDragHandlers(
                    () => BeginStickerDragFromInventory(runtimeId),
                    ContinueStickerDragFromInventory,
                    EndStickerDragFromInventory);
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

        void RefreshSettings()
        {
            Set(settingsTitle, "设置");
            Set(settingsHint, "游戏已暂停。继续游戏会回到当前战斗或中场界面。");
            SetButtonLabel(settingsResumeButton, "继续游戏");
            SetButtonLabel(settingsMenuButton, "返回菜单");
            SetButtonLabel(settingsQuitButton, "退出游戏");
        }

        void RefreshRemoval(List<CanvasListEntryView> views, RectTransform root, IReadOnlyList<BlockCardState> cards, bool disabled)
        {
            EnsureEntries(views, root, cards.Count);
            for (var index = 0; index < cards.Count; index++)
            {
                var card = cards[index];
                var view = views[index];
                var tooltip = BlockPresentationUtility.BuildTooltip(card);
                view.gameObject.SetActive(true);
                view.Set(card.cardName, BlockIcon(card.baseBlockType), Format(card), BlockColor(card), () => Run(new HudCommand(HudCommandType.TryRemoveBlockInShop, 0, card.id)));
                view.SetInteractable(!disabled);
                view.SetTooltip(tooltip.Title, tooltip.Body, tooltip.AccentColor, this);
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
                view.SetTypePlaceholder("-", new Color(0.24f, 0.26f, 0.3f, 0.8f), null);
                view.SetTypeTooltip("空槽位", activeSection ? "这里还没有上阵方块。" : "这里还没有仓库方块。", new Color(0.72f, 0.76f, 0.84f, 1f), this);
                view.SetStickerCount(0);
                view.SetSocketCount(0);
                return;
            }

            var blockVisual = BlockPresentationUtility.GetBlockVisual(game?.Config?.board, model.Card);
            var tooltip = BlockPresentationUtility.BuildTooltip(model.Card);
            view.SetTypeVisual(model.Card, blockVisual, () => ClickBlock(model.Card, activeSection));
            view.SetTypeTooltip(tooltip.Title, tooltip.Body, tooltip.AccentColor, this);

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
                view.SetSocket(
                    index,
                    icon,
                    color,
                    () => ClickSocket(model.Card, capturedIndex),
                    () => DropStickerOnSocket(model.Card, capturedIndex));
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

        bool BeginStickerDragFromInventory(string runtimeId)
        {
            if (game == null || !game.CanManageBlockAssignments || game.IsSettingsOpen || string.IsNullOrWhiteSpace(runtimeId))
                return false;

            Run(new HudCommand(HudCommandType.BeginStickerDrag, 0, runtimeId));
            var started = game.StickerInventory?.DraggingSticker?.runtimeId == runtimeId;
            if (!started)
                return false;

            ClearTooltip();
            ClearPassiveTooltip();
            ForceRefresh();
            return true;
        }

        void ContinueStickerDragFromInventory()
        {
            RefreshDragPanel();
        }

        void EndStickerDragFromInventory()
        {
            if (game?.StickerInventory?.DraggingSticker != null)
                Run(new HudCommand(HudCommandType.CancelStickerDrag));

            ForceRefresh();
        }

        void DropStickerOnSocket(BlockCardState card, int socketIndex)
        {
            if (game == null || card == null || !game.CanManageBlockAssignments)
                return;

            if (game.StickerInventory?.DraggingSticker == null)
                return;

            Run(new HudCommand(HudCommandType.TryInstallDraggedSticker, socketIndex, card.id));
            ForceRefresh();
        }

        void ClickBlock(BlockCardState card, bool activeSection)
        {
            if (card == null)
                return;

            if (!game.CanManageBlockAssignments)
            {
                var tooltip = BlockPresentationUtility.BuildTooltip(card);
                SetTooltip(tooltip.Title, tooltip.Body, tooltip.AccentColor);
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
                ? "从背包里拖拽嵌片，放到右侧高亮槽位进行安装。松在空白处会取消并归位。"
                : subtitle;
        }

        string BlockTooltip(BlockCardState card)
        {
            return BlockPresentationUtility.BuildTooltip(card).Body;
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

        TMP_Text TOptional(string path)
        {
            var node = transform.Find(path);
            return node == null ? null : node.GetComponent<TMP_Text>();
        }

        Image ImageOptional(string path)
        {
            var node = transform.Find(path);
            return node == null ? null : node.GetComponent<Image>();
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

        GameObject GOptional(string path)
        {
            var node = transform.Find(path);
            return node == null ? null : node.gameObject;
        }

        Button BOptional(string path)
        {
            var node = transform.Find(path);
            return node == null ? null : node.GetComponent<Button>();
        }

        static void ApplyTopStatusIcon(Image icon, TMP_Text fallbackLabel, Sprite sprite, string fallbackText, Color fallbackColor)
        {
            var effectiveSprite = sprite != null ? sprite : icon != null ? icon.sprite : null;
            if (icon != null)
            {
                if (sprite != null)
                {
                    icon.sprite = sprite;
                    icon.color = Color.white;
                }
                else if (effectiveSprite != null && icon.color.a <= 0.001f)
                {
                    icon.color = Color.white;
                }

                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.enabled = effectiveSprite != null;
            }

            if (fallbackLabel != null)
            {
                fallbackLabel.text = fallbackText;
                fallbackLabel.color = fallbackColor;
                fallbackLabel.gameObject.SetActive(effectiveSprite == null);
            }
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

        void EnsureDragGhostVisuals()
        {
            if (dragPanel == null)
                return;

            dragPanel.SetAsLastSibling();
            dragPanel.sizeDelta = new Vector2(28f, 28f);
            var layout = dragPanel.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
                layout.enabled = false;

            var dragCanvas = dragPanel.GetComponent<Canvas>();
            if (dragCanvas == null)
                dragCanvas = dragPanel.gameObject.AddComponent<Canvas>();
            dragCanvas.overrideSorting = true;
            if (canvas != null)
                dragCanvas.sortingLayerID = canvas.sortingLayerID;
            dragCanvas.sortingOrder = (canvas != null ? canvas.sortingOrder : 0) + 200;

            dragBackground ??= dragPanel.GetComponent<Image>();
            if (dragBackground == null)
                dragBackground = dragPanel.gameObject.AddComponent<Image>();
            dragBackground.raycastTarget = false;

            if (dragIcon == null)
            {
                var iconRoot = CanvasUiFactory.Node("Icon", dragPanel);
                iconRoot.anchorMin = new Vector2(0.5f, 0.5f);
                iconRoot.anchorMax = new Vector2(0.5f, 0.5f);
                iconRoot.pivot = new Vector2(0.5f, 0.5f);
                iconRoot.sizeDelta = new Vector2(22f, 22f);
                dragIcon = iconRoot.gameObject.AddComponent<Image>();
                dragIcon.preserveAspect = true;
            }
            dragIcon.raycastTarget = false;

            if (dragFallbackLabel == null)
            {
                dragFallbackLabel = CanvasUiFactory.Text("FallbackLabel", dragPanel, 12, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
                dragFallbackLabel.rectTransform.anchorMin = Vector2.zero;
                dragFallbackLabel.rectTransform.anchorMax = Vector2.one;
                dragFallbackLabel.rectTransform.offsetMin = Vector2.zero;
                dragFallbackLabel.rectTransform.offsetMax = Vector2.zero;
            }
            dragFallbackLabel.raycastTarget = false;
            dragFallbackLabel.enableWordWrapping = false;
            dragFallbackLabel.overflowMode = TextOverflowModes.Ellipsis;

            SetActive(dragName, false);
            SetActive(dragMask, false);
            SetActive(dragHint, false);
        }

        void ConfigureDragGhost(StickerInstance sticker)
        {
            EnsureDragGhostVisuals();
            if (sticker?.data == null)
                return;

            var accent = StickerColor(sticker.data.rarity);
            if (dragBackground != null)
                dragBackground.color = new Color(accent.r * 0.5f + 0.08f, accent.g * 0.5f + 0.08f, accent.b * 0.5f + 0.1f, 0.96f);

            var hasIcon = sticker.data.iconSprite != null;
            if (dragIcon != null)
            {
                dragIcon.sprite = sticker.data.iconSprite;
                dragIcon.color = Color.white;
                dragIcon.gameObject.SetActive(hasIcon);
            }

            if (dragFallbackLabel != null)
            {
                dragFallbackLabel.text = StickerShort(sticker);
                dragFallbackLabel.color = Color.white;
                dragFallbackLabel.gameObject.SetActive(!hasIcon);
            }
        }

        static void EnsureFloatingPanelIgnoresRaycasts(RectTransform panel)
        {
            if (panel == null)
                return;

            var group = panel.GetComponent<CanvasGroup>();
            if (group == null)
                group = panel.gameObject.AddComponent<CanvasGroup>();

            group.blocksRaycasts = false;
            group.interactable = false;
        }

        void EnsureRows(List<CanvasBlockRowView> rows, RectTransform root, int count, BlockCellView blockCellPrefab)
        {
            while (rows.Count < count)
                rows.Add(CanvasBlockRowView.Create(root, blockCellPrefab));
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

        void EnsureStickerCells(List<CanvasStickerCellView> entries, RectTransform root, int count)
        {
            while (entries.Count < count)
                entries.Add(CanvasStickerCellView.Create(root));
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

        static void HideExtra(List<CanvasStickerCellView> entries, int usedCount)
        {
            for (var index = 0; index < entries.Count; index++)
                entries[index].gameObject.SetActive(index < usedCount);
        }

        void PositionFloatingPanel(RectTransform rect, Vector3 screenPosition)
        {
            if (rect == null || canvas == null || rect.parent is not RectTransform parent)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                screenPosition,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out var localPoint);

            var parentRect = parent.rect;
            var size = rect.rect.size;
            if (size.x <= 1f)
                size.x = Mathf.Max(rect.sizeDelta.x, 260f);
            if (size.y <= 1f)
                size.y = Mathf.Max(rect.sizeDelta.y, 120f);

            const float gap = 8f;
            const float margin = 10f;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);

            var mouseFromTopLeft = new Vector2(localPoint.x - parentRect.xMin, localPoint.y - parentRect.yMax);
            var x = mouseFromTopLeft.x + gap;
            if (x + size.x > parentRect.width - margin)
                x = mouseFromTopLeft.x - size.x - gap;

            var y = mouseFromTopLeft.y - gap;
            if (y - size.y < -parentRect.height + margin)
                y = mouseFromTopLeft.y + size.y + gap;

            x = Mathf.Clamp(x, margin, Mathf.Max(margin, parentRect.width - size.x - margin));
            y = Mathf.Clamp(y, -parentRect.height + size.y + margin, -margin);
            rect.anchoredPosition = new Vector2(x, y);
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
