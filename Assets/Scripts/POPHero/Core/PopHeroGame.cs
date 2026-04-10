using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public class PopHeroGame : MonoBehaviour, IGameReadModel, IHudCommandSink
    {
        static readonly float[] WallStoneShadePattern = { -0.22f, 0.1f, -0.08f, 0.16f, -0.14f, 0.05f, 0.12f, -0.04f };

        enum IntermissionActionKind
        {
            None,
            SelectBlockReward,
            SkipBlockReward,
            EnterStickerRewardPhase,
            SelectReward,
            RerollRewardChoices,
            SkipRewardChoices,
            OpenShop,
            CloseShop,
            FinishLoadout
        }

        public PopHeroPrototypeConfig config;
        public PopHeroPrototypeConfig Config => config;

        // Scene references assigned from the Battle scene.
        [Header("Scene References")]
        [SerializeField] Transform worldRoot;
        [SerializeField] Transform boardRoot;
        [SerializeField] Transform blockRoot;
        [SerializeField] Transform enemyLayerRoot;
        [SerializeField] Transform battleStageRef;
        [SerializeField] Transform battleEffectsRef;

        [Header("Board Visuals")]
        [SerializeField] SpriteRenderer boardFrame;
        [SerializeField] SpriteRenderer boardBackground;
        [SerializeField] SpriteRenderer launchGuide;
        [SerializeField] Transform launchMarkerRef;
        [SerializeField] GameObject bottomLineObject;
        [SerializeField] Transform wallTopRoot;
        [SerializeField] Transform wallLeftRoot;
        [SerializeField] Transform wallRightRoot;

        [Header("Enemy Layer Visuals")]
        [SerializeField] SpriteRenderer enemyPanel;

        [Header("Characters")]
        [SerializeField] PlayerPresenter playerPresenterRef;
        [SerializeField] EnemyController enemyControllerRef;

        [Header("Ball")]
        [SerializeField] BallController ballControllerRef;
        [SerializeField] Rigidbody2D ballRigidbody;
        [SerializeField] CircleCollider2D ballCircleCollider;
        [SerializeField] TrailRenderer ballTrail;
        [SerializeField] PlayerLauncher launcherRef;

        [Header("Components on this GameObject")]
        [SerializeField] PopHeroHud hudRef;
        [SerializeField] DamageCounterView damageCounterRef;
        [SerializeField] CanvasHudController canvasHudRef;

        public RoundState State { get; private set; }
        public PlayerData Player { get; private set; }
        public EnemyData CurrentEnemy { get; private set; }
        public Rect BoardRect { get; private set; }
        public float LaunchY { get; private set; }
        public int RemainingLaunchesForEnemy { get; private set; }
        public string GameOverMessage { get; private set; } = "本局结束。";
        public string IntermissionMessage { get; private set; } = string.Empty;
        public int PreviewAttackScore { get; private set; }
        public int PreviewShieldGain { get; private set; }
        public int PreviewHitCount { get; private set; }
        public int PreviewAttackBlockCount { get; private set; }
        public int PreviewShieldBlockCount { get; private set; }
        public int PreviewMultiplierBlockCount { get; private set; }

        public PlayerLauncher Launcher => launcher;
        public BallController Ball => ballController;
        public RoundController RoundController => roundController;
        public BoardManager BoardManager => boardManager;
        public EnemyController EnemyPresenter => enemyController;
        public PlayerPresenter HeroPresenter => playerPresenter;
        public StickerCatalog StickerCatalog => stickerCatalog;
        public StickerInventory StickerInventory => stickerInventory;
        public StickerEffectRunner StickerEffectRunner => stickerEffectRunner;
        public RewardChoiceController RewardChoiceController => rewardChoiceController;
        public ModManager ModManager => modManager;
        public ShopManager ShopManager => shopManager;
        public ICombatEventHub CombatEventHub => combatEventHub;
        public IBounceStepSolver BounceStepSolver => bounceStepSolver;
        public IBlockCollectionService BlockCollections => blockCollectionService;
        public IBlockRewardService BlockRewards => blockRewardService;
        public IRuntimeBoardService RuntimeBoard => runtimeBoardService;
        public IModService Mods => modService;
        public IShopService Shops => shopService;
        public int EncounterIndex => enemyEncounterIndex + 1;
        public int MaxLaunchesPerEnemy => Mathf.Max(1, config.enemies.maxLaunchesPerEnemy + (Player?.BonusLaunchesPerEnemy ?? 0));
        public InputAimMode CurrentAimMode => config.aim.currentAimMode;
        public bool IsInitialBlockDraftPending => initialBlockDraftPending;
        public bool CanManageBlockAssignments => State == RoundState.Shop || State == RoundState.LoadoutManage;
        public string AimModeDisplayText => CurrentAimMode == InputAimMode.PCMouseAimClick ? "移动鼠标瞄准，左键发射" : "拖动瞄准，再点一次发射";
        public string CurrentAimModeLabel => CurrentAimMode == InputAimMode.PCMouseAimClick ? "移动鼠标瞄准，左键发射" : "拖动瞄准，再点一次发射";
        public Vector2 CurrentLaunchPoint => roundController != null ? roundController.LaunchPosition : new Vector2(BoardRect.center.x, LaunchY);
        public IReadOnlyList<WallAimPoint> WallAimPoints => wallAimPoints;

        PlayerLauncher launcher;
        BallController ballController;
        TrajectoryPredictor trajectoryPredictor;
        BoardManager boardManager;
        RoundController roundController;
        EnemyController enemyController;
        PlayerPresenter playerPresenter;
        PopHeroHud hud;
        CanvasHudController canvasHud;
        DamageCounterView damageCounterView;
        PhysicsMaterial2D bounceMaterial;
        Transform launchMarker;
        Transform battleStageRoot;
        Transform battleEffectsRoot;
        StickerCatalog stickerCatalog;
        StickerInventory stickerInventory;
        StickerEffectRunner stickerEffectRunner;
        RewardChoiceController rewardChoiceController;
        ModManager modManager;
        ShopManager shopManager;
        ICombatEventHub combatEventHub;
        IBounceStepSolver bounceStepSolver;
        IBlockCollectionService blockCollectionService;
        IBlockRewardService blockRewardService;
        IRuntimeBoardService runtimeBoardService;
        IModService modService;
        IShopService shopService;
        GamePhaseStateMachine phaseStateMachine;
        GameSessionController gameSessionController;
        BattleFlowController battleFlowController;
        IntermissionFlowController intermissionFlowController;
        readonly List<WallAimPoint> wallAimPoints = new();
        int enemyEncounterIndex;
        bool initialBlockDraftPending;
        IntermissionActionKind pendingIntermissionAction;
        int pendingIntermissionIndex = -1;
        bool isBattlePresentationPlaying;
        Coroutine battlePresentationRoutine;
        Vector3 playerIdlePosition;
        Vector3 enemyIdlePosition;

        void Awake()
        {
            config = Resources.Load<PopHeroPrototypeConfig>("PopHeroPrototypeConfig") ?? PopHeroPrototypeConfig.CreateRuntimeDefault();
            config.aim ??= new AimSettings();
            CacheArenaRect();
            SetupCamera();
            BuildPrototype();
            StartPrototype();
        }

        public bool CanSimulate()
        {
            return State != RoundState.GameOver && !isBattlePresentationPlaying;
        }

        public void ApplyPreviewResult(TrajectoryPreviewResult preview)
        {
            if (State != RoundState.Aim || preview == null || !preview.HasValidPath)
            {
                ClearAimPreview();
                return;
            }

            PreviewAttackScore = 0;
            PreviewShieldGain = 0;
            PreviewHitCount = preview.hitBlocks.Count;
            PreviewAttackBlockCount = 0;
            PreviewShieldBlockCount = 0;
            PreviewMultiplierBlockCount = 0;
            foreach (var block in preview.hitBlocks)
            {
                if (block == null)
                    continue;

                switch (block.blockType)
                {
                    case BoardBlockType.AttackAdd:
                        PreviewAttackBlockCount += 1;
                        break;
                    case BoardBlockType.AttackMultiply:
                        PreviewMultiplierBlockCount += 1;
                        break;
                    case BoardBlockType.Shield:
                        PreviewShieldBlockCount += 1;
                        break;
                }
            }

            boardManager?.ApplyPreviewState(preview.hitBlocks);
        }

        public void ClearAimPreview()
        {
            PreviewAttackScore = 0;
            PreviewShieldGain = 0;
            PreviewHitCount = 0;
            PreviewAttackBlockCount = 0;
            PreviewShieldBlockCount = 0;
            PreviewMultiplierBlockCount = 0;
            boardManager?.ClearPreviewState();
            enemyController?.ClearPreviewDamage();
        }

        public void RefreshPendingDamagePreview()
        {
            if (State == RoundState.BallFlying)
            {
                var pendingDamage = roundController != null ? roundController.PendingDamage : 0;
                damageCounterView?.Show();
                damageCounterView?.SetValue(pendingDamage);
                return;
            }

            damageCounterView?.Hide();
        }

        public float GetAimRecalcDistanceThreshold()
        {
            return Mathf.Max(0.05f, config.aim.inputRecalcDistance * modManager.GetFastFingerMultiplier());
        }

        public float GetAimHoldDistanceThreshold()
        {
            var bonus = 1f + config.aim.aimAssistBonus * modManager.GetAimAssistBonus();
            return Mathf.Max(0.05f, config.aim.inputReleaseDistance * bonus * modManager.GetSlowFingerMultiplier());
        }

        public float GetAimRecalcAngleThreshold()
        {
            return Mathf.Max(0.25f, config.aim.inputRecalcAngle * modManager.GetFastFingerMultiplier());
        }

        public float GetAimHoldAngleThreshold()
        {
            return Mathf.Max(0.25f, config.aim.inputHoldAngle * (1f + modManager.GetStableAimBonus()) * modManager.GetSlowFingerMultiplier());
        }

        void CacheArenaRect()
        {
            var size = config.arena.boardSize;
            var center = config.arena.boardCenter;
            BoardRect = new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
            LaunchY = BoardRect.yMin + config.arena.launchLineOffset;
        }

        void SetupCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogWarning("[POPHero] Main Camera not found in scene. Creating one.");
                var cameraGo = new GameObject("Main Camera");
                camera = cameraGo.AddComponent<Camera>();
                cameraGo.tag = "MainCamera";
            }

            camera.orthographic = true;
            camera.orthographicSize = config.arena.cameraSize;
            camera.transform.position = new Vector3(0f, 0.25f, -10f);
            camera.backgroundColor = config.arena.backgroundColor;
            camera.clearFlags = CameraClearFlags.SolidColor;
        }

        void BuildPrototype()
        {
            bounceMaterial = new PhysicsMaterial2D("POPHeroBounce") { bounciness = 1f, friction = 0f };

            // Use scene references instead of creating frontend GameObjects.
            if (worldRoot == null) worldRoot = transform.Find("World");
            if (boardRoot == null) boardRoot = worldRoot?.Find("Board");
            if (blockRoot == null) blockRoot = worldRoot?.Find("Blocks");
            if (enemyLayerRoot == null) enemyLayerRoot = worldRoot?.Find("EnemyLayer");
            if (worldRoot == null) Debug.LogError("[POPHero] Battle scene is missing World root.");
            if (boardRoot == null) Debug.LogError("[POPHero] Battle scene is missing World/Board.");
            if (blockRoot == null) Debug.LogError("[POPHero] Battle scene is missing World/Blocks.");
            if (enemyLayerRoot == null) Debug.LogError("[POPHero] Battle scene is missing World/EnemyLayer.");

            BindEnemyLayer();
            BindBoard();
            BindBall();

            roundController = GetComponent<RoundController>() ?? gameObject.AddComponent<RoundController>();
            boardManager = GetComponent<BoardManager>() ?? gameObject.AddComponent<BoardManager>();
            trajectoryPredictor = GetComponent<TrajectoryPredictor>() ?? gameObject.AddComponent<TrajectoryPredictor>();
            canvasHud = canvasHudRef != null ? canvasHudRef : FindCanvasHudControllerInScene();
            hud = hudRef != null ? hudRef : GetComponent<PopHeroHud>();
            damageCounterView = damageCounterRef != null ? damageCounterRef : GetComponent<DamageCounterView>();

            boardManager.Initialize(this, blockRoot, bounceMaterial);
            trajectoryPredictor.Initialize(this, ballController);
            ballController.SetTrajectoryPredictor(trajectoryPredictor);
            bounceStepSolver = new BounceStepSolver(this, ballController);

            stickerCatalog = new StickerCatalog();
            stickerInventory = new StickerInventory();
            stickerEffectRunner = new StickerEffectRunner();
            rewardChoiceController = new RewardChoiceController();
            modManager = new ModManager();
            shopManager = new ShopManager();
            combatEventHub = new CombatEventHub();

            stickerInventory.Initialize(this);
            stickerEffectRunner.Initialize(this);
            rewardChoiceController.Initialize(this);
            modManager.Initialize(this);
            shopManager.Initialize(this);

            blockCollectionService = new BlockCollectionServiceFacade(boardManager);
            blockRewardService = new BlockRewardServiceFacade(boardManager);
            runtimeBoardService = new RuntimeBoardServiceFacade(boardManager);
            modService = new ModServiceFacade(modManager);
            shopService = new ShopServiceFacade(shopManager);
            ConfigurePhaseStateMachine();
            gameSessionController = new GameSessionController(this);
            battleFlowController = new BattleFlowController(this);
            intermissionFlowController = new IntermissionFlowController(this);

            launcher = launcherRef != null ? launcherRef : (ballController.GetComponent<PlayerLauncher>() ?? ballController.gameObject.AddComponent<PlayerLauncher>());
            launcher.Initialize(this, ballController, trajectoryPredictor);
            if (canvasHud != null)
            {
                canvasHud.Initialize(this);
                if (hud != null)
                    hud.enabled = false;
                if (damageCounterView != null)
                    damageCounterView.enabled = false;
            }
            else
            {
                Debug.LogError("[POPHero] CanvasHudController not found in Battle scene. Falling back to legacy IMGUI HUD. Check CanvasFrontend bindings.");
                hud ??= gameObject.AddComponent<PopHeroHud>();
                damageCounterView ??= gameObject.AddComponent<DamageCounterView>();
                hud.Initialize(this);
                damageCounterView.Initialize(this);
            }
        }

        CanvasHudController FindCanvasHudControllerInScene()
        {
            var local = GetComponentInChildren<CanvasHudController>(true) ?? GetComponent<CanvasHudController>();
            if (local != null)
                return local;

            var all = FindObjectsOfType<CanvasHudController>(true);
            if (all != null && all.Length > 0)
                return all[0];

            return null;
        }

        void Update()
        {
            intermissionFlowController?.ProcessPendingAction();
        }

        void StartPrototype()
        {
            gameSessionController?.StartSession();
        }

        internal void StartPrototypeCore()
        {
            Player = new PlayerData(config.player.maxHp, config.player.currentHp, config.player.startShield, config.player.startGold);
            Player.IncreaseInventoryCapacity(config.stickers.baseInventoryCapacity - Player.StickerInventoryCapacity);
            roundController.Initialize(this, new Vector2(BoardRect.center.x, LaunchY));
            boardManager.ResetBlockProgression();
            ballController.PlaceAt(CurrentLaunchPoint);
            enemyEncounterIndex = 0;
            CurrentEnemy = null;
            initialBlockDraftPending = false;
            GameOverMessage = "本局结束。";
            IntermissionMessage = string.Empty;
            ClearPendingIntermissionAction();
            damageCounterView?.ResetCounter();
            isBattlePresentationPlaying = false;
            enemyController.gameObject.SetActive(false);
            playerPresenter?.Refresh(Player);
            ResetBattleActorPositions();
            UpdateLaunchMarker();
            if (!boardManager.GrantStartingCard(BoardBlockType.AttackAdd, BlockRarity.White, out _, out var failReason))
                throw new InvalidOperationException($"[POPHero] Failed to grant starting block: {failReason}");

            SpawnEnemy(enemyEncounterIndex);
            PrepareNextRound();
        }

        void BindEnemyLayer()
        {
            var panelCenter = new Vector2(BoardRect.center.x, BoardRect.yMax + config.arena.topPanelHeight * 0.56f);

            // Use scene reference or find in hierarchy
            if (enemyPanel == null)
            {
                var panelObj = enemyLayerRoot?.Find("EnemyPanel");
                if (panelObj != null) enemyPanel = panelObj.GetComponent<SpriteRenderer>();
            }
            if (enemyPanel != null)
            {
                enemyPanel.transform.position = panelCenter;
                enemyPanel.sprite = PrototypeVisualFactory.SquareSprite;
                enemyPanel.color = config.arena.topPanelColor;
                enemyPanel.sortingOrder = 1;
                enemyPanel.transform.localScale = new Vector3(BoardRect.width, config.arena.topPanelHeight, 1f);
            }

            if (battleStageRef == null) battleStageRef = enemyLayerRoot?.Find("BattleStage");
            battleStageRoot = battleStageRef;
            if (battleEffectsRef == null) battleEffectsRef = enemyLayerRoot?.Find("BattleEffects");
            battleEffectsRoot = battleEffectsRef;

            playerIdlePosition = panelCenter + new Vector2(-BoardRect.width * 0.28f, -0.16f);
            enemyIdlePosition = panelCenter + new Vector2(BoardRect.width * 0.2f, -0.08f);

            // Bind Hero
            if (playerPresenterRef == null) playerPresenterRef = battleStageRoot?.GetComponentInChildren<PlayerPresenter>(true);
            playerPresenter = playerPresenterRef;
            if (playerPresenter != null)
            {
                playerPresenter.transform.position = playerIdlePosition;
                playerPresenter.Initialize();
            }

            // Bind Enemy
            if (enemyControllerRef == null) enemyControllerRef = battleStageRoot?.GetComponentInChildren<EnemyController>(true);
            enemyController = enemyControllerRef;
            if (enemyController != null)
            {
                enemyController.transform.position = enemyIdlePosition;
                enemyController.Initialize(this);
            }
        }

        void BindBall()
        {
            // Use scene references
            if (ballControllerRef == null) ballControllerRef = worldRoot?.GetComponentInChildren<BallController>(true);
            ballController = ballControllerRef;

            if (ballController == null)
            {
                Debug.LogError("[POPHero] BallController not found in scene! Make sure Ball object exists under World.");
                return;
            }

            if (ballRigidbody == null) ballRigidbody = ballController.GetComponent<Rigidbody2D>();
            if (ballCircleCollider == null) ballCircleCollider = ballController.GetComponent<CircleCollider2D>();
            if (ballTrail == null) ballTrail = ballController.GetComponent<TrailRenderer>();

            // Configure physics
            ballRigidbody.gravityScale = 0f;
            ballRigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            ballRigidbody.sharedMaterial = bounceMaterial;
            ballRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            ballCircleCollider.radius = config.ball.radius;
            ballCircleCollider.sharedMaterial = bounceMaterial;

            // Configure trail
            if (ballTrail != null)
            {
                ballTrail.material = new Material(Shader.Find("Sprites/Default"));
                ballTrail.sortingLayerName = "Default";
                ballTrail.sortingOrder = 39;
                ballTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ballTrail.receiveShadows = false;
                ballTrail.minVertexDistance = 0.03f;
                ballTrail.numCornerVertices = 2;
                ballTrail.numCapVertices = 2;
                ballTrail.startColor = new Color(0.2f, 1f, 0.78f, 0.82f);
                ballTrail.endColor = new Color(0.2f, 1f, 0.78f, 0f);
                ballTrail.emitting = false;
            }

            ballController.Initialize(this, ballRigidbody, ballCircleCollider, ballTrail);
        }

        void BindBoard()
        {
            // Board Frame
            if (boardFrame == null)
            {
                var t = boardRoot?.Find("BoardFrame");
                if (t != null) boardFrame = t.GetComponent<SpriteRenderer>();
            }
            if (boardFrame != null)
            {
                boardFrame.sprite = PrototypeVisualFactory.SquareSprite;
                boardFrame.color = config.arena.boardFrameColor;
                boardFrame.sortingOrder = 2;
                boardFrame.transform.localScale = new Vector3(BoardRect.width + config.arena.wallThickness * 2f, BoardRect.height + config.arena.wallThickness * 2f, 1f);
                boardFrame.transform.position = BoardRect.center;
            }

            // Board Background
            if (boardBackground == null)
            {
                var t = boardRoot?.Find("BoardBackground");
                if (t != null) boardBackground = t.GetComponent<SpriteRenderer>();
            }
            if (boardBackground != null)
            {
                boardBackground.sprite = PrototypeVisualFactory.SquareSprite;
                boardBackground.color = config.arena.boardBackgroundColor;
                boardBackground.sortingOrder = 3;
                boardBackground.transform.localScale = new Vector3(BoardRect.width, BoardRect.height, 1f);
                boardBackground.transform.position = BoardRect.center;
            }

            // Launch Guide
            if (launchGuide == null)
            {
                var t = boardRoot?.Find("LaunchGuide");
                if (t != null) launchGuide = t.GetComponent<SpriteRenderer>();
            }
            if (launchGuide != null)
            {
                launchGuide.sprite = PrototypeVisualFactory.SquareSprite;
                launchGuide.color = config.arena.launchGuideColor;
                launchGuide.sortingOrder = 6;
                launchGuide.transform.localScale = new Vector3(BoardRect.width - 0.4f, 0.34f, 1f);
                launchGuide.transform.position = new Vector3(BoardRect.center.x, LaunchY - 0.15f, 0f);
            }

            // Launch Marker
            if (launchMarkerRef == null) launchMarkerRef = boardRoot?.Find("LaunchMarker");
            launchMarker = launchMarkerRef;
            if (launchMarker != null)
            {
                var launchMarkerSize = Mathf.Max(0.14f, config.ball.radius * 2.2f);
                var sr = launchMarker.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = PrototypeVisualFactory.CircleSprite;
                    sr.color = new Color(0.97f, 0.97f, 1f, 0.65f);
                    sr.sortingOrder = 25;
                }
                launchMarker.localScale = Vector3.one * launchMarkerSize;
            }

            // Walls 鈥?containers exist in scene, bricks are built at runtime
            if (wallTopRoot == null) wallTopRoot = boardRoot?.Find("WallTop");
            if (wallLeftRoot == null) wallLeftRoot = boardRoot?.Find("WallLeft");
            if (wallRightRoot == null) wallRightRoot = boardRoot?.Find("WallRight");
            CreateBrickWall(wallTopRoot, "WallTop", ArenaSurfaceType.Top, 0);
            CreateBrickWall(wallLeftRoot, "WallLeft", ArenaSurfaceType.Left, 3);
            CreateBrickWall(wallRightRoot, "WallRight", ArenaSurfaceType.Right, 6);
            RebuildWallAimPoints();

            // Bottom Line
            if (bottomLineObject == null)
            {
                var t = boardRoot?.Find("BottomLine");
                if (t != null) bottomLineObject = t.gameObject;
            }
            if (bottomLineObject != null)
            {
                var sr = bottomLineObject.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = PrototypeVisualFactory.SquareSprite;
                    sr.color = new Color(0.93f, 0.66f, 0.18f, 0.36f);
                    sr.sortingOrder = 7;
                }
                bottomLineObject.transform.localScale = new Vector3(BoardRect.width, 0.14f, 1f);
                bottomLineObject.transform.position = new Vector3(BoardRect.center.x, BoardRect.yMin + 0.02f, 0f);

                var bottomTrigger = bottomLineObject.GetComponent<BoxCollider2D>();
                if (bottomTrigger == null) bottomTrigger = bottomLineObject.AddComponent<BoxCollider2D>();
                bottomTrigger.isTrigger = true;
                bottomTrigger.size = new Vector2(BoardRect.width, config.arena.bottomTriggerHeight);

                var marker = bottomLineObject.GetComponent<ArenaSurfaceMarker>();
                if (marker == null) marker = bottomLineObject.AddComponent<ArenaSurfaceMarker>();
                marker.surfaceType = ArenaSurfaceType.Bottom;
            }
        }

        public bool TryGetWallSnap(ArenaSurfaceType surfaceType, Vector2 rawBallCenter, out Vector2 snappedBallCenter, out Vector2 wallNormal)
        {
            snappedBallCenter = rawBallCenter;
            wallNormal = Vector2.zero;
            var radius = config.ball.radius;
            switch (surfaceType)
            {
                case ArenaSurfaceType.Top:
                    snappedBallCenter.x = SnapToWallAnchor(rawBallCenter.x, BoardRect.xMin, BoardRect.xMax, GetWallAnchorSpacing(surfaceType));
                    snappedBallCenter.y = BoardRect.yMax - radius;
                    wallNormal = Vector2.down;
                    return true;
                case ArenaSurfaceType.Left:
                    snappedBallCenter.x = BoardRect.xMin + radius;
                    snappedBallCenter.y = SnapToWallAnchor(rawBallCenter.y, BoardRect.yMin, BoardRect.yMax, GetWallAnchorSpacing(surfaceType));
                    wallNormal = Vector2.right;
                    return true;
                case ArenaSurfaceType.Right:
                    snappedBallCenter.x = BoardRect.xMax - radius;
                    snappedBallCenter.y = SnapToWallAnchor(rawBallCenter.y, BoardRect.yMin, BoardRect.yMax, GetWallAnchorSpacing(surfaceType));
                    wallNormal = Vector2.left;
                    return true;
            }

            return false;
        }

        void CreateBrickWall(Transform root, string objectName, ArenaSurfaceType surfaceType, int patternOffset)
        {
            if (root == null) return;

            // Clear any existing brick children (for re-init)
            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);

            var thickness = config.arena.wallThickness;
            var visualGap = Mathf.Clamp(config.arena.wallStoneVisualGap, 0f, thickness * 0.5f);
            var colliderOverlap = Mathf.Clamp(config.arena.wallStoneColliderOverlap, 0f, 0.12f);
            var count = GetWallAnchorCount(surfaceType);
            var spacing = GetWallAnchorSpacing(surfaceType);
            var start = surfaceType == ArenaSurfaceType.Top ? BoardRect.xMin : BoardRect.yMin;

            for (var index = 0; index < count; index++)
            {
                var anchor = start + spacing * (index + 0.5f);
                var position = surfaceType switch
                {
                    ArenaSurfaceType.Top => new Vector2(anchor, BoardRect.yMax + thickness * 0.5f),
                    ArenaSurfaceType.Left => new Vector2(BoardRect.xMin - thickness * 0.5f, anchor),
                    ArenaSurfaceType.Right => new Vector2(BoardRect.xMax + thickness * 0.5f, anchor),
                    _ => Vector2.zero
                };
                var colliderSize = surfaceType == ArenaSurfaceType.Top ? new Vector2(spacing + colliderOverlap, thickness) : new Vector2(thickness, spacing + colliderOverlap);
                var visualSize = surfaceType == ArenaSurfaceType.Top ? new Vector2(Mathf.Max(0.12f, spacing - visualGap), thickness * 0.86f) : new Vector2(thickness * 0.86f, Mathf.Max(0.12f, spacing - visualGap));
                CreateWallBrick(root, $"{objectName}_{index:00}", position, colliderSize, visualSize, surfaceType, index + patternOffset);
            }
        }

        void CreateWallBrick(Transform parent, string objectName, Vector2 position, Vector2 colliderSize, Vector2 visualSize, ArenaSurfaceType surfaceType, int patternIndex)
        {
            var wall = new GameObject(objectName);
            wall.transform.SetParent(parent, false);
            wall.transform.position = position;

            var collider = wall.AddComponent<BoxCollider2D>();
            collider.sharedMaterial = bounceMaterial;
            collider.size = colliderSize;
            var marker = wall.AddComponent<ArenaSurfaceMarker>();
            marker.surfaceType = surfaceType;

            var colorVariance = Mathf.Clamp(config.arena.wallStoneColorVariance, 0f, 0.3f);
            var shade = GetSignedPatternValue(WallStoneShadePattern, patternIndex);
            var stoneColor = config.arena.wallColor;
            if (shade > 0f)
                stoneColor = Color.Lerp(stoneColor, Color.white, Mathf.Clamp01(shade * colorVariance * 2.2f));
            else if (shade < 0f)
                stoneColor = Color.Lerp(stoneColor, Color.black, Mathf.Clamp01(-shade * colorVariance * 1.8f));

            var stoneVisual = PrototypeVisualFactory.CreateSpriteObject("Visual", wall.transform, PrototypeVisualFactory.SquareSprite, stoneColor, 8, visualSize);
            stoneVisual.transform.localPosition = Vector3.zero;
            var highlight = PrototypeVisualFactory.CreateSpriteObject("Highlight", wall.transform, PrototypeVisualFactory.SquareSprite, new Color(1f, 1f, 1f, 0.12f), 9, visualSize * 0.78f);
            highlight.transform.localPosition = surfaceType switch
            {
                ArenaSurfaceType.Top => new Vector3(0f, visualSize.y * 0.08f, 0f),
                ArenaSurfaceType.Left => new Vector3(visualSize.x * 0.08f, 0f, 0f),
                ArenaSurfaceType.Right => new Vector3(-visualSize.x * 0.08f, 0f, 0f),
                _ => Vector3.zero
            };
        }

        int GetWallAnchorCount(ArenaSurfaceType surfaceType)
        {
            var span = surfaceType == ArenaSurfaceType.Top ? BoardRect.width : BoardRect.height;
            var unitLength = Mathf.Max(config.arena.wallStoneUnitLength, config.ball.radius * 2.6f);
            var baseCount = Mathf.Max(3, Mathf.RoundToInt(span / unitLength));
            return baseCount * Mathf.Max(1, config.arena.wallPointSubdivisions);
        }

        float GetWallAnchorSpacing(ArenaSurfaceType surfaceType)
        {
            var span = surfaceType == ArenaSurfaceType.Top ? BoardRect.width : BoardRect.height;
            return span / GetWallAnchorCount(surfaceType);
        }

        void RebuildWallAimPoints()
        {
            wallAimPoints.Clear();
            AddWallAimPoints(ArenaSurfaceType.Top);
            AddWallAimPoints(ArenaSurfaceType.Left);
            AddWallAimPoints(ArenaSurfaceType.Right);
        }

        void AddWallAimPoints(ArenaSurfaceType wallSide)
        {
            var count = GetWallAnchorCount(wallSide);
            var spacing = GetWallAnchorSpacing(wallSide);
            var start = wallSide == ArenaSurfaceType.Top ? BoardRect.xMin : BoardRect.yMin;
            var radius = config.ball.radius;

            for (var index = 0; index < count; index++)
            {
                var anchor = start + spacing * (index + 0.5f);
                wallAimPoints.Add(new WallAimPoint
                {
                    id = $"{wallSide}_{index:000}",
                    position = wallSide switch
                    {
                        ArenaSurfaceType.Top => new Vector2(anchor, BoardRect.yMax - radius),
                        ArenaSurfaceType.Left => new Vector2(BoardRect.xMin + radius, anchor),
                        ArenaSurfaceType.Right => new Vector2(BoardRect.xMax - radius, anchor),
                        _ => Vector2.zero
                    },
                    wallSide = wallSide,
                    normal = wallSide switch
                    {
                        ArenaSurfaceType.Top => Vector2.down,
                        ArenaSurfaceType.Left => Vector2.right,
                        ArenaSurfaceType.Right => Vector2.left,
                        _ => Vector2.zero
                    },
                    priority = index
                });
            }
        }

        static float SnapToWallAnchor(float value, float min, float max, float spacing)
        {
            if (spacing <= 0.001f)
                return Mathf.Clamp(value, min, max);

            var firstAnchor = min + spacing * 0.5f;
            var anchorCount = Mathf.Max(1, Mathf.RoundToInt((max - min) / spacing));
            var index = Mathf.RoundToInt((value - firstAnchor) / spacing);
            index = Mathf.Clamp(index, 0, anchorCount - 1);
            return Mathf.Clamp(firstAnchor + index * spacing, min, max);
        }

        static float GetSignedPatternValue(float[] pattern, int index)
        {
            if (pattern == null || pattern.Length == 0)
                return 0f;

            return Mathf.Clamp(pattern[Mathf.Abs(index) % pattern.Length], -1f, 1f);
        }

        public void TryLaunchBall(Vector2 direction, TrajectoryPreviewResult preview = null)
        {
            battleFlowController?.TryLaunchBall(direction, preview);
        }

        internal void TryLaunchBallCore(Vector2 direction, TrajectoryPreviewResult preview = null)
        {
            if (State != RoundState.Aim || direction.sqrMagnitude <= 0.001f || RemainingLaunchesForEnemy <= 0)
                return;

            preview ??= trajectoryPredictor?.BuildPreview(CurrentLaunchPoint, direction, config.ball.previewSegments, config.ball.previewDistance);
            RemainingLaunchesForEnemy = Mathf.Max(0, RemainingLaunchesForEnemy - 1);
            RefreshLaunchCounter();
            roundController.BeginRound();
            ChangeState(RoundState.BallFlying);
            RefreshPendingDamagePreview();
            ballController.Launch(direction, config.ball.speed, preview);
        }

        public void OnBallReturned(Vector2 landingPoint)
        {
            battleFlowController?.OnBallReturned(landingPoint);
        }

        internal void OnBallReturnedCore(Vector2 landingPoint)
        {
            if (State != RoundState.BallFlying)
                return;

            var enemyDisplayBefore = CurrentEnemy != null ? CurrentEnemy.CurrentHp : 0;
            var enemyMaxHp = CurrentEnemy != null ? CurrentEnemy.MaxHp : 0;
            var playerDisplayBefore = Player != null ? Player.CurrentHp : 0;
            var playerMaxHp = Player != null ? Player.MaxHp : 0;
            ChangeState(RoundState.RoundResolve);
            var result = roundController.ResolveRound(landingPoint);
            result.enemyDisplayHpBeforeHit = enemyDisplayBefore;
            result.enemyDisplayHpAfterHit = CurrentEnemy != null ? CurrentEnemy.CurrentHp : 0;
            result.playerDisplayHpBeforeCounter = playerDisplayBefore;
            result.playerDisplayHpAfterCounter = Player != null ? Player.CurrentHp : 0;
            enemyController?.SetHpSnapshot(result.enemyDisplayHpBeforeHit, enemyMaxHp);
            playerPresenter?.SetHpSnapshot(result.playerDisplayHpBeforeCounter, playerMaxHp);
            UpdateLaunchMarker();
            if (battlePresentationRoutine != null)
                StopCoroutine(battlePresentationRoutine);
            battlePresentationRoutine = StartCoroutine(PlayResolvePresentation(result));
        }

        void HandleEnemyDefeated()
        {
            gameSessionController?.HandleEnemyDefeated();
        }

        internal void HandleEnemyDefeatedCore()
        {
            if (CurrentEnemy != null)
            {
                var rewardGold = Mathf.RoundToInt(CurrentEnemy.RewardGold * modManager.GetRewardGoldMultiplier());
                Player.AddGold(rewardGold);
                Player.RestoreToFullHealth();
            }

            playerPresenter?.Refresh(Player);
            Player.RegisterKillAndTryLevelUp();
            BeginBlockRewardDraft(false);
        }

        void BeginBlockRewardDraft(bool initialDraft)
        {
            initialBlockDraftPending = initialDraft;
            boardManager.GenerateRewardOptions(Player.TotalKills, initialDraft ? config.blockRewards.initialChoiceCount : config.blockRewards.rewardChoiceCount);
            IntermissionMessage = initialDraft
                ? "在第一场战斗开始前，先选择一张起始方块。"
                : !boardManager.CanAcceptRewardBlock
                    ? "上阵和仓库都已满。请先跳过本次方块奖励，之后再到商店或整理阶段处理。"
                    : boardManager.RewardWillGoToReserve
                        ? "上阵已满，选中的方块会被送入仓库。"
                        : "击败敌人后，可选择一张方块加入上阵，或直接跳过。";
            ChangeState(RoundState.BlockRewardChoose);
        }

        void CompleteInitialDraft()
        {
            initialBlockDraftPending = false;
            IntermissionMessage = string.Empty;
            SpawnEnemy(enemyEncounterIndex);
            PrepareNextRound();
        }

        void EnterStickerRewardPhase()
        {
            rewardChoiceController.GenerateChoices();
            IntermissionMessage = string.Empty;
            ChangeState(RoundState.RewardChoose);
        }

        void PrepareNextRound()
        {
            boardManager.EnsureAtLeastOneActive();
            boardManager.AdvanceBlockProgression();
            boardManager.ShuffleBlocks(CurrentLaunchPoint);
            ballController.PlaceAt(CurrentLaunchPoint);
            ResetBattleActorPositions();
            playerPresenter?.Refresh(Player);
            enemyController?.Refresh();
            RefreshLaunchCounter();
            UpdateLaunchMarker();
            IntermissionMessage = string.Empty;
            ChangeState(RoundState.Aim);
        }

        public void ContinueToNextEnemy()
        {
            enemyEncounterIndex += 1;
            SpawnEnemy(enemyEncounterIndex);
            PrepareNextRound();
        }

        void SpawnEnemy(int index)
        {
            CurrentEnemy = BuildEnemyForIndex(index);
            RemainingLaunchesForEnemy = MaxLaunchesPerEnemy;
            RefreshLaunchCounter();
            enemyController.gameObject.SetActive(true);
            ResetBattleActorPositions();
            enemyController.SetEnemy(CurrentEnemy);
        }

        EnemyData BuildEnemyForIndex(int index)
        {
            var templates = config.enemies.templates;
            var clampedIndex = Mathf.Clamp(index, 0, Mathf.Max(0, templates.Count - 1));
            var template = templates[clampedIndex];
            var overflow = Mathf.Max(0, index - (templates.Count - 1));
            var hp = template.maxHp + overflow * config.enemies.endlessHpGrowth;
            var rewardGold = template.rewardGold + overflow * config.enemies.endlessGoldGrowth;
            var rewardHeal = template.rewardHeal + overflow * config.enemies.endlessHealGrowth;
            var attackDamage = template.attackDamage + overflow * config.enemies.endlessAttackGrowth;
            var baseName = GetCleanEnemyName(clampedIndex, template.displayName);
            var name = overflow > 0 ? $"{baseName}+{overflow}" : baseName;
            return new EnemyData(name, hp, rewardGold, rewardHeal, attackDamage, template.color);
        }

                static string GetCleanEnemyName(int index, string fallbackName)
        {
            return index switch
            {
                0 => "荆棘神像",
                1 => "铁信使",
                2 => "尖刺图腾",
                3 => "战祭司",
                4 => "深渊领主",
                _ => string.IsNullOrWhiteSpace(fallbackName) ? "敌人" : fallbackName
            };
        }

        void UpdateLaunchMarker()
        {
            if (launchMarker != null)
                launchMarker.position = CurrentLaunchPoint;
        }

        public void TrySelectBlockReward(int index)
        {
            if (State != RoundState.BlockRewardChoose)
                return;

            QueueIntermissionAction(IntermissionActionKind.SelectBlockReward, index);
        }

        void ExecuteSelectBlockReward(int index)
        {
            if (State != RoundState.BlockRewardChoose)
                return;

            if (!boardManager.TryClaimRewardOption(index, out _, out var addedToReserve, out var failReason))
            {
                IntermissionMessage = failReason;
                return;
            }

            IntermissionMessage = addedToReserve ? "新方块已加入仓库。" : "新方块已加入上阵。";

            if (initialBlockDraftPending)
                CompleteInitialDraft();
            else
                QueueIntermissionAction(IntermissionActionKind.EnterStickerRewardPhase);
        }

        public void SkipBlockReward()
        {
            if (State != RoundState.BlockRewardChoose || initialBlockDraftPending)
                return;

            QueueIntermissionAction(IntermissionActionKind.SkipBlockReward);
        }

        void ExecuteSkipBlockReward()
        {
            if (State != RoundState.BlockRewardChoose || initialBlockDraftPending)
                return;

            boardManager.ClearRewardOptions();
            QueueIntermissionAction(IntermissionActionKind.EnterStickerRewardPhase);
        }

        public void TrySelectReward(int index)
        {
            if (State != RoundState.RewardChoose)
                return;

            QueueIntermissionAction(IntermissionActionKind.SelectReward, index);
        }

        void ExecuteSelectReward(int index)
        {
            if (State != RoundState.RewardChoose)
                return;

            if (!rewardChoiceController.TrySelectChoice(index))
                return;

            QueueIntermissionAction(IntermissionActionKind.OpenShop);
        }

        public void TryRerollRewardChoices()
        {
            if (State == RoundState.RewardChoose)
                QueueIntermissionAction(IntermissionActionKind.RerollRewardChoices);
        }

        void ExecuteRerollRewardChoices()
        {
            if (State == RoundState.RewardChoose && rewardChoiceController.TryRerollChoices())
                IntermissionMessage = rewardChoiceController.LastStatusMessage;
        }

        public void SkipRewardChoices()
        {
            if (State != RoundState.RewardChoose)
                return;

            QueueIntermissionAction(IntermissionActionKind.SkipRewardChoices);
        }

        void ExecuteSkipRewardChoices()
        {
            if (State != RoundState.RewardChoose)
                return;

            rewardChoiceController.SkipChoices();
            IntermissionMessage = rewardChoiceController.LastStatusMessage;
            QueueIntermissionAction(IntermissionActionKind.OpenShop);
        }

        void EnterShop()
        {
            shopManager.OpenShop();
            IntermissionMessage = string.Empty;
            ChangeState(RoundState.Shop);
        }

        public void TryBuyShopItem(int index)
        {
            if (State != RoundState.Shop)
                return;

            shopManager.TryBuy(index);
            IntermissionMessage = shopManager.LastFeedback;
        }

        public void TryRerollShop()
        {
            if (State != RoundState.Shop)
                return;

            shopManager.TryReroll();
            IntermissionMessage = shopManager.LastFeedback;
        }

        public void TryRemoveBlockInShop(string cardId)
        {
            if (State != RoundState.Shop)
                return;

            shopManager.TryRemoveBlock(cardId);
            IntermissionMessage = shopManager.LastFeedback;
        }

        public void TrySwapActiveReserve(string activeCardId, string reserveCardId)
        {
            if (!CanManageBlockAssignments)
                return;

            if (boardManager.TrySwapActiveAndReserve(activeCardId, reserveCardId, out var failReason))
                IntermissionMessage = "已交换上阵和仓库中的方块。";
            else
                IntermissionMessage = failReason;
        }

        public void CloseShop()
        {
            if (State != RoundState.Shop)
                return;

            QueueIntermissionAction(IntermissionActionKind.CloseShop);
        }

        void ExecuteCloseShop()
        {
            if (State != RoundState.Shop)
                return;

            shopManager.CloseShop();
            ChangeState(RoundState.LoadoutManage);
        }

        public bool BeginStickerDrag(string runtimeId)
        {
            return stickerInventory.BeginDrag(runtimeId);
        }

        public void CancelStickerDrag()
        {
            stickerInventory.CancelDrag();
        }

        public void SetIntermissionMessage(string message)
        {
            IntermissionMessage = message ?? string.Empty;
        }

        public bool TryInstallDraggedSticker(string cardId, int socketIndex, out string failReason)
        {
            failReason = string.Empty;
            var dragging = stickerInventory.DraggingSticker;
            if (dragging == null)
            {
                failReason = "No sticker is currently selected.";
                return false;
            }

            if (!boardManager.TryInstallSticker(cardId, socketIndex, dragging, out failReason))
                return false;

            stickerInventory.TakeDraggingSticker();
            return true;
        }

        public void RemoveStickerFromCard(string cardId, int socketIndex)
        {
            var removed = boardManager.RemoveSticker(cardId, socketIndex);
            if (removed != null)
                stickerInventory.ReturnToInventory(removed);
        }

        public void ToggleModActivation(string runtimeId)
        {
            modManager.ToggleActivation(runtimeId);
        }

        public void ApplyGrowthReward(GrowthRewardData rewardData)
        {
            if (rewardData == null)
                return;

            switch (rewardData.rewardType)
            {
                case GrowthRewardType.UnlockSocket:
                    boardManager.UnlockRandomSocket();
                    break;
                case GrowthRewardType.IncreaseInventoryCapacity:
                    Player.IncreaseInventoryCapacity(rewardData.value);
                    break;
                case GrowthRewardType.IncreaseLaunchCapacity:
                    Player.IncreaseLaunchCapacity(rewardData.value);
                    RemainingLaunchesForEnemy = Mathf.Max(RemainingLaunchesForEnemy, MaxLaunchesPerEnemy);
                    RefreshLaunchCounter();
                    break;
            }
        }

        public void FinishLoadout()
        {
            if (State == RoundState.LoadoutManage)
                QueueIntermissionAction(IntermissionActionKind.FinishLoadout);
        }

        public void DebugShuffleBoard()
        {
            if (State != RoundState.GameOver)
            {
                boardManager.ShuffleBlocks(CurrentLaunchPoint);
                UpdateLaunchMarker();
            }
        }

        public void DebugAddGold(int amount)
        {
            Player.AddGold(amount);
        }

        public void ToggleAimMode()
        {
            config.aim.currentAimMode = config.aim.currentAimMode == InputAimMode.PCMouseAimClick ? InputAimMode.MobileDragConfirm : InputAimMode.PCMouseAimClick;
            if (State == RoundState.Aim)
                launcher?.CancelAim();
        }

        public void ExecuteHudCommand(HudCommand command)
        {
            switch (command.Type)
            {
                case HudCommandType.ToggleAimMode:
                    ToggleAimMode();
                    break;
                case HudCommandType.DebugShuffleBoard:
                    DebugShuffleBoard();
                    break;
                case HudCommandType.DebugAddGold:
                    DebugAddGold(command.IntValue);
                    break;
                case HudCommandType.DebugKillEnemy:
                    DebugKillEnemy();
                    break;
                case HudCommandType.DebugDamagePlayer:
                    DebugDamagePlayer(command.IntValue);
                    break;
                case HudCommandType.TrySelectBlockReward:
                    TrySelectBlockReward(command.IntValue);
                    break;
                case HudCommandType.SkipBlockReward:
                    SkipBlockReward();
                    break;
                case HudCommandType.TrySelectReward:
                    TrySelectReward(command.IntValue);
                    break;
                case HudCommandType.TryRerollRewardChoices:
                    TryRerollRewardChoices();
                    break;
                case HudCommandType.SkipRewardChoices:
                    SkipRewardChoices();
                    break;
                case HudCommandType.TryBuyShopItem:
                    TryBuyShopItem(command.IntValue);
                    break;
                case HudCommandType.TryRerollShop:
                    TryRerollShop();
                    break;
                case HudCommandType.CloseShop:
                    CloseShop();
                    break;
                case HudCommandType.FinishLoadout:
                    FinishLoadout();
                    break;
                case HudCommandType.BeginStickerDrag:
                    BeginStickerDrag(command.PrimaryId);
                    break;
                case HudCommandType.CancelStickerDrag:
                    CancelStickerDrag();
                    break;
                case HudCommandType.ToggleModActivation:
                    ToggleModActivation(command.PrimaryId);
                    break;
                case HudCommandType.TryRemoveBlockInShop:
                    TryRemoveBlockInShop(command.PrimaryId);
                    break;
                case HudCommandType.TrySwapActiveReserve:
                    TrySwapActiveReserve(command.PrimaryId, command.SecondaryId);
                    break;
                case HudCommandType.TryInstallDraggedSticker:
                    if (TryInstallDraggedSticker(command.PrimaryId, command.IntValue, out var failReason))
                        SetIntermissionMessage("Sticker installed.");
                    else
                        SetIntermissionMessage(failReason);
                    break;
                case HudCommandType.RemoveStickerFromCard:
                    RemoveStickerFromCard(command.PrimaryId, command.IntValue);
                    break;
            }
        }

        public void DebugKillEnemy()
        {
            if (CurrentEnemy == null || State == RoundState.GameOver || State == RoundState.BlockRewardChoose || State == RoundState.RewardChoose || State == RoundState.Shop || State == RoundState.LoadoutManage)
                return;

            CurrentEnemy.ApplyDamage(CurrentEnemy.CurrentHp);
            enemyController.PlayHitFeedback(true);
            enemyController.Refresh();
            HandleEnemyDefeated();
        }

        public void DebugDamagePlayer(int amount)
        {
            if (State == RoundState.GameOver)
                return;

            Player.ApplyDamage(amount);
            playerPresenter?.Refresh(Player);
            playerPresenter?.PlayHitFeedback(amount >= 18);
            if (Player.IsDead)
            TriggerGameOver("生命归零，本局结束。");
        }

        public void TriggerGameOver(string reason = null)
        {
            GameOverMessage = string.IsNullOrWhiteSpace(reason) ? "本局结束。" : reason;
            ClearPendingIntermissionAction();
            if (battlePresentationRoutine != null)
            {
                StopCoroutine(battlePresentationRoutine);
                battlePresentationRoutine = null;
            }
            isBattlePresentationPlaying = false;
            ChangeState(RoundState.GameOver);
            ballController?.StopImmediately();
        }

        IEnumerator PlayResolvePresentation(RoundResolveResult result)
        {
            isBattlePresentationPlaying = true;
            if (result.attackDamage > 0)
            {
                yield return PlayAttackLeap(playerPresenter != null ? playerPresenter.transform : null, playerIdlePosition, enemyController != null ? enemyController.transform.position + new Vector3(0f, 1.18f, 0f) : enemyIdlePosition, new Color(0.35f, 0.92f, 1f, 1f), () =>
                {
                    enemyController?.Refresh();
                    enemyController?.PlayHitFeedback(result.enemyDefeated);
                });
            }
            else
            {
                enemyController?.SetHpSnapshot(result.enemyDisplayHpAfterHit, CurrentEnemy != null ? CurrentEnemy.MaxHp : Mathf.Max(1, result.enemyDisplayHpAfterHit));
            }

            if (!result.enemyDefeated && CurrentEnemy != null && result.enemyCounterDamage > 0)
            {
                yield return new WaitForSeconds(0.06f);
                yield return PlayAttackLeap(enemyController != null ? enemyController.transform : null, enemyIdlePosition, playerPresenter != null ? playerPresenter.transform.position + new Vector3(0f, 1.04f, 0f) : playerIdlePosition, CurrentEnemy.AccentColor, () =>
                {
                    playerPresenter?.Refresh(Player);
                    playerPresenter?.PlayHitFeedback(result.playerDefeated || result.enemyCounterDamage >= 18);
                });
            }
            else if (result.enemyCounterDamage <= 0)
            {
                playerPresenter?.SetHpSnapshot(result.playerDisplayHpAfterCounter, Player != null ? Player.MaxHp : Mathf.Max(1, result.playerDisplayHpAfterCounter));
            }

            isBattlePresentationPlaying = false;
            battlePresentationRoutine = null;
            CompleteResolvePresentation(result);
        }

        void CompleteResolvePresentation(RoundResolveResult result)
        {
            if (result.playerDefeated)
            {
                TriggerGameOver("生命归零，本局结束。");
                return;
            }

            if (result.enemyDefeated)
            {
                HandleEnemyDefeated();
                return;
            }

            var interest = modManager.GetInterestIncome(Player.Gold);
            if (interest > 0)
                Player.AddGold(interest);

            playerPresenter?.Refresh(Player);
            if (RemainingLaunchesForEnemy <= 0)
            {
                TriggerGameOver("该敌人的发射次数已经耗尽。");
                return;
            }

            PrepareNextRound();
        }

        IEnumerator PlayAttackLeap(Transform actor, Vector3 startWorldPosition, Vector3 impactWorldPosition, Color impactColor, Action onImpact)
        {
            if (actor == null)
                yield break;

            const float leapOutDuration = 0.16f;
            const float returnDuration = 0.18f;
            const float arcHeight = 0.78f;
            var startScale = actor.localScale;
            var targetScale = startScale * 1.08f;

            for (var t = 0f; t < 1f; t += Time.deltaTime / leapOutDuration)
            {
                var lerpT = Mathf.Clamp01(t);
                actor.position = Vector3.Lerp(startWorldPosition, impactWorldPosition, lerpT) + Vector3.up * Mathf.Sin(lerpT * Mathf.PI) * arcHeight;
                actor.localScale = Vector3.Lerp(startScale, targetScale, Mathf.Sin(lerpT * Mathf.PI));
                yield return null;
            }

            actor.position = impactWorldPosition;
            actor.localScale = targetScale;
            onImpact?.Invoke();
            yield return StartCoroutine(PlayImpactBurst(impactWorldPosition, impactColor));

            for (var t = 0f; t < 1f; t += Time.deltaTime / returnDuration)
            {
                var lerpT = Mathf.Clamp01(t);
                actor.position = Vector3.Lerp(impactWorldPosition, startWorldPosition, lerpT) + Vector3.up * Mathf.Sin((1f - lerpT) * Mathf.PI) * (arcHeight * 0.55f);
                actor.localScale = Vector3.Lerp(targetScale, startScale, lerpT);
                yield return null;
            }

            actor.position = startWorldPosition;
            actor.localScale = startScale;
        }

        IEnumerator PlayImpactBurst(Vector3 position, Color color)
        {
            if (battleEffectsRoot == null)
                yield break;

            var ringObject = PrototypeVisualFactory.CreateSpriteObject("ImpactBurst", battleEffectsRoot, PrototypeVisualFactory.CircleSprite, color, 22, Vector2.one * 0.18f);
            ringObject.transform.position = position;
            var renderer = ringObject.GetComponent<SpriteRenderer>();
            const float duration = 0.14f;
            for (var t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                var lerpT = Mathf.Clamp01(t);
                var alpha = 1f - lerpT;
                renderer.color = new Color(color.r, color.g, color.b, alpha);
                ringObject.transform.localScale = Vector3.one * Mathf.Lerp(0.18f, 0.92f, lerpT);
                yield return null;
            }

            Destroy(ringObject);
        }

        void ResetBattleActorPositions()
        {
            if (playerPresenter != null)
            {
                playerPresenter.transform.position = playerIdlePosition;
                playerPresenter.transform.localScale = Vector3.one;
            }

            if (enemyController != null)
            {
                enemyController.transform.position = enemyIdlePosition;
                enemyController.transform.localScale = Vector3.one;
            }
        }

        void RefreshLaunchCounter()
        {
            ballController?.SetLaunchCounter(RemainingLaunchesForEnemy, MaxLaunchesPerEnemy);
        }

        void QueueIntermissionAction(IntermissionActionKind actionKind, int index = -1)
        {
            if (pendingIntermissionAction != IntermissionActionKind.None)
                return;

            pendingIntermissionAction = actionKind;
            pendingIntermissionIndex = index;
        }

        void ClearPendingIntermissionAction()
        {
            pendingIntermissionAction = IntermissionActionKind.None;
            pendingIntermissionIndex = -1;
        }

        void ProcessPendingIntermissionAction()
        {
            intermissionFlowController?.ProcessPendingAction();
        }

        internal void ProcessPendingIntermissionActionCore()
        {
            if (pendingIntermissionAction == IntermissionActionKind.None)
                return;

            var action = pendingIntermissionAction;
            var index = pendingIntermissionIndex;
            ClearPendingIntermissionAction();

            switch (action)
            {
                case IntermissionActionKind.SelectBlockReward:
                    ExecuteSelectBlockReward(index);
                    break;
                case IntermissionActionKind.SkipBlockReward:
                    ExecuteSkipBlockReward();
                    break;
                case IntermissionActionKind.EnterStickerRewardPhase:
                    EnterStickerRewardPhase();
                    break;
                case IntermissionActionKind.SelectReward:
                    ExecuteSelectReward(index);
                    break;
                case IntermissionActionKind.RerollRewardChoices:
                    ExecuteRerollRewardChoices();
                    break;
                case IntermissionActionKind.SkipRewardChoices:
                    ExecuteSkipRewardChoices();
                    break;
                case IntermissionActionKind.OpenShop:
                    EnterShop();
                    break;
                case IntermissionActionKind.CloseShop:
                    ExecuteCloseShop();
                    break;
                case IntermissionActionKind.FinishLoadout:
                    if (State == RoundState.LoadoutManage)
                    {
                        boardManager.EnsureAtLeastOneActive();
                        ContinueToNextEnemy();
                    }
                    break;
            }
        }

        void ConfigurePhaseStateMachine()
        {
            phaseStateMachine = new GamePhaseStateMachine();
            phaseStateMachine.Register(new SimpleGamePhaseState(RoundState.Aim, true));
            phaseStateMachine.Register(new SimpleGamePhaseState(RoundState.BallFlying, false));
            phaseStateMachine.Register(new SimpleGamePhaseState(RoundState.RoundResolve, false));
            phaseStateMachine.Register(new SimpleGamePhaseState(RoundState.BlockRewardChoose, false));
            phaseStateMachine.Register(new SimpleGamePhaseState(RoundState.RewardChoose, false));
            phaseStateMachine.Register(new SimpleGamePhaseState(RoundState.Shop, false));
            phaseStateMachine.Register(new SimpleGamePhaseState(RoundState.LoadoutManage, false));
            phaseStateMachine.Register(new SimpleGamePhaseState(RoundState.GameOver, false));
        }

        void ChangeState(RoundState newState)
        {
            var previousState = State;
            phaseStateMachine?.Change(newState);
            State = newState;
            if (previousState == RoundState.Aim && newState != RoundState.Aim)
            {
                if (launcher != null)
                    launcher.CancelAim();
                else
                    ClearAimPreview();
            }

            ballController?.SetLaunchCounterVisible(newState == RoundState.Aim);
            if (newState == RoundState.BallFlying)
            {
                damageCounterView?.Show();
                damageCounterView?.SetValue(roundController != null ? roundController.PendingDamage : 0, false);
            }
            else
            {
                damageCounterView?.Hide();
            }
        }
    }

    public class DamageCounterView : MonoBehaviour
    {
        const float PunchDuration = 0.22f;

        PopHeroGame game;
        GUIStyle panelStyle;
        GUIStyle titleStyle;
        GUIStyle valueStyle;
        Texture2D panelTexture;
        bool isVisible;
        int currentValue;
        float punchTimer;
        float punchStrength;

        public void Initialize(PopHeroGame owner)
        {
            game = owner;
            ResetCounter();
        }

        public void Show()
        {
            isVisible = true;
        }

        public void Hide()
        {
            isVisible = false;
            punchTimer = 0f;
        }

        public void ResetCounter()
        {
            currentValue = 0;
            isVisible = false;
            punchTimer = 0f;
            punchStrength = 0f;
        }

        public void SetValue(int value, bool animate = true)
        {
            value = Mathf.Max(0, value);
            var delta = Mathf.Abs(value - currentValue);
            currentValue = value;
            if (!animate || delta <= 0)
                return;

            punchTimer = PunchDuration;
            punchStrength = Mathf.Clamp(0.12f + delta / 80f, 0.14f, 0.34f);
        }

        void Update()
        {
            if (punchTimer > 0f)
                punchTimer = Mathf.Max(0f, punchTimer - Time.deltaTime);
        }

        void OnGUI()
        {
            if (!isVisible || game == null || game.State != RoundState.BallFlying)
                return;

            EnsureStyles();
            var panelRect = new Rect(24f, Screen.height * 0.33f, 220f, 138f);
            var pivot = new Vector2(panelRect.x + panelRect.width * 0.5f, panelRect.y + panelRect.height * 0.5f);
            var scale = 1f;
            if (punchTimer > 0f)
            {
                var t = 1f - punchTimer / PunchDuration;
                scale += Mathf.Sin(t * Mathf.PI) * punchStrength;
            }

            var oldMatrix = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), pivot);
            GUI.Box(panelRect, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panelRect.x, panelRect.y + 16f, panelRect.width, 28f), "\u4f24\u5bb3", titleStyle);
            GUI.Label(new Rect(panelRect.x, panelRect.y + 44f, panelRect.width, 72f), currentValue.ToString(), valueStyle);
            GUI.matrix = oldMatrix;
        }

        void EnsureStyles()
        {
            if (panelStyle != null)
                return;

            panelTexture = new Texture2D(1, 1);
            panelTexture.SetPixel(0, 0, new Color(0.09f, 0.11f, 0.16f, 0.84f));
            panelTexture.Apply();

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(14, 14, 12, 12),
                normal = { background = panelTexture, textColor = Color.white }
            };
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.94f, 0.72f, 1f) }
            };
            valueStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 46,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }
    }
}


