using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace POPHero
{
    public class PopHeroHud : MonoBehaviour
    {
        PopHeroGame game;
        IHudCommandSink commandSink;
        StatusPanelPresenter statusPanelPresenter;
        CombatPanelPresenter combatPanelPresenter;
        BlockManagementPresenter blockManagementPresenter;
        IntermissionPanelPresenter intermissionPanelPresenter;
        GUIStyle boxStyle;
        GUIStyle titleStyle;
        GUIStyle textStyle;
        GUIStyle buttonStyle;
        GUIStyle badgeStyle;
        GUIStyle cardStyle;
        GUIStyle tooltipTitleStyle;
        GUIStyle tooltipBodyStyle;
        GUIStyle iconButtonStyle;
        GUIStyle smallCenterLabelStyle;
        GUIStyle tinyLabelStyle;
        Texture2D panelTexture;
        Texture2D cardTexture;
        string hoveredInstallError = string.Empty;
        string hoveredTooltipTitle = string.Empty;
        string hoveredTooltipBody = string.Empty;
        Color hoveredTooltipTitleColor = Color.white;
        string selectedActiveSwapId;
        string selectedReserveSwapId;
        Vector2 blockRewardScroll;
        Vector2 rewardScroll;
        Vector2 shopScroll;
        Vector2 inventoryScroll;
        Vector2 modScroll;

        public void Initialize(PopHeroGame owner)
        {
            game = owner;
            commandSink = owner;
            statusPanelPresenter ??= new StatusPanelPresenter();
            combatPanelPresenter ??= new CombatPanelPresenter();
            blockManagementPresenter ??= new BlockManagementPresenter();
            intermissionPanelPresenter ??= new IntermissionPanelPresenter();
        }

        void Awake()
        {
            game = GetComponent<PopHeroGame>();
            commandSink = game;
            statusPanelPresenter = new StatusPanelPresenter();
            combatPanelPresenter = new CombatPanelPresenter();
            blockManagementPresenter = new BlockManagementPresenter();
            intermissionPanelPresenter = new IntermissionPanelPresenter();
        }

        void OnGUI()
        {
            EnsureStyles();
            hoveredInstallError = string.Empty;
            hoveredTooltipTitle = string.Empty;
            hoveredTooltipBody = string.Empty;
            hoveredTooltipTitleColor = Color.white;
            if (!game.CanManageBlockAssignments)
            {
                selectedActiveSwapId = null;
                selectedReserveSwapId = null;
            }

            DrawStatusPanel();
            DrawCombatPanel();
            DrawBlockManagementPanel();

            if (game.State == RoundState.BlockRewardChoose)
                DrawBlockRewardPanel();

            if (game.State == RoundState.RewardChoose)
                DrawRewardPanel();

            if (game.State == RoundState.Shop)
                DrawShopPanel();

            if (game.State == RoundState.LoadoutManage)
                DrawLoadoutPanel();

            if (game.State == RoundState.GameOver)
                DrawGameOverPanel();

            DrawDraggingSticker();
            DrawTooltip();
        }

        void DrawStatusPanel()
        {
            var model = statusPanelPresenter.Build(game);
            GUILayout.BeginArea(new Rect(16f, 16f, 430f, 292f), boxStyle);
            GUILayout.Label("POPHero Prototype", titleStyle);
            GUILayout.Space(4f);
            GUILayout.Label(model.StateText, badgeStyle);
            GUILayout.Label(model.AimModeText, badgeStyle);
            GUILayout.Label(model.LevelText, badgeStyle);
            GUILayout.Label(model.KillsText, badgeStyle);
            GUILayout.Label(model.BlockText, badgeStyle);
            GUILayout.Space(8f);
            GUILayout.Label(model.HpText, textStyle);
            GUILayout.Label(model.ShieldText, textStyle);
            GUILayout.Label(model.GoldText, textStyle);
            GUILayout.Label(model.InventoryText, textStyle);
            GUILayout.Label(model.LaunchesText, textStyle);
            GUILayout.Space(6f);
            GUILayout.Label(model.EnemyText, badgeStyle);
            GUILayout.Label(model.EnemyHpText, textStyle);
            GUILayout.Label(model.EnemyAttackText, textStyle);
            GUILayout.EndArea();
        }

        void DrawCombatPanel()
        {
            var model = combatPanelPresenter.Build(game);
            GUILayout.BeginArea(new Rect(16f, Screen.height - 220f, 780f, 204f), boxStyle);
            GUILayout.Label("战斗与调试", titleStyle);
            GUILayout.Label(model.RoundAttackText, badgeStyle);
            GUILayout.Label(model.RoundShieldText, badgeStyle);
            GUILayout.Label(model.RoundHitText, badgeStyle);
            if (!string.IsNullOrWhiteSpace(model.PreviewText))
                GUILayout.Label(model.PreviewText, textStyle);
            if (!string.IsNullOrWhiteSpace(model.IntermissionText))
                GUILayout.Label(model.IntermissionText, textStyle);
            if (!string.IsNullOrWhiteSpace(hoveredInstallError))
                GUILayout.Label(hoveredInstallError, textStyle);

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("切换瞄准模式", buttonStyle, GUILayout.Width(140f)))
                RunCommand(new HudCommand(HudCommandType.ToggleAimMode));
            if (GUILayout.Button("重排方块", buttonStyle, GUILayout.Width(110f)))
                RunCommand(new HudCommand(HudCommandType.DebugShuffleBoard));
            if (GUILayout.Button("金币 +25", buttonStyle, GUILayout.Width(110f)))
                RunCommand(new HudCommand(HudCommandType.DebugAddGold, 25));
            if (GUILayout.Button("击败敌人", buttonStyle, GUILayout.Width(110f)))
                RunCommand(new HudCommand(HudCommandType.DebugKillEnemy));
            if (GUILayout.Button("玩家 -10 生命", buttonStyle, GUILayout.Width(130f)))
                RunCommand(new HudCommand(HudCommandType.DebugDamagePlayer, 10));
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void DrawBlockManagementPanel()
        {
            var model = blockManagementPresenter.Build(game);
            var panelWidth = Mathf.Min(390f, Screen.width * 0.36f);
            var panelRect = new Rect(Screen.width - panelWidth - 16f, 16f, panelWidth, Screen.height - 32f);
            GUI.Box(panelRect, GUIContent.none, boxStyle);

            var x = panelRect.x + 12f;
            var y = panelRect.y + 12f;
            var width = panelRect.width - 24f;
            GUI.Label(new Rect(x, y, width, 22f), model.HeaderText, titleStyle);
            y += 24f;
            GUI.Label(new Rect(x, y, width, 36f), model.HintText, textStyle);
            y += 42f;

            y = DrawCompactSection(panelRect, y, "上阵", model.ActiveRows, true);
            y += 10f;
            DrawCompactSection(panelRect, y, "仓库", model.ReserveRows, false);
        }

        float DrawCompactSection(Rect panelRect, float y, string title, System.Collections.Generic.IReadOnlyList<BlockRowModel> rows, bool isActiveSection)
        {
            var x = panelRect.x + 12f;
            var width = panelRect.width - 24f;
            GUI.Label(new Rect(x, y, width, 20f), $"{title} {(isActiveSection ? game.BlockCollections.ActiveCardCount : game.BlockCollections.ReserveCardCount)}/{rows.Count}", badgeStyle);
            y += 24f;

            for (var index = 0; index < rows.Count; index++)
            {
                var rowRect = new Rect(x, y, width, 34f);
                DrawCompactRow(rowRect, rows[index].DisplayIndex, rows[index].Card, isActiveSection);
                y += 38f;
            }

            return y;
        }

        void DrawCompactRow(Rect rowRect, int displayIndex, BlockCardState card, bool isActiveSection)
        {
            var selected = card != null && ((isActiveSection && selectedActiveSwapId == card.id) || (!isActiveSection && selectedReserveSwapId == card.id));
            var previousColor = GUI.color;
            GUI.color = selected ? new Color(1f, 0.92f, 0.45f, 1f) : Color.white;
            GUI.Box(rowRect, GUIContent.none, cardStyle);
            GUI.color = previousColor;

            GUI.Label(new Rect(rowRect.x + 6f, rowRect.y + 6f, 28f, 20f), $"{displayIndex + 1:00}", badgeStyle);

            if (card == null)
            {
                GUI.Label(new Rect(rowRect.x + 42f, rowRect.y + 7f, rowRect.width - 48f, 20f), "空位", tinyLabelStyle);
                return;
            }

            var typeRect = new Rect(rowRect.x + 42f, rowRect.y + 4f, 28f, 26f);
            DrawTypeIcon(typeRect, card, isActiveSection);

            var nextX = typeRect.xMax + 8f;
            foreach (var socket in card.sockets)
            {
                if (socket.installedSticker == null)
                    continue;

                var stickerRect = new Rect(nextX, rowRect.y + 5f, 24f, 24f);
                DrawStickerIcon(stickerRect, card, socket.installedSticker);
                nextX += 28f;
            }

            var socketStartX = rowRect.xMax - 6f - card.sockets.Count * 22f;
            for (var socketIndex = 0; socketIndex < card.sockets.Count; socketIndex++)
            {
                var socketRect = new Rect(socketStartX + socketIndex * 22f, rowRect.y + 7f, 18f, 18f);
                DrawSocketStatusIcon(socketRect, card, card.sockets[socketIndex], socketIndex);
                HandleSocketHotspot(socketRect, card, card.sockets[socketIndex], socketIndex);
            }
        }

        void DrawTypeIcon(Rect rect, BlockCardState card, bool isActiveSection)
        {
            var previousColor = GUI.color;
            GUI.color = GetBlockIconColor(card);
            if (GUI.Button(rect, GetBlockTypeIconText(card.baseBlockType), iconButtonStyle))
                HandleBlockIconClick(card, isActiveSection);
            GUI.color = previousColor;

            if (rect.Contains(Event.current.mousePosition))
                SetTooltip(card.cardName, BuildBlockTooltip(card), GetRarityColor(card.rarity));
        }

        void DrawStickerIcon(Rect rect, BlockCardState card, StickerInstance sticker)
        {
            var previousColor = GUI.color;
            GUI.color = GetStickerRarityColor(sticker.data.rarity);
            GUI.Box(rect, ShortStickerText(sticker), iconButtonStyle);
            GUI.color = previousColor;

            if (rect.Contains(Event.current.mousePosition))
                SetTooltip(sticker.data.name, BuildStickerTooltip(card, sticker), GetStickerRarityColor(sticker.data.rarity));
        }

        void DrawSocketStatusIcon(Rect rect, BlockCardState card, SocketSlotState socket, int socketIndex)
        {
            var previewInstallable = CanInstallDraggingSticker(card, socket);
            var previousColor = GUI.color;
            GUI.color = previewInstallable
                ? new Color(0.34f, 0.95f, 0.52f, 1f)
                : !socket.isUnlocked
                    ? new Color(0.28f, 0.3f, 0.36f, 1f)
                    : socket.installedSticker != null
                        ? new Color(0.95f, 0.82f, 0.32f, 1f)
                        : new Color(0.36f, 0.4f, 0.48f, 1f);
            GUI.Box(rect, socket.isUnlocked ? (socket.installedSticker != null ? "●" : "+") : "锁", smallCenterLabelStyle);
            GUI.color = previousColor;

            if (rect.Contains(Event.current.mousePosition))
                SetTooltip($"槽位 {socketIndex + 1}", BuildSocketTooltip(card, socket), Color.white);
        }

        bool CanInstallDraggingSticker(BlockCardState card, SocketSlotState socket)
        {
            var dragging = game.StickerInventory.DraggingSticker;
            if (!game.CanManageBlockAssignments || dragging?.data == null || card == null || socket == null)
                return false;

            if (!socket.isUnlocked || socket.installedSticker != null)
                return false;

            var cardMask = GetMaskForBlock(card.baseBlockType);
            return (socket.targetMask & cardMask) != 0 && (dragging.data.targetBlockType & cardMask) != 0;
        }

        void HandleSocketHotspot(Rect rect, BlockCardState card, SocketSlotState socket, int socketIndex)
        {
            if (!game.CanManageBlockAssignments || card == null || socket == null)
                return;

            if (Event.current.type != EventType.MouseDown || Event.current.button != 0 || !rect.Contains(Event.current.mousePosition))
                return;

            if (game.StickerInventory.DraggingSticker != null)
            {
                RunCommand(new HudCommand(HudCommandType.TryInstallDraggedSticker, socketIndex, card.id));
                hoveredInstallError = string.Empty;

                Event.current.Use();
                return;
            }

            if (socket.installedSticker == null)
                return;

            var stickerName = socket.installedSticker.data?.name ?? "嵌片";
            RunCommand(new HudCommand(HudCommandType.RemoveStickerFromCard, socketIndex, card.id));
            hoveredInstallError = string.Empty;
            game.SetIntermissionMessage($"已卸下 {stickerName}。");
            Event.current.Use();
        }

        void HandleBlockIconClick(BlockCardState card, bool isActiveSection)
        {
            if (!game.CanManageBlockAssignments || card == null)
                return;

            if (isActiveSection)
            {
                if (!string.IsNullOrEmpty(selectedReserveSwapId))
                {
                    RunCommand(new HudCommand(HudCommandType.TrySwapActiveReserve, 0, card.id, selectedReserveSwapId));
                    selectedActiveSwapId = null;
                    selectedReserveSwapId = null;
                    return;
                }

                selectedActiveSwapId = selectedActiveSwapId == card.id ? null : card.id;
                return;
            }

            if (!string.IsNullOrEmpty(selectedActiveSwapId))
            {
                RunCommand(new HudCommand(HudCommandType.TrySwapActiveReserve, 0, selectedActiveSwapId, card.id));
                selectedActiveSwapId = null;
                selectedReserveSwapId = null;
                return;
            }

            selectedReserveSwapId = selectedReserveSwapId == card.id ? null : card.id;
        }

        void RunCommand(HudCommand command)
        {
            commandSink?.ExecuteHudCommand(command);
        }

        void DrawBlockRewardPanel()
        {
            var model = intermissionPanelPresenter.BuildBlockReward(game);
            var panelRect = GetIntermissionRect(1080f, 520f);
            GUILayout.BeginArea(panelRect, boxStyle);
            GUILayout.Label(model.TitleText, titleStyle);
            GUILayout.Label(model.SubtitleText, textStyle);

            var contentRect = new Rect(12f, 74f, panelRect.width - 24f, panelRect.height - 132f);
            GUILayout.BeginArea(contentRect);
            blockRewardScroll = GUILayout.BeginScrollView(blockRewardScroll, false, true);
            var cardWidth = GetModalCardWidth(contentRect.width, Mathf.Max(1, model.Cards.Count), 220f, 310f);
            var cardHeight = Mathf.Clamp(contentRect.height - 24f, 220f, 280f);
            GUILayout.BeginHorizontal();
            for (var index = 0; index < model.Cards.Count; index++)
            {
                var card = model.Cards[index];
                GUILayout.BeginVertical(cardStyle, GUILayout.Width(cardWidth), GUILayout.Height(cardHeight));
                var previousColor = GUI.contentColor;
                GUI.contentColor = card.AccentColor;
                GUILayout.Label(card.DisplayName, titleStyle);
                GUI.contentColor = previousColor;
                GUILayout.Label(card.TypeText, badgeStyle);
                GUILayout.Label(card.RarityText, badgeStyle);
                GUILayout.Label(card.ValueText, badgeStyle);
                GUILayout.Label(card.Description, textStyle, GUILayout.Height(84f));
                GUILayout.FlexibleSpace();
                GUI.enabled = card.CanSelect;
                if (GUILayout.Button(card.SelectButtonText, buttonStyle))
                    RunCommand(new HudCommand(HudCommandType.TrySelectBlockReward, card.Index));
                GUI.enabled = true;
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(12f, panelRect.height - 48f, panelRect.width - 24f, 32f));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (model.ShowSkipButton && GUILayout.Button(model.SkipButtonText, buttonStyle, GUILayout.Width(170f)))
                RunCommand(new HudCommand(HudCommandType.SkipBlockReward));
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            GUILayout.EndArea();
        }

        void DrawRewardPanel()
        {
            var model = intermissionPanelPresenter.BuildRewardPanel(game);
            var panelRect = GetIntermissionRect(1080f, 500f);
            GUILayout.BeginArea(panelRect, boxStyle);
            GUILayout.Label(model.TitleText, titleStyle);
            GUILayout.Label(model.SubtitleText, textStyle);

            var contentRect = new Rect(12f, 68f, panelRect.width - 24f, panelRect.height - 124f);
            GUILayout.BeginArea(contentRect);
            rewardScroll = GUILayout.BeginScrollView(rewardScroll, false, true);
            var cardWidth = GetModalCardWidth(contentRect.width, Mathf.Max(1, model.Cards.Count), model.Cards.Count >= 4 ? 176f : 220f, model.Cards.Count >= 4 ? 214f : 300f);
            var cardHeight = Mathf.Clamp(contentRect.height - 24f, 220f, 260f);
            GUILayout.BeginHorizontal();
            for (var index = 0; index < model.Cards.Count; index++)
            {
                var card = model.Cards[index];
                GUILayout.BeginVertical(cardStyle, GUILayout.Width(cardWidth), GUILayout.Height(cardHeight));
                GUILayout.Label(card.Title, titleStyle);
                GUILayout.Label(card.Description, textStyle, GUILayout.Height(90f));
                GUILayout.Label(card.KindText, badgeStyle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("选择", buttonStyle))
                    RunCommand(new HudCommand(HudCommandType.TrySelectReward, card.Index));
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(12f, panelRect.height - 48f, panelRect.width - 24f, 32f));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(model.RerollButtonText, buttonStyle, GUILayout.Width(160f)))
                RunCommand(new HudCommand(HudCommandType.TryRerollRewardChoices));
            if (GUILayout.Button(model.SkipButtonText, buttonStyle, GUILayout.Width(180f)))
                RunCommand(new HudCommand(HudCommandType.SkipRewardChoices));
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            GUILayout.EndArea();
        }

        void DrawShopPanel()
        {
            var model = intermissionPanelPresenter.BuildShopPanel(game);
            var panelRect = GetIntermissionRect(1160f, 660f);
            GUILayout.BeginArea(panelRect, boxStyle);
            GUILayout.Label(model.TitleText, titleStyle);
            GUILayout.Label(model.SubtitleText, textStyle);

            var contentRect = new Rect(12f, 68f, panelRect.width - 24f, panelRect.height - 124f);
            GUILayout.BeginArea(contentRect);
            shopScroll = GUILayout.BeginScrollView(shopScroll, false, true);

            var itemWidth = GetModalCardWidth(contentRect.width, Mathf.Max(1, model.Items.Count), 132f, 176f);
            var itemHeight = 208f;
            GUILayout.BeginHorizontal();
            for (var index = 0; index < model.Items.Count; index++)
            {
                var item = model.Items[index];
                GUILayout.BeginVertical(cardStyle, GUILayout.Width(itemWidth), GUILayout.Height(itemHeight));
                GUILayout.Label(item.Title, titleStyle);
                GUILayout.Label(item.Description, textStyle, GUILayout.Height(84f));
                GUILayout.Label(item.KindText, badgeStyle);
                GUILayout.Label(item.PriceText, badgeStyle);
                GUILayout.FlexibleSpace();
                GUI.enabled = !item.Purchased;
                if (GUILayout.Button(item.ButtonText, buttonStyle))
                    RunCommand(new HudCommand(HudCommandType.TryBuyShopItem, item.Index));
                GUI.enabled = true;
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

              GUILayout.Space(12f);
              GUILayout.BeginVertical(cardStyle);
              GUILayout.Label("删除方块", titleStyle);
              GUILayout.Label(model.DeleteHintText, model.HasRemovedBlockThisVisit ? badgeStyle : textStyle);

              GUILayout.BeginHorizontal();
              DrawShopRemovalSection("上阵区", model.ActiveCards);
              GUILayout.Space(6f);
              DrawShopRemovalSection("仓库区", model.ReserveCards);
              GUILayout.Space(10f);
              GUILayout.BeginVertical(cardStyle, GUILayout.Width((contentRect.width - 64f) * 0.24f));
              GUILayout.Label("商店状态", titleStyle);
              GUILayout.Label(model.GoldText, badgeStyle);
              GUILayout.Label(model.RerollCostText, badgeStyle);
              if (!string.IsNullOrWhiteSpace(model.LastFeedbackText))
                  GUILayout.Label(model.LastFeedbackText, textStyle);
              GUILayout.EndVertical();
              GUILayout.EndHorizontal();
              GUILayout.EndVertical();

            GUILayout.EndScrollView();
            GUILayout.EndArea();

              GUILayout.BeginArea(new Rect(12f, panelRect.height - 48f, panelRect.width - 24f, 32f));
              GUILayout.BeginHorizontal();
              if (GUILayout.Button(model.RerollButtonText, buttonStyle, GUILayout.Width(150f)))
                  RunCommand(new HudCommand(HudCommandType.TryRerollShop));
              GUILayout.FlexibleSpace();
              if (GUILayout.Button(model.CloseButtonText, buttonStyle, GUILayout.Width(140f)))
                  RunCommand(new HudCommand(HudCommandType.CloseShop));
              GUILayout.EndHorizontal();
              GUILayout.EndArea();
              GUILayout.EndArea();
        }

        void DrawLoadoutPanel()
        {
            var model = intermissionPanelPresenter.BuildLoadoutPanel(game);
            var panelRect = GetIntermissionRect(980f, 560f);
            GUILayout.BeginArea(panelRect, boxStyle);
            GUILayout.Label(model.TitleText, titleStyle);
            GUILayout.Label(model.SubtitleText, textStyle);

            var contentRect = new Rect(12f, 68f, panelRect.width - 24f, panelRect.height - 124f);
            GUILayout.BeginArea(contentRect);
            GUILayout.BeginHorizontal();

            var columnWidth = Mathf.Max(260f, (contentRect.width - 18f) * 0.5f);
              var columnHeight = contentRect.height;

              GUILayout.BeginVertical(cardStyle, GUILayout.Width(columnWidth), GUILayout.Height(columnHeight));
              GUILayout.Label("库存嵌片", titleStyle);
              inventoryScroll = GUILayout.BeginScrollView(inventoryScroll, false, true);
              DrawInventoryGrid(model.Inventory, columnWidth - 28f, columnHeight - 70f);
              GUILayout.EndScrollView();
              GUILayout.EndVertical();

            GUILayout.Space(8f);

              GUILayout.BeginVertical(cardStyle, GUILayout.Width(columnWidth), GUILayout.Height(columnHeight));
              GUILayout.Label("模组栏", titleStyle);
              modScroll = GUILayout.BeginScrollView(modScroll, false, true);
              DrawModGrid(model.ActiveMods, model.ReserveMods, columnWidth - 28f, columnHeight - 70f);
              GUILayout.EndScrollView();
              GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

              GUILayout.BeginArea(new Rect(12f, panelRect.height - 48f, panelRect.width - 24f, 32f));
              GUILayout.BeginHorizontal();
              if (model.CanCancelDrag && GUILayout.Button(model.CancelDragText, buttonStyle, GUILayout.Width(180f)))
                  RunCommand(new HudCommand(HudCommandType.CancelStickerDrag));
              GUILayout.FlexibleSpace();
              if (GUILayout.Button(model.ContinueButtonText, buttonStyle, GUILayout.Width(140f)))
                  RunCommand(new HudCommand(HudCommandType.FinishLoadout));
              GUILayout.EndHorizontal();
              GUILayout.EndArea();
              GUILayout.EndArea();
        }

        void DrawInventoryGrid(System.Collections.Generic.IReadOnlyList<StickerInstance> stickers, float availableWidth, float availableHeight)
        {
            if (stickers.Count == 0)
            {
                GUILayout.Label("当前没有可安装的嵌片。", textStyle);
                return;
            }

            var columns = availableWidth >= 360f ? 3 : 2;
            const float gap = 8f;
            const float tileHeight = 84f;
            var tileWidth = Mathf.Max(120f, (availableWidth - gap * (columns - 1)) / columns);
            var rows = Mathf.CeilToInt(stickers.Count / (float)columns);
            var totalHeight = rows * tileHeight + Mathf.Max(0, rows - 1) * gap;
            var gridRect = GUILayoutUtility.GetRect(availableWidth, totalHeight, GUILayout.Width(availableWidth), GUILayout.Height(totalHeight));

            for (var index = 0; index < stickers.Count; index++)
            {
                var row = index / columns;
                var column = index % columns;
                var rect = new Rect(gridRect.x + column * (tileWidth + gap), gridRect.y + row * (tileHeight + gap), tileWidth, tileHeight);
                DrawInventoryStickerTile(rect, stickers[index]);
            }
        }

        void DrawInventoryStickerTile(Rect rect, StickerInstance sticker)
        {
            GUI.Box(rect, GUIContent.none, cardStyle);
            var titleRect = new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, 18f);
            var maskRect = new Rect(rect.x + 8f, rect.y + 28f, 58f, 18f);
            var bodyRect = new Rect(rect.x + 8f, rect.y + 46f, rect.width - 72f, 18f);
            var buttonRect = new Rect(rect.xMax - 56f, rect.yMax - 28f, 48f, 22f);

            var previousColor = GUI.contentColor;
            GUI.contentColor = GetStickerRarityColor(sticker.data.rarity);
            GUI.Label(titleRect, sticker.data.name, badgeStyle);
            GUI.contentColor = GetSocketMaskColor(sticker.data.targetBlockType);
            GUI.Label(maskRect, GetSocketMaskIconText(sticker.data.targetBlockType), badgeStyle);
            GUI.contentColor = previousColor;
            GUI.Label(bodyRect, sticker.data.mainActionText, tinyLabelStyle);
            if (GUI.Button(buttonRect, "拿起", iconButtonStyle))
                RunCommand(new HudCommand(HudCommandType.BeginStickerDrag, 0, sticker.runtimeId));

            if (rect.Contains(Event.current.mousePosition))
                SetTooltip(sticker.data.name, BuildInventoryStickerTooltip(sticker), GetStickerRarityColor(sticker.data.rarity));
        }

        void DrawModGrid(System.Collections.Generic.IReadOnlyList<ModInstance> activeMods, System.Collections.Generic.IReadOnlyList<ModInstance> reserveMods, float availableWidth, float availableHeight)
        {
            if (activeMods.Count == 0 && reserveMods.Count == 0)
            {
                GUILayout.Label("当前还没有模组。", textStyle);
                return;
            }

            GUILayout.Label("已启用", badgeStyle);
            DrawModTiles(activeMods, true, availableWidth, availableHeight * 0.52f);
            GUILayout.Space(8f);
            GUILayout.Label("备用", badgeStyle);
            DrawModTiles(reserveMods, false, availableWidth, availableHeight * 0.38f);
        }

        void DrawModTiles(System.Collections.Generic.IReadOnlyList<ModInstance> mods, bool isActive, float availableWidth, float availableHeight)
        {
            if (mods.Count == 0)
            {
                GUILayout.Label(isActive ? "当前没有启用模组。" : "当前没有备用模组。", tinyLabelStyle);
                return;
            }

            var columns = availableWidth >= 360f ? 3 : 2;
            const float gap = 8f;
            const float tileHeight = 58f;
            var tileWidth = Mathf.Max(116f, (availableWidth - gap * (columns - 1)) / columns);
            var rows = Mathf.CeilToInt(mods.Count / (float)columns);
            var totalHeight = rows * tileHeight + Mathf.Max(0, rows - 1) * gap;
            var gridRect = GUILayoutUtility.GetRect(availableWidth, totalHeight, GUILayout.Width(availableWidth), GUILayout.Height(totalHeight));

            for (var index = 0; index < mods.Count; index++)
            {
                var row = index / columns;
                var column = index % columns;
                var rect = new Rect(gridRect.x + column * (tileWidth + gap), gridRect.y + row * (tileHeight + gap), tileWidth, tileHeight);
                DrawModTile(rect, mods[index], isActive);
            }
        }

        void DrawModTile(Rect rect, ModInstance mod, bool isActive)
        {
            GUI.Box(rect, GUIContent.none, cardStyle);
            GUI.Label(new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, 18f), mod.data.name, badgeStyle);
            GUI.Label(new Rect(rect.x + 8f, rect.y + 26f, rect.width - 80f, 16f), isActive ? "已启用" : "备用", tinyLabelStyle);
            if (GUI.Button(new Rect(rect.xMax - 64f, rect.yMax - 26f, 56f, 20f), isActive ? "停用" : "启用", iconButtonStyle))
                RunCommand(new HudCommand(HudCommandType.ToggleModActivation, 0, mod.runtimeId));

            if (rect.Contains(Event.current.mousePosition))
                SetTooltip(mod.data.name, mod.data.description, Color.white);
        }

        string BuildInventoryStickerTooltip(StickerInstance sticker)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"稀有度：{GetStickerRarityText(sticker.data.rarity)}");
            builder.AppendLine($"适用：{GetSocketMaskText(sticker.data.targetBlockType)}");
            builder.AppendLine(sticker.data.mainActionText);
            foreach (var line in sticker.data.detailLines)
                builder.AppendLine($"• {line}");
            return builder.ToString().TrimEnd();
        }

        void DrawGameOverPanel()
        {
            GUILayout.BeginArea(new Rect(Screen.width * 0.5f - 200f, Screen.height * 0.5f - 100f, 400f, 196f), boxStyle);
            GUILayout.Label("游戏结束", titleStyle);
            GUILayout.Label(game.GameOverMessage, textStyle);
            GUILayout.Space(10f);
            if (GUILayout.Button("再来一次", buttonStyle, GUILayout.Height(34f)))
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            GUILayout.EndArea();
        }

        void DrawDraggingSticker()
        {
            var dragging = game.StickerInventory.DraggingSticker;
            if (dragging == null)
                return;

            var mouse = Event.current.mousePosition;
            GUI.Box(new Rect(mouse.x + 14f, mouse.y + 14f, 206f, 70f), GUIContent.none, cardStyle);
            GUI.Label(new Rect(mouse.x + 22f, mouse.y + 18f, 120f, 22f), dragging.data.name, badgeStyle);
            var previousColor = GUI.contentColor;
            GUI.contentColor = GetSocketMaskColor(dragging.data.targetBlockType);
            GUI.Label(new Rect(mouse.x + 146f, mouse.y + 18f, 64f, 22f), GetSocketMaskIconText(dragging.data.targetBlockType), badgeStyle);
            GUI.contentColor = previousColor;
            GUI.Label(new Rect(mouse.x + 22f, mouse.y + 40f, 150f, 20f), "点击右侧空槽位安装", textStyle);
        }

        void DrawShopRemovalSection(string title, System.Collections.Generic.IReadOnlyList<BlockCardState> cards)
        {
            GUILayout.BeginVertical(cardStyle, GUILayout.Width(280f));
            GUILayout.Label(title, badgeStyle);
            if (cards.Count == 0)
            {
                GUILayout.Label("空", textStyle);
                GUILayout.EndVertical();
                return;
            }

            for (var index = 0; index < cards.Count; index++)
            {
                var card = cards[index];
                GUILayout.BeginHorizontal();
                var previousColor = GUI.contentColor;
                GUI.contentColor = GetBlockIconColor(card);
                GUILayout.Label(GetBlockTypeIconText(card.baseBlockType), badgeStyle, GUILayout.Width(26f));
                GUI.contentColor = previousColor;
                GUILayout.Label(card.cardName, badgeStyle, GUILayout.Width(132f));
                GUILayout.Label(FormatBlockValue(card), tinyLabelStyle, GUILayout.Width(84f));
                GUI.enabled = !game.ShopManager.HasRemovedBlockThisVisit;
                if (GUILayout.Button("删除", buttonStyle, GUILayout.Width(52f)))
                    RunCommand(new HudCommand(HudCommandType.TryRemoveBlockInShop, 0, card.id));
                GUI.enabled = true;
                GUILayout.EndHorizontal();

                var lastRect = GUILayoutUtility.GetLastRect();
                if (lastRect.Contains(Event.current.mousePosition))
                    SetTooltip(card.cardName, BuildBlockTooltip(card), GetRarityColor(card.rarity));
            }
            GUILayout.EndVertical();
        }

        void DrawTooltip()
        {
            if (string.IsNullOrWhiteSpace(hoveredTooltipTitle))
                return;

            var mouse = Event.current.mousePosition;
            var width = Mathf.Min(320f, Screen.width * 0.28f);
            var height = Mathf.Clamp(72f + CountLines(hoveredTooltipBody) * 18f, 90f, 260f);
            var x = Mathf.Min(mouse.x + 18f, Screen.width - width - 12f);
            var y = Mathf.Min(mouse.y + 18f, Screen.height - height - 12f);
            var rect = new Rect(x, y, width, height);

            GUI.Box(rect, GUIContent.none, boxStyle);
            var previous = GUI.contentColor;
            GUI.contentColor = hoveredTooltipTitleColor;
            GUI.Label(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, 22f), hoveredTooltipTitle, tooltipTitleStyle);
            GUI.contentColor = previous;
            GUI.Label(new Rect(rect.x + 10f, rect.y + 32f, rect.width - 20f, rect.height - 40f), hoveredTooltipBody, tooltipBodyStyle);
        }

        void SetTooltip(string title, string body, Color titleColor)
        {
            hoveredTooltipTitle = title;
            hoveredTooltipBody = body;
            hoveredTooltipTitleColor = titleColor;
        }

        string BuildBlockTooltip(BlockCardState card)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"类型：{GetBlockTypeText(card.baseBlockType)}");
            builder.AppendLine($"稀有度：{GetRarityText(card.rarity)}");
            builder.AppendLine($"基础值：{FormatBlockValue(card)}");
            builder.AppendLine($"已装嵌片：{card.InstalledStickerCount}/{card.sockets.Count}");
            builder.Append(card.mainActionText);
            if (card.detailLines.Count > 0)
            {
                builder.AppendLine();
                for (var index = 0; index < card.detailLines.Count; index++)
                    builder.AppendLine($"• {card.detailLines[index]}");
            }

            return builder.ToString().TrimEnd();
        }

        string BuildStickerTooltip(BlockCardState card, StickerInstance sticker)
        {
            var data = sticker.data;
            var builder = new StringBuilder();
            builder.AppendLine($"稀有度：{GetStickerRarityText(data.rarity)}");
            builder.AppendLine($"适用：{GetSocketMaskText(data.targetBlockType)}");
            builder.AppendLine(data.mainActionText);
            foreach (var line in data.detailLines)
                builder.AppendLine($"• {line}");
            builder.AppendLine($"family：{data.family}");
            builder.Append($"载体：{card.cardName}");
            return builder.ToString().TrimEnd();
        }

        string BuildSocketTooltip(BlockCardState card, SocketSlotState socket)
        {
            var builder = new StringBuilder();
            if (!socket.isUnlocked)
            {
                builder.AppendLine("锁定槽位");
                builder.Append("当前还不能安装嵌片。");
                return builder.ToString();
            }

            if (socket.installedSticker != null)
            {
                builder.AppendLine($"已装：{socket.installedSticker.data.name}");
                builder.AppendLine(socket.installedSticker.data.mainActionText);
            }
            else
            {
                builder.AppendLine("空槽");
                builder.AppendLine("点击这里可以安装当前拿起的嵌片。");
            }

            builder.Append($"槽位限制：{GetSocketMaskText(socket.targetMask)}");
            if (card != null)
                builder.AppendLine($"\n载体：{card.cardName}");
            return builder.ToString().TrimEnd();
        }

        string GetStateText(RoundState state)
        {
            return state switch
            {
                RoundState.Aim => "瞄准",
                RoundState.BallFlying => "飞行",
                RoundState.RoundResolve => "结算",
                RoundState.BlockRewardChoose => "选方块",
                RoundState.RewardChoose => "奖励",
                RoundState.Shop => "商店",
                RoundState.LoadoutManage => "整理",
                RoundState.GameOver => "结束",
                _ => state.ToString()
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

        static string GetBlockTypeIconText(BoardBlockType blockType)
        {
            return blockType switch
            {
                BoardBlockType.AttackAdd => "攻",
                BoardBlockType.Shield => "防",
                BoardBlockType.AttackMultiply => "倍",
                _ => "混"
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

        static string GetStickerRarityText(StickerRarity rarity)
        {
            return rarity switch
            {
                StickerRarity.Common => "普通",
                StickerRarity.Uncommon => "进阶",
                StickerRarity.Rare => "稀有",
                StickerRarity.Epic => "史诗",
                _ => "普通"
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

        static string GetShopItemKindText(ShopItemKind kind)
        {
            return kind switch
            {
                ShopItemKind.Sticker => "嵌片",
                ShopItemKind.Mod => "模组",
                ShopItemKind.Growth => "成长项",
                _ => kind.ToString()
            };
        }

        static string GetSocketMaskText(SocketTargetMask mask)
        {
            if (mask == SocketTargetMask.Any)
                return "任意方块";

            var builder = new StringBuilder();
            if ((mask & SocketTargetMask.Attack) != 0)
                builder.Append("攻击 ");
            if ((mask & SocketTargetMask.Shield) != 0)
                builder.Append("防御 ");
            if ((mask & SocketTargetMask.Multiplier) != 0)
                builder.Append("倍率 ");
            if ((mask & SocketTargetMask.Hybrid) != 0)
                builder.Append("混合 ");
            return builder.ToString().TrimEnd();
        }

        static SocketTargetMask GetMaskForBlock(BoardBlockType type)
        {
            return type switch
            {
                BoardBlockType.AttackAdd => SocketTargetMask.Attack,
                BoardBlockType.AttackMultiply => SocketTargetMask.Multiplier,
                BoardBlockType.Shield => SocketTargetMask.Shield,
                _ => SocketTargetMask.Hybrid
            };
        }

        static string GetSocketMaskIconText(SocketTargetMask mask)
        {
            if (mask == SocketTargetMask.Any)
                return "攻 防 倍";

            var builder = new StringBuilder();
            if ((mask & SocketTargetMask.Attack) != 0)
                builder.Append("攻");
            if ((mask & SocketTargetMask.Shield) != 0)
                builder.Append("防");
            if ((mask & SocketTargetMask.Multiplier) != 0)
                builder.Append("倍");
            if ((mask & SocketTargetMask.Hybrid) != 0)
                builder.Append("混");
            return builder.ToString().TrimEnd();
        }

        static Color GetSocketMaskColor(SocketTargetMask mask)
        {
            if (mask == SocketTargetMask.Any)
                return new Color(0.95f, 0.95f, 0.95f, 1f);
            if ((mask & SocketTargetMask.Attack) != 0 && (mask & (SocketTargetMask.Shield | SocketTargetMask.Multiplier | SocketTargetMask.Hybrid)) == 0)
                return new Color(0.98f, 0.56f, 0.56f, 1f);
            if ((mask & SocketTargetMask.Shield) != 0 && (mask & (SocketTargetMask.Attack | SocketTargetMask.Multiplier | SocketTargetMask.Hybrid)) == 0)
                return new Color(0.45f, 0.92f, 0.58f, 1f);
            if ((mask & SocketTargetMask.Multiplier) != 0 && (mask & (SocketTargetMask.Attack | SocketTargetMask.Shield | SocketTargetMask.Hybrid)) == 0)
                return new Color(0.52f, 0.76f, 1f, 1f);
            return new Color(0.92f, 0.86f, 0.5f, 1f);
        }

        static string ShortStickerText(StickerInstance sticker)
        {
            if (sticker?.data == null)
                return "?";

            var shortTitle = sticker.data.shortTitle;
            return string.IsNullOrWhiteSpace(shortTitle)
                ? sticker.data.name.Substring(0, Mathf.Min(2, sticker.data.name.Length))
                : shortTitle.Substring(0, Mathf.Min(3, shortTitle.Length));
        }

        static string FormatBlockValue(BlockRewardOption option)
        {
            return option.blockType switch
            {
                BoardBlockType.AttackAdd => $"+{Mathf.RoundToInt(option.baseValue)} 伤害",
                BoardBlockType.Shield => $"+{Mathf.RoundToInt(option.baseValue)} 护盾",
                BoardBlockType.AttackMultiply => $"x{option.baseValue:0.0#} 倍率",
                _ => option.baseValue.ToString("0.0#")
            };
        }

        static string FormatBlockValue(BlockCardState card)
        {
            return card.baseBlockType switch
            {
                BoardBlockType.AttackAdd => $"+{Mathf.RoundToInt(card.baseValueA)} 伤害",
                BoardBlockType.Shield => $"+{Mathf.RoundToInt(card.baseValueA)} 护盾",
                BoardBlockType.AttackMultiply => $"x{card.baseValueA:0.0#} 倍率",
                _ => card.baseValueA.ToString("0.0#")
            };
        }

        static Color GetRarityColor(BlockRarity rarity)
        {
            return rarity switch
            {
                BlockRarity.White => new Color(0.95f, 0.95f, 0.96f, 1f),
                BlockRarity.Blue => new Color(0.48f, 0.74f, 1f, 1f),
                BlockRarity.Purple => new Color(0.86f, 0.53f, 1f, 1f),
                BlockRarity.Gold => new Color(1f, 0.82f, 0.3f, 1f),
                _ => Color.white
            };
        }

        Color GetBlockIconColor(BlockCardState card)
        {
            var typeColor = card.baseBlockType switch
            {
                BoardBlockType.AttackAdd => game.config.board.attackAddColor,
                BoardBlockType.Shield => game.config.board.shieldColor,
                BoardBlockType.AttackMultiply => game.config.board.attackMultiplyColor,
                _ => Color.gray
            };
            return Color.Lerp(typeColor, GetRarityColor(card.rarity), 0.35f);
        }

        static Color GetStickerRarityColor(StickerRarity rarity)
        {
            return rarity switch
            {
                StickerRarity.Common => new Color(0.84f, 0.86f, 0.92f, 1f),
                StickerRarity.Uncommon => new Color(0.5f, 0.88f, 0.56f, 1f),
                StickerRarity.Rare => new Color(0.52f, 0.76f, 1f, 1f),
                StickerRarity.Epic => new Color(0.88f, 0.55f, 1f, 1f),
                _ => Color.white
            };
        }

        static Rect GetIntermissionRect(float targetWidth, float targetHeight)
        {
            const float margin = 16f;
            var rightPanelWidth = Mathf.Min(390f, Screen.width * 0.36f);
            var leftRegionWidth = Mathf.Max(320f, Screen.width - rightPanelWidth - margin * 3f);
            var width = Mathf.Min(targetWidth, leftRegionWidth);
            var height = Mathf.Min(targetHeight, Screen.height - margin * 2f);
            var x = margin + Mathf.Max(0f, (leftRegionWidth - width) * 0.5f);
            var y = (Screen.height - height) * 0.5f;
            return new Rect(x, y, width, height);
        }

        static Rect GetCenteredModalRect(float targetWidth, float targetHeight)
        {
            const float margin = 24f;
            var width = Mathf.Min(targetWidth, Screen.width - margin * 2f);
            var height = Mathf.Min(targetHeight, Screen.height - margin * 2f);
            var x = (Screen.width - width) * 0.5f;
            var y = (Screen.height - height) * 0.5f;
            return new Rect(x, y, width, height);
        }

        static float GetModalCardWidth(float panelWidth, int count, float minWidth, float maxWidth)
        {
            var available = Mathf.Max(120f, panelWidth - 48f);
            var width = available / Mathf.Max(1, count) - 12f;
            return Mathf.Clamp(width, minWidth, maxWidth);
        }

        static int CountLines(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            var count = 1;
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                    count += 1;
            }

            return count;
        }

        void EnsureStyles()
        {
            if (boxStyle != null)
                return;

            panelTexture = new Texture2D(1, 1);
            panelTexture.SetPixel(0, 0, new Color(0.07f, 0.1f, 0.15f, 0.88f));
            panelTexture.Apply();

            cardTexture = new Texture2D(1, 1);
            cardTexture.SetPixel(0, 0, new Color(0.12f, 0.15f, 0.21f, 0.96f));
            cardTexture.Apply();

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(14, 14, 12, 12),
                normal = { textColor = Color.white, background = panelTexture }
            };
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = new Color(0.9f, 0.95f, 1f, 1f) }
            };
            tinyLabelStyle = new GUIStyle(textStyle) { fontSize = 12 };
            tooltipTitleStyle = new GUIStyle(titleStyle) { fontSize = 15 };
            tooltipBodyStyle = new GUIStyle(textStyle) { fontSize = 12, wordWrap = true };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, fixedHeight = 28f };
            iconButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(2, 2, 2, 2),
                margin = new RectOffset(0, 0, 0, 0)
            };
            smallCenterLabelStyle = new GUIStyle(iconButtonStyle) { fontSize = 11 };
            badgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.96f, 0.8f, 1f) }
            };
            cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 12, 12),
                margin = new RectOffset(6, 6, 4, 4),
                normal = { textColor = Color.white, background = cardTexture }
            };
        }
    }
}

