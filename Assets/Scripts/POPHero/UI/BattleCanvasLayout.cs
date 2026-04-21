using UnityEngine;
using UnityEngine.UI;

namespace POPHero
{
    public sealed class BattleCanvasLayout : MonoBehaviour
    {
        readonly struct LayoutPreset
        {
            public LayoutPreset(
                float margin,
                float topBarHeight,
                float statusWidth,
                float combatWidth,
                float combatHeight,
                float damageWidth,
                float damageGap,
                float railWidth,
                int combatButtonColumns,
                Vector2 combatButtonSize,
                Vector2 blockRewardSize,
                Vector2 rewardSize,
                Vector2 shopSize,
                Vector2 loadoutSize,
                Vector2 gameOverSize)
            {
                Margin = margin;
                TopBarHeight = topBarHeight;
                StatusWidth = statusWidth;
                CombatWidth = combatWidth;
                CombatHeight = combatHeight;
                DamageWidth = damageWidth;
                DamageGap = damageGap;
                RailWidth = railWidth;
                CombatButtonColumns = combatButtonColumns;
                CombatButtonSize = combatButtonSize;
                BlockRewardSize = blockRewardSize;
                RewardSize = rewardSize;
                ShopSize = shopSize;
                LoadoutSize = loadoutSize;
                GameOverSize = gameOverSize;
            }

            public float Margin { get; }
            public float TopBarHeight { get; }
            public float StatusWidth { get; }
            public float CombatWidth { get; }
            public float CombatHeight { get; }
            public float DamageWidth { get; }
            public float DamageGap { get; }
            public float RailWidth { get; }
            public int CombatButtonColumns { get; }
            public Vector2 CombatButtonSize { get; }
            public Vector2 BlockRewardSize { get; }
            public Vector2 RewardSize { get; }
            public Vector2 ShopSize { get; }
            public Vector2 LoadoutSize { get; }
            public Vector2 GameOverSize { get; }
        }

        RectTransform canvasRoot;
        RectTransform topLeftZone;
        RectTransform bottomLeftZone;
        RectTransform rightRailZone;
        RectTransform centerModalZone;

        RectTransform topStatusBar;
        RectTransform statusPanel;
        RectTransform combatPanel;
        RectTransform combatButtons;
        RectTransform damagePanel;
        RectTransform blockPanel;
        RectTransform activeSection;
        RectTransform reserveSection;
        RectTransform activeScrollView;
        RectTransform reserveScrollView;

        RectTransform blockRewardWindow;
        RectTransform blockRewardHeader;
        RectTransform blockRewardBody;
        RectTransform blockRewardFooter;
        RectTransform blockRewardScrollView;
        RectTransform blockRewardContent;

        RectTransform rewardWindow;
        RectTransform rewardHeader;
        RectTransform rewardBody;
        RectTransform rewardFooter;
        RectTransform rewardScrollView;
        RectTransform rewardContent;

        RectTransform shopWindow;
        RectTransform shopHeader;
        RectTransform shopBody;
        RectTransform shopFooter;
        RectTransform shopItemsPanel;
        RectTransform shopItemsScrollView;
        RectTransform shopItemsContent;
        RectTransform shopDeletePanel;
        RectTransform shopDeleteTitles;
        RectTransform shopDeleteHint;
        RectTransform shopDeleteColumns;
        RectTransform shopDeleteActiveColumn;
        RectTransform shopDeleteReserveColumn;
        RectTransform shopDeleteActiveScroll;
        RectTransform shopDeleteReserveScroll;

        RectTransform blockOperationsWindow;
        RectTransform blockOperationsHeader;
        RectTransform blockOperationsBody;
        RectTransform blockOperationsFooter;
        RectTransform blockOperationsHint;
        RectTransform blockOperationsFeedback;
        RectTransform blockOperationsStatusRow;
        RectTransform blockOperationsColumns;
        RectTransform blockOperationsActiveColumn;
        RectTransform blockOperationsReserveColumn;
        RectTransform blockOperationsActiveScrollView;
        RectTransform blockOperationsReserveScrollView;

        RectTransform loadoutWindow;
        RectTransform loadoutHeader;
        RectTransform loadoutBody;
        RectTransform loadoutFooter;
        RectTransform loadoutColumns;
        RectTransform loadoutInventoryPanel;
        RectTransform loadoutInventoryTitle;
        RectTransform loadoutInventoryScrollView;
        RectTransform loadoutInventoryContent;
        RectTransform loadoutModsPanel;
        RectTransform loadoutActiveModsTitle;
        RectTransform loadoutActiveModsContent;
        RectTransform loadoutReserveModsTitle;
        RectTransform loadoutReserveModsContent;
        RectTransform loadoutCancelButton;
        RectTransform loadoutContinueButton;

