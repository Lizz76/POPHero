using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace POPHero.Editor
{
    public static class SceneBuilder
    {
        // Editor-only scaffold entry points for Boot / MainMenu / Battle scene generation.
        const string AutoBuildFlagPath = "Temp/POPHero.BuildScenes.flag";
        const string BootScenePath = "Assets/Scenes/Boot.unity";
        const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        const string BattleScenePath = "Assets/Scenes/Battle.unity";

        [MenuItem("POPHero/Build Boot Scene")]
        public static void BuildBootScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = SceneNames.Boot;

            var bootstrap = new GameObject("ProjectBootstrap");
            bootstrap.AddComponent<ProjectBootstrap>();

            EditorSceneManager.SaveScene(scene, BootScenePath);
            Debug.Log("[POPHero] Boot scene generated.");
        }

        [MenuItem("POPHero/Build Main Menu Scene")]
        public static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = SceneNames.MainMenu;

            BuildCamera(new Color(0.08f, 0.1f, 0.14f, 1f), 10f);
            EnsureEventSystem();

            var canvasRoot = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var root = StretchPanel("Root", canvasRoot.transform);
            root.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 1f);

            var content = StretchPanel("Content", root);
            var contentLayout = AddVertical(content, 18, 32);
            contentLayout.childAlignment = TextAnchor.MiddleCenter;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = false;

            var topSpacer = Panel("TopSpacer", content, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 0f));
            topSpacer.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            var menuPanel = Panel("MenuPanel", content, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(720f, 0f));
            menuPanel.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.24f, 0.96f);
            menuPanel.gameObject.AddComponent<LayoutElement>().preferredWidth = 720f;
            menuPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = 0f;
            AddContentFitter(menuPanel, false, true);
            var menuLayout = AddVertical(menuPanel, 20, 30);
            menuLayout.childAlignment = TextAnchor.MiddleCenter;
            menuLayout.childControlHeight = false;
            menuLayout.childForceExpandWidth = true;

            var title = Text("TitleText", menuPanel, 52, FontStyles.Bold, "POPHero", TextAlignmentOptions.Center);
            title.rectTransform.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
            var subtitle = Text("SubtitleText", menuPanel, 24, FontStyles.Normal, "弹珠构筑战斗原型", TextAlignmentOptions.Center);
            subtitle.color = new Color(0.86f, 0.9f, 1f, 1f);
            subtitle.rectTransform.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;

            var spacer = Panel("Spacer", menuPanel, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 24f));
            spacer.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

            var startButton = Button("StartButton", menuPanel, "开始游戏");
            startButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 320f;
            startButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
            var quitButton = Button("QuitButton", menuPanel, "退出游戏");
            quitButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 320f;
            quitButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
            var version = Text("VersionText", menuPanel, 18, FontStyles.Normal, "原型版本", TextAlignmentOptions.Center);
            version.color = new Color(0.7f, 0.74f, 0.82f, 1f);
            version.rectTransform.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

            var bottomSpacer = Panel("BottomSpacer", content, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 0f));
            bottomSpacer.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            var controllerGo = new GameObject("MainMenuController");
            var controller = controllerGo.AddComponent<MainMenuController>();
            var so = new SerializedObject(controller);
            so.FindProperty("titleLabel").objectReferenceValue = title;
            so.FindProperty("subtitleLabel").objectReferenceValue = subtitle;
            so.FindProperty("startButton").objectReferenceValue = startButton;
            so.FindProperty("quitButton").objectReferenceValue = quitButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            Debug.Log("[POPHero] MainMenu scene generated.");
        }

        [MenuItem("POPHero/Build Battle Scene")]
        public static void BuildBattleScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = SceneNames.Battle;

            var config = Resources.Load<PopHeroPrototypeConfig>("PopHeroPrototypeConfig") ?? PopHeroPrototypeConfig.CreateRuntimeDefault();
            var boardSize = config.arena.boardSize;
            var boardCenter = config.arena.boardCenter;
            var boardRect = new Rect(boardCenter.x - boardSize.x * 0.5f, boardCenter.y - boardSize.y * 0.5f, boardSize.x, boardSize.y);
            var launchY = boardRect.yMin + config.arena.launchLineOffset;
            var panelCenter = new Vector2(boardRect.center.x, boardRect.yMax + config.arena.topPanelHeight * 0.56f);
            var heroPos = panelCenter + new Vector2(-boardRect.width * 0.28f, -0.16f);
            var enemyPos = panelCenter + new Vector2(boardRect.width * 0.2f, -0.08f);

            BuildCamera(config.arena.backgroundColor, config.arena.cameraSize);
            EnsureEventSystem();

            var gameRoot = new GameObject("POPHeroGame");
            var game = gameRoot.AddComponent<PopHeroGame>();
            gameRoot.AddComponent<RoundController>();
            gameRoot.AddComponent<BoardManager>();
            gameRoot.AddComponent<TrajectoryPredictor>();

            var world = CreateChild(gameRoot, "World");
            var board = CreateChild(world, "Board");
            var blocks = CreateChild(world, "Blocks");
            var enemyLayer = CreateChild(world, "EnemyLayer");
            var battleStage = CreateChild(enemyLayer, "BattleStage");
            var battleEffects = CreateChild(enemyLayer, "BattleEffects");

            var boardFrame = CreateSpriteChild(board, "BoardFrame", new Vector3(boardRect.center.x, boardRect.center.y, 0f), new Vector3(boardRect.width + config.arena.wallThickness * 2f, boardRect.height + config.arena.wallThickness * 2f, 1f), config.arena.boardFrameColor, 2);
            var boardBackground = CreateSpriteChild(board, "BoardBackground", new Vector3(boardRect.center.x, boardRect.center.y, 0f), new Vector3(boardRect.width, boardRect.height, 1f), config.arena.boardBackgroundColor, 3);
            var launchGuide = CreateSpriteChild(board, "LaunchGuide", new Vector3(boardRect.center.x, launchY - 0.15f, 0f), new Vector3(boardRect.width - 0.4f, 0.34f, 1f), config.arena.launchGuideColor, 6);
            var launchMarker = CreateSpriteChild(board, "LaunchMarker", new Vector3(boardRect.center.x, launchY, 0f), Vector3.one * Mathf.Max(0.14f, config.ball.radius * 2.2f), new Color(0.97f, 0.97f, 1f, 0.65f), 25, true);
            var bottomLine = CreateSpriteChild(board, "BottomLine", new Vector3(boardRect.center.x, boardRect.yMin + 0.02f, 0f), new Vector3(boardRect.width, 0.14f, 1f), new Color(0.93f, 0.66f, 0.18f, 0.36f), 7);
            var bottomCollider = bottomLine.AddComponent<BoxCollider2D>();
            bottomCollider.isTrigger = true;
            bottomCollider.size = new Vector2(boardRect.width, config.arena.bottomTriggerHeight);
            var bottomMarker = bottomLine.AddComponent<ArenaSurfaceMarker>();
            bottomMarker.surfaceType = ArenaSurfaceType.Bottom;
            var wallTop = CreateChild(board, "WallTop");
            var wallLeft = CreateChild(board, "WallLeft");
            var wallRight = CreateChild(board, "WallRight");

            var enemyPanel = CreateSpriteChild(enemyLayer, "EnemyPanel", panelCenter, new Vector3(boardRect.width, config.arena.topPanelHeight, 1f), config.arena.topPanelColor, 1);
            var hero = CreateHero(battleStage, heroPos);
            var enemy = CreateEnemy(battleStage, enemyPos);
            var ball = CreateBall(world, boardRect.center.x, launchY, config);
            var canvasHud = BuildBattleCanvasFrontend().GetComponent<CanvasHudController>();

            WirePlayerPresenter(hero);
            WireEnemyController(enemy);
            WireBallLauncher(ball.GetComponent<PlayerLauncher>(), ball.transform);
            WireGame(game, config, world.transform, board.transform, blocks.transform, enemyLayer.transform, battleStage.transform, battleEffects.transform, boardFrame.GetComponent<SpriteRenderer>(), boardBackground.GetComponent<SpriteRenderer>(), launchGuide.GetComponent<SpriteRenderer>(), launchMarker.transform, bottomLine, wallTop.transform, wallLeft.transform, wallRight.transform, enemyPanel.GetComponent<SpriteRenderer>(), hero.GetComponent<PlayerPresenter>(), enemy.GetComponent<EnemyController>(), ball.GetComponent<BallController>(), ball.GetComponent<Rigidbody2D>(), ball.GetComponent<CircleCollider2D>(), ball.GetComponent<TrailRenderer>(), ball.GetComponent<PlayerLauncher>(), canvasHud);

            EditorSceneManager.SaveScene(scene, BattleScenePath);
            Debug.Log("[POPHero] Battle scene generated.");
        }

        [MenuItem("POPHero/Build All Scenes")]
        public static void BuildAllScenes()
        {
            BuildBootScene();
            BuildMainMenuScene();
            BuildBattleScene();
            ApplyBuildSettings();
            Debug.Log("[POPHero] Boot/MainMenu/Battle scenes generated and Build Settings updated.");
        }

        [MenuItem("POPHero/Build Game Scene")]
        public static void BuildGameScene()
        {
            BuildBattleScene();
        }

        [InitializeOnLoadMethod]
        static void AutoBuildOnReload()
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), AutoBuildFlagPath);
            if (!File.Exists(fullPath))
                return;

            File.Delete(fullPath);
            EditorApplication.delayCall += BuildAllScenes;
        }

        static void ApplyBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootScenePath, true),
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(BattleScenePath, true)
            };
        }

        static void BuildCamera(Color backgroundColor, float orthographicSize)
        {
            var cameraGo = new GameObject("Main Camera");
            var camera = cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();
            cameraGo.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.transform.position = new Vector3(0f, 0.25f, -10f);
            camera.backgroundColor = backgroundColor;
            camera.clearFlags = CameraClearFlags.SolidColor;
        }

        static void EnsureEventSystem()
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        static GameObject CreateHero(GameObject parent, Vector2 position)
        {
            var hero = new GameObject("Hero");
            hero.transform.SetParent(parent.transform, false);
            hero.transform.position = position;
            hero.AddComponent<PlayerPresenter>();
            CreateSpriteChild(hero, "HeroBody", Vector3.zero, new Vector3(1.7f, 1.9f, 1f), new Color(0.28f, 0.72f, 0.96f, 1f), 10);
            CreateSpriteChild(hero, "HeroCore", new Vector3(0f, 0.06f, 0f), Vector3.one * 0.58f, new Color(1f, 1f, 1f, 0.24f), 11, true);
            CreateSpriteChild(hero, "HpBack", new Vector3(0f, -1.48f, 0f), new Vector3(2.45f, 0.28f, 1f), new Color(0f, 0f, 0f, 0.55f), 12);
            CreateSpriteChild(hero, "HpFill", new Vector3(0f, -1.48f, -0.02f), new Vector3(2.2f, 0.15f, 1f), new Color(0.58f, 0.94f, 0.7f, 1f), 13);
            CreateTextChild(hero, "HeroName", new Vector3(0f, 1.38f, 0f), "主角", Color.white, 15, 0.095f);
            CreateTextChild(hero, "HeroHp", new Vector3(0f, -1.84f, 0f), "0/0", Color.white, 15, 0.075f, FontStyle.Normal);
            return hero;
        }

        static GameObject CreateEnemy(GameObject parent, Vector2 position)
        {
            var enemy = new GameObject("Enemy");
            enemy.transform.SetParent(parent.transform, false);
            enemy.transform.position = position;
            enemy.AddComponent<EnemyController>();
            CreateSpriteChild(enemy, "EnemyBody", Vector3.zero, new Vector3(2.2f, 2.2f, 1f), Color.white, 10);
            CreateSpriteChild(enemy, "EnemyCore", new Vector3(0f, 0.12f, 0f), Vector3.one * 0.72f, new Color(1f, 1f, 1f, 0.2f), 11, true);
            CreateSpriteChild(enemy, "HpBack", new Vector3(0f, -1.8f, 0f), new Vector3(2.8f, 0.3f, 1f), new Color(0f, 0f, 0f, 0.55f), 12);
            CreateSpriteChild(enemy, "HpFill", new Vector3(0f, -1.8f, -0.02f), new Vector3(2.5f, 0.16f, 1f), new Color(0.98f, 0.92f, 0.72f, 1f), 13);
            CreateSpriteChild(enemy, "HpPreview", new Vector3(0f, -1.8f, -0.015f), new Vector3(2.5f, 0.16f, 1f), new Color(0.56f, 0.16f, 0.18f, 0.92f), 14);
            CreateTextChild(enemy, "EnemyName", new Vector3(0f, 1.65f, 0f), "敌人", Color.white, 15, 0.11f);
            CreateTextChild(enemy, "EnemyIntent", new Vector3(0f, 2.15f, 0f), "攻击 0", new Color(1f, 0.78f, 0.34f, 1f), 16, 0.085f);
            CreateTextChild(enemy, "EnemyHp", new Vector3(0f, -2.2f, 0f), "0/0", Color.white, 15, 0.08f, FontStyle.Normal);
            return enemy;
        }

        static GameObject CreateBall(GameObject parent, float x, float y, PopHeroPrototypeConfig config)
        {
            var ball = new GameObject("Ball");
            ball.transform.SetParent(parent.transform, false);
            ball.transform.position = new Vector3(x, y, 0f);
            CreateSpriteChild(ball, "BallVisual", Vector3.zero, Vector3.one * (config.ball.radius * 2f), config.ball.color, 40, true);
            var trail = ball.AddComponent<TrailRenderer>();
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.sortingOrder = 39;
            trail.minVertexDistance = 0.03f;
            trail.numCornerVertices = 2;
            trail.numCapVertices = 2;
            trail.startColor = new Color(0.2f, 1f, 0.78f, 0.82f);
            trail.endColor = new Color(0.2f, 1f, 0.78f, 0f);
            trail.emitting = false;
            var rb = ball.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var circle = ball.AddComponent<CircleCollider2D>();
            circle.radius = config.ball.radius;
            ball.AddComponent<BallController>();
            ball.AddComponent<PlayerLauncher>();
            CreateLineRenderer(ball, "AimPreviewLine", config.ball.previewColor, config.ball.previewLineStartWidth, config.ball.previewLineEndWidth, 500);
            CreateLineRenderer(ball, "AimMemoryLine", new Color(0.22f, 0.78f, 1f, 0.35f), config.ball.previewLineStartWidth * 0.72f, config.ball.previewLineEndWidth * 0.72f, 499);
            return ball;
        }

        static GameObject BuildBattleCanvasFrontend()
        {
            var canvasRoot = new GameObject("CanvasFrontend", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasHudController), typeof(BattleCanvasLayout));
            var canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var hudRoot = StretchPanel("HudRoot", canvasRoot.transform);
            var tooltipRoot = StretchPanel("TooltipRoot", canvasRoot.transform);
            var modalRoot = StretchPanel("ModalRoot", canvasRoot.transform);
            var debugRoot = StretchPanel("DebugRoot", canvasRoot.transform);
            debugRoot.gameObject.SetActive(false);

            StretchPanel("TopLeftZone", hudRoot);
            StretchPanel("BottomLeftZone", hudRoot);
            StretchPanel("RightRailZone", hudRoot);
            StretchPanel("CenterModalZone", modalRoot);

            BuildStatusPanel(hudRoot);
            BuildCombatPanel(hudRoot);
            BuildBlockManagementPanel(hudRoot);
            BuildDamagePanel(hudRoot);
            BuildTooltipPanels(tooltipRoot);
            BuildBlockRewardModal(modalRoot);
            BuildRewardModal(modalRoot);
            BuildShopModal(modalRoot);
            BuildLoadoutModal(modalRoot);
            BuildGameOverModal(modalRoot);
            return canvasRoot;
        }

        static void BuildStatusPanel(RectTransform hudRoot)
        {
            var panel = Panel("StatusPanel", hudRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(360f, 0f));
            panel.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.9f);
            var width = panel.gameObject.AddComponent<LayoutElement>();
            width.minWidth = 300f;
            width.preferredWidth = 360f;
            var layout = AddVertical(panel, 8, 18);
            AddContentFitter(panel, false, true);
            Text("TitleText", panel, 26, FontStyles.Bold, "战斗状态", TextAlignmentOptions.MidlineLeft);
            Text("StateText", panel, 20, FontStyles.Normal, string.Empty, TextAlignmentOptions.MidlineLeft);
            Text("AimModeText", panel, 18, FontStyles.Normal, string.Empty, TextAlignmentOptions.MidlineLeft);
            Text("LevelText", panel, 18, FontStyles.Normal, string.Empty, TextAlignmentOptions.MidlineLeft);
            Text("KillsText", panel, 18, FontStyles.Normal, string.Empty, TextAlignmentOptions.MidlineLeft);
            Text("BlockText", panel, 18, FontStyles.Normal, string.Empty, TextAlignmentOptions.MidlineLeft);
            Text("HpText", panel, 18, FontStyles.Normal, string.Empty, TextAlignmentOptions.MidlineLeft);
            Text("ShieldText", panel, 18, FontStyles.Normal, string.Empty, TextAlignmentOptions.MidlineLeft);
            Text("GoldText", panel, 18, FontStyles.Normal, string.Empty, TextAlignmentOptions.MidlineLeft);
            Text("InventoryText", panel, 18, FontStyles.Normal, string.Empty, TextAlignmentOptions.MidlineLeft);
            Text("LaunchesText", panel, 18, FontStyles.Normal, string.Empty, TextAlignmentOptions.MidlineLeft);
            Text("EnemyText", panel, 18, FontStyles.Normal, string.Empty, TextAlignmentOptions.MidlineLeft);
            Text("EnemyHpText", panel, 18, FontStyles.Normal, string.Empty, TextAlignmentOptions.MidlineLeft);
            Text("EnemyAttackText", panel, 18, FontStyles.Normal, string.Empty, TextAlignmentOptions.MidlineLeft);
        }

        static void BuildCombatPanel(RectTransform hudRoot)
        {
            var panel = Panel("CombatPanel", hudRoot, new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, new Vector2(560f, 236f));
            panel.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.9f);
            var element = panel.gameObject.AddComponent<LayoutElement>();
            element.minWidth = 420f;
            element.preferredWidth = 560f;
            element.minHeight = 220f;
            element.preferredHeight = 248f;
            var layout = AddVertical(panel, 8, 18);
            Text("TitleText", panel, 24, FontStyles.Bold, "战斗信息", TextAlignmentOptions.MidlineLeft);
            Text("RoundAttackText", panel, 18, FontStyles.Normal, string.Empty, TextAlignmentOptions.MidlineLeft);
            Text("RoundShieldText", panel, 18, FontStyles.Normal, string.Empty, TextAlignmentOptions.MidlineLeft);
            Text("RoundHitText", panel, 18, FontStyles.Normal, string.Empty, TextAlignmentOptions.MidlineLeft);
            var preview = Text("PreviewText", panel, 16, FontStyles.Normal, string.Empty, TextAlignmentOptions.TopLeft);
            preview.rectTransform.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
            var message = Text("MessageText", panel, 16, FontStyles.Normal, string.Empty, TextAlignmentOptions.TopLeft);
            message.rectTransform.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;
            var buttons = Panel("Buttons", panel, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 40f));
            var buttonsElement = buttons.gameObject.AddComponent<LayoutElement>();
            buttonsElement.minHeight = 76f;
            buttonsElement.preferredHeight = 84f;
            var grid = buttons.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.cellSize = new Vector2(156f, 34f);
            grid.spacing = new Vector2(8f, 8f);
            buttons.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Button("ToggleAimButton", buttons, "切换瞄准");
            Button("ShuffleButton", buttons, "重排方块");
            Button("AddGoldButton", buttons, "金币 +25");
            Button("KillEnemyButton", buttons, "秒杀敌人");
            Button("DamagePlayerButton", buttons, "主角 -10 血");
        }

        static void BuildBlockManagementPanel(RectTransform hudRoot)
        {
            var panel = Panel("BlockManagementPanel", hudRoot, new Vector2(1f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(420f, -48f));
            panel.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.9f);
            var rootElement = panel.gameObject.AddComponent<LayoutElement>();
            rootElement.minWidth = 320f;
            rootElement.preferredWidth = 420f;
            var layout = AddVertical(panel, 10, 18);
            layout.childForceExpandHeight = false;
            Text("HeaderText", panel, 24, FontStyles.Bold, "方块管理", TextAlignmentOptions.MidlineLeft);
            var hint = Text("HintText", panel, 16, FontStyles.Normal, string.Empty, TextAlignmentOptions.TopLeft);
            hint.rectTransform.gameObject.AddComponent<LayoutElement>().preferredHeight = 54f;

            var activeSection = Panel("ActiveSection", panel, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 0f));
            activeSection.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.2f, 0.94f);
            var activeLayout = AddVertical(activeSection, 6, 10);
            activeLayout.childForceExpandHeight = false;
            var activeElement = activeSection.gameObject.AddComponent<LayoutElement>();
            activeElement.flexibleHeight = 1f;
            activeElement.minHeight = 420f;
            activeElement.preferredHeight = 560f;
            Text("TitleText", activeSection, 22, FontStyles.Bold, "上阵", TextAlignmentOptions.MidlineLeft);
            var activeRows = ScrollArea("ScrollView", activeSection);
            activeRows.name = "Rows";
            activeRows.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            activeRows.GetComponent<VerticalLayoutGroup>().childControlHeight = true;

            var reserveSection = Panel("ReserveSection", panel, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 0f));
            reserveSection.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.2f, 0.94f);
            var reserveLayout = AddVertical(reserveSection, 6, 10);
            reserveLayout.childForceExpandHeight = false;
            var reserveElement = reserveSection.gameObject.AddComponent<LayoutElement>();
            reserveElement.minHeight = 180f;
            reserveElement.preferredHeight = 220f;
            reserveElement.flexibleHeight = 0f;
            Text("TitleText", reserveSection, 22, FontStyles.Bold, "仓库", TextAlignmentOptions.MidlineLeft);
            var reserveRows = ScrollArea("ScrollView", reserveSection);
            reserveRows.name = "Rows";
            reserveRows.GetComponent<VerticalLayoutGroup>().childControlHeight = true;
        }

        static void BuildDamagePanel(RectTransform hudRoot)
        {
            var panel = Panel("DamageCounterPanel", hudRoot, new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, new Vector2(220f, 0f));
            panel.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.78f);
            var element = panel.gameObject.AddComponent<LayoutElement>();
            element.minWidth = 180f;
            element.preferredWidth = 220f;
            element.minHeight = 112f;
            element.preferredHeight = 132f;
            var layout = AddVertical(panel, 4, 12);
            AddContentFitter(panel, false, true);
            var label = Text("LabelText", panel, 28, FontStyles.Bold, "伤害", TextAlignmentOptions.Center);
            label.color = new Color(1f, 0.9f, 0.55f, 1f);
            label.rectTransform.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
            var value = Text("ValueText", panel, 52, FontStyles.Bold, "0", TextAlignmentOptions.Center);
            value.color = Color.white;
            value.rectTransform.gameObject.AddComponent<LayoutElement>().preferredHeight = 62f;
        }

        static void BuildTooltipPanels(RectTransform tooltipRoot)
        {
            var tooltipPanel = Panel("TooltipPanel", tooltipRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(320f, 180f));
            tooltipPanel.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.96f);
            AddVertical(tooltipPanel, 6, 12);
            Text("TitleText", tooltipPanel, 22, FontStyles.Bold, string.Empty, TextAlignmentOptions.MidlineLeft);
            Text("BodyText", tooltipPanel, 16, FontStyles.Normal, string.Empty, TextAlignmentOptions.TopLeft).rectTransform.sizeDelta = new Vector2(0f, 120f);
            tooltipPanel.gameObject.SetActive(false);

            var dragPanel = Panel("DragStickerPanel", tooltipRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(260f, 120f));
            dragPanel.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.96f);
            AddVertical(dragPanel, 6, 12);
            Text("NameText", dragPanel, 22, FontStyles.Bold, string.Empty, TextAlignmentOptions.MidlineLeft);
            Text("MaskText", dragPanel, 18, FontStyles.Normal, string.Empty, TextAlignmentOptions.MidlineLeft);
            Text("HintText", dragPanel, 16, FontStyles.Normal, string.Empty, TextAlignmentOptions.TopLeft);
            dragPanel.gameObject.SetActive(false);
        }

        static void BuildBlockRewardModal(RectTransform modalRoot)
        {
            var modal = Modal("BlockRewardModal", modalRoot, new Vector2(1080f, 620f));
            var header = ModalHeader(modal.window);
            Text("TitleText", header, 34, FontStyles.Bold, "选择 1 个新方块", TextAlignmentOptions.Center);
            var subtitle = Text("SubtitleText", header, 18, FontStyles.Normal, string.Empty, TextAlignmentOptions.Center);
            subtitle.rectTransform.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;
            var body = ModalBody(modal.window);
            ConfigureGridContent(ScrollArea("ScrollView", body), 3, new Vector2(300f, 236f), new Vector2(12f, 12f));
            Footer(modal.window, ("SkipButton", "跳过"));
            modal.overlay.gameObject.SetActive(false);
        }

        static void BuildRewardModal(RectTransform modalRoot)
        {
            var modal = Modal("RewardModal", modalRoot, new Vector2(1080f, 620f));
            var header = ModalHeader(modal.window);
            Text("TitleText", header, 34, FontStyles.Bold, "选择奖励", TextAlignmentOptions.Center);
            var subtitle = Text("SubtitleText", header, 18, FontStyles.Normal, string.Empty, TextAlignmentOptions.Center);
            subtitle.rectTransform.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;
            var body = ModalBody(modal.window);
            ConfigureGridContent(ScrollArea("ScrollView", body), 3, new Vector2(300f, 236f), new Vector2(12f, 12f));
            Footer(modal.window, ("RerollButton", "刷新奖励"), ("SkipButton", "跳过并得金币"));
            modal.overlay.gameObject.SetActive(false);
        }

        static void BuildShopModal(RectTransform modalRoot)
        {
            var modal = Modal("ShopModal", modalRoot, new Vector2(1180f, 760f));
            var header = ModalHeader(modal.window);
            Text("TitleText", header, 34, FontStyles.Bold, "商店", TextAlignmentOptions.Center);
            Text("SubtitleText", header, 18, FontStyles.Normal, string.Empty, TextAlignmentOptions.Center);
            Text("GoldText", header, 18, FontStyles.Bold, string.Empty, TextAlignmentOptions.Center);
            var feedback = Text("FeedbackText", header, 16, FontStyles.Normal, string.Empty, TextAlignmentOptions.Center);
            feedback.rectTransform.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

            var body = ModalBody(modal.window);
            var itemsPanel = Panel("ItemsPanel", body, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 0f));
            ConfigureVerticalChild(itemsPanel, flexibleHeight: 1f, minHeight: 260f, preferredHeight: 340f);
            itemsPanel.gameObject.AddComponent<Image>().color = new Color(0.14f, 0.17f, 0.22f, 0.75f);
            AddVertical(itemsPanel, 8, 12);
            var itemsPanelElement = itemsPanel.gameObject.AddComponent<LayoutElement>();
            itemsPanelElement.flexibleHeight = 1f;
            itemsPanelElement.minHeight = 260f;
            itemsPanelElement.preferredHeight = 340f;
            ConfigureGridContent(ScrollArea("ItemsScroll", itemsPanel), 3, new Vector2(300f, 206f), new Vector2(12f, 12f));

            var deletePanel = Panel("DeletePanel", body, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 0f));
            ConfigureVerticalChild(deletePanel, flexibleHeight: 0f, minHeight: 240f, preferredHeight: 280f);
            deletePanel.gameObject.AddComponent<Image>().color = new Color(0.14f, 0.17f, 0.22f, 0.75f);
            AddVertical(deletePanel, 8, 12);
            var deleteElement = deletePanel.gameObject.AddComponent<LayoutElement>();
            deleteElement.minHeight = 220f;
            deleteElement.preferredHeight = 250f;

            var deleteTitles = Panel("Titles", deletePanel, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 0f));
            ConfigureVerticalChild(deleteTitles, preferredHeight: 30f, minHeight: 30f);
            var titlesLayout = AddHorizontal(deleteTitles, 12, 0);
            titlesLayout.childControlHeight = false;
            titlesLayout.childForceExpandWidth = true;
            Text("ActiveTitleText", deleteTitles, 20, FontStyles.Bold, "删除：上阵", TextAlignmentOptions.Left);
            Text("ReserveTitleText", deleteTitles, 20, FontStyles.Bold, "删除：仓库", TextAlignmentOptions.Left);
            var hint = Text("HintText", deletePanel, 16, FontStyles.Normal, string.Empty, TextAlignmentOptions.Left);
            hint.rectTransform.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

            var columns = Panel("Columns", deletePanel, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 0f));
            ConfigureVerticalChild(columns, flexibleHeight: 1f, minHeight: 120f);
            var columnsLayout = AddHorizontal(columns, 12, 0);
            columnsLayout.childForceExpandWidth = true;
            columns.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            var activeColumn = Panel("ActiveColumn", columns, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 0f));
            ConfigureHorizontalChild(activeColumn, flexibleWidth: 1f);
            activeColumn.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddVertical(activeColumn, 6, 0);
            var active = ScrollArea("ActiveScroll", activeColumn);
            active.name = "ActiveContent";
            active.GetComponent<VerticalLayoutGroup>().childControlHeight = true;

            var reserveColumn = Panel("ReserveColumn", columns, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 0f));
            ConfigureHorizontalChild(reserveColumn, flexibleWidth: 1f);
            reserveColumn.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddVertical(reserveColumn, 6, 0);
            var reserve = ScrollArea("ReserveScroll", reserveColumn);
            reserve.name = "ReserveContent";
            reserve.GetComponent<VerticalLayoutGroup>().childControlHeight = true;

            Footer(modal.window, ("RerollButton", "刷新商店"), ("CloseButton", "离开商店"));
            modal.overlay.gameObject.SetActive(false);
        }

        static void BuildLoadoutModal(RectTransform modalRoot)
        {
            var modal = Modal("LoadoutModal", modalRoot, new Vector2(1220f, 760f));
            var header = ModalHeader(modal.window);
            Text("TitleText", header, 34, FontStyles.Bold, "整理配置", TextAlignmentOptions.Center);
            var subtitle = Text("SubtitleText", header, 18, FontStyles.Normal, string.Empty, TextAlignmentOptions.Center);
            subtitle.rectTransform.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;

            var body = ModalBody(modal.window);
            body.name = "Body";
            var columns = Panel("Columns", body, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 0f));
            ConfigureVerticalChild(columns, flexibleHeight: 1f, minHeight: 360f);
            columns.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            var bodyLayout = AddHorizontal(columns, 12, 0);
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandHeight = true;

            var inventoryPanel = Panel("InventoryPanel", columns, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 0f));
            ConfigureHorizontalChild(inventoryPanel, flexibleWidth: 1.4f, minWidth: 360f);
            inventoryPanel.gameObject.AddComponent<Image>().color = new Color(0.14f, 0.17f, 0.22f, 0.75f);
            var inventoryElement = inventoryPanel.gameObject.AddComponent<LayoutElement>();
            inventoryElement.flexibleWidth = 1.4f;
            inventoryElement.minWidth = 360f;
            AddVertical(inventoryPanel, 8, 12);
            Text("InventoryTitleText", inventoryPanel, 22, FontStyles.Bold, "嵌片", TextAlignmentOptions.Left);
            ScrollArea("ScrollView", inventoryPanel);

            var modsPanel = Panel("ModsPanel", columns, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 0f));
            ConfigureHorizontalChild(modsPanel, flexibleWidth: 1f, minWidth: 300f);
            modsPanel.gameObject.AddComponent<Image>().color = new Color(0.14f, 0.17f, 0.22f, 0.75f);
            var modsElement = modsPanel.gameObject.AddComponent<LayoutElement>();
            modsElement.flexibleWidth = 1f;
            modsElement.minWidth = 300f;
            AddVertical(modsPanel, 8, 12);
            Text("ActiveTitleText", modsPanel, 22, FontStyles.Bold, "启用模组", TextAlignmentOptions.Left);
            var active = Panel("ActiveContent", modsPanel, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 0f));
            ConfigureVerticalChild(active, flexibleHeight: 1f);
            AddVertical(active, 6, 0).childControlHeight = true;
            active.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Text("ReserveTitleText", modsPanel, 22, FontStyles.Bold, "待机模组", TextAlignmentOptions.Left);
            var reserve = Panel("ReserveContent", modsPanel, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 0f));
            ConfigureVerticalChild(reserve, flexibleHeight: 1f);
            AddVertical(reserve, 6, 0).childControlHeight = true;
            reserve.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Footer(modal.window, ("CancelButton", "取消拖拽"), ("ContinueButton", "继续战斗"));
            modal.overlay.gameObject.SetActive(false);
        }

        static void BuildGameOverModal(RectTransform modalRoot)
        {
            var modal = Modal("GameOverModal", modalRoot, new Vector2(720f, 340f));
            var header = ModalHeader(modal.window);
            Text("TitleText", header, 38, FontStyles.Bold, "本局结束", TextAlignmentOptions.Center);
            var body = ModalBody(modal.window);
            var message = Text("MessageText", body, 20, FontStyles.Normal, string.Empty, TextAlignmentOptions.Center);
            message.rectTransform.gameObject.AddComponent<LayoutElement>().preferredHeight = 110f;
            Footer(modal.window, ("RetryButton", "再来一局"), ("MenuButton", "主菜单"));
            modal.overlay.gameObject.SetActive(false);
        }

        static (RectTransform overlay, RectTransform window) Modal(string name, Transform parent, Vector2 size)
        {
            var overlay = StretchPanel(name, parent);
            overlay.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
            var window = Panel("Window", overlay, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            window.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.12f, 0.18f, 0.98f);
            window.gameObject.AddComponent<LayoutElement>();
            var layout = AddVertical(window, 12, 18);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            return (overlay, window);
        }

        static RectTransform ModalHeader(RectTransform window)
        {
            var header = Panel("Header", window, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 0f));
            ConfigureVerticalChild(header, preferredHeight: 108f, minHeight: 92f);
            AddVertical(header, 6, 0).childControlHeight = false;
            return header;
        }

        static RectTransform ModalBody(RectTransform window)
        {
            var body = Panel("Body", window, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 0f));
            ConfigureVerticalChild(body, flexibleHeight: 1f, minHeight: 180f);
            var layout = AddVertical(body, 12, 0);
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            return body;
        }

        static RectTransform Footer(RectTransform modalWindow, params (string name, string label)[] buttons)
        {
            var footer = Panel("Footer", modalWindow, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 56f));
            ConfigureVerticalChild(footer, preferredHeight: 56f, minHeight: 56f);
            var layout = AddHorizontal(footer, 10, 0);
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childAlignment = TextAnchor.MiddleRight;
            var element = footer.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 56f;
            element.preferredHeight = 56f;
            foreach (var (name, label) in buttons)
                Button(name, footer, label);
            return footer;
        }

        static RectTransform ScrollArea(string name, Transform parent)
        {
            var scroll = Panel(name, parent, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 0f));
            ConfigureVerticalChild(scroll, flexibleHeight: 1f, minHeight: 120f);
            scroll.gameObject.AddComponent<Image>().color = new Color(0.14f, 0.17f, 0.22f, 0.75f);
            var scrollElement = scroll.gameObject.AddComponent<LayoutElement>();
            scrollElement.flexibleHeight = 1f;
            scrollElement.minHeight = 120f;
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 32f;
            var viewport = StretchPanel("Viewport", scroll);
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var content = StretchPanel("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            AddVertical(content, 10, 12);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            return content;
        }

        static void ConfigureGridContent(RectTransform content, int columns, Vector2 cellSize, Vector2 spacing)
        {
            var vertical = content.GetComponent<VerticalLayoutGroup>();
            if (vertical != null)
                Object.DestroyImmediate(vertical);

            var fitter = content.GetComponent<ContentSizeFitter>() ?? content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var grid = content.GetComponent<GridLayoutGroup>() ?? content.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = cellSize;
            grid.spacing = spacing;
            grid.padding = new RectOffset(12, 12, 12, 12);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        }

        static void ConfigureVerticalChild(RectTransform rect, float flexibleHeight = 0f, float minHeight = -1f, float preferredHeight = -1f)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;

            var element = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
            if (minHeight >= 0f)
                element.minHeight = minHeight;
            if (preferredHeight >= 0f)
                element.preferredHeight = preferredHeight;
            element.flexibleHeight = flexibleHeight;
        }

        static void ConfigureHorizontalChild(RectTransform rect, float flexibleWidth = 0f, float minWidth = -1f, float preferredWidth = -1f)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;

            var element = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
            if (minWidth >= 0f)
                element.minWidth = minWidth;
            if (preferredWidth >= 0f)
                element.preferredWidth = preferredWidth;
            element.flexibleWidth = flexibleWidth;
        }

        static RectTransform Panel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return rect;
        }

        static RectTransform StretchPanel(string name, Transform parent)
        {
            var rect = Panel(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        static TMP_Text Text(string name, Transform parent, float size, FontStyles style, string value, TextAlignmentOptions align)
        {
            var rect = Panel(name, parent, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 30f));
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = PrototypeVisualFactory.GetCjkTmpFontAsset() ?? TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = align;
            text.text = value;
            text.color = Color.white;
            return text;
        }

        static Button Button(string name, Transform parent, string label)
        {
            var rect = Panel(name, parent, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 40f));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.24f, 0.42f, 0.82f, 1f);
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = image.color * 1.08f;
            colors.pressedColor = image.color * 0.92f;
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.25f, 0.3f, 0.4f, 0.7f);
            button.colors = colors;
            var text = Text("Label", rect, 20, FontStyles.Bold, label, TextAlignmentOptions.Center);
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return button;
        }

        static VerticalLayoutGroup AddVertical(RectTransform rect, int spacing, int padding)
        {
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            return layout;
        }

        static HorizontalLayoutGroup AddHorizontal(RectTransform rect, int spacing, int padding)
        {
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            return layout;
        }

        static void AddContentFitter(RectTransform rect, bool horizontalPreferred, bool verticalPreferred)
        {
            var fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = horizontalPreferred ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = verticalPreferred ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
        }

        static GameObject CreateChild(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static GameObject CreateSpriteChild(GameObject parent, string name, Vector3 localPosition, Vector3 localScale, Color color, int sortingOrder, bool circle = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = circle ? PrototypeVisualFactory.CircleSprite : PrototypeVisualFactory.SquareSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return go;
        }

        static TextMesh CreateTextChild(GameObject parent, string name, Vector3 localPosition, string text, Color color, int sortingOrder, float scale, FontStyle style = FontStyle.Bold)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPosition;
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.color = color;
            mesh.fontSize = 64;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = scale;
            mesh.fontStyle = style;
            mesh.font = PrototypeVisualFactory.GetCjkRuntimeFont();
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = mesh.font.material;
            renderer.sortingOrder = sortingOrder;
            return mesh;
        }

        static LineRenderer CreateLineRenderer(GameObject parent, string name, Color color, float startWidth, float endWidth, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = color;
            line.endColor = color;
            line.startWidth = startWidth;
            line.endWidth = endWidth;
            line.positionCount = 0;
            line.useWorldSpace = true;
            line.sortingOrder = sortingOrder;
            return line;
        }

        static void WirePlayerPresenter(GameObject hero)
        {
            var presenter = hero.GetComponent<PlayerPresenter>();
            var so = new SerializedObject(presenter);
            so.FindProperty("bodyRenderer").objectReferenceValue = hero.transform.Find("HeroBody")?.GetComponent<SpriteRenderer>();
            so.FindProperty("coreRenderer").objectReferenceValue = hero.transform.Find("HeroCore")?.GetComponent<SpriteRenderer>();
            so.FindProperty("hpFillRenderer").objectReferenceValue = hero.transform.Find("HpFill")?.GetComponent<SpriteRenderer>();
            so.FindProperty("nameLabel").objectReferenceValue = hero.transform.Find("HeroName")?.GetComponent<TextMesh>();
            so.FindProperty("hpLabel").objectReferenceValue = hero.transform.Find("HeroHp")?.GetComponent<TextMesh>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireEnemyController(GameObject enemy)
        {
            var controller = enemy.GetComponent<EnemyController>();
            var so = new SerializedObject(controller);
            so.FindProperty("bodyRenderer").objectReferenceValue = enemy.transform.Find("EnemyBody")?.GetComponent<SpriteRenderer>();
            so.FindProperty("coreRenderer").objectReferenceValue = enemy.transform.Find("EnemyCore")?.GetComponent<SpriteRenderer>();
            so.FindProperty("hpFillRenderer").objectReferenceValue = enemy.transform.Find("HpFill")?.GetComponent<SpriteRenderer>();
            so.FindProperty("hpPreviewRenderer").objectReferenceValue = enemy.transform.Find("HpPreview")?.GetComponent<SpriteRenderer>();
            so.FindProperty("nameLabel").objectReferenceValue = enemy.transform.Find("EnemyName")?.GetComponent<TextMesh>();
            so.FindProperty("intentLabel").objectReferenceValue = enemy.transform.Find("EnemyIntent")?.GetComponent<TextMesh>();
            so.FindProperty("hpLabel").objectReferenceValue = enemy.transform.Find("EnemyHp")?.GetComponent<TextMesh>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireBallLauncher(PlayerLauncher launcher, Transform ballTransform)
        {
            var so = new SerializedObject(launcher);
            so.FindProperty("aimLine").objectReferenceValue = ballTransform.Find("AimPreviewLine")?.GetComponent<LineRenderer>();
            so.FindProperty("memoryLine").objectReferenceValue = ballTransform.Find("AimMemoryLine")?.GetComponent<LineRenderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireGame(PopHeroGame game, PopHeroPrototypeConfig config, Transform worldRoot, Transform boardRoot, Transform blockRoot, Transform enemyLayerRoot, Transform battleStageRoot, Transform battleEffectsRoot, SpriteRenderer boardFrame, SpriteRenderer boardBackground, SpriteRenderer launchGuide, Transform launchMarker, GameObject bottomLineObject, Transform wallTopRoot, Transform wallLeftRoot, Transform wallRightRoot, SpriteRenderer enemyPanel, PlayerPresenter playerPresenter, EnemyController enemyController, BallController ballController, Rigidbody2D ballBody, CircleCollider2D ballCollider, TrailRenderer ballTrail, PlayerLauncher launcher, CanvasHudController canvasHud)
        {
            var so = new SerializedObject(game);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("worldRoot").objectReferenceValue = worldRoot;
            so.FindProperty("boardRoot").objectReferenceValue = boardRoot;
            so.FindProperty("blockRoot").objectReferenceValue = blockRoot;
            so.FindProperty("enemyLayerRoot").objectReferenceValue = enemyLayerRoot;
            so.FindProperty("battleStageRef").objectReferenceValue = battleStageRoot;
            so.FindProperty("battleEffectsRef").objectReferenceValue = battleEffectsRoot;
            so.FindProperty("boardFrame").objectReferenceValue = boardFrame;
            so.FindProperty("boardBackground").objectReferenceValue = boardBackground;
            so.FindProperty("launchGuide").objectReferenceValue = launchGuide;
            so.FindProperty("launchMarkerRef").objectReferenceValue = launchMarker;
            so.FindProperty("bottomLineObject").objectReferenceValue = bottomLineObject;
            so.FindProperty("wallTopRoot").objectReferenceValue = wallTopRoot;
            so.FindProperty("wallLeftRoot").objectReferenceValue = wallLeftRoot;
            so.FindProperty("wallRightRoot").objectReferenceValue = wallRightRoot;
            so.FindProperty("enemyPanel").objectReferenceValue = enemyPanel;
            so.FindProperty("playerPresenterRef").objectReferenceValue = playerPresenter;
            so.FindProperty("enemyControllerRef").objectReferenceValue = enemyController;
            so.FindProperty("ballControllerRef").objectReferenceValue = ballController;
            so.FindProperty("ballRigidbody").objectReferenceValue = ballBody;
            so.FindProperty("ballCircleCollider").objectReferenceValue = ballCollider;
            so.FindProperty("ballTrail").objectReferenceValue = ballTrail;
            so.FindProperty("launcherRef").objectReferenceValue = launcher;
            so.FindProperty("canvasHudRef").objectReferenceValue = canvasHud;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
