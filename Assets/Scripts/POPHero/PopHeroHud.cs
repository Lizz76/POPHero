using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace POPHero
{
    public class PopHeroHud : MonoBehaviour
    {
        PopHeroGame game;
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
        Vector2 inventoryScroll;
        Vector2 removeBlockScroll;
        Vector2 blockRewardScroll;
        Vector2 rewardScroll;
        Vector2 shopScroll;
        Vector2 loadoutModScroll;
        string hoveredInstallError = string.Empty;
        string hoveredTooltipTitle = string.Empty;
        string hoveredTooltipBody = string.Empty;
        Color hoveredTooltipTitleColor = Color.white;
        string selectedActiveSwapId;
        string selectedReserveSwapId;

        void Awake()
        {
            game = GetComponent<PopHeroGame>();
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
            GUILayout.BeginArea(new Rect(16f, 16f, 430f, 292f), boxStyle);
            GUILayout.Label("POPHero Prototype", titleStyle);
            GUILayout.Space(4f);
            GUILayout.Label($"状态：{GetStateText(game.State)}", badgeStyle);
            GUILayout.Label($"瞄准：{game.CurrentAimModeLabel}", badgeStyle);
            GUILayout.Label($"等级：{game.Player.Level}", badgeStyle);
            GUILayout.Label($"击杀进度：{game.Player.KillsTowardNextLevel} / {(game.Player.IsMaxLevel ? "满级" : game.Player.KillsRequiredForNextLevel.ToString())}", badgeStyle);
            GUILayout.Label($"方块：上阵 {game.BoardManager.ActiveCardCount}/{game.BoardManager.ActiveCapacity}  仓库 {game.BoardManager.ReserveCardCount}/{game.BoardManager.ReserveCapacity}", badgeStyle);
            GUILayout.Space(6f);
            GUILayout.Label($"生命：{game.Player.CurrentHp}/{game.Player.MaxHp}", textStyle);
            GUILayout.Label($"护盾：{game.Player.CurrentShield}", textStyle);
            GUILayout.Label($"金币：{game.Player.Gold}", textStyle);
            GUILayout.Label($"嵌片库存：{game.StickerInventory.Stored.Count}/{game.Player.StickerInventoryCapacity + game.ModManager.GetInventoryCapacityBonus()}", textStyle);
            GUILayout.Label($"可发射数：{game.RemainingLaunchesForEnemy}/{game.MaxLaunchesPerEnemy}", textStyle);
            GUILayout.Space(6f);
            GUILayout.Label($"敌人 #{game.EncounterIndex}：{game.CurrentEnemy?.DisplayName ?? "--"}", badgeStyle);
            GUILayout.Label($"敌人生命：{game.CurrentEnemy?.CurrentHp ?? 0}/{game.CurrentEnemy?.MaxHp ?? 0}", textStyle);
            GUILayout.Label($"敌人攻击：{game.CurrentEnemy?.AttackDamage ?? 0}", textStyle);
            GUILayout.EndArea();
        }

        void DrawCombatPanel()
        {
            GUILayout.BeginArea(new Rect(16f, Screen.height - 220f, 780f, 204f), boxStyle);
            GUILayout.Label("战斗与调试", titleStyle);
            GUILayout.Label($"本轮伤害：{game.RoundController.RoundAttackScore}", badgeStyle);
            GUILayout.Label($"本轮护盾：{game.RoundController.RoundShieldGain}", badgeStyle);
            GUILayout.Label($"命中次数：{game.RoundController.RoundHitCount}", badgeStyle);
            if (game.ModManager.ShowHitCounter() && game.State == RoundState.Aim)
                GUILayout.Label($"锁定路线预览：总命中 {game.PreviewHitCount}，攻击 {game.PreviewAttackBlockCount}，防御 {game.PreviewShieldBlockCount}，倍率 {game.PreviewMultiplierBlockCount}", textStyle);
            if (!string.IsNullOrWhiteSpace(game.IntermissionMessage))
                GUILayout.Label(game.IntermissionMessage, textStyle);
            if (!string.IsNullOrWhiteSpace(hoveredInstallError))
                GUILayout.Label(hoveredInstallError, textStyle);

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("切换瞄准模式", buttonStyle, GUILayout.Width(140f)))
                game.ToggleAimMode();
            if (GUILayout.Button("重排方块", buttonStyle, GUILayout.Width(110f)))
                game.DebugShuffleBoard();
            if (GUILayout.Button("金币 +25", buttonStyle, GUILayout.Width(110f)))
                game.DebugAddGold(25);
            if (GUILayout.Button("击败敌人", buttonStyle, GUILayout.Width(110f)))
                game.DebugKillEnemy();
            if (GUILayout.Button("玩家 -10 生命", buttonStyle, GUILayout.Width(130f)))
                game.DebugDamagePlayer(10);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void DrawBlockManagementPanel()
        {
            var panelWidth = Mathf.Min(390f, Screen.width * 0.36f);
            var panelRect = new Rect(Screen.width - panelWidth - 16f, 16f, panelWidth, Screen.height - 32f);
            GUI.Box(panelRect, GUIContent.none, boxStyle);

            var x = panelRect.x + 12f;
            var y = panelRect.y + 12f;
            var width = panelRect.width - 24f;
            GUI.Label(new Rect(x, y, width, 22f), "方块管理", titleStyle);
            y += 24f;
            GUI.Label(new Rect(x, y, width, 36f), game.CanManageBlockAssignments
                ? "点击仓库方块图标，再点击上阵方块图标即可互换。默认只显示图标，悬停时看详情。"
                : "默认只显示紧凑图标。把鼠标放到方块、嵌片或槽位图标上可查看详细 tooltip。", textStyle);
            y += 42f;

            y = DrawCompactSection(panelRect, y, "上阵", game.BoardManager.ActiveCardStates, game.BoardManager.ActiveCapacity, true);
            y += 10f;
            DrawCompactSection(panelRect, y, "仓库", game.BoardManager.ReserveCardStates, game.BoardManager.ReserveCapacity, false);
        }

        float DrawCompactSection(Rect panelRect, float y, string title, System.Collections.Generic.IReadOnlyList<BlockCardState> cards, int capacity, bool isActiveSection)
        {
            var x = panelRect.x + 12f;
            var width = panelRect.width - 24f;
            GUI.Label(new Rect(x, y, width, 20f), $"{title} {cards.Count}/{capacity}", badgeStyle);
            y += 24f;

            for (var index = 0; index < capacity; index++)
            {
                var rowRect = new Rect(x, y, width, 34f);
                var card = index < cards.Count ? cards[index] : null;
                DrawCompactRow(rowRect, index, card, isActiveSection);
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
            var previousColor = GUI.color;
            GUI.color = !socket.isUnlocked
                ? new Color(0.28f, 0.3f, 0.36f, 1f)
                : socket.installedSticker != null
                    ? new Color(0.95f, 0.82f, 0.32f, 1f)
                    : new Color(0.36f, 0.4f, 0.48f, 1f);
            GUI.Box(rect, socket.isUnlocked ? (socket.installedSticker != null ? "●" : "+") : "锁", smallCenterLabelStyle);
            GUI.color = previousColor;

            if (rect.Contains(Event.current.mousePosition))
                SetTooltip($"槽位 {socketIndex + 1}", BuildSocketTooltip(card, socket), Color.white);
        }

        void HandleBlockIconClick(BlockCardState card, bool isActiveSection)
        {
            if (!game.CanManageBlockAssignments || card == null)
                return;

            if (isActiveSection)
            {
                if (!string.IsNullOrEmpty(selectedReserveSwapId))
                {
                    game.TrySwapActiveReserve(card.id, selectedReserveSwapId);
                    selectedActiveSwapId = null;
                    selectedReserveSwapId = null;
                    return;
                }

                selectedActiveSwapId = selectedActiveSwapId == card.id ? null : card.id;
                return;
            }

            if (!string.IsNullOrEmpty(selectedActiveSwapId))
            {
                game.TrySwapActiveReserve(selectedActiveSwapId, card.id);
                selectedActiveSwapId = null;
                selectedReserveSwapId = null;
                return;
            }

            selectedReserveSwapId = selectedReserveSwapId == card.id ? null : card.id;
        }

        void DrawBlockRewardPanel()
        {
            var options = game.BoardManager.ActiveRewardOptions;
            var panelRect = GetCenteredModalRect(920f, 360f);
            GUILayout.BeginArea(panelRect, boxStyle);
            GUILayout.Label(game.IsInitialBlockDraftPending ? "初始方块三选一" : "新增方块三选一", titleStyle);
            if (game.IsInitialBlockDraftPending)
                GUILayout.Label("先选 1 张方块加入上阵区，然后才开始第一只怪的战斗。", textStyle);
            else if (!game.BoardManager.CanAcceptRewardBlock)
                GUILayout.Label("上阵区与仓库区都已满，本次只能跳过。先在后续中场里删除或替换方块。", textStyle);
            else if (game.BoardManager.RewardWillGoToReserve)
                GUILayout.Label("上阵区已满，这次拿到的新方块会进入仓库。", textStyle);
            else
                GUILayout.Label("上阵区未满，选中的方块会直接加入上阵区。", textStyle);
            GUILayout.Space(10f);
            blockRewardScroll = GUILayout.BeginScrollView(blockRewardScroll, GUILayout.Height(panelRect.height - 110f));
            var cardWidth = GetModalCardWidth(panelRect.width, options.Count, 210f, 280f);
            GUILayout.BeginHorizontal();
            for (var index = 0; index < options.Count; index++)
            {
                var option = options[index];
                GUILayout.BeginVertical(cardStyle, GUILayout.Width(cardWidth), GUILayout.Height(210f));
                var previousColor = GUI.contentColor;
                GUI.contentColor = GetRarityColor(option.rarity);
                GUILayout.Label(option.displayName, titleStyle);
                GUI.contentColor = previousColor;
                GUILayout.Label($"类型：{GetBlockTypeText(option.blockType)}", badgeStyle);
                GUILayout.Label($"稀有度：{GetRarityText(option.rarity)}", badgeStyle);
                GUILayout.Label($"数值：{FormatBlockValue(option)}", badgeStyle);
                GUILayout.Label(option.desc, textStyle, GUILayout.Height(66f));
                GUILayout.FlexibleSpace();
                GUI.enabled = game.IsInitialBlockDraftPending || game.BoardManager.CanAcceptRewardBlock;
                if (GUILayout.Button(game.BoardManager.RewardWillGoToReserve && !game.IsInitialBlockDraftPending ? "放入仓库" : "加入方块组", buttonStyle))
                    game.TrySelectBlockReward(index);
                GUI.enabled = true;
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            GUILayout.Space(10f);
            if (!game.IsInitialBlockDraftPending && GUILayout.Button("跳过，不添加方块", buttonStyle, GUILayout.Width(170f)))
                game.SkipBlockReward();
            GUILayout.EndArea();
        }

        void DrawRewardPanel()
        {
            var choices = game.RewardChoiceController.ActiveChoices;
            var panelRect = GetCenteredModalRect(940f, 340f);
            GUILayout.BeginArea(panelRect, boxStyle);
            GUILayout.Label("奖励阶段：选择一项", titleStyle);
            GUILayout.Label("拿到新的嵌片、模组或成长项，然后进入商店。", textStyle);
            GUILayout.Space(10f);
            rewardScroll = GUILayout.BeginScrollView(rewardScroll, GUILayout.Height(panelRect.height - 110f));
            var cardWidth = GetModalCardWidth(panelRect.width, choices.Count, 200f, choices.Count >= 4 ? 210f : 280f);
            GUILayout.BeginHorizontal();
            for (var index = 0; index < choices.Count; index++)
            {
                var choice = choices[index];
                GUILayout.BeginVertical(cardStyle, GUILayout.Width(cardWidth), GUILayout.Height(220f));
                GUILayout.Label(choice.title, titleStyle);
                GUILayout.Label(choice.description, textStyle, GUILayout.Height(90f));
                GUILayout.Label($"类别：{GetRewardKindText(choice.kind)}", badgeStyle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("选择", buttonStyle))
                    game.TrySelectReward(index);
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"刷新奖励 (-{game.config.shop.stickerRerollMoney})", buttonStyle, GUILayout.Width(160f)))
                game.TryRerollRewardChoices();
            if (GUILayout.Button($"跳过并拿金币 (+{game.config.shop.stickerSkipMoney})", buttonStyle, GUILayout.Width(180f)))
                game.SkipRewardChoices();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void DrawShopPanel()
        {
            var panelRect = GetCenteredModalRect(1000f, 520f);
            GUILayout.BeginArea(panelRect, boxStyle);
            GUILayout.Label("商店", titleStyle);
            GUILayout.Label("可以购买嵌片、模组和成长项，也可以刷新商店；每次进店还可以删除 1 张方块。", textStyle);
            GUILayout.Space(10f);

            shopScroll = GUILayout.BeginScrollView(shopScroll, GUILayout.Height(panelRect.height - 120f));
            var itemWidth = GetModalCardWidth(panelRect.width, Mathf.Max(1, game.ShopManager.Items.Count), 150f, 178f);
            GUILayout.BeginHorizontal();
            for (var index = 0; index < game.ShopManager.Items.Count; index++)
            {
                var item = game.ShopManager.Items[index];
                GUILayout.BeginVertical(cardStyle, GUILayout.Width(itemWidth), GUILayout.Height(210f));
                GUILayout.Label(item.title, titleStyle);
                GUILayout.Label(item.description, textStyle, GUILayout.Height(92f));
                GUILayout.Label($"类别：{GetShopItemKindText(item.kind)}", badgeStyle);
                GUILayout.Label($"价格：{item.price}", badgeStyle);
                GUI.enabled = !item.purchased;
                if (GUILayout.Button(item.purchased ? "已购买" : "购买", buttonStyle))
                    game.TryBuyShopItem(index);
                GUI.enabled = true;
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(12f);
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label("删除方块", titleStyle);
            if (game.ShopManager.HasRemovedBlockThisVisit)
                GUILayout.Label("本次商店已经删除过 1 张方块。", badgeStyle);
            else
                GUILayout.Label($"可花费 {game.config.shop.blockRemovalCost} 金币删除 1 张方块。至少保留 1 张。", textStyle);

            removeBlockScroll = GUILayout.BeginScrollView(removeBlockScroll, GUILayout.Height(Mathf.Min(180f, panelRect.height * 0.32f)));
            DrawShopRemovalSection("上阵区", game.BoardManager.ActiveCardStates);
            GUILayout.Space(6f);
            DrawShopRemovalSection("仓库区", game.BoardManager.ReserveCardStates);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"刷新商店 (-{Mathf.Max(1, game.config.shop.shopRerollMoney - game.ModManager.GetShopRerollDiscount())})", buttonStyle, GUILayout.Width(150f)))
                game.TryRerollShop();
            if (GUILayout.Button("离开商店", buttonStyle, GUILayout.Width(140f)))
                game.CloseShop();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void DrawLoadoutPanel()
        {
            var panelRect = GetCenteredModalRect(860f, 360f);
            GUILayout.BeginArea(panelRect, boxStyle);
            GUILayout.Label("中场整理", titleStyle);
            GUILayout.Label("点击库存中的嵌片把它附着到鼠标上，再点击右侧方块卡片上的合法槽位安装。", textStyle);
            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();

            var columnWidth = Mathf.Max(220f, (panelRect.width - 52f) * 0.5f);
            var columnHeight = panelRect.height - 118f;

            GUILayout.BeginVertical(cardStyle, GUILayout.Width(columnWidth), GUILayout.Height(columnHeight));
            GUILayout.Label("库存嵌片", titleStyle);
            inventoryScroll = GUILayout.BeginScrollView(inventoryScroll, GUILayout.Height(columnHeight - 50f));
            if (game.StickerInventory.Stored.Count == 0)
                GUILayout.Label("当前没有可安装的嵌片。", textStyle);
            foreach (var sticker in game.StickerInventory.Stored)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(sticker.data.name, badgeStyle, GUILayout.Width(160f));
                GUILayout.Label(sticker.data.mainActionText, textStyle, GUILayout.Width(170f));
                if (GUILayout.Button("拿起", buttonStyle, GUILayout.Width(56f)))
                    game.BeginStickerDrag(sticker.runtimeId);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(cardStyle, GUILayout.Width(columnWidth), GUILayout.Height(columnHeight));
            GUILayout.Label("模组栏", titleStyle);
            loadoutModScroll = GUILayout.BeginScrollView(loadoutModScroll, GUILayout.Height(columnHeight - 50f));
            if (game.ModManager.ActiveMods.Count == 0 && game.ModManager.ReserveMods.Count == 0)
                GUILayout.Label("当前还没有模组。", textStyle);

            for (var index = 0; index < game.ModManager.ActiveMods.Count; index++)
            {
                var mod = game.ModManager.ActiveMods[index];
                GUILayout.BeginHorizontal();
                GUILayout.Label($"启用：{mod.data.name}", badgeStyle, GUILayout.Width(170f));
                if (GUILayout.Button("停用", buttonStyle, GUILayout.Width(60f)))
                    game.ToggleModActivation(mod.runtimeId);
                GUILayout.EndHorizontal();
            }
            for (var index = 0; index < game.ModManager.ReserveMods.Count; index++)
            {
                var mod = game.ModManager.ReserveMods[index];
                GUILayout.BeginHorizontal();
                GUILayout.Label($"备用：{mod.data.name}", textStyle, GUILayout.Width(170f));
                if (GUILayout.Button("启用", buttonStyle, GUILayout.Width(60f)))
                    game.ToggleModActivation(mod.runtimeId);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            if (game.StickerInventory.DraggingSticker != null && GUILayout.Button("取消当前拖拽嵌片", buttonStyle, GUILayout.Width(180f)))
                game.CancelStickerDrag();
            if (GUILayout.Button("继续战斗", buttonStyle, GUILayout.Width(140f)))
                game.FinishLoadout();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
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
            GUI.Box(new Rect(mouse.x + 14f, mouse.y + 14f, 170f, 64f), GUIContent.none, cardStyle);
            GUI.Label(new Rect(mouse.x + 22f, mouse.y + 20f, 150f, 22f), dragging.data.name, badgeStyle);
            GUI.Label(new Rect(mouse.x + 22f, mouse.y + 40f, 150f, 20f), "点击右侧空槽位安装", textStyle);
        }

        void DrawShopRemovalSection(string title, System.Collections.Generic.IReadOnlyList<BlockCardState> cards)
        {
            GUILayout.Label(title, badgeStyle);
            if (cards.Count == 0)
            {
                GUILayout.Label("空", textStyle);
                return;
            }

            for (var index = 0; index < cards.Count; index++)
            {
                var card = cards[index];
                GUILayout.BeginHorizontal();
                GUILayout.Label(card.cardName, badgeStyle, GUILayout.Width(180f));
                GUILayout.Label(card.mainActionText, textStyle, GUILayout.Width(340f));
                GUI.enabled = !game.ShopManager.HasRemovedBlockThisVisit;
                if (GUILayout.Button("删除", buttonStyle, GUILayout.Width(60f)))
                    game.TryRemoveBlockInShop(card.id);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
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