        RectTransform gameOverWindow;
        RectTransform gameOverHeader;
        RectTransform gameOverBody;
        RectTransform gameOverFooter;

        void OnEnable()
        {
            EnsureRefs();
            ApplyLayout();
        }

        void OnRectTransformDimensionsChange()
        {
            ApplyLayout();
        }

        void EnsureRefs()
        {
            canvasRoot ??= transform as RectTransform;

            topLeftZone ??= FindRect("HudRoot/TopLeftZone");
            bottomLeftZone ??= FindRect("HudRoot/BottomLeftZone");
            rightRailZone ??= FindRect("HudRoot/RightRailZone");
            centerModalZone ??= FindRect("ModalRoot/CenterModalZone");

            topStatusBar ??= FindRect("HudRoot/TopStatusBar");
            statusPanel ??= FindRect("HudRoot/StatusPanel");
            combatPanel ??= FindRect("HudRoot/CombatPanel");
            combatButtons ??= FindRect("HudRoot/CombatPanel/Buttons");
            damagePanel ??= FindRect("HudRoot/DamageCounterPanel");
            blockPanel ??= FindRect("HudRoot/BlockManagementPanel");
            activeSection ??= FindRect("HudRoot/BlockManagementPanel/ActiveSection");
            reserveSection ??= FindRect("HudRoot/BlockManagementPanel/ReserveSection");
            activeScrollView ??= FindRect("HudRoot/BlockManagementPanel/ActiveSection/ScrollView");
            reserveScrollView ??= FindRect("HudRoot/BlockManagementPanel/ReserveSection/ScrollView");

            blockRewardWindow ??= FindRect("ModalRoot/BlockRewardModal/Window");
            blockRewardHeader ??= FindRect("ModalRoot/BlockRewardModal/Window/Header");
            blockRewardBody ??= FindRect("ModalRoot/BlockRewardModal/Window/Body");
            blockRewardFooter ??= FindRect("ModalRoot/BlockRewardModal/Window/Footer");
            blockRewardScrollView ??= FindRect("ModalRoot/BlockRewardModal/Window/Body/ScrollView");
            blockRewardContent ??= FindRect("ModalRoot/BlockRewardModal/Window/Body/ScrollView/Viewport/Content");

            rewardWindow ??= FindRect("ModalRoot/RewardModal/Window");
            rewardHeader ??= FindRect("ModalRoot/RewardModal/Window/Header");
            rewardBody ??= FindRect("ModalRoot/RewardModal/Window/Body");
            rewardFooter ??= FindRect("ModalRoot/RewardModal/Window/Footer");
            rewardScrollView ??= FindRect("ModalRoot/RewardModal/Window/Body/ScrollView");
            rewardContent ??= FindRect("ModalRoot/RewardModal/Window/Body/ScrollView/Viewport/Content");

            shopWindow ??= FindRect("ModalRoot/ShopModal/Window");
            shopHeader ??= FindRect("ModalRoot/ShopModal/Window/Header");
            shopBody ??= FindRect("ModalRoot/ShopModal/Window/Body");
            shopFooter ??= FindRect("ModalRoot/ShopModal/Window/Footer");
            shopItemsPanel ??= FindRect("ModalRoot/ShopModal/Window/Body/ItemsPanel");
            shopItemsScrollView ??= FindRect("ModalRoot/ShopModal/Window/Body/ItemsPanel/ItemsScroll");
            shopItemsContent ??= FindRect("ModalRoot/ShopModal/Window/Body/ItemsPanel/ItemsScroll/Viewport/Content");
            shopDeletePanel ??= FindRect("ModalRoot/ShopModal/Window/Body/DeletePanel");
            shopDeleteTitles ??= FindRect("ModalRoot/ShopModal/Window/Body/DeletePanel/Titles");
            shopDeleteHint ??= FindRect("ModalRoot/ShopModal/Window/Body/DeletePanel/HintText");
            shopDeleteColumns ??= FindRect("ModalRoot/ShopModal/Window/Body/DeletePanel/Columns");
            shopDeleteActiveColumn ??= FindRect("ModalRoot/ShopModal/Window/Body/DeletePanel/Columns/ActiveColumn");
            shopDeleteReserveColumn ??= FindRect("ModalRoot/ShopModal/Window/Body/DeletePanel/Columns/ReserveColumn");
            shopDeleteActiveScroll ??= FindRectAny(
                "ModalRoot/ShopModal/Window/Body/DeletePanel/Columns/ActiveColumn/ActiveScroll",
                "ModalRoot/ShopModal/Window/Body/DeletePanel/Columns/ActiveColumn/ActiveContent");
            shopDeleteReserveScroll ??= FindRectAny(
                "ModalRoot/ShopModal/Window/Body/DeletePanel/Columns/ReserveColumn/ReserveScroll",
                "ModalRoot/ShopModal/Window/Body/DeletePanel/Columns/ReserveColumn/ReserveContent");

            blockOperationsWindow ??= FindRect("ModalRoot/BlockOperationsModal/Window");
            blockOperationsHeader ??= FindRect("ModalRoot/BlockOperationsModal/Window/Header");
            blockOperationsBody ??= FindRect("ModalRoot/BlockOperationsModal/Window/Body");
            blockOperationsFooter ??= FindRect("ModalRoot/BlockOperationsModal/Window/Footer");
            blockOperationsHint ??= FindRect("ModalRoot/BlockOperationsModal/Window/Body/HintText");
            blockOperationsFeedback ??= FindRect("ModalRoot/BlockOperationsModal/Window/Body/FeedbackText");
            blockOperationsStatusRow ??= FindRect("ModalRoot/BlockOperationsModal/Window/Body/StatusRow");
            blockOperationsColumns ??= FindRect("ModalRoot/BlockOperationsModal/Window/Body/Columns");
            blockOperationsActiveColumn ??= FindRect("ModalRoot/BlockOperationsModal/Window/Body/Columns/ActiveColumn");
            blockOperationsReserveColumn ??= FindRect("ModalRoot/BlockOperationsModal/Window/Body/Columns/ReserveColumn");
            blockOperationsActiveScrollView ??= FindRect("ModalRoot/BlockOperationsModal/Window/Body/Columns/ActiveColumn/ScrollView");
            blockOperationsReserveScrollView ??= FindRect("ModalRoot/BlockOperationsModal/Window/Body/Columns/ReserveColumn/ScrollView");

            loadoutWindow ??= FindRect("ModalRoot/LoadoutModal/Window");
            loadoutHeader ??= FindRect("ModalRoot/LoadoutModal/Window/Header");
            loadoutBody ??= FindRect("ModalRoot/LoadoutModal/Window/Body");
            loadoutFooter ??= FindRect("ModalRoot/LoadoutModal/Window/Footer");
            loadoutColumns ??= FindRect("ModalRoot/LoadoutModal/Window/Body/Columns");
            loadoutInventoryPanel ??= FindRect("ModalRoot/LoadoutModal/Window/Body/Columns/InventoryPanel");
            loadoutInventoryTitle ??= FindRect("ModalRoot/LoadoutModal/Window/Body/Columns/InventoryPanel/InventoryTitleText");
            loadoutInventoryScrollView ??= FindRect("ModalRoot/LoadoutModal/Window/Body/Columns/InventoryPanel/ScrollView");
            loadoutInventoryContent ??= FindRect("ModalRoot/LoadoutModal/Window/Body/Columns/InventoryPanel/ScrollView/Viewport/Content");
            loadoutModsPanel ??= FindRect("ModalRoot/LoadoutModal/Window/Body/Columns/ModsPanel");
            loadoutActiveModsTitle ??= FindRect("ModalRoot/LoadoutModal/Window/Body/Columns/ModsPanel/ActiveTitleText");
            loadoutActiveModsContent ??= FindRect("ModalRoot/LoadoutModal/Window/Body/Columns/ModsPanel/ActiveContent");
            loadoutReserveModsTitle ??= FindRect("ModalRoot/LoadoutModal/Window/Body/Columns/ModsPanel/ReserveTitleText");
            loadoutReserveModsContent ??= FindRect("ModalRoot/LoadoutModal/Window/Body/Columns/ModsPanel/ReserveContent");
            loadoutCancelButton ??= FindRect("ModalRoot/LoadoutModal/Window/Footer/CancelButton");
            loadoutContinueButton ??= FindRect("ModalRoot/LoadoutModal/Window/Footer/ContinueButton");

            gameOverWindow ??= FindRect("ModalRoot/GameOverModal/Window");
            gameOverHeader ??= FindRect("ModalRoot/GameOverModal/Window/Header");
            gameOverBody ??= FindRect("ModalRoot/GameOverModal/Window/Body");
            gameOverFooter ??= FindRect("ModalRoot/GameOverModal/Window/Footer");
        }

