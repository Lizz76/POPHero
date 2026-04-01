using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public class PopHeroGame : MonoBehaviour
    {
        static readonly float[] WallStoneShadePattern = { -0.22f, 0.1f, -0.08f, 0.16f, -0.14f, 0.05f, 0.12f, -0.04f };

        public PopHeroPrototypeConfig config;

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
        public StickerCatalog StickerCatalog => stickerCatalog;
        public StickerInventory StickerInventory => stickerInventory;
        public StickerEffectRunner StickerEffectRunner => stickerEffectRunner;
        public RewardChoiceController RewardChoiceController => rewardChoiceController;
        public ModManager ModManager => modManager;
        public ShopManager ShopManager => shopManager;
        public int EncounterIndex => enemyEncounterIndex + 1;
        public int MaxLaunchesPerEnemy => Mathf.Max(1, config.enemies.maxLaunchesPerEnemy + (Player?.BonusLaunchesPerEnemy ?? 0));
        public InputAimMode CurrentAimMode => config.aim.currentAimMode;
        public bool IsInitialBlockDraftPending => initialBlockDraftPending;
        public bool CanManageBlockAssignments => State == RoundState.Shop || State == RoundState.LoadoutManage;
        public string CurrentAimModeLabel => CurrentAimMode == InputAimMode.PCMouseAimClick ? "鼠标移动瞄准，左键确认发射" : "拖动定方向，第二次点击发射";
        public Vector2 CurrentLaunchPoint => roundController != null ? roundController.LaunchPosition : new Vector2(BoardRect.center.x, LaunchY);
        public IReadOnlyList<WallAimPoint> WallAimPoints => wallAimPoints;

        PlayerLauncher launcher;
        BallController ballController;
        TrajectoryPredictor trajectoryPredictor;
        BoardManager boardManager;
        RoundController roundController;
        EnemyController enemyController;
        PopHeroHud hud;
        DamageCounterView damageCounterView;
        PhysicsMaterial2D bounceMaterial;
        Transform launchMarker;
        StickerCatalog stickerCatalog;
        StickerInventory stickerInventory;
        StickerEffectRunner stickerEffectRunner;
        RewardChoiceController rewardChoiceController;
        ModManager modManager;
        ShopManager shopManager;
        readonly List<WallAimPoint> wallAimPoints = new();
        int enemyEncounterIndex;
        bool initialBlockDraftPending;

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
            return State != RoundState.GameOver;
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
                enemyController?.SetPreviewDamage(pendingDamage);
                damageCounterView?.Show();
                damageCounterView?.SetValue(pendingDamage);
                return;
            }

            enemyController?.ClearPreviewDamage();
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

            var worldRoot = new GameObject("World").transform;
            var boardRoot = new GameObject("Board").transform;
            boardRoot.SetParent(worldRoot, false);
            var blockRoot = new GameObject("Blocks").transform;
            blockRoot.SetParent(worldRoot, false);
            var enemyRoot = new GameObject("EnemyLayer").transform;
            enemyRoot.SetParent(worldRoot, false);

            BuildEnemyLayer(enemyRoot);
            BuildBoard(boardRoot);
            BuildBall(worldRoot);

            roundController = gameObject.AddComponent<RoundController>();
            boardManager = gameObject.AddComponent<BoardManager>();
            trajectoryPredictor = gameObject.AddComponent<TrajectoryPredictor>();
            hud = gameObject.AddComponent<PopHeroHud>();
            damageCounterView = gameObject.AddComponent<DamageCounterView>();

            boardManager.Initialize(this, blockRoot, bounceMaterial);
            trajectoryPredictor.Initialize(this, ballController);
            ballController.SetTrajectoryPredictor(trajectoryPredictor);

            stickerCatalog = new StickerCatalog();
            stickerInventory = new StickerInventory();
            stickerEffectRunner = new StickerEffectRunner();
            rewardChoiceController = new RewardChoiceController();
            modManager = new ModManager();
            shopManager = new ShopManager();

            stickerInventory.Initialize(this);
            stickerEffectRunner.Initialize(this);
            rewardChoiceController.Initialize(this);
            modManager.Initialize(this);
            shopManager.Initialize(this);

            launcher = ballController.gameObject.AddComponent<PlayerLauncher>();
            launcher.Initialize(this, ballController, trajectoryPredictor);
            damageCounterView.Initialize(this);
        }

        void StartPrototype()
        {
            Player = new PlayerData(config.player.maxHp, config.player.currentHp, config.player.startShield, config.player.startGold);
            Player.IncreaseInventoryCapacity(config.stickers.baseInventoryCapacity - Player.StickerInventoryCapacity);
            roundController.Initialize(this, new Vector2(BoardRect.center.x, LaunchY));
            boardManager.ResetBlockProgression();
            ballController.PlaceAt(CurrentLaunchPoint);
            enemyEncounterIndex = 0;
            CurrentEnemy = null;
            initialBlockDraftPending = true;
            GameOverMessage = "本局结束。";
            IntermissionMessage = string.Empty;
            damageCounterView?.ResetCounter();
            UpdateLaunchMarker();
            BeginBlockRewardDraft(true);
        }

        void BuildEnemyLayer(Transform parent)
        {
            var panelCenter = new Vector2(BoardRect.center.x, BoardRect.yMax + config.arena.topPanelHeight * 0.56f);
            var panel = PrototypeVisualFactory.CreateSpriteObject("EnemyPanel", parent, PrototypeVisualFactory.SquareSprite, config.arena.topPanelColor, 1, new Vector2(BoardRect.width, config.arena.topPanelHeight));
            panel.transform.position = panelCenter;
            var accentLeft = PrototypeVisualFactory.CreateSpriteObject("AccentLeft", parent, PrototypeVisualFactory.SquareSprite, config.arena.enemyPanelAccent, 2, new Vector2(2.5f, 0.32f));
            accentLeft.transform.position = panelCenter + new Vector2(-BoardRect.width * 0.25f, 0.9f);
            var accentRight = PrototypeVisualFactory.CreateSpriteObject("AccentRight", parent, PrototypeVisualFactory.SquareSprite, config.arena.enemyPanelAccent, 2, new Vector2(2.5f, 0.32f));
            accentRight.transform.position = panelCenter + new Vector2(BoardRect.width * 0.25f, 0.9f);

            var enemyGo = new GameObject("Enemy");
            enemyGo.transform.SetParent(parent, false);
            enemyGo.transform.position = panelCenter + new Vector2(0f, -0.12f);
            enemyController = enemyGo.AddComponent<EnemyController>();
            enemyController.Initialize(this);
        }

        void BuildBall(Transform parent)
        {
            var ballGo = new GameObject("Ball");
            ballGo.transform.SetParent(parent, false);
            PrototypeVisualFactory.CreateSpriteObject("BallVisual", ballGo.transform, PrototypeVisualFactory.CircleSprite, config.ball.color, 40, Vector2.one * (config.ball.radius * 2f));

            var trail = ballGo.AddComponent<TrailRenderer>();
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.sortingLayerName = "Default";
            trail.sortingOrder = 39;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.minVertexDistance = 0.03f;
            trail.numCornerVertices = 2;
            trail.numCapVertices = 2;
            trail.startColor = new Color(0.2f, 1f, 0.78f, 0.82f);
            trail.endColor = new Color(0.2f, 1f, 0.78f, 0f);
            trail.emitting = false;

            var rigidbody2D = ballGo.AddComponent<Rigidbody2D>();
            rigidbody2D.gravityScale = 0f;
            rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rigidbody2D.sharedMaterial = bounceMaterial;
            rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;

            var circleCollider = ballGo.AddComponent<CircleCollider2D>();
            circleCollider.radius = config.ball.radius;
            circleCollider.sharedMaterial = bounceMaterial;

            ballController = ballGo.AddComponent<BallController>();
            ballController.Initialize(this, rigidbody2D, circleCollider, trail);
        }

        void BuildBoard(Transform parent)
        {
            var frame = PrototypeVisualFactory.CreateSpriteObject("BoardFrame", parent, PrototypeVisualFactory.SquareSprite, config.arena.boardFrameColor, 2, new Vector2(BoardRect.width + config.arena.wallThickness * 2f, BoardRect.height + config.arena.wallThickness * 2f));
            frame.transform.position = BoardRect.center;
            var background = PrototypeVisualFactory.CreateSpriteObject("BoardBackground", parent, PrototypeVisualFactory.SquareSprite, config.arena.boardBackgroundColor, 3, BoardRect.size);
            background.transform.position = BoardRect.center;
            var launchGuide = PrototypeVisualFactory.CreateSpriteObject("LaunchGuide", parent, PrototypeVisualFactory.SquareSprite, config.arena.launchGuideColor, 6, new Vector2(BoardRect.width - 0.4f, 0.34f));
            launchGuide.transform.position = new Vector3(BoardRect.center.x, LaunchY - 0.15f, 0f);

            var launchMarkerSize = Mathf.Max(0.14f, config.ball.radius * 2.2f);
            launchMarker = PrototypeVisualFactory.CreateSpriteObject("LaunchMarker", parent, PrototypeVisualFactory.CircleSprite, new Color(0.97f, 0.97f, 1f, 0.65f), 25, Vector2.one * launchMarkerSize).transform;

            CreateBrickWall(parent, "WallTop", ArenaSurfaceType.Top, 0);
            CreateBrickWall(parent, "WallLeft", ArenaSurfaceType.Left, 3);
            CreateBrickWall(parent, "WallRight", ArenaSurfaceType.Right, 6);
            RebuildWallAimPoints();

            var bottomLine = PrototypeVisualFactory.CreateSpriteObject("BottomLine", parent, PrototypeVisualFactory.SquareSprite, new Color(0.93f, 0.66f, 0.18f, 0.36f), 7, new Vector2(BoardRect.width, 0.14f));
            bottomLine.transform.position = new Vector3(BoardRect.center.x, BoardRect.yMin + 0.02f, 0f);
            var bottomTrigger = bottomLine.AddComponent<BoxCollider2D>();
            bottomTrigger.isTrigger = true;
            bottomTrigger.size = new Vector2(BoardRect.width, config.arena.bottomTriggerHeight);
            var marker = bottomLine.AddComponent<ArenaSurfaceMarker>();
            marker.surfaceType = ArenaSurfaceType.Bottom;
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

        void CreateBrickWall(Transform parent, string objectName, ArenaSurfaceType surfaceType, int patternOffset)
        {
            var root = new GameObject(objectName).transform;
            root.SetParent(parent, false);

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
            if (State != RoundState.BallFlying)
                return;

            ChangeState(RoundState.RoundResolve);
            var result = roundController.ResolveRound(landingPoint);
            enemyController.Refresh();
            UpdateLaunchMarker();

            if (result.playerDefeated)
            {
                TriggerGameOver("生命归零，战斗结束。");
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

            if (RemainingLaunchesForEnemy <= 0)
            {
                TriggerGameOver("当前敌人的可发射次数已经用完。");
                return;
            }

            PrepareNextRound();
        }

        void HandleEnemyDefeated()
        {
            if (CurrentEnemy != null)
            {
                var rewardGold = Mathf.RoundToInt(CurrentEnemy.RewardGold * modManager.GetRewardGoldMultiplier());
                Player.AddGold(rewardGold);
                Player.RestoreToFullHealth();
            }

            Player.RegisterKillAndTryLevelUp();
            BeginBlockRewardDraft(false);
        }

        void BeginBlockRewardDraft(bool initialDraft)
        {
            initialBlockDraftPending = initialDraft;
            boardManager.GenerateRewardOptions(Player.TotalKills, initialDraft ? config.blockRewards.initialChoiceCount : config.blockRewards.rewardChoiceCount);
            if (initialDraft)
            {
                IntermissionMessage = "先选一张初始方块，再开始第一场战斗。";
            }
            else if (!boardManager.CanAcceptRewardBlock)
            {
                IntermissionMessage = "上阵区与仓库区都已满，请跳过本次方块奖励，稍后在商店或整理阶段删除/替换方块。";
            }
            else if (boardManager.RewardWillGoToReserve)
            {
                IntermissionMessage = "上阵区已满，本次选到的新方块会进入仓库。";
            }
            else
            {
                IntermissionMessage = "击败敌人后，选择一张方块加入上阵区，或直接跳过。";
            }

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
            var name = overflow > 0 ? $"{template.displayName}+{overflow}" : template.displayName;
            return new EnemyData(name, hp, rewardGold, rewardHeal, attackDamage, template.color);
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

            if (!boardManager.TryClaimRewardOption(index, out _, out var addedToReserve, out var failReason))
            {
                IntermissionMessage = failReason;
                return;
            }

            IntermissionMessage = addedToReserve ? "新方块已进入仓库区。" : "新方块已加入上阵区。";

            if (initialBlockDraftPending)
                CompleteInitialDraft();
            else
                EnterStickerRewardPhase();
        }

        public void SkipBlockReward()
        {
            if (State != RoundState.BlockRewardChoose || initialBlockDraftPending)
                return;

            boardManager.ClearRewardOptions();
            EnterStickerRewardPhase();
        }

        public void TrySelectReward(int index)
        {
            if (State != RoundState.RewardChoose)
                return;

            if (!rewardChoiceController.TrySelectChoice(index))
                return;

            EnterShop();
        }

        public void TryRerollRewardChoices()
        {
            if (State == RoundState.RewardChoose && rewardChoiceController.TryRerollChoices())
                IntermissionMessage = rewardChoiceController.LastStatusMessage;
        }

        public void SkipRewardChoices()
        {
            if (State != RoundState.RewardChoose)
                return;

            rewardChoiceController.SkipChoices();
            IntermissionMessage = rewardChoiceController.LastStatusMessage;
            EnterShop();
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
                IntermissionMessage = "已完成上阵区与仓库区方块互换。";
            else
                IntermissionMessage = failReason;
        }

        public void CloseShop()
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

        public bool TryInstallDraggedSticker(string cardId, int socketIndex, out string failReason)
        {
            failReason = string.Empty;
            var dragging = stickerInventory.DraggingSticker;
            if (dragging == null)
            {
                failReason = "当前没有选中的嵌片。";
                return false;
            }

            var sticker = stickerInventory.TakeDraggingSticker();
            if (!boardManager.TryInstallSticker(cardId, socketIndex, sticker, out failReason))
            {
                stickerInventory.ReturnToInventory(sticker);
                return false;
            }

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
            {
                boardManager.EnsureAtLeastOneActive();
                ContinueToNextEnemy();
            }
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
            if (Player.IsDead)
                TriggerGameOver("生命归零，战斗结束。");
        }

        public void TriggerGameOver(string reason = null)
        {
            GameOverMessage = string.IsNullOrWhiteSpace(reason) ? "本局结束。" : reason;
            ChangeState(RoundState.GameOver);
            ballController?.StopImmediately();
        }

        void RefreshLaunchCounter()
        {
            ballController?.SetLaunchCounter(RemainingLaunchesForEnemy, MaxLaunchesPerEnemy);
        }

        void ChangeState(RoundState newState)
        {
            var previousState = State;
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
                enemyController?.ClearPreviewDamage();
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
            GUI.Label(new Rect(panelRect.x, panelRect.y + 16f, panelRect.width, 28f), "伤害", titleStyle);
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
