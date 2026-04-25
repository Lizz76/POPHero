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
        TMP_Text mapTitle;
        TMP_Text mapSubtitle;
        TMP_Text mapFeedback;
        CanvasMapRouteView mapRouteView;
        TMP_Text mapEventTitle;
        TMP_Text mapEventSubtitle;
        TMP_Text blockOperationsTitle;
        TMP_Text blockOperationsSubtitle;
        TMP_Text blockOperationsHint;
        TMP_Text blockOperationsFeedback;
        TMP_Text blockOperationsActiveTitle;
        TMP_Text blockOperationsReserveTitle;
        TMP_Text blockOperationsDeleteStatus;
        TMP_Text blockOperationsSwapStatus;
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
        Button shopBlockOperationsButton;
        Button shopCloseButton;
        Button blockOperationsCloseButton;
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
        RectTransform mapEventOptionsContent;
        RectTransform blockOperationsActiveContent;
        RectTransform blockOperationsReserveContent;
        RectTransform inventoryContent;
        RectTransform activeModsContent;
        RectTransform reserveModsContent;

        GameObject blockManagementPanel;
        GameObject reserveSectionObject;
        GameObject blockRewardModal;
        GameObject rewardModal;
        GameObject shopModal;
        GameObject mapModal;
        GameObject mapEventModal;
        GameObject blockOperationsModal;
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
        readonly List<CanvasCardView> mapEventCards = new();
        readonly List<CanvasBlockOperationEntryView> blockOperationActiveEntries = new();
        readonly List<CanvasBlockOperationEntryView> blockOperationReserveEntries = new();
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
        string selectedBlockOperationActiveId;
        string selectedBlockOperationReserveId;
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
                selectedBlockOperationActiveId = null;
                selectedBlockOperationReserveId = null;
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
            EnsureRuntimeMapUi();
            EnsureRuntimeBlockOperationsUi();
            GetComponent<BattleCanvasLayout>()?.RefreshLayout();

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
            reserveSectionObject = GOptional("HudRoot/BlockManagementPanel/ReserveSection");
            activeRowsRoot = R("HudRoot/BlockManagementPanel/ActiveSection/ScrollView/Viewport/Rows");
            reserveRowsRoot = ROptional("HudRoot/BlockManagementPanel/ReserveSection/ScrollView/Viewport/Rows");

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
            shopRerollButton = B("ModalRoot/ShopModal/Window/Footer/RerollButton");
            shopBlockOperationsButton = BOptional("ModalRoot/ShopModal/Window/Footer/BlockOperationsButton");
            shopCloseButton = B("ModalRoot/ShopModal/Window/Footer/CloseButton");

            mapModal = GOptional("ModalRoot/MapModal");
            mapTitle = TOptional("ModalRoot/MapModal/Window/Header/TitleText");
            mapSubtitle = TOptional("ModalRoot/MapModal/Window/Header/SubtitleText");
            mapFeedback = TOptional("ModalRoot/MapModal/Window/Header/FeedbackText");
            var routeRoot = ROptional("ModalRoot/MapModal/Window/Body/RouteView");
            mapRouteView = routeRoot != null ? routeRoot.GetComponent<CanvasMapRouteView>() : null;

            mapEventModal = GOptional("ModalRoot/MapEventModal");
            mapEventTitle = TOptional("ModalRoot/MapEventModal/Window/Header/TitleText");
            mapEventSubtitle = TOptional("ModalRoot/MapEventModal/Window/Header/SubtitleText");
            mapEventOptionsContent = ROptional("ModalRoot/MapEventModal/Window/Body/ScrollView/Viewport/Content");

            blockOperationsModal = GOptional("ModalRoot/BlockOperationsModal");
            blockOperationsTitle = TOptional("ModalRoot/BlockOperationsModal/Window/Header/TitleText");
            blockOperationsSubtitle = TOptional("ModalRoot/BlockOperationsModal/Window/Header/SubtitleText");
            blockOperationsHint = TOptional("ModalRoot/BlockOperationsModal/Window/Body/HintText");
            blockOperationsFeedback = TOptional("ModalRoot/BlockOperationsModal/Window/Body/FeedbackText");
            blockOperationsActiveTitle = TOptional("ModalRoot/BlockOperationsModal/Window/Body/Columns/ActiveColumn/TitleText");
            blockOperationsReserveTitle = TOptional("ModalRoot/BlockOperationsModal/Window/Body/Columns/ReserveColumn/TitleText");
            blockOperationsDeleteStatus = TOptional("ModalRoot/BlockOperationsModal/Window/Body/StatusRow/DeleteStatusText");
            blockOperationsSwapStatus = TOptional("ModalRoot/BlockOperationsModal/Window/Body/StatusRow/SwapStatusText");
            blockOperationsActiveContent = ROptional("ModalRoot/BlockOperationsModal/Window/Body/Columns/ActiveColumn/ScrollView/Viewport/Content");
            blockOperationsReserveContent = ROptional("ModalRoot/BlockOperationsModal/Window/Body/Columns/ReserveColumn/ScrollView/Viewport/Content");
            blockOperationsCloseButton = BOptional("ModalRoot/BlockOperationsModal/Window/Footer/CloseButton");

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
            Bind(shopBlockOperationsButton, () => Run(new HudCommand(HudCommandType.OpenBlockOperations, 0, game?.Config?.shop?.blockOperationProfileId, RoundState.Shop.ToString())));
            Bind(shopCloseButton, () => Run(new HudCommand(HudCommandType.CloseShop)));
            Bind(blockOperationsCloseButton, () => Run(new HudCommand(HudCommandType.CloseBlockOperations)));
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
            SetActive(reserveSectionObject, false);
            if (reserveTitle != null)
                reserveTitle.text = string.Empty;

            EnsureRows(activeRows, activeRowsRoot, model.ActiveRows.Count, blockCellPrefab);
            for (var index = 0; index < model.ActiveRows.Count; index++)
                ConfigureRow(activeRows[index], model.ActiveRows[index], true);

            HideExtra(reserveRows, 0);
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
            SetActive(mapModal, state == RoundState.Map);
            SetActive(mapEventModal, state == RoundState.MapEvent);
            SetActive(blockRewardModal, state == RoundState.BlockRewardChoose);
            SetActive(rewardModal, state == RoundState.RewardChoose);
            SetActive(shopModal, state == RoundState.Shop);
            SetActive(blockOperationsModal, state == RoundState.BlockOperations);
            SetActive(loadoutModal, state == RoundState.LoadoutManage);
            SetActive(gameOverModal, state == RoundState.GameOver);
            SetActive(settingsModal, game != null && game.IsSettingsOpen);

            if (state == RoundState.Map)
                RefreshMap();

            if (state == RoundState.MapEvent)
                RefreshMapEvent();
            else
                HideExtra(mapEventCards, 0);

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
            }

            if (state == RoundState.BlockOperations)
                RefreshBlockOperations();
            else
            {
                HideExtra(blockOperationActiveEntries, 0);
                HideExtra(blockOperationReserveEntries, 0);
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
            var visible = dragging != null && game != null && game.CanManageStickerLoadout;
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
            if (shopBlockOperationsButton != null)
                SetButtonLabel(shopBlockOperationsButton, model.BlockOperationsButtonText);
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
        }

        void RefreshMap()
        {
            var model = intermissionPresenter.BuildMapPanel(game);
            Set(mapTitle, model.TitleText);
            Set(mapSubtitle, model.SubtitleText);
            Set(mapFeedback, model.FeedbackText);
            mapRouteView?.Set(
                model,
                game?.Config?.hud?.map,
                nodeId => Run(new HudCommand(HudCommandType.SelectMapNode, 0, nodeId)));
        }

        void RefreshMapEvent()
        {
            var model = intermissionPresenter.BuildMapEventPanel(game);
            Set(mapEventTitle, model.TitleText);
            Set(mapEventSubtitle, model.SubtitleText);
            if (mapEventOptionsContent == null)
                return;

            EnsureCards(mapEventCards, mapEventOptionsContent, model.Options.Count);
            ApplyMapEventCardLayout(mapEventOptionsContent);
            for (var index = 0; index < model.Options.Count; index++)
            {
                var option = model.Options[index];
                var view = mapEventCards[index];
                view.gameObject.SetActive(true);
                view.Set(option.Title, "路线事件", string.Empty, option.Description, option.ButtonText, new Color(0.66f, 0.48f, 0.92f, 1f));
                view.SetInteractable(true);
                var capturedIndex = option.Index;
                view.SetAction(() => Run(new HudCommand(HudCommandType.ChooseMapEventOption, capturedIndex)));
            }
        }

        void RefreshBlockOperations()
        {
            var model = intermissionPresenter.BuildBlockOperationsPanel(game);
            Set(blockOperationsTitle, model.TitleText);
            Set(blockOperationsSubtitle, model.SubtitleText);
            Set(blockOperationsHint, model.HintText);
            Set(blockOperationsFeedback, model.FeedbackText);
            Set(blockOperationsActiveTitle, model.ActiveColumnTitle);
            Set(blockOperationsReserveTitle, model.ReserveColumnTitle);
            Set(blockOperationsDeleteStatus, model.DeleteStatusText);
            Set(blockOperationsSwapStatus, model.SwapStatusText);
            SetButtonLabel(blockOperationsCloseButton, model.CloseButtonText);

            if (blockOperationsActiveContent == null || blockOperationsReserveContent == null)
                return;

            EnsureBlockOperationEntries(blockOperationActiveEntries, blockOperationsActiveContent, model.ActiveCards.Count);
            for (var index = 0; index < model.ActiveCards.Count; index++)
            {
                var card = model.ActiveCards[index];
                var view = blockOperationActiveEntries[index];
                var tooltip = BlockPresentationUtility.BuildTooltip(card);
                view.gameObject.SetActive(true);
                view.Set(
                    card.cardName,
                    $"{BlockIcon(card.baseBlockType)} {Format(card)}",
                    model.AllowDelete ? "选择替换，或直接删除" : "选择后可与背包方块替换",
                    BlockColor(card),
                    selectedBlockOperationActiveId == card.id,
                    () => ClickBlockOperationCard(card, true),
                    model.AllowDelete ? () => Run(new HudCommand(HudCommandType.TryRemoveBlock, 0, card.id)) : null,
                    model.AllowDelete ? "删除" : string.Empty);
                view.SetTooltip(tooltip.Title, tooltip.Body, tooltip.AccentColor, this);
            }

            EnsureBlockOperationEntries(blockOperationReserveEntries, blockOperationsReserveContent, model.ReserveCards.Count);
            for (var index = 0; index < model.ReserveCards.Count; index++)
            {
                var card = model.ReserveCards[index];
                var view = blockOperationReserveEntries[index];
                var tooltip = BlockPresentationUtility.BuildTooltip(card);
                view.gameObject.SetActive(true);
                view.Set(
                    card.cardName,
                    $"{BlockIcon(card.baseBlockType)} {Format(card)}",
                    model.AllowSwap ? "选择后可与上阵方块替换" : "当前规则不允许替换",
                    BlockColor(card),
                    selectedBlockOperationReserveId == card.id,
                    () => ClickBlockOperationCard(card, false),
                    null,
                    string.Empty);
                view.SetTooltip(tooltip.Title, tooltip.Body, tooltip.AccentColor, this);
            }
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
                view.SetSocketCount(0);
                return;
            }

            var blockVisual = BlockPresentationUtility.GetBlockVisual(game?.Config?.board, model.Card);
            var tooltip = BlockPresentationUtility.BuildTooltip(model.Card);
            view.SetTypeVisual(model.Card, blockVisual, () => ClickBlock(model.Card, activeSection));
            view.SetTypeTooltip(tooltip.Title, tooltip.Body, tooltip.AccentColor, this);

            view.SetSocketCount(model.Card.sockets.Count);
            for (var index = 0; index < model.Card.sockets.Count; index++)
            {
                var socket = model.Card.sockets[index];
                var fallbackText = socket.isUnlocked
                    ? socket.installedSticker != null ? StickerShort(socket.installedSticker) : "+"
                    : "L";
                var iconSprite = socket.installedSticker?.data?.iconSprite;
                var color = socket.isUnlocked
                    ? socket.installedSticker != null
                        ? StickerColor(socket.installedSticker.data.rarity)
                        : (CanInstall(model.Card, socket) ? new Color(0.42f, 0.86f, 0.54f, 1f) : new Color(0.32f, 0.36f, 0.44f, 1f))
                    : new Color(0.22f, 0.24f, 0.28f, 1f);
                var capturedIndex = index;
                view.SetSocket(
                    index,
                    fallbackText,
                    iconSprite,
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
            if (!socket.isUnlocked || socket.installedSticker != null || !game.CanManageStickerLoadout)
                return false;

            var cardMask = CardMask(card.baseBlockType);
            return (socket.targetMask & cardMask) != 0 && (dragging.data.targetBlockType & cardMask) != 0;
        }

        void ClickSocket(BlockCardState card, int socketIndex)
        {
            if (card == null || !game.CanManageStickerLoadout)
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
            if (game == null || !game.CanManageStickerLoadout || game.IsSettingsOpen || string.IsNullOrWhiteSpace(runtimeId))
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
            if (game == null || card == null || !game.CanManageStickerLoadout)
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

            var tooltip = BlockPresentationUtility.BuildTooltip(card);
            SetTooltip(tooltip.Title, tooltip.Body, tooltip.AccentColor);
        }

        void ClickBlockOperationCard(BlockCardState card, bool activeSection)
        {
            if (card == null || !game.CanManageBlockAssignments)
                return;

            if (activeSection)
            {
                if (!string.IsNullOrEmpty(selectedBlockOperationReserveId))
                {
                    Run(new HudCommand(HudCommandType.TrySwapActiveReserve, 0, card.id, selectedBlockOperationReserveId));
                    selectedBlockOperationActiveId = null;
                    selectedBlockOperationReserveId = null;
                    return;
                }

                selectedBlockOperationActiveId = selectedBlockOperationActiveId == card.id ? null : card.id;
                selectedBlockOperationReserveId = null;
                return;
            }

            if (!string.IsNullOrEmpty(selectedBlockOperationActiveId))
            {
                Run(new HudCommand(HudCommandType.TrySwapActiveReserve, 0, selectedBlockOperationActiveId, card.id));
                selectedBlockOperationActiveId = null;
                selectedBlockOperationReserveId = null;
                return;
            }

            selectedBlockOperationReserveId = selectedBlockOperationReserveId == card.id ? null : card.id;
            selectedBlockOperationActiveId = null;
        }

        string BuildBlockHint(string hint)
        {
            return "右侧只显示上阵方块。悬停可查看详情。";
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

        void EnsureRuntimeMapUi()
        {
            var modalRoot = ROptional("ModalRoot");
            if (modalRoot == null)
                return;

            EnsureMapModalRuntime(modalRoot);
            EnsureMapEventModalRuntime(modalRoot);
        }

        void EnsureMapModalRuntime(RectTransform modalRoot)
        {
            var existing = modalRoot.Find("MapModal") as RectTransform;
            if (existing != null)
            {
                var existingWindow = existing.Find("Window") as RectTransform;
                if (existingWindow != null)
                    EnsureMapRouteViewRuntime(existingWindow);
                return;
            }

            var modal = CanvasUiFactory.Node("MapModal", modalRoot);
            Stretch(modal);
            modal.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.58f);
            modal.gameObject.SetActive(false);
            modal.SetAsLastSibling();

            var window = CanvasUiFactory.Node("Window", modal);
            window.anchorMin = new Vector2(0.5f, 0.5f);
            window.anchorMax = new Vector2(0.5f, 0.5f);
            window.pivot = new Vector2(0.5f, 0.5f);
            window.sizeDelta = new Vector2(1220f, 780f);
            window.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.15f, 0.98f);

            var header = CanvasUiFactory.Node("Header", window);
            SetTopStretch(header, 20f, 20f, 18f, 112f);
            var title = CanvasUiFactory.Text("TitleText", header, 36, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            SetTopStretch(title.rectTransform, 12f, 12f, 4f, 42f);
            var subtitle = CanvasUiFactory.Text("SubtitleText", header, 18, new Color(0.82f, 0.86f, 0.94f, 1f), TextAlignmentOptions.Center);
            SetTopStretch(subtitle.rectTransform, 12f, 12f, 48f, 28f);
            var feedback = CanvasUiFactory.Text("FeedbackText", header, 17, new Color(0.76f, 0.9f, 1f, 1f), TextAlignmentOptions.Center, FontStyles.Bold);
            SetTopStretch(feedback.rectTransform, 12f, 12f, 80f, 26f);

            var body = CanvasUiFactory.Node("Body", window);
            SetFill(body, 20f, 20f, 138f, 20f);

            EnsureMapRouteViewRuntime(window);
        }

        void EnsureMapRouteViewRuntime(RectTransform window)
        {
            var body = window.Find("Body") as RectTransform;
            if (body == null)
            {
                body = CanvasUiFactory.Node("Body", window);
                SetFill(body, 20f, 20f, 138f, 20f);
            }

            var legacyNodes = body.Find("NodesPanel");
            if (legacyNodes != null)
                legacyNodes.gameObject.SetActive(false);
            var legacyConnections = body.Find("ConnectionsPanel");
            if (legacyConnections != null)
                legacyConnections.gameObject.SetActive(false);

            if (body.Find("RouteView") == null)
                CanvasMapRouteView.Create(body);
        }

        void EnsureMapEventModalRuntime(RectTransform modalRoot)
        {
            var existing = modalRoot.Find("MapEventModal") as RectTransform;
            if (existing != null)
            {
                var existingWindow = existing.Find("Window") as RectTransform;
                if (existingWindow != null)
                    EnsureMapEventOptionsRuntime(existingWindow);
                return;
            }

            var modal = CanvasUiFactory.Node("MapEventModal", modalRoot);
            Stretch(modal);
            modal.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.62f);
            modal.gameObject.SetActive(false);
            modal.SetAsLastSibling();

            var window = CanvasUiFactory.Node("Window", modal);
            window.anchorMin = new Vector2(0.5f, 0.5f);
            window.anchorMax = new Vector2(0.5f, 0.5f);
            window.pivot = new Vector2(0.5f, 0.5f);
            window.sizeDelta = new Vector2(960f, 640f);
            window.gameObject.AddComponent<Image>().color = new Color(0.09f, 0.1f, 0.16f, 0.98f);

            var header = CanvasUiFactory.Node("Header", window);
            SetTopStretch(header, 20f, 20f, 18f, 96f);
            var title = CanvasUiFactory.Text("TitleText", header, 34, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            SetTopStretch(title.rectTransform, 12f, 12f, 8f, 38f);
            var subtitle = CanvasUiFactory.Text("SubtitleText", header, 18, new Color(0.82f, 0.86f, 0.94f, 1f), TextAlignmentOptions.Center);
            SetTopStretch(subtitle.rectTransform, 12f, 12f, 50f, 30f);

            var body = CanvasUiFactory.Node("Body", window);
            SetFill(body, 20f, 20f, 124f, 20f);
            EnsureMapEventOptionsRuntime(window);
        }

        void EnsureMapEventOptionsRuntime(RectTransform window)
        {
            var body = window.Find("Body") as RectTransform;
            if (body == null)
            {
                body = CanvasUiFactory.Node("Body", window);
                SetFill(body, 20f, 20f, 124f, 20f);
            }

            if (body.Find("ScrollView") != null)
                return;

            var scroll = CreateHorizontalOptionsArea("ScrollView", body);
            SetFill(scroll, 0f, 0f, 0f, 0f);
        }

        void EnsureRuntimeBlockOperationsUi()
        {
            var modalRoot = ROptional("ModalRoot");
            if (modalRoot == null)
                return;

            var legacyDeletePanel = modalRoot.Find("ShopModal/Window/Body/DeletePanel");
            if (legacyDeletePanel != null)
                legacyDeletePanel.gameObject.SetActive(false);

            EnsureShopBlockOperationsButton(modalRoot);
            EnsureBlockOperationsModalRuntime(modalRoot);
        }

        void EnsureShopBlockOperationsButton(RectTransform modalRoot)
        {
            var footer = modalRoot.Find("ShopModal/Window/Footer") as RectTransform;
            if (footer == null)
                return;

            if (footer.Find("BlockOperationsButton") == null)
            {
                var button = CanvasUiFactory.Button(
                    "BlockOperationsButton",
                    footer,
                    "方块操作",
                    new Color(0.16f, 0.23f, 0.62f, 0.96f),
                    Color.white,
                    20);
                button.transform.SetSiblingIndex(Mathf.Max(0, footer.childCount - 1));
            }

            LayoutFooterButtons(footer, 160f, 44f, 10f);
        }

        void EnsureBlockOperationsModalRuntime(RectTransform modalRoot)
        {
            if (modalRoot.Find("BlockOperationsModal") != null)
                return;

            var modal = CanvasUiFactory.Node("BlockOperationsModal", modalRoot);
            Stretch(modal);
            var overlay = modal.gameObject.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.62f);
            modal.gameObject.SetActive(false);
            modal.SetAsLastSibling();

            var window = CanvasUiFactory.Node("Window", modal);
            window.anchorMin = new Vector2(0.5f, 0.5f);
            window.anchorMax = new Vector2(0.5f, 0.5f);
            window.pivot = new Vector2(0.5f, 0.5f);
            window.sizeDelta = new Vector2(1180f, 760f);
            var windowImage = window.gameObject.AddComponent<Image>();
            windowImage.color = new Color(0.09f, 0.11f, 0.16f, 0.98f);

            var header = CanvasUiFactory.Node("Header", window);
            SetTopStretch(header, 18f, 18f, 18f, 104f);
            var title = CanvasUiFactory.Text("TitleText", header, 34, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            SetTopStretch(title.rectTransform, 12f, 12f, 8f, 38f);
            var subtitle = CanvasUiFactory.Text("SubtitleText", header, 18, new Color(0.82f, 0.86f, 0.94f, 1f), TextAlignmentOptions.Center);
            SetTopStretch(subtitle.rectTransform, 12f, 12f, 48f, 30f);

            var body = CanvasUiFactory.Node("Body", window);
            SetFill(body, 18f, 18f, 134f, 102f);

            var hint = CanvasUiFactory.Text("HintText", body, 18, new Color(0.9f, 0.92f, 0.97f, 1f), TextAlignmentOptions.TopLeft);
            SetTopStretch(hint.rectTransform, 12f, 12f, 12f, 44f);
            var feedback = CanvasUiFactory.Text("FeedbackText", body, 16, new Color(0.72f, 0.86f, 1f, 1f), TextAlignmentOptions.TopLeft);
            SetTopStretch(feedback.rectTransform, 12f, 12f, 66f, 32f);

            var statusRow = CanvasUiFactory.Node("StatusRow", body);
            SetTopStretch(statusRow, 12f, 12f, 108f, 28f);
            var deleteStatus = CanvasUiFactory.Text("DeleteStatusText", statusRow, 16, new Color(1f, 0.9f, 0.62f, 1f), TextAlignmentOptions.Left, FontStyles.Bold);
            SetLeftStretch(deleteStatus.rectTransform, 0f, 6f, 0f, 0f, 0.5f);
            var swapStatus = CanvasUiFactory.Text("SwapStatusText", statusRow, 16, new Color(0.82f, 0.96f, 1f, 1f), TextAlignmentOptions.Right, FontStyles.Bold);
            SetRightStretch(swapStatus.rectTransform, 6f, 0f, 0f, 0f, 0.5f);

            var columns = CanvasUiFactory.Node("Columns", body);
            SetFill(columns, 12f, 12f, 148f, 12f);

            var activeColumn = CanvasUiFactory.Node("ActiveColumn", columns);
            SetLeftStretch(activeColumn, 0f, 6f, 0f, 0f, 0.5f);
            activeColumn.gameObject.AddComponent<Image>().color = new Color(0.14f, 0.17f, 0.22f, 0.86f);
            var activeColumnTitle = CanvasUiFactory.Text("TitleText", activeColumn, 22, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            SetTopStretch(activeColumnTitle.rectTransform, 12f, 12f, 8f, 28f);
            var activeScroll = CreateScrollArea("ScrollView", activeColumn);
            SetFill(activeScroll, 12f, 12f, 44f, 12f);

            var reserveColumn = CanvasUiFactory.Node("ReserveColumn", columns);
            SetRightStretch(reserveColumn, 6f, 0f, 0f, 0f, 0.5f);
            reserveColumn.gameObject.AddComponent<Image>().color = new Color(0.14f, 0.17f, 0.22f, 0.86f);
            var reserveColumnTitle = CanvasUiFactory.Text("TitleText", reserveColumn, 22, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            SetTopStretch(reserveColumnTitle.rectTransform, 12f, 12f, 8f, 28f);
            var reserveScroll = CreateScrollArea("ScrollView", reserveColumn);
            SetFill(reserveScroll, 12f, 12f, 44f, 12f);

            var footer = CanvasUiFactory.Node("Footer", window);
            SetBottomStretch(footer, 18f, 18f, 18f, 72f);
            CanvasUiFactory.Button(
                "CloseButton",
                footer,
                "关闭",
                new Color(0.16f, 0.23f, 0.62f, 0.96f),
                Color.white,
                20);
            LayoutFooterButtons(footer, 200f, 44f, 10f);
        }

        static RectTransform CreateScrollArea(string name, Transform parent)
        {
            var scroll = CanvasUiFactory.Node(name, parent);
            scroll.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.88f);

            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;

            var viewport = CanvasUiFactory.Node("Viewport", scroll);
            Stretch(viewport);
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            var mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var content = CanvasUiFactory.Node("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            return scroll;
        }

        static RectTransform CreateHorizontalOptionsArea(string name, Transform parent)
        {
            var scroll = CanvasUiFactory.Node(name, parent);
            scroll.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.15f, 0.9f);

            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;

            var viewport = CanvasUiFactory.Node("Viewport", scroll);
            Stretch(viewport);
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            var mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var content = CanvasUiFactory.Node("Content", viewport);
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 18, 18);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            return scroll;
        }

        static void LayoutFooterButtons(RectTransform footer, float width, float height, float spacing)
        {
            if (footer == null)
                return;

            var buttons = new List<RectTransform>();
            for (var index = 0; index < footer.childCount; index++)
            {
                if (footer.GetChild(index) is RectTransform child && child.GetComponent<Button>() != null)
                    buttons.Add(child);
            }

            if (buttons.Count == 0)
                return;

            var totalWidth = buttons.Count * width + Mathf.Max(0, buttons.Count - 1) * spacing;
            var startX = -totalWidth * 0.5f + width * 0.5f;
            for (var index = 0; index < buttons.Count; index++)
            {
                var button = buttons[index];
                button.anchorMin = new Vector2(0.5f, 0.5f);
                button.anchorMax = new Vector2(0.5f, 0.5f);
                button.pivot = new Vector2(0.5f, 0.5f);
                button.sizeDelta = new Vector2(width, height);
                button.anchoredPosition = new Vector2(startX + index * (width + spacing), 0f);
            }
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        static void SetFill(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        static void SetTopStretch(RectTransform rect, float left, float right, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -(top + height));
            rect.offsetMax = new Vector2(-right, -top);
        }

        static void SetBottomStretch(RectTransform rect, float left, float right, float bottom, float height)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, bottom + height);
        }

        static void SetLeftStretch(RectTransform rect, float left, float gap, float top, float bottom, float widthPercent)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(widthPercent, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-gap, -top);
        }

        static void SetRightStretch(RectTransform rect, float gap, float right, float top, float bottom, float widthPercent)
        {
            rect.anchorMin = new Vector2(widthPercent, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(gap, bottom);
            rect.offsetMax = new Vector2(-right, -top);
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

        static void ApplyMapEventCardLayout(RectTransform root)
        {
            if (root == null)
                return;

            var viewport = root.parent as RectTransform;
            var scroll = viewport != null ? viewport.parent as RectTransform : null;
            var width = scroll != null ? scroll.rect.width : root.rect.width;
            var height = scroll != null ? scroll.rect.height : root.rect.height;
            var cardWidth = Mathf.Clamp((Mathf.Max(720f, width) - 64f) / 3f, 240f, 320f);
            var cardHeight = Mathf.Max(240f, height - 36f);

            for (var index = 0; index < root.childCount; index++)
            {
                if (root.GetChild(index) is not RectTransform child)
                    continue;

                var layout = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
                layout.preferredWidth = cardWidth;
                layout.minWidth = Mathf.Min(220f, cardWidth);
                layout.preferredHeight = cardHeight;
                layout.minHeight = Mathf.Min(220f, cardHeight);
            }
        }

        void EnsureEntries(List<CanvasListEntryView> entries, RectTransform root, int count)
        {
            while (entries.Count < count)
                entries.Add(CanvasListEntryView.Create(root));
            HideExtra(entries, count);
        }

        void EnsureBlockOperationEntries(List<CanvasBlockOperationEntryView> entries, RectTransform root, int count)
        {
            while (entries.Count < count)
                entries.Add(CanvasBlockOperationEntryView.Create(root));
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

        static void HideExtra(List<CanvasBlockOperationEntryView> entries, int usedCount)
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