        void ApplyLayout()
        {
            if (canvasRoot == null || canvasRoot.rect.width <= 0f)
                return;

            var preset = ChoosePreset(canvasRoot.rect.width);

            ApplyZones(preset);
            ApplyTopStatusBar(preset);
            ApplyStatusPanel(preset);
            ApplyCombatPanel(preset);
            ApplyDamagePanel(preset);
            ApplyBlockPanel(preset);

            ApplyBlockRewardModal(preset);
            ApplyRewardModal(preset);
            ApplyShopModal(preset);
            ApplyBlockOperationsModal(preset);
            ApplyLoadoutModal(preset);
            ApplyGameOverModal(preset);

            ApplyRewardGrids(preset);
        }

        void ApplyZones(LayoutPreset preset)
        {
            var reservedTop = preset.Margin + preset.TopBarHeight + 12f;

            if (topLeftZone != null)
            {
                SetTopLeft(topLeftZone, preset.Margin, reservedTop, preset.StatusWidth);
                topLeftZone.sizeDelta = new Vector2(preset.StatusWidth, canvasRoot.rect.height * 0.42f);
            }

            if (bottomLeftZone != null)
            {
                var width = Mathf.Max(preset.CombatWidth, preset.DamageWidth);
                var height = preset.CombatHeight + 148f;
                SetBottomLeft(bottomLeftZone, preset.Margin, preset.Margin, width, height);
            }

            if (rightRailZone != null)
                StretchRight(rightRailZone, reservedTop, preset.Margin, preset.Margin, preset.RailWidth);

            if (centerModalZone != null)
            {
                centerModalZone.anchorMin = new Vector2(0f, 0f);
                centerModalZone.anchorMax = new Vector2(1f, 1f);
                centerModalZone.pivot = new Vector2(0.5f, 0.5f);
                centerModalZone.offsetMin = new Vector2(preset.Margin * 2f, preset.Margin);
                centerModalZone.offsetMax = new Vector2(-(preset.RailWidth + preset.Margin * 2f), -reservedTop);
            }
        }

