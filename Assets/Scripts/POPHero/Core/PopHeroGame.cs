using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace POPHero
{
    public class PopHeroGame : MonoBehaviour, IGameReadModel, IHudCommandSink
    {
        static readonly float[] WallStoneShadePattern = { -0.22f, 0.1f, -0.08f, 0.16f, -0.14f, 0.05f, 0.12f, -0.04f };
        static readonly Color NeutralEnemyImpactColor = new(1f, 0.96f, 0.9f, 1f);
        const int AttackForegroundSortingOffset = 20;

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
            FinishLoadout,
            CompleteMapNode
        }

        public PopHeroPrototypeConfig config;
        public PopHeroPrototypeConfig Config => config;
        [SerializeField] PopHeroTableConfig tableConfig;
        public ConfigTableService Tables { get; private set; }

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
        public EnemyEncounterState CurrentEnemyEncounter { get; private set; }
        public IReadOnlyList<EnemyEncounterState> CurrentEnemyEncounters => currentEnemyEncounters;
        public EnemyEncounterGroupState CurrentEnemyGroup { get; private set; }
        EnemyEncounterState IGameReadModel.CurrentEnemyEncounter => CurrentEnemyEncounter;
        public Rect BoardRect { get; private set; }
        public Rect PlayAreaRect => new(BoardRect.xMin, CurrentBottomBoundaryY, BoardRect.width, BoardRect.yMax - CurrentBottomBoundaryY);
        public float CurrentBottomBoundaryY => GetBottomBoundaryY(CurrentLaunchPoint.y);
        public float LaunchY => CurrentLaunchPoint.y;
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
        public EnemyController EnemyPresenter => GetEnemyPresenter(CurrentEnemyEncounter);
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
        public IBlockOperationService BlockOperations => blockOperationService;
        public IRunMapService RunMap => runMapManager;
        public int EncounterIndex => enemyEncounterIndex + 1;
        public int MaxLaunchesPerEnemy => Mathf.Max(1, config.enemies.maxLaunchesPerEnemy + (Player?.BonusLaunchesPerEnemy ?? 0));
        public InputAimMode CurrentAimMode => config.aim.currentAimMode;
        public bool IsInitialBlockDraftPending => initialBlockDraftPending;
        public bool IsSettingsOpen { get; private set; }
        public float RunElapsedSeconds => runElapsedSeconds;
        public bool CanManageBlockAssignments => !IsSettingsOpen && State == RoundState.BlockOperations;
        public bool CanManageStickerLoadout => !IsSettingsOpen && State == RoundState.LoadoutManage;
        public string AimModeDisplayText => CurrentAimMode == InputAimMode.PCMouseAimClick ? "移动鼠标瞄准，左键发射" : "拖动瞄准，再点一次发射";
        public string CurrentAimModeLabel => CurrentAimMode == InputAimMode.PCMouseAimClick ? "移动鼠标瞄准，左键发射" : "拖动瞄准，再点一次发射";
        public Vector2 CurrentLaunchPoint => roundController != null ? roundController.LaunchPosition : initialLaunchPoint;
        public IReadOnlyList<WallAimPoint> WallAimPoints => wallAimPoints;

        PlayerLauncher launcher;
        BallController ballController;
        TrajectoryPredictor trajectoryPredictor;
        BoardManager boardManager;
        RoundController roundController;
        EnemyController enemyController;
        EnemyController supportEnemyController;
        PlayerPresenter playerPresenter;
        PopHeroHud hud;
        CanvasHudController canvasHud;
        DamageCounterView damageCounterView;
        PhysicsMaterial2D bounceMaterial;
        Transform battleStageRoot;
        Transform battleEffectsRoot;
        StickerCatalog stickerCatalog;
        StickerInventory stickerInventory;
        StickerEffectRunner stickerEffectRunner;
        RewardChoiceController rewardChoiceController;
        ModManager modManager;
        ShopManager shopManager;
        BlockOperationManager blockOperationManager;
        RunMapManager runMapManager;
        ICombatEventHub combatEventHub;
        IBounceStepSolver bounceStepSolver;
        IBlockCollectionService blockCollectionService;
        IBlockRewardService blockRewardService;
        IRuntimeBoardService runtimeBoardService;
        IModService modService;
        IShopService shopService;
        IBlockOperationService blockOperationService;
        GameRuntimeContext runtimeContext;
        EncounterDirector encounterDirector;
        HudCommandDispatcher hudCommandDispatcher;
        GamePhaseStateMachine phaseStateMachine;
        GameSessionController gameSessionController;
        BattleFlowController battleFlowController;
        BattlePresentationController battlePresentationController;
        IntermissionFlowController intermissionFlowController;
        MapFlowController mapFlowController;
        readonly List<WallAimPoint> wallAimPoints = new();
        readonly List<RaycastResult> uiRaycastResults = new();
        readonly List<EnemyEncounterState> currentEnemyEncounters = new(2);
        int enemyEncounterIndex;
        bool initialBlockDraftPending;
        IntermissionActionKind pendingIntermissionAction;
        int pendingIntermissionIndex = -1;
        bool isBattlePresentationPlaying;
        Vector3 playerIdlePosition;
        Vector3 enemyMeleeAnchor;
        Vector3 enemyAttackImpactPosition;
        Vector3 enemySupportOriginPosition;
        Vector3 enemyRangedImpactPosition;
        float preferredEnemyStepDistanceWorld = 1.62f;
        float enemySpawnXLimit;
        BoardBlock hoveredWorldTooltipBlock;
        float runElapsedSeconds;
        float timeScaleBeforeSettings = 1f;
        bool suppressAimInputAfterUi;
        int suppressAimInputReleaseFrame = int.MaxValue;
        Vector2 initialLaunchPoint;
        bool loadoutReturnsToMap;
        bool debugBattleReturnActive;
        bool debugShopReturnActive;
        bool debugBlockOperationsReturnActive;
        RoundState debugBattleReturnState = RoundState.Map;
        RoundState debugShopReturnState = RoundState.Map;
        RoundState debugBlockOperationsReturnState = RoundState.Map;

        void Awake()
        {
            config = Resources.Load<PopHeroPrototypeConfig>("PopHeroPrototypeConfig") ?? PopHeroPrototypeConfig.CreateRuntimeDefault();
            if (ConfigTableCsvRuntimeLoader.TryLoadFromProjectCsv(out var csvTables, out var csvFolder, out var csvError))
            {
                tableConfig = csvTables;
                Debug.Log($"[POPHero] Loaded gameplay tables directly from CSV: {csvFolder}");
            }
            else
            {
                tableConfig = Resources.Load<PopHeroTableConfig>("POPHeroTableConfig");
                if (Application.isEditor && !string.IsNullOrWhiteSpace(csvError))
                    Debug.LogWarning($"[POPHero] Failed to load CSV tables directly, falling back to runtime asset. Reason: {csvError}");
            }

            Tables = new ConfigTableService(tableConfig, config);
            if (tableConfig == null || !tableConfig.HasGameplayTables)
                Debug.LogError("[POPHero] POPHeroTableConfig is missing or empty. Run POPHero/Config/Rebuild Tables to generate runtime table data.");
            else
                Tables.ApplyToPrototypeConfig(config);

            config.aim ??= new AimSettings();
            CacheArenaRect();
            SetupCamera();
            BuildPrototype();
            StartPrototype();
        }

        bool IsBattlePresentationPlaying => battlePresentationController?.IsPlaying ?? isBattlePresentationPlaying;

        public bool CanSimulate()
        {
            return !IsSettingsOpen && !suppressAimInputAfterUi && State != RoundState.GameOver && !IsBattlePresentationPlaying;
        }

        public bool IsLaunchPointerAllowed(Vector2 screenPosition, int pointerId = -1)
        {
            if (!CanSimulate() || State != RoundState.Aim)
                return false;

            var eventSystem = EventSystem.current;
            if (eventSystem != null && IsScreenPositionOverUi(eventSystem, screenPosition, pointerId))
                return false;

            var camera = Camera.main;
            if (camera == null)
                return false;

            var worldPoint = camera.ScreenToWorldPoint(screenPosition);
            const float padding = 0.15f;
            var playArea = PlayAreaRect;
            var launchRect = new Rect(
                playArea.xMin - padding,
                playArea.yMin - padding,
                playArea.width + padding * 2f,
                playArea.height + padding * 2f);
            return launchRect.Contains(new Vector2(worldPoint.x, worldPoint.y));
        }

        bool IsScreenPositionOverUi(EventSystem eventSystem, Vector2 screenPosition, int pointerId)
        {
            uiRaycastResults.Clear();
            var eventData = new PointerEventData(eventSystem)
            {
                position = screenPosition,
                pointerId = pointerId
            };
            eventSystem.RaycastAll(eventData, uiRaycastResults);
            return uiRaycastResults.Count > 0;
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
            supportEnemyController?.ClearPreviewDamage();
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
            initialLaunchPoint = new Vector2(BoardRect.center.x, BoardRect.yMin + config.arena.launchLineOffset);
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
            BindBall();
            ResolveInitialLaunchPointFromScene();
            BindBoard();

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

            stickerCatalog = new StickerCatalog(tableConfig);
            stickerInventory = new StickerInventory();
            stickerEffectRunner = new StickerEffectRunner();
            rewardChoiceController = new RewardChoiceController();
            modManager = new ModManager();
            shopManager = new ShopManager();
            blockOperationManager = new BlockOperationManager();
            runMapManager = new RunMapManager();
            combatEventHub = new CombatEventHub();

            stickerInventory.Initialize(this);
            stickerEffectRunner.Initialize(this);
            rewardChoiceController.Initialize(this);
            modManager.Initialize(this);
            shopManager.Initialize(this);
            blockOperationManager.Initialize(this);
            runMapManager.Initialize(this);

            blockCollectionService = new BlockCollectionServiceFacade(boardManager);
            blockRewardService = new BlockRewardServiceFacade(boardManager);
            runtimeBoardService = new RuntimeBoardServiceFacade(boardManager);
            modService = new ModServiceFacade(modManager);
            shopService = new ShopServiceFacade(shopManager);
            blockOperationService = blockOperationManager;
            runtimeContext = new GameRuntimeContext();
            UpdateRuntimeContext();
            encounterDirector = new EncounterDirector(runtimeContext);
            hudCommandDispatcher = new HudCommandDispatcher(this);
            ConfigurePhaseStateMachine();
            gameSessionController = new GameSessionController(this);
            battleFlowController = new BattleFlowController(this);
            battlePresentationController = new BattlePresentationController(this, PlayResolvePresentation, ClearAttackForegroundSorting);
            intermissionFlowController = new IntermissionFlowController(this);
            mapFlowController = new MapFlowController(this);

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

        void UpdateRuntimeContext()
        {
            runtimeContext ??= new GameRuntimeContext();
            runtimeContext.Config = config;
            runtimeContext.Tables = Tables;
            runtimeContext.Player = Player;
            runtimeContext.Board = boardManager;
            runtimeContext.Round = roundController;
            runtimeContext.StickerCatalog = stickerCatalog;
            runtimeContext.StickerInventory = stickerInventory;
            runtimeContext.StickerEffectRunner = stickerEffectRunner;
            runtimeContext.RewardChoices = rewardChoiceController;
            runtimeContext.Mods = modManager;
            runtimeContext.Shop = shopManager;
            runtimeContext.CombatEvents = combatEventHub;
        }

        void Update()
        {
            UpdateRunTimer();
            UpdateUiInputSuppression();
            if (!IsSettingsOpen)
                intermissionFlowController?.ProcessPendingAction();
            RefreshWorldBlockTooltip();
        }

        void OnDisable()
        {
            RestoreTimeScaleAfterSettings();
        }

        void OnDestroy()
        {
            RestoreTimeScaleAfterSettings();
        }

        void StartPrototype()
        {
            gameSessionController?.StartSession();
        }

        internal void StartPrototypeCore()
        {
            Player = new PlayerData(config.player.maxHp, config.player.currentHp, config.player.startShield, config.player.startGold);
            UpdateRuntimeContext();
            Player.IncreaseInventoryCapacity(config.stickers.baseInventoryCapacity - Player.StickerInventoryCapacity);
            roundController.Initialize(this, initialLaunchPoint);
            boardManager.ResetBlockProgression();
            ballController.PlaceAt(CurrentLaunchPoint);
            enemyEncounterIndex = 0;
            encounterDirector?.Reset();
            CurrentEnemyGroup = null;
            CurrentEnemy = null;
            CurrentEnemyEncounter = null;
            currentEnemyEncounters.Clear();
            initialBlockDraftPending = false;
            GameOverMessage = "本局结束。";
            IntermissionMessage = string.Empty;
            runElapsedSeconds = 0f;
            loadoutReturnsToMap = false;
            ClearDebugReturnState();
            ClearPendingIntermissionAction();
            blockOperationManager?.Close();
            runMapManager?.GenerateNewMap();
            damageCounterView?.ResetCounter();
            isBattlePresentationPlaying = false;
            if (enemyController != null)
                enemyController.gameObject.SetActive(false);
            if (supportEnemyController != null)
                supportEnemyController.gameObject.SetActive(false);
            playerPresenter?.Refresh(Player);
            ResetBattleActorPositions();
            RefreshLaunchGeometry();
            if (!boardManager.GrantStartingCard(BoardBlockType.AttackAdd, BlockRarity.White, out _, out var failReason))
                throw new InvalidOperationException($"[POPHero] Failed to grant starting block: {failReason}");

            IntermissionMessage = runMapManager?.LastFeedback ?? "选择一个地图节点开始路线。";
            ChangeState(RoundState.Map);
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
                if (enemyPanel.sprite == null)
                    enemyPanel.sprite = PrototypeVisualFactory.SquareSprite;
                enemyPanel.sortingOrder = 1;
                enemyPanel.transform.localScale = new Vector3(BoardRect.width, config.arena.topPanelHeight, 1f);
            }

            if (battleStageRef == null) battleStageRef = enemyLayerRoot?.Find("BattleStage");
            battleStageRoot = battleStageRef;
            if (battleEffectsRef == null) battleEffectsRef = enemyLayerRoot?.Find("BattleEffects");
            battleEffectsRoot = battleEffectsRef;

            playerIdlePosition = panelCenter + new Vector2(-BoardRect.width * 0.28f, -0.16f);
            enemyMeleeAnchor = playerIdlePosition + new Vector3(1.92f, 0.06f, 0f);
            enemyAttackImpactPosition = playerIdlePosition + new Vector3(1.0f, 1.04f, 0f);
            enemySupportOriginPosition = panelCenter + new Vector2(BoardRect.width * 0.34f, 0.62f);
            enemyRangedImpactPosition = playerIdlePosition + new Vector3(0.18f, 1.28f, 0f);
            enemySpawnXLimit = panelCenter.x + BoardRect.width * 0.36f;

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
                enemyController.transform.position = GetEnemyDefaultPosition(EnemyEncounterSlot.Primary);
                enemyController.Initialize(this);
            }

            supportEnemyController = EnsureSupportEnemyController();
            if (supportEnemyController != null)
            {
                supportEnemyController.transform.position = GetEnemyDefaultPosition(EnemyEncounterSlot.Support);
                supportEnemyController.Initialize(this);
                supportEnemyController.gameObject.SetActive(false);
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

        void ResolveInitialLaunchPointFromScene()
        {
            if (ballController == null)
                return;

            initialLaunchPoint = ballController.transform.position;
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

                var bottomTrigger = bottomLineObject.GetComponent<BoxCollider2D>();
                if (bottomTrigger == null) bottomTrigger = bottomLineObject.AddComponent<BoxCollider2D>();
                bottomTrigger.isTrigger = true;
                bottomTrigger.size = new Vector2(BoardRect.width, config.arena.bottomTriggerHeight);

                var marker = bottomLineObject.GetComponent<ArenaSurfaceMarker>();
                if (marker == null) marker = bottomLineObject.AddComponent<ArenaSurfaceMarker>();
                marker.surfaceType = ArenaSurfaceType.Bottom;
            }

            ApplyLaunchGeometryVisuals();
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
            if (IsSettingsOpen || suppressAimInputAfterUi || State != RoundState.Aim || direction.sqrMagnitude <= 0.001f || RemainingLaunchesForEnemy <= 0)
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

            var playerMaxHp = Player != null ? Player.MaxHp : 0;
            ChangeState(RoundState.RoundResolve);
            var result = roundController.ResolveRound(landingPoint);
            ApplyEnemySnapshots(result, true);
            playerPresenter?.SetHpSnapshot(result.playerDisplayHpBeforeCounter, playerMaxHp);
            RefreshLaunchGeometry();
            battlePresentationController ??= new BattlePresentationController(this, PlayResolvePresentation, ClearAttackForegroundSorting);
            battlePresentationController.Play(result);
        }

        void HandleEnemyDefeated()
        {
            gameSessionController?.HandleEnemyDefeated();
        }

        internal void HandleEnemyDefeatedCore()
        {
            var clearRewards = encounterDirector != null
                ? encounterDirector.BuildClearRewardSummary()
                : new EncounterClearRewardSummary(0, 0, 0);
            var defeatedBoss = !debugBattleReturnActive && runMapManager?.CurrentNode?.kind == MapNodeKind.Boss;

            if (clearRewards.RewardGold > 0)
                Player.AddGold(Mathf.RoundToInt(clearRewards.RewardGold * modManager.GetRewardGoldMultiplier()));

            if (defeatedBoss)
                MapHealingRules.ApplyHeal(Player);

            playerPresenter?.Refresh(Player);
            for (var killIndex = 0; killIndex < clearRewards.DefeatedEnemyCount; killIndex++)
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
            RefreshEnemyTargetSelection();
            RefreshEnemyPresenters();
            ResetBattleActorPositions();
            playerPresenter?.Refresh(Player);
            RefreshLaunchCounter();
            RefreshLaunchGeometry();
            IntermissionMessage = string.Empty;
            ChangeState(RoundState.Aim);
        }

        public void ContinueToNextEnemy()
        {
            enemyEncounterIndex += 1;
            SpawnEnemy(enemyEncounterIndex);
            PrepareNextRound();
        }

        public void SelectMapNode(string nodeId)
        {
            mapFlowController?.SelectNode(nodeId);
        }

        internal void SelectMapNodeCore(string nodeId)
        {
            if (State != RoundState.Map)
                return;

            if (runMapManager == null)
            {
                IntermissionMessage = "地图系统不可用。";
                return;
            }

            if (!runMapManager.TrySelectNode(nodeId, out var node, out var failReason))
            {
                IntermissionMessage = failReason;
                return;
            }

            IntermissionMessage = runMapManager.LastFeedback;
            switch (node.kind)
            {
                case MapNodeKind.Battle:
                case MapNodeKind.Boss:
                    enemyEncounterIndex = Mathf.Max(0, node.enemyIndex);
                    SpawnEnemy(enemyEncounterIndex);
                    PrepareNextRound();
                    break;
                case MapNodeKind.Shop:
                    EnterShop();
                    break;
                case MapNodeKind.Workbench:
                    OpenBlockOperations("map_workbench", RoundState.Map);
                    break;
                case MapNodeKind.Rest:
                    CompleteCurrentMapNodeAndReturnCore(ApplyMapHeal(MapHealingRules.DefaultHealPercent, "休息点"));
                    break;
                case MapNodeKind.Event:
                    ChangeState(RoundState.MapEvent);
                    break;
            }
        }

        public void ChooseMapEventOption(int index)
        {
            mapFlowController?.ChooseEventOption(index);
        }

        internal void ChooseMapEventOptionCore(int index)
        {
            if (State != RoundState.MapEvent || runMapManager?.CurrentNode == null)
                return;

            if (!TryFindCurrentMapEventChoice(index, out var choice))
            {
                IntermissionMessage = "无效的事件选项。";
                return;
            }

            ExecuteMapEventChoice(choice, true);
        }

        void ExecuteMapEventChoice(MapEventChoiceState choice, bool completeCurrentMapNode)
        {
            if (choice == null)
            {
                IntermissionMessage = "无效的事件选项。";
                return;
            }

            switch (choice.actionType)
            {
                case MapEventActionType.GainGold:
                    var gold = Mathf.Max(0, choice.intValue);
                    Player.AddGold(gold);
                    playerPresenter?.Refresh(Player);
                    FinishMapEventChoice(gold > 0 ? $"旧货箱里有 {gold} 金币。" : "旧货箱里什么也没有。", completeCurrentMapNode);
                    break;
                case MapEventActionType.TakeDamageUnlockSocket:
                    var damage = Mathf.Max(0, choice.intValue);
                    Player.ApplyDamage(damage);
                    boardManager.UnlockRandomSocket();
                    playerPresenter?.Refresh(Player);
                    if (Player.IsDead)
                        TriggerGameOver("训练过度，生命归零，本局结束。");
                    else
                        FinishMapEventChoice($"训练造成 {damage} 点伤害，但一个方块槽位被打开了。", completeCurrentMapNode);
                    break;
                case MapEventActionType.OpenWorkbench:
                    OpenMapEventWorkbench(choice, completeCurrentMapNode);
                    break;
                case MapEventActionType.Heal:
                    FinishMapEventChoice(ApplyMapHeal(choice.healPercent, choice.title), completeCurrentMapNode);
                    break;
                default:
                    IntermissionMessage = "无效的事件选项。";
                    break;
            }
        }

        void FinishMapEventChoice(string message, bool completeCurrentMapNode)
        {
            if (completeCurrentMapNode)
            {
                CompleteCurrentMapNodeAndReturnCore(message);
                return;
            }

            IntermissionMessage = string.IsNullOrWhiteSpace(message) ? "GM 调试事件已执行。" : $"GM 调试：{message}";
        }

        void OpenMapEventWorkbench(MapEventChoiceState choice, bool completeCurrentMapNode)
        {
            var profileId = string.IsNullOrWhiteSpace(choice.profileId) ? "map_workbench" : choice.profileId;
            if (completeCurrentMapNode)
            {
                OpenBlockOperations(profileId, RoundState.Map);
                return;
            }

            OpenDebugBlockOperations(profileId, "临时工坊");
        }

        bool TryFindCurrentMapEventChoice(int index, out MapEventChoiceState choice)
        {
            choice = null;
            var choices = runMapManager?.CurrentEventChoices;
            if (choices == null)
                return false;

            for (var choiceIndex = 0; choiceIndex < choices.Count; choiceIndex++)
            {
                if (choices[choiceIndex].index == index)
                {
                    choice = choices[choiceIndex];
                    return true;
                }
            }

            return false;
        }

        string ApplyMapHeal(float healPercent, string sourceName)
        {
            var label = string.IsNullOrWhiteSpace(sourceName) ? "治疗" : sourceName;
            var healed = MapHealingRules.ApplyHeal(Player, healPercent);
            playerPresenter?.Refresh(Player);

            if (Player == null || Player.IsDead)
                return $"{label}无法治疗已经倒下的玩家。";
            return healed > 0
                ? $"{label}恢复了 {healed} 点生命。"
                : $"{label}没有恢复生命，当前生命已满。";
        }

        internal void CompleteCurrentMapNodeAndReturnCore(string overrideFeedback = null)
        {
            if (runMapManager == null)
                return;

            if (!runMapManager.TryCompleteCurrentNode(out var completedBoss, out var failReason))
            {
                IntermissionMessage = failReason;
                ChangeState(RoundState.Map);
                return;
            }

            IntermissionMessage = string.IsNullOrWhiteSpace(overrideFeedback)
                ? runMapManager.LastFeedback
                : overrideFeedback;
            if (completedBoss)
            {
                TriggerGameOver("路线完成，Boss 已被击败。");
                return;
            }

            enemyController?.gameObject.SetActive(false);
            supportEnemyController?.gameObject.SetActive(false);
            encounterDirector?.Reset();
            CurrentEnemyGroup = null;
            CurrentEnemy = null;
            CurrentEnemyEncounter = null;
            currentEnemyEncounters.Clear();
            RemainingLaunchesForEnemy = 0;
            ballController?.StopImmediately();
            ChangeState(RoundState.Map);
        }

        void SpawnEnemy(int index)
        {
            UpdateRuntimeContext();
            CurrentEnemyGroup = encounterDirector != null
                ? encounterDirector.SpawnEncounter(index)
                : null;
            RefreshEnemyTargetSelection();
            RemainingLaunchesForEnemy = MaxLaunchesPerEnemy;
            RefreshLaunchCounter();
            RefreshEnemyPresenters();
            ResetBattleActorPositions();
        }

        internal void RefreshEnemyTargetSelection()
        {
            currentEnemyEncounters.Clear();
            CurrentEnemyEncounter = null;
            CurrentEnemy = null;

            if (encounterDirector == null)
                return;

            encounterDirector.RefreshTargetSelection();
            CurrentEnemyGroup = encounterDirector.CurrentEnemyGroup;
            var aliveEncounters = encounterDirector.CurrentEnemyEncounters;
            for (var index = 0; index < aliveEncounters.Count; index++)
                currentEnemyEncounters.Add(aliveEncounters[index]);

            CurrentEnemyEncounter = encounterDirector.CurrentEnemyEncounter;
            CurrentEnemy = encounterDirector.CurrentEnemy;
        }

        void RefreshEnemyPresenters()
        {
            RefreshEnemyPresenter(enemyController, CurrentEnemyGroup?.GetEncounter(EnemyEncounterSlot.Primary));
            RefreshEnemyPresenter(supportEnemyController, CurrentEnemyGroup?.GetEncounter(EnemyEncounterSlot.Support));
        }

        void RefreshEnemyPresenter(EnemyController presenter, EnemyEncounterState encounter)
        {
            if (presenter == null)
                return;

            var shouldShow = encounter != null && encounter.Enemy != null && encounter.IsAlive;
            presenter.gameObject.SetActive(shouldShow);
            if (!shouldShow)
                return;

            presenter.SetEncounter(encounter);
            presenter.SetIntentSuppressed(false);
        }

        EnemyController GetEnemyPresenter(EnemyEncounterState encounter)
        {
            return encounter == null ? null : GetEnemyPresenter(encounter.Slot);
        }

        EnemyController GetEnemyPresenter(EnemyEncounterSlot slot)
        {
            return slot == EnemyEncounterSlot.Support ? supportEnemyController : enemyController;
        }

        Vector3 GetEnemyWorldPosition(EnemyEncounterState encounter)
        {
            if (encounter == null)
                return GetEnemyDefaultPosition(EnemyEncounterSlot.Primary);

            return encounter.BehaviorType == EnemyBehaviorType.FlyingRangedOrigin
                ? enemySupportOriginPosition
                : GetEnemyApproachPosition(encounter.DistanceStepsRemaining, encounter.StartingDistanceSteps);
        }

        Vector3 GetEnemyDefaultPosition(EnemyEncounterSlot slot)
        {
            return slot == EnemyEncounterSlot.Support
                ? enemySupportOriginPosition
                : new Vector3(enemySpawnXLimit, enemyMeleeAnchor.y, enemyMeleeAnchor.z);
        }

        Vector3 GetEnemyApproachPosition(int distanceStepsRemaining, int startingDistanceSteps)
        {
            var clampedDistance = Mathf.Max(0, distanceStepsRemaining);
            var maxDistance = Mathf.Max(0, startingDistanceSteps);
            if (maxDistance <= 0 || clampedDistance <= 0)
                return enemyMeleeAnchor;

            return enemyMeleeAnchor + Vector3.right * GetEnemyStepDistanceWorld(maxDistance) * clampedDistance;
        }

        float GetEnemyStepDistanceWorld(int startingDistanceSteps)
        {
            var clampedSteps = Mathf.Max(1, startingDistanceSteps);
            var maxTravelDistance = Mathf.Max(0f, enemySpawnXLimit - enemyMeleeAnchor.x);
            return Mathf.Min(preferredEnemyStepDistanceWorld, maxTravelDistance / clampedSteps);
        }

        EnemyController EnsureSupportEnemyController()
        {
            if (battleStageRoot == null || enemyController == null)
                return null;

            var existing = battleStageRoot.Find("EnemySupport");
            if (existing != null && existing.TryGetComponent(out EnemyController existingController))
                return existingController;

            var clone = Instantiate(enemyController.gameObject, enemyController.transform.parent);
            clone.name = "EnemySupport";
            return clone.GetComponent<EnemyController>();
        }

        void RefreshLaunchGeometry()
        {
            ApplyLaunchGeometryVisuals();
        }

        void ApplyLaunchGeometryVisuals()
        {
            var launchPoint = CurrentLaunchPoint;
            if (launchGuide != null)
                launchGuide.transform.position = new Vector3(BoardRect.center.x, launchPoint.y - 0.15f, 0f);

            if (bottomLineObject == null)
                return;

            bottomLineObject.transform.position = new Vector3(BoardRect.center.x, GetBottomTriggerCenterY(launchPoint.y), 0f);
        }

        float GetBottomBoundaryY(float launchY)
        {
            return launchY - GetLaunchBallRadius() - GetBottomBoundaryClearance();
        }

        float GetBottomTriggerCenterY(float launchY)
        {
            return GetBottomBoundaryY(launchY) - Mathf.Max(0.02f, config.arena.bottomTriggerHeight) * 0.5f;
        }

        float GetLaunchBallRadius()
        {
            if (ballController != null)
                return Mathf.Max(0.01f, ballController.BallRadiusWorld);

            return Mathf.Max(0.01f, config.ball.radius);
        }

        float GetBottomBoundaryClearance()
        {
            return Mathf.Max(0.03f, config.ball.previewHitEpsilon * 2f);
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

            QueueIntermissionAction(IntermissionActionKind.CompleteMapNode);
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
            QueueIntermissionAction(IntermissionActionKind.CompleteMapNode);
        }

        void EnterShop()
        {
            shopManager.OpenShop();
            IntermissionMessage = string.Empty;
            ChangeState(RoundState.Shop);
        }

        public void OpenBlockOperations(string profileId, RoundState returnState)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                IntermissionMessage = "当前没有配置可用的方块操作。";
                return;
            }

            if (!blockOperationManager.TryOpen(profileId, returnState, out var failReason))
            {
                IntermissionMessage = failReason;
                return;
            }

            IntermissionMessage = blockOperationManager.Session.lastFeedback;
            ChangeState(RoundState.BlockOperations);
        }

        public void CloseBlockOperations()
        {
            if (State != RoundState.BlockOperations)
                return;

            var returnState = blockOperationManager.Session.returnState;
            IntermissionMessage = blockOperationManager.Session.lastFeedback;
            blockOperationManager.Close();
            if (debugBlockOperationsReturnActive)
            {
                var debugReturnState = debugBlockOperationsReturnState;
                debugBlockOperationsReturnActive = false;
                debugBlockOperationsReturnState = RoundState.Map;
                ChangeState(debugReturnState);
                return;
            }

            if (returnState == RoundState.Map)
            {
                CompleteCurrentMapNodeAndReturnCore();
                return;
            }

            ChangeState(returnState);
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

        public void TryRemoveBlock(string cardId)
        {
            if (State != RoundState.BlockOperations)
                return;

            if (blockOperationManager.TryRemoveBlock(cardId, out var failReason))
                IntermissionMessage = blockOperationManager.Session.lastFeedback;
            else
                IntermissionMessage = string.IsNullOrWhiteSpace(failReason) ? blockOperationManager.Session.lastFeedback : failReason;
        }

        public void TrySwapActiveReserve(string activeCardId, string reserveCardId)
        {
            if (State != RoundState.BlockOperations)
                return;

            if (blockOperationManager.TrySwapActiveReserve(activeCardId, reserveCardId, out var failReason))
                IntermissionMessage = blockOperationManager.Session.lastFeedback;
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
            if (debugShopReturnActive)
            {
                var debugReturnState = debugShopReturnState;
                debugShopReturnActive = false;
                debugShopReturnState = RoundState.Map;
                IntermissionMessage = "GM 调试商店已关闭。";
                ChangeState(debugReturnState);
                return;
            }

            loadoutReturnsToMap = runMapManager?.CurrentNode?.kind == MapNodeKind.Shop;
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
                RefreshLaunchGeometry();
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

        public void OpenSettings()
        {
            if (IsSettingsOpen)
                return;

            IsSettingsOpen = true;
            timeScaleBeforeSettings = Mathf.Approximately(Time.timeScale, 0f) ? 1f : Time.timeScale;
            Time.timeScale = 0f;
            launcher?.CancelAim();
            ClearAimPreview();
            canvasHud?.ClearTooltip();
            canvasHud?.ClearPassiveTooltip();
            canvasHud?.RefreshNow();
        }

        void UpdateRunTimer()
        {
            if (IsSettingsOpen || State == RoundState.GameOver)
                return;

            runElapsedSeconds += Time.unscaledDeltaTime;
        }

        public void CloseSettings()
        {
            if (!IsSettingsOpen)
                return;

            RestoreTimeScaleAfterSettings();
            launcher?.CancelAim();
            ClearAimPreview();
            BeginUiInputSuppression();
            canvasHud?.RefreshNow();
        }

        public void BackToMenu()
        {
            CloseSettings();
            SceneFlowService.Instance.LoadMainMenu();
        }

        public void QuitGame()
        {
            CloseSettings();
            SceneFlowService.Instance.QuitGame();
        }

        void RestoreTimeScaleAfterSettings()
        {
            if (!IsSettingsOpen)
                return;

            Time.timeScale = Mathf.Approximately(timeScaleBeforeSettings, 0f) ? 1f : timeScaleBeforeSettings;
            IsSettingsOpen = false;
        }

        void BeginUiInputSuppression()
        {
            suppressAimInputAfterUi = true;
            suppressAimInputReleaseFrame = int.MaxValue;
        }

        void UpdateUiInputSuppression()
        {
            if (!suppressAimInputAfterUi)
                return;

            var pointerActive = Input.GetMouseButton(0) || Input.GetMouseButtonUp(0) || Input.touchCount > 0;
            if (pointerActive)
            {
                suppressAimInputReleaseFrame = int.MaxValue;
                return;
            }

            if (suppressAimInputReleaseFrame == int.MaxValue)
            {
                suppressAimInputReleaseFrame = Time.frameCount + 1;
                return;
            }

            if (Time.frameCount < suppressAimInputReleaseFrame)
                return;

            suppressAimInputAfterUi = false;
            suppressAimInputReleaseFrame = int.MaxValue;
        }

        public void ExecuteHudCommand(HudCommand command)
        {
            hudCommandDispatcher ??= new HudCommandDispatcher(this);
            hudCommandDispatcher.Execute(command);
        }

        public void DebugKillEnemy()
        {
            if (CurrentEnemy == null || State == RoundState.GameOver || State == RoundState.Map || State == RoundState.MapEvent || State == RoundState.BlockRewardChoose || State == RoundState.RewardChoose || State == RoundState.Shop || State == RoundState.BlockOperations || State == RoundState.LoadoutManage)
                return;

            var targetEncounter = CurrentEnemyEncounter;
            var targetPresenter = GetEnemyPresenter(targetEncounter);
            CurrentEnemy.ApplyDamage(CurrentEnemy.CurrentHp);
            targetPresenter?.PlayHitFeedback(true);
            targetPresenter?.Refresh();
            RefreshEnemyTargetSelection();
            if (CurrentEnemyGroup == null || CurrentEnemyGroup.AllDefeated)
            {
                HandleEnemyDefeated();
                return;
            }

            PrepareNextRound();
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

        public void DebugTriggerMapNode(string kindKey)
        {
            if (!CanRunGmEventDebugTrigger())
                return;

            if (!Enum.TryParse(kindKey, true, out MapNodeKind kind))
            {
                IntermissionMessage = "GM 调试：未知地图节点。";
                canvasHud?.RefreshNow();
                return;
            }

            switch (kind)
            {
                case MapNodeKind.Battle:
                    StartDebugEncounter(Mathf.Max(0, enemyEncounterIndex), "普通战斗");
                    break;
                case MapNodeKind.Boss:
                    StartDebugEncounter(ResolveDebugBossEnemyIndex(), "Boss 战");
                    break;
                case MapNodeKind.Shop:
                    OpenDebugShop();
                    break;
                case MapNodeKind.Workbench:
                    OpenDebugBlockOperations("map_workbench", "工坊");
                    break;
                case MapNodeKind.Rest:
                    IntermissionMessage = $"GM 调试：{ApplyMapHeal(MapHealingRules.DefaultHealPercent, "休息点")}";
                    break;
                case MapNodeKind.Event:
                    IntermissionMessage = "GM 调试：事件节点请直接点击下方路线事件选项。";
                    break;
            }

            canvasHud?.RefreshNow();
        }

        public void DebugTriggerMapEventChoice(string actionKey)
        {
            if (!CanRunGmEventDebugTrigger())
                return;

            if (!Enum.TryParse(actionKey, true, out MapEventActionType actionType))
            {
                IntermissionMessage = "GM 调试：未知路线事件。";
                canvasHud?.RefreshNow();
                return;
            }

            var choices = RunMapManager.CreateDefaultEventChoices();
            for (var index = 0; index < choices.Count; index++)
            {
                if (choices[index].actionType != actionType)
                    continue;

                ExecuteMapEventChoice(choices[index], false);
                canvasHud?.RefreshNow();
                return;
            }

            IntermissionMessage = "GM 调试：没有找到对应路线事件。";
            canvasHud?.RefreshNow();
        }

        void StartDebugEncounter(int encounterIndex, string label)
        {
            ClearPendingIntermissionAction();
            ClearDebugReturnState();
            if (shopManager.InShop)
                shopManager.CloseShop();
            if (blockOperationManager.IsOpen)
                blockOperationManager.Close();

            debugBattleReturnActive = true;
            debugBattleReturnState = RoundState.Map;
            enemyEncounterIndex = Mathf.Max(0, encounterIndex);
            SpawnEnemy(enemyEncounterIndex);
            PrepareNextRound();
            IntermissionMessage = $"GM 调试：已启动{label}。";
        }

        bool CanRunGmEventDebugTrigger()
        {
            if (State == RoundState.GameOver)
                return false;

            if (State == RoundState.BallFlying || State == RoundState.RoundResolve || IsBattlePresentationPlaying)
            {
                IntermissionMessage = "GM 调试：当前正在飞行或结算，稍后再触发事件。";
                canvasHud?.RefreshNow();
                return false;
            }

            return true;
        }

        void OpenDebugShop()
        {
            if (State == RoundState.Shop)
            {
                IntermissionMessage = "GM 调试：商店已经打开。";
                return;
            }

            debugShopReturnState = GetSafeDebugReturnState();
            debugShopReturnActive = true;
            EnterShop();
            IntermissionMessage = "GM 调试：已打开商店。关闭后回到调试前状态。";
        }

        void OpenDebugBlockOperations(string profileId, string label)
        {
            if (State == RoundState.BlockOperations)
            {
                IntermissionMessage = "GM 调试：工坊已经打开。";
                return;
            }

            debugBlockOperationsReturnState = GetSafeDebugReturnState();
            debugBlockOperationsReturnActive = true;
            OpenBlockOperations(profileId, debugBlockOperationsReturnState);
            if (State == RoundState.BlockOperations)
                IntermissionMessage = $"GM 调试：已打开{label}。关闭后回到调试前状态。";
        }

        int ResolveDebugBossEnemyIndex()
        {
            var mapConfig = Tables?.GetRunMapConfig();
            var templateCount = Mathf.Max(1, config?.enemies?.templates?.Count ?? 1);
            if (mapConfig != null && mapConfig.bossEnemyIndex >= 0)
                return Mathf.Clamp(mapConfig.bossEnemyIndex, 0, templateCount - 1);

            return templateCount - 1;
        }

        RoundState GetSafeDebugReturnState()
        {
            return State switch
            {
                RoundState.GameOver => RoundState.Map,
                RoundState.BallFlying => RoundState.Aim,
                RoundState.RoundResolve => RoundState.Aim,
                _ => State
            };
        }

        void CompleteDebugBattleAndReturnCore()
        {
            debugBattleReturnActive = false;
            boardManager.ClearRewardOptions();
            enemyController?.gameObject.SetActive(false);
            supportEnemyController?.gameObject.SetActive(false);
            encounterDirector?.Reset();
            CurrentEnemyGroup = null;
            CurrentEnemy = null;
            CurrentEnemyEncounter = null;
            currentEnemyEncounters.Clear();
            RemainingLaunchesForEnemy = 0;
            ballController?.StopImmediately();
            RefreshLaunchCounter();
            IntermissionMessage = "GM 调试战斗结束，已返回地图。";
            ChangeState(debugBattleReturnState);
            debugBattleReturnState = RoundState.Map;
        }

        void ClearDebugReturnState()
        {
            debugBattleReturnActive = false;
            debugShopReturnActive = false;
            debugBlockOperationsReturnActive = false;
            debugBattleReturnState = RoundState.Map;
            debugShopReturnState = RoundState.Map;
            debugBlockOperationsReturnState = RoundState.Map;
        }

        public void TriggerGameOver(string reason = null)
        {
            GameOverMessage = string.IsNullOrWhiteSpace(reason) ? "本局结束。" : reason;
            ClearPendingIntermissionAction();
            ClearDebugReturnState();
            battlePresentationController?.Stop();
            isBattlePresentationPlaying = false;
            ClearAttackForegroundSorting();
            ChangeState(RoundState.GameOver);
            ballController?.StopImmediately();
        }

        IEnumerator PlayResolvePresentation(RoundResolveResult result)
        {
            isBattlePresentationPlaying = true;
            var playerMaxHp = Player != null ? Player.MaxHp : Mathf.Max(1, result.playerDisplayHpAfterCounter);
            var targetResult = result.FindEnemyResult(result.targetSlot);
            var targetPresenter = GetEnemyPresenter(result.targetSlot);
            if (result.attackDamage > 0 && targetResult.HasValue)
            {
                playerPresenter?.SetSortingOffset(AttackForegroundSortingOffset);
                var impactPosition = targetPresenter != null
                    ? targetPresenter.transform.position + new Vector3(0f, 1.18f, 0f)
                    : GetEnemyDefaultPosition(result.targetSlot) + new Vector3(0f, 1.18f, 0f);
                yield return PlayAttackLeap(playerPresenter != null ? playerPresenter.transform : null, playerIdlePosition, impactPosition, new Color(0.35f, 0.92f, 1f, 1f), () =>
                {
                    targetPresenter?.Refresh();
                    targetPresenter?.PlayHitFeedback(targetResult.Value.wasDefeated);
                });
                playerPresenter?.SetSortingOffset(0);
            }
            else if (targetResult.HasValue)
            {
                targetPresenter?.SetHpSnapshot(result.enemyDisplayHpAfterHit, Mathf.Max(1, targetResult.Value.maxHp));
            }

            if (result.enemyTurns != null && result.enemyTurns.Count > 0)
            {
                for (var turnIndex = 0; turnIndex < result.enemyTurns.Count; turnIndex++)
                {
                    var actingPresenter = GetEnemyPresenter(result.enemyTurns[turnIndex].Slot);
                    actingPresenter?.SetIntentSuppressed(true);
                }

                for (var turnIndex = 0; turnIndex < result.enemyTurns.Count; turnIndex++)
                {
                    var turn = result.enemyTurns[turnIndex];
                    var actingPresenter = GetEnemyPresenter(turn.Slot);
                    yield return new WaitForSeconds(0.06f);

                    switch (turn.ActionType)
                    {
                        case EnemyTurnActionType.Advance:
                            yield return PlayEnemyAdvance(
                                actingPresenter != null ? actingPresenter.transform : null,
                                GetEnemyTurnPosition(turn.Slot, turn.BehaviorType, turn.DistanceBefore),
                                GetEnemyTurnPosition(turn.Slot, turn.BehaviorType, turn.DistanceAfter));
                            playerPresenter?.SetHpSnapshot(turn.PlayerHpAfterAction, playerMaxHp);
                            break;

                        case EnemyTurnActionType.Attack:
                            if (turn.BehaviorType == EnemyBehaviorType.MeleeAdvance && turn.DidAdvance)
                            {
                                yield return PlayEnemyAdvance(
                                    actingPresenter != null ? actingPresenter.transform : null,
                                    GetEnemyTurnPosition(turn.Slot, turn.BehaviorType, turn.DistanceBefore),
                                    GetEnemyTurnPosition(turn.Slot, turn.BehaviorType, turn.DistanceAfter));
                            }

                            actingPresenter?.SetSortingOffset(AttackForegroundSortingOffset);
                            if (turn.IsRangedAttack)
                            {
                                yield return PlayEnemyRangedPulse(
                                    actingPresenter != null ? actingPresenter.transform : null,
                                    enemyRangedImpactPosition,
                                    NeutralEnemyImpactColor,
                                    () => ApplyPlayerTurnSnapshot(turn, result.playerDefeated, playerMaxHp));
                            }
                            else
                            {
                                yield return PlayAttackLeap(
                                    actingPresenter != null ? actingPresenter.transform : null,
                                    actingPresenter != null ? actingPresenter.transform.position : GetEnemyTurnPosition(turn.Slot, turn.BehaviorType, turn.DistanceAfter),
                                    enemyAttackImpactPosition,
                                    NeutralEnemyImpactColor,
                                    () => ApplyPlayerTurnSnapshot(turn, result.playerDefeated, playerMaxHp));
                            }

                            actingPresenter?.SetSortingOffset(0);
                            break;

                        default:
                            playerPresenter?.SetHpSnapshot(turn.PlayerHpAfterAction, playerMaxHp);
                            break;
                    }

                    actingPresenter?.SetIntentSuppressed(false);
                }
            }
            else
            {
                playerPresenter?.SetHpSnapshot(result.playerDisplayHpAfterCounter, playerMaxHp);
            }

            ApplyEnemySnapshots(result, false);

            isBattlePresentationPlaying = false;
            battlePresentationController?.MarkCompleted();
            CompleteResolvePresentation(result);
        }

        void CompleteResolvePresentation(RoundResolveResult result)
        {
            if (result.playerDefeated)
            {
                TriggerGameOver("生命归零，本局结束。");
                return;
            }

            if (result.encounterCleared)
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
                TriggerGameOver("该遭遇的发射次数已经耗尽。");
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

        IEnumerator PlayEnemyAdvance(Transform actor, Vector3 startWorldPosition, Vector3 endWorldPosition)
        {
            if (actor == null)
                yield break;

            const float duration = 0.18f;
            const float arcHeight = 0.18f;
            var startScale = actor.localScale;
            var targetScale = startScale * 1.04f;

            for (var t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                var lerpT = Mathf.Clamp01(t);
                actor.position = Vector3.Lerp(startWorldPosition, endWorldPosition, lerpT) + Vector3.up * Mathf.Sin(lerpT * Mathf.PI) * arcHeight;
                actor.localScale = Vector3.Lerp(startScale, targetScale, Mathf.Sin(lerpT * Mathf.PI));
                yield return null;
            }

            actor.position = endWorldPosition;
            actor.localScale = Vector3.one;
        }

        IEnumerator PlayEnemyRangedPulse(Transform actor, Vector3 impactWorldPosition, Color impactColor, Action onImpact)
        {
            if (actor == null)
                yield break;

            const float chargeDuration = 0.12f;
            const float releaseDuration = 0.1f;
            var startScale = actor.localScale;
            var chargedScale = startScale * 1.1f;

            for (var t = 0f; t < 1f; t += Time.deltaTime / chargeDuration)
            {
                var lerpT = Mathf.Clamp01(t);
                actor.localScale = Vector3.Lerp(startScale, chargedScale, lerpT);
                yield return null;
            }

            actor.localScale = chargedScale;
            yield return StartCoroutine(PlayImpactBurst(actor.position + new Vector3(0f, 0.35f, 0f), impactColor));
            onImpact?.Invoke();
            yield return StartCoroutine(PlayImpactBurst(impactWorldPosition, impactColor));

            for (var t = 0f; t < 1f; t += Time.deltaTime / releaseDuration)
            {
                var lerpT = Mathf.Clamp01(t);
                actor.localScale = Vector3.Lerp(chargedScale, startScale, lerpT);
                yield return null;
            }

            actor.localScale = startScale;
        }

        void ApplyPlayerTurnSnapshot(EnemyTurnOutcome turn, bool playerDefeated, int playerMaxHp)
        {
            playerPresenter?.SetHpSnapshot(turn.PlayerHpAfterAction, playerMaxHp);
            if (turn.DamageDealt > 0)
                playerPresenter?.PlayHitFeedback(playerDefeated || turn.DamageDealt >= 18);
        }

        void ApplyEnemySnapshots(RoundResolveResult result, bool useBeforeSnapshots)
        {
            if (result.enemyResults == null)
                return;

            for (var index = 0; index < result.enemyResults.Count; index++)
            {
                var enemyResult = result.enemyResults[index];
                var presenter = GetEnemyPresenter(enemyResult.slot);
                if (presenter == null || !presenter.gameObject.activeSelf)
                    continue;

                var displayHp = useBeforeSnapshots ? enemyResult.displayHpBefore : enemyResult.displayHpAfter;
                presenter.SetHpSnapshot(displayHp, Mathf.Max(1, enemyResult.maxHp));
            }
        }

        Vector3 GetEnemyTurnPosition(EnemyEncounterSlot slot, EnemyBehaviorType behaviorType, int distanceStepsRemaining)
        {
            if (behaviorType == EnemyBehaviorType.FlyingRangedOrigin)
                return enemySupportOriginPosition;

            var encounter = CurrentEnemyGroup?.GetEncounter(slot);
            var startingDistanceSteps = encounter != null ? encounter.StartingDistanceSteps : distanceStepsRemaining;
            return GetEnemyApproachPosition(distanceStepsRemaining, startingDistanceSteps);
        }

        void ResetEnemyPresenterPosition(EnemyController presenter, EnemyEncounterState encounter, EnemyEncounterSlot slot)
        {
            if (presenter == null)
                return;

            presenter.transform.position = encounter != null ? GetEnemyWorldPosition(encounter) : GetEnemyDefaultPosition(slot);
            presenter.transform.localScale = Vector3.one;
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

            ResetEnemyPresenterPosition(enemyController, CurrentEnemyGroup?.GetEncounter(EnemyEncounterSlot.Primary), EnemyEncounterSlot.Primary);
            ResetEnemyPresenterPosition(supportEnemyController, CurrentEnemyGroup?.GetEncounter(EnemyEncounterSlot.Support), EnemyEncounterSlot.Support);

            ClearAttackForegroundSorting();
        }

        void ClearAttackForegroundSorting()
        {
            playerPresenter?.SetSortingOffset(0);
            enemyController?.SetSortingOffset(0);
            supportEnemyController?.SetSortingOffset(0);
            enemyController?.SetIntentSuppressed(false);
            supportEnemyController?.SetIntentSuppressed(false);
        }

        void RefreshWorldBlockTooltip()
        {
            if (canvasHud == null)
                return;

            if (IsSettingsOpen || State != RoundState.Aim || Input.GetMouseButton(0) || Input.touchCount > 0)
            {
                ClearWorldBlockTooltip();
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                ClearWorldBlockTooltip();
                return;
            }

            var hoveredBlock = FindHoveredBoardBlock();
            if (hoveredBlock == null || hoveredBlock.CardState == null)
            {
                ClearWorldBlockTooltip();
                return;
            }

            if (hoveredWorldTooltipBlock == hoveredBlock)
                return;

            hoveredWorldTooltipBlock = hoveredBlock;
            var tooltip = BlockPresentationUtility.BuildTooltip(hoveredBlock.CardState);
            canvasHud.SetPassiveTooltip(tooltip.Title, tooltip.Body, tooltip.AccentColor);
        }

        BoardBlock FindHoveredBoardBlock()
        {
            var camera = Camera.main;
            if (camera == null)
                return null;

            var worldPoint = camera.ScreenToWorldPoint(Input.mousePosition);
            var hits = Physics2D.OverlapPointAll(new Vector2(worldPoint.x, worldPoint.y));
            foreach (var hit in hits)
            {
                if (hit == null)
                    continue;

                var block = hit.GetComponent<BoardBlock>() ?? hit.GetComponentInParent<BoardBlock>();
                if (block != null)
                    return block;
            }

            return null;
        }

        void ClearWorldBlockTooltip()
        {
            hoveredWorldTooltipBlock = null;
            canvasHud?.ClearPassiveTooltip();
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
                case IntermissionActionKind.CompleteMapNode:
                    if (debugBattleReturnActive)
                    {
                        CompleteDebugBattleAndReturnCore();
                        break;
                    }

                    CompleteCurrentMapNodeAndReturnCore();
                    break;
                case IntermissionActionKind.FinishLoadout:
                    if (State == RoundState.LoadoutManage)
                    {
                        boardManager.EnsureAtLeastOneActive();
                        if (loadoutReturnsToMap)
                        {
                            loadoutReturnsToMap = false;
                            CompleteCurrentMapNodeAndReturnCore();
                        }
                        else
                        {
                            ContinueToNextEnemy();
                        }
                    }
                    break;
            }
        }

        void ConfigurePhaseStateMachine()
        {
            phaseStateMachine = new GamePhaseStateMachine();
            phaseStateMachine.Register(new SimpleGamePhaseState(RoundState.Map, false));
            phaseStateMachine.Register(new SimpleGamePhaseState(RoundState.Aim, true));
            phaseStateMachine.Register(new SimpleGamePhaseState(RoundState.BallFlying, false));
            phaseStateMachine.Register(new SimpleGamePhaseState(RoundState.RoundResolve, false));
            phaseStateMachine.Register(new SimpleGamePhaseState(RoundState.BlockRewardChoose, false));
            phaseStateMachine.Register(new SimpleGamePhaseState(RoundState.RewardChoose, false));
            phaseStateMachine.Register(new SimpleGamePhaseState(RoundState.Shop, false));
            phaseStateMachine.Register(new SimpleGamePhaseState(RoundState.BlockOperations, false));
            phaseStateMachine.Register(new SimpleGamePhaseState(RoundState.LoadoutManage, false));
            phaseStateMachine.Register(new SimpleGamePhaseState(RoundState.MapEvent, false));
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

                ClearWorldBlockTooltip();
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