        void ApplyTopStatusBar(LayoutPreset preset)
        {
            if (topStatusBar == null)
                return;

            SetTopStretch(topStatusBar, preset.Margin, preset.Margin, preset.Margin, preset.TopBarHeight);
        }

        void ApplyStatusPanel(LayoutPreset preset)
        {
            if (statusPanel == null)
                return;

            statusPanel.anchorMin = topLeftZone != null ? topLeftZone.anchorMin : new Vector2(0f, 1f);
            statusPanel.anchorMax = topLeftZone != null ? topLeftZone.anchorMax : new Vector2(0f, 1f);
            statusPanel.pivot = topLeftZone != null ? topLeftZone.pivot : new Vector2(0f, 1f);
            statusPanel.anchoredPosition = topLeftZone != null ? topLeftZone.anchoredPosition : new Vector2(preset.Margin, -preset.Margin);
            statusPanel.sizeDelta = new Vector2(preset.StatusWidth, 0f);
        }

        void ApplyCombatPanel(LayoutPreset preset)
        {
            if (combatPanel == null)
                return;

            if (bottomLeftZone != null)
            {
                combatPanel.anchorMin = bottomLeftZone.anchorMin;
                combatPanel.anchorMax = bottomLeftZone.anchorMin;
                combatPanel.pivot = new Vector2(0f, 0f);
                combatPanel.anchoredPosition = bottomLeftZone.anchoredPosition;
                combatPanel.sizeDelta = new Vector2(preset.CombatWidth, preset.CombatHeight);
            }
            else
            {
                SetBottomLeft(combatPanel, preset.Margin, preset.Margin, preset.CombatWidth, preset.CombatHeight);
            }

            if (combatButtons != null)
            {
                var grid = combatButtons.GetComponent<GridLayoutGroup>();
                if (grid != null)
                {
                    grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    grid.constraintCount = preset.CombatButtonColumns;
                    grid.cellSize = preset.CombatButtonSize;
                    grid.spacing = new Vector2(8f, 8f);
                }
            }
        }

        void ApplyDamagePanel(LayoutPreset preset)
        {
            if (damagePanel == null)
                return;

            const float panelHeight = 116f;
            DisableContentFitter(damagePanel);

            if (bottomLeftZone != null)
            {
                damagePanel.anchorMin = bottomLeftZone.anchorMin;
                damagePanel.anchorMax = bottomLeftZone.anchorMin;
                damagePanel.pivot = new Vector2(0f, 0f);
                damagePanel.anchoredPosition = bottomLeftZone.anchoredPosition + new Vector2(0f, preset.CombatHeight + preset.DamageGap);
                damagePanel.sizeDelta = new Vector2(preset.DamageWidth, panelHeight);
            }
            else
            {
                SetBottomLeft(damagePanel, preset.Margin, preset.Margin + preset.CombatHeight + preset.DamageGap, preset.DamageWidth, panelHeight);
            }
        }

        void ApplyBlockPanel(LayoutPreset preset)
        {
            if (blockPanel == null)
                return;

            if (rightRailZone != null)
            {
                blockPanel.anchorMin = rightRailZone.anchorMin;
                blockPanel.anchorMax = rightRailZone.anchorMax;
                blockPanel.pivot = rightRailZone.pivot;
                blockPanel.offsetMin = rightRailZone.offsetMin;
                blockPanel.offsetMax = rightRailZone.offsetMax;
            }
            else
            {
                StretchRight(blockPanel, preset.Margin, preset.Margin, preset.Margin, preset.RailWidth);
            }

            if (activeSection != null)
            {
                var layout = activeSection.GetComponent<LayoutElement>() ?? activeSection.gameObject.AddComponent<LayoutElement>();
                layout.flexibleHeight = 1f;
                layout.minHeight = 520f;
                layout.preferredHeight = 720f;
            }

            if (reserveSection != null)
            {
                reserveSection.gameObject.SetActive(false);
            }

            if (activeScrollView != null)
            {
                var layout = activeScrollView.GetComponent<LayoutElement>() ?? activeScrollView.gameObject.AddComponent<LayoutElement>();
                layout.flexibleHeight = 1f;
                layout.minHeight = 440f;
            }

            if (reserveScrollView != null)
                reserveScrollView.gameObject.SetActive(false);
        }

        void ApplyBlockRewardModal(LayoutPreset preset)
        {
            ApplyModalWindow(blockRewardWindow, preset.BlockRewardSize);
            ApplyStandardModalChrome(blockRewardWindow, blockRewardHeader, blockRewardBody, blockRewardFooter, 104f, 72f);
            if (blockRewardScrollView != null)
                SetFill(blockRewardScrollView, 0f, 0f, 0f, 0f);
        }

        void ApplyRewardModal(LayoutPreset preset)
        {
            ApplyModalWindow(rewardWindow, preset.RewardSize);
            ApplyStandardModalChrome(rewardWindow, rewardHeader, rewardBody, rewardFooter, 104f, 72f);
            if (rewardScrollView != null)
                SetFill(rewardScrollView, 0f, 0f, 0f, 0f);
        }

        void ApplyShopModal(LayoutPreset preset)
        {
            ApplyModalWindow(shopWindow, preset.ShopSize);
            ApplyStandardModalChrome(shopWindow, shopHeader, shopBody, shopFooter, 122f, 72f);

            if (shopBody == null)
                return;

            DisableLayoutGroup(shopBody);
            var bodyHeight = Mathf.Max(360f, shopBody.rect.height);
            var itemsHeight = Mathf.Clamp(bodyHeight * 0.44f, 228f, 340f);

            if (shopItemsPanel != null)
            {
                DisableLayoutGroup(shopItemsPanel);
                SetTopStretch(shopItemsPanel, 0f, 0f, 0f, itemsHeight);
            }

            if (shopItemsScrollView != null)
                SetFill(shopItemsScrollView, 12f, 12f, 12f, 12f);
        }

        void ApplyBlockOperationsModal(LayoutPreset preset)
        {
            ApplyModalWindow(blockOperationsWindow, preset.ShopSize);
            ApplyStandardModalChrome(blockOperationsWindow, blockOperationsHeader, blockOperationsBody, blockOperationsFooter, 104f, 72f);

            if (blockOperationsBody == null)
                return;

            DisableLayoutGroup(blockOperationsBody);

            const float padding = 12f;
            const float gap = 10f;
            const float hintHeight = 44f;
            const float feedbackHeight = 32f;
            const float statusHeight = 28f;

            if (blockOperationsHint != null)
                SetTopStretch(blockOperationsHint, padding, padding, padding, hintHeight);

            if (blockOperationsFeedback != null)
                SetTopStretch(blockOperationsFeedback, padding, padding, padding + hintHeight + gap, feedbackHeight);

            if (blockOperationsStatusRow != null)
                SetTopStretch(blockOperationsStatusRow, padding, padding, padding + hintHeight + feedbackHeight + gap * 2f, statusHeight);

            if (blockOperationsColumns != null)
            {
                DisableLayoutGroup(blockOperationsColumns);
                SetFill(
                    blockOperationsColumns,
                    padding,
                    padding,
                    padding + hintHeight + feedbackHeight + statusHeight + gap * 3f,
                    padding);
                SplitColumns(blockOperationsColumns, blockOperationsActiveColumn, blockOperationsReserveColumn, 12f, 0.5f);
            }

            if (blockOperationsActiveScrollView != null)
                SetFill(blockOperationsActiveScrollView, 0f, 0f, 36f, 0f);

            if (blockOperationsReserveScrollView != null)
                SetFill(blockOperationsReserveScrollView, 0f, 0f, 36f, 0f);
        }

        void ApplyLoadoutModal(LayoutPreset preset)
        {
            ApplyLoadoutWindow(preset.LoadoutSize);
            ApplyStandardModalChrome(loadoutWindow, loadoutHeader, loadoutBody, loadoutFooter, 104f, 72f);

            if (loadoutBody == null || loadoutColumns == null)
                return;

            DisableLayoutGroup(loadoutBody);
            DisableLayoutGroup(loadoutColumns);
            SetFill(loadoutColumns, 0f, 0f, 0f, 0f);

            var totalWidth = Mathf.Max(780f, loadoutColumns.rect.width);
            var gap = canvasRoot.rect.width >= 1600f ? 16f : 12f;
            var inventoryWidth = Mathf.Clamp((totalWidth - gap) * 0.44f, 360f, 520f);

            if (loadoutInventoryPanel != null)
            {
                DisableLayoutGroup(loadoutInventoryPanel);
                DisableContentFitter(loadoutInventoryPanel);
                SetLeftFill(loadoutInventoryPanel, 0f, totalWidth - inventoryWidth, 0f, 0f);
            }

            if (loadoutModsPanel != null)
            {
                DisableLayoutGroup(loadoutModsPanel);
                DisableContentFitter(loadoutModsPanel);
                SetRightFill(loadoutModsPanel, inventoryWidth + gap, 0f, 0f, 0f);
            }

            ApplyLoadoutInventoryLayout();
            ApplyLoadoutModsLayout();
            ApplyLoadoutFooterLayout();
        }

        void ApplyGameOverModal(LayoutPreset preset)
        {
            ApplyModalWindow(gameOverWindow, preset.GameOverSize);
            ApplyStandardModalChrome(gameOverWindow, gameOverHeader, gameOverBody, gameOverFooter, 88f, 72f);
        }

        void ApplyStandardModalChrome(RectTransform window, RectTransform header, RectTransform body, RectTransform footer, float headerHeight, float footerHeight)
        {
            if (window == null)
                return;

            DisableLayoutGroup(window);
            DisableContentFitter(window);

            const float padding = 18f;
            const float gap = 12f;

            if (header != null)
            {
                DisableLayoutGroup(header);
                SetTopStretch(header, padding, padding, padding, headerHeight);
            }

            if (footer != null)
            {
                DisableLayoutGroup(footer);
                SetBottomStretch(footer, padding, padding, padding, footerHeight);
            }

            if (body != null)
            {
                DisableLayoutGroup(body);
                SetFill(body, padding, padding, padding + headerHeight + gap, padding + footerHeight + gap);
            }
        }

        void ApplyRewardGrids(LayoutPreset preset)
        {
            ApplyGrid(blockRewardContent, canvasRoot.rect.width >= 1600f ? 3 : 2, blockRewardWindow != null ? blockRewardWindow.rect.width : preset.BlockRewardSize.x, 236f);
            ApplyGrid(rewardContent, canvasRoot.rect.width >= 1600f ? 3 : 2, rewardWindow != null ? rewardWindow.rect.width : preset.RewardSize.x, 236f);
            ApplyGrid(shopItemsContent, canvasRoot.rect.width >= 1700f ? 3 : 2, shopWindow != null ? shopWindow.rect.width : preset.ShopSize.x, 206f);
        }

        static void ApplyGrid(RectTransform root, int columns, float containerWidth, float cellHeight)
        {
            if (root == null)
                return;

            var grid = root.GetComponent<GridLayoutGroup>();
            if (grid == null)
                return;

            var horizontalPadding = grid.padding.left + grid.padding.right;
            var spacingWidth = grid.spacing.x * Mathf.Max(0, columns - 1);
            var availableWidth = Mathf.Max(280f, containerWidth - 72f - horizontalPadding - spacingWidth);
            var cellWidth = Mathf.Max(220f, availableWidth / Mathf.Max(1, columns));

            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = new Vector2(cellWidth, cellHeight);
        }

        void ApplyModalWindow(RectTransform window, Vector2 size)
        {
            if (window == null)
                return;

            window.anchorMin = new Vector2(0.5f, 0.5f);
            window.anchorMax = new Vector2(0.5f, 0.5f);
            window.pivot = new Vector2(0.5f, 0.5f);
            window.anchoredPosition = Vector2.zero;

            var availableWidth = centerModalZone != null ? Mathf.Max(640f, centerModalZone.rect.width) : canvasRoot.rect.width;
            var availableHeight = centerModalZone != null ? Mathf.Max(360f, centerModalZone.rect.height) : canvasRoot.rect.height;

            window.sizeDelta = new Vector2(
                Mathf.Min(size.x, availableWidth),
                Mathf.Min(size.y, availableHeight - 8f));
        }

        void ApplyLoadoutWindow(Vector2 size)
        {
            if (loadoutWindow == null)
                return;

            loadoutWindow.anchorMin = new Vector2(0.5f, 0.5f);
            loadoutWindow.anchorMax = new Vector2(0.5f, 0.5f);
            loadoutWindow.pivot = new Vector2(0.5f, 0.5f);

            var parent = loadoutWindow.parent as RectTransform;
            var center = Vector2.zero;
            var availableWidth = canvasRoot.rect.width;
            var availableHeight = canvasRoot.rect.height - 8f;

            if (parent != null && centerModalZone != null)
            {
                GetLocalRect(parent, centerModalZone, out var zoneMin, out var zoneMax);
                center = (zoneMin + zoneMax) * 0.5f;

                var safeRight = zoneMax.x;
                if (blockPanel != null)
                {
                    GetLocalRect(parent, blockPanel, out var blockMin, out _);
                    safeRight = Mathf.Min(safeRight, blockMin.x - 24f);
                }

                var leftHalf = center.x - zoneMin.x;
                var rightHalf = safeRight - center.x;
                var safeHalfWidth = Mathf.Max(180f, Mathf.Min(leftHalf, rightHalf));
                availableWidth = Mathf.Max(360f, safeHalfWidth * 2f);
                availableHeight = Mathf.Max(280f, zoneMax.y - zoneMin.y - 8f);
            }

            loadoutWindow.anchoredPosition = center;
            loadoutWindow.sizeDelta = new Vector2(
                Mathf.Min(size.x, availableWidth),
                Mathf.Min(size.y, availableHeight));
        }

        void ApplyLoadoutInventoryLayout()
        {
            const float padding = 12f;
            const float titleHeight = 32f;
            const float gap = 12f;

            if (loadoutInventoryTitle != null)
                SetTopStretch(loadoutInventoryTitle, padding, padding, padding, titleHeight);

            if (loadoutInventoryScrollView != null)
                SetFill(loadoutInventoryScrollView, padding, padding, padding + titleHeight + gap, padding);

            ApplyLoadoutInventoryGrid();
        }

        void ApplyLoadoutInventoryGrid()
        {
            if (loadoutInventoryContent == null)
                return;

            var vertical = loadoutInventoryContent.GetComponent<VerticalLayoutGroup>();
            if (vertical != null)
                DestroyImmediate(vertical);

            var grid = loadoutInventoryContent.GetComponent<GridLayoutGroup>() ?? loadoutInventoryContent.gameObject.AddComponent<GridLayoutGroup>();
            var fitter = loadoutInventoryContent.GetComponent<ContentSizeFitter>() ?? loadoutInventoryContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var containerWidth = loadoutInventoryScrollView != null ? loadoutInventoryScrollView.rect.width : loadoutInventoryContent.rect.width;
            var columns = containerWidth >= 420f ? 4 : 3;
            const float spacing = 10f;
            const float padding = 12f;
            var availableWidth = Mathf.Max(180f, containerWidth - padding * 2f - spacing * Mathf.Max(0, columns - 1));
            var cellSize = Mathf.Clamp(availableWidth / Mathf.Max(1, columns), 76f, 94f);

            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = new Vector2(cellSize, cellSize);
            grid.spacing = new Vector2(spacing, spacing);
            grid.padding = new RectOffset(12, 12, 12, 12);
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        }

        void ApplyLoadoutModsLayout()
        {
            if (loadoutModsPanel == null)
                return;

            const float padding = 12f;
            const float titleHeight = 32f;
            const float titleGap = 8f;
            const float sectionGap = 12f;

            DisableContentFitter(loadoutActiveModsContent);
            DisableContentFitter(loadoutReserveModsContent);

            if (loadoutActiveModsTitle != null)
                SetTopStretch(loadoutActiveModsTitle, padding, padding, padding, titleHeight);

            var contentAvailable = Mathf.Max(
                160f,
                loadoutModsPanel.rect.height - padding * 2f - titleHeight * 2f - titleGap * 2f - sectionGap);
            var topContentHeight = Mathf.Max(72f, contentAvailable * 0.5f);
            var activeContentTop = padding + titleHeight + titleGap;
            var reserveTitleTop = activeContentTop + topContentHeight + sectionGap;

            if (loadoutActiveModsContent != null)
                SetTopStretch(loadoutActiveModsContent, padding, padding, activeContentTop, topContentHeight);

            if (loadoutReserveModsTitle != null)
                SetTopStretch(loadoutReserveModsTitle, padding, padding, reserveTitleTop, titleHeight);

            if (loadoutReserveModsContent != null)
                SetFill(loadoutReserveModsContent, padding, padding, reserveTitleTop + titleHeight + titleGap, padding);
        }

        void ApplyLoadoutFooterLayout()
        {
            if (loadoutFooter == null)
                return;

            const float buttonWidth = 220f;
            const float buttonHeight = 44f;
            const float buttonGap = 12f;

            SetRightAlignedButton(loadoutContinueButton, 0f, buttonWidth, buttonHeight);
            SetRightAlignedButton(loadoutCancelButton, buttonWidth + buttonGap, buttonWidth, buttonHeight);
        }

        static LayoutPreset ChoosePreset(float width)
        {
            if (width >= 1900f)
            {
                return new LayoutPreset(
                    24f,
                    72f,
                    360f,
                    560f,
                    236f,
                    220f,
                    14f,
                    420f,
                    3,
                    new Vector2(156f, 34f),
                    new Vector2(1080f, 620f),
                    new Vector2(1080f, 620f),
                    new Vector2(1180f, 760f),
                    new Vector2(1220f, 760f),
                    new Vector2(720f, 340f));
            }

            if (width >= 1280f)
            {
                return new LayoutPreset(
                    16f,
                    68f,
                    320f,
                    460f,
                    214f,
                    196f,
                    12f,
                    360f,
                    2,
                    new Vector2(150f, 32f),
                    new Vector2(960f, 560f),
                    new Vector2(960f, 560f),
                    new Vector2(1040f, 700f),
                    new Vector2(1100f, 700f),
                    new Vector2(660f, 320f));
            }

            return new LayoutPreset(
                12f,
                64f,
                280f,
                420f,
                198f,
                180f,
                10f,
                320f,
                2,
                new Vector2(136f, 30f),
                new Vector2(900f, 520f),
                new Vector2(900f, 520f),
                new Vector2(980f, 660f),
                new Vector2(1040f, 660f),
                new Vector2(620f, 300f));
        }

        static void DisableLayoutGroup(RectTransform rect)
        {
            if (rect == null)
                return;

            var vertical = rect.GetComponent<VerticalLayoutGroup>();
            if (vertical != null)
                vertical.enabled = false;

            var horizontal = rect.GetComponent<HorizontalLayoutGroup>();
            if (horizontal != null)
                horizontal.enabled = false;
        }

        static void DisableContentFitter(RectTransform rect)
        {
            if (rect == null)
                return;

            var fitter = rect.GetComponent<ContentSizeFitter>();
            if (fitter != null)
                fitter.enabled = false;
        }

        static void SetTopLeft(RectTransform rect, float left, float top, float width)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, 0f);
        }

        static void SetBottomLeft(RectTransform rect, float left, float bottom, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(left, bottom);
            rect.sizeDelta = new Vector2(width, height);
        }

        static void StretchRight(RectTransform rect, float top, float right, float bottom, float width)
        {
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.offsetMin = new Vector2(-width - right, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        static void SetFill(RectTransform rect, float left, float right, float top, float bottom)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        static void SetTopStretch(RectTransform rect, float left, float right, float top, float height)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -(top + height));
            rect.offsetMax = new Vector2(-right, -top);
        }

        static void SetBottomStretch(RectTransform rect, float left, float right, float bottom, float height)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, bottom + height);
        }

        static void SetLeftFill(RectTransform rect, float left, float right, float top, float bottom)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        static void SetRightFill(RectTransform rect, float left, float right, float top, float bottom)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        static void SplitColumns(RectTransform parent, RectTransform left, RectTransform right, float gap, float ratio)
        {
            if (parent == null || left == null || right == null)
                return;

            var width = Mathf.Max(200f, parent.rect.width);
            var leftWidth = Mathf.Clamp((width - gap) * ratio, 120f, width - gap - 120f);
            var rightWidth = Mathf.Max(120f, width - gap - leftWidth);

            left.anchorMin = new Vector2(0f, 0f);
            left.anchorMax = new Vector2(0f, 1f);
            left.pivot = new Vector2(0f, 0.5f);
            left.offsetMin = new Vector2(0f, 0f);
            left.offsetMax = new Vector2(leftWidth, 0f);

            right.anchorMin = new Vector2(1f, 0f);
            right.anchorMax = new Vector2(1f, 1f);
            right.pivot = new Vector2(1f, 0.5f);
            right.offsetMin = new Vector2(-rightWidth, 0f);
            right.offsetMax = new Vector2(0f, 0f);
        }

        static void SetRightAlignedButton(RectTransform rect, float rightOffset, float width, float height)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-rightOffset, 0f);
            rect.sizeDelta = new Vector2(width, height);
        }

        static void GetLocalRect(RectTransform parent, RectTransform rect, out Vector2 min, out Vector2 max)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            min = parent.InverseTransformPoint(corners[0]);
            max = parent.InverseTransformPoint(corners[2]);
        }

        RectTransform FindRect(string path)
        {
            return transform.Find(path) as RectTransform;
        }

        RectTransform FindRectAny(params string[] paths)
        {
            foreach (var path in paths)
            {
                var rect = FindRect(path);
                if (rect != null)
                    return rect;
            }

            return null;
        }
    }
}
