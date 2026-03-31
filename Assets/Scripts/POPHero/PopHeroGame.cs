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
        public int PreviewAttackScore { get; private set; }
        public int PreviewShieldGain { get; private set; }

        public PlayerLauncher Launcher => launcher;
        public BallController Ball => ballController;
        public RoundController RoundController => roundController;
        public BoardManager BoardManager => boardManager;
        public BuffManager BuffManager => buffManager;
        public EnemyController EnemyPresenter => enemyController;
        public int EncounterIndex => enemyEncounterIndex + 1;
        public int MaxLaunchesPerEnemy => Mathf.Max(1, config.enemies.maxLaunchesPerEnemy);
        public InputAimMode CurrentAimMode => config.aim.currentAimMode;
        public string CurrentAimModeLabel => CurrentAimMode == InputAimMode.PCMouseAimClick ? "PC 鼠标移动瞄准 + 点击发射" : "手机拖动定方向 + 再点一次发射";
        public Vector2 CurrentLaunchPoint => roundController != null ? roundController.LaunchPosition : new Vector2(BoardRect.center.x, LaunchY);
        public IReadOnlyList<WallAimPoint> WallAimPoints => wallAimPoints;

        PlayerLauncher launcher;
        BallController ballController;
        TrajectoryPredictor trajectoryPredictor;
        BoardManager boardManager;
        RoundController roundController;
        BuffManager buffManager;
        EnemyController enemyController;
        PopHeroHud hud;
        DamageCounterView damageCounterView;
        PhysicsMaterial2D bounceMaterial;
        Transform launchMarker;
        readonly List<WallAimPoint> wallAimPoints = new();
        int enemyEncounterIndex;

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

            PreviewAttackScore = Mathf.Max(0, preview.predictedAttackScore);
            PreviewShieldGain = Mathf.Max(0, preview.predictedShieldGain);
            boardManager?.ApplyPreviewState(preview.hitBlocks);
        }

        public void ClearAimPreview()
        {
            PreviewAttackScore = 0;
            PreviewShieldGain = 0;
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
            buffManager = gameObject.AddComponent<BuffManager>();
            boardManager = gameObject.AddComponent<BoardManager>();
            boardManager.Initialize(this, blockRoot, bounceMaterial);
            trajectoryPredictor = gameObject.AddComponent<TrajectoryPredictor>();
            trajectoryPredictor.Initialize(this, ballController);
            ballController.SetTrajectoryPredictor(trajectoryPredictor);
            launcher = ballController.gameObject.AddComponent<PlayerLauncher>();
            launcher.Initialize(this, ballController, trajectoryPredictor);
            hud = gameObject.AddComponent<PopHeroHud>();
            damageCounterView = gameObject.AddComponent<DamageCounterView>();
            damageCounterView.Initialize(this);
        }

        void StartPrototype()
        {
            Player = new PlayerData(config.player.maxHp, config.player.currentHp, config.player.startShield, config.player.startGold);
            roundController.Initialize(this, new Vector2(BoardRect.center.x, LaunchY));
            buffManager.Initialize(this);
            boardManager.ResetBlockProgression();
            boardManager.ShuffleBlocks(CurrentLaunchPoint);
            ballController.PlaceAt(CurrentLaunchPoint);
            enemyEncounterIndex = 0;
            SpawnEnemy(enemyEncounterIndex);
            GameOverMessage = "本局结束。";
            damageCounterView?.ResetCounter();
            UpdateLaunchMarker();
            ChangeState(RoundState.Aim);
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
                {
                    var spacing = GetWallAnchorSpacing(surfaceType);
                    snappedBallCenter.x = SnapToWallAnchor(rawBallCenter.x, BoardRect.xMin, BoardRect.xMax, spacing);
                    snappedBallCenter.y = BoardRect.yMax - radius;
                    wallNormal = Vector2.down;
                    return true;
                }
                case ArenaSurfaceType.Left:
                {
                    var spacing = GetWallAnchorSpacing(surfaceType);
                    snappedBallCenter.x = BoardRect.xMin + radius;
                    snappedBallCenter.y = SnapToWallAnchor(rawBallCenter.y, BoardRect.yMin, BoardRect.yMax, spacing);
                    wallNormal = Vector2.right;
                    return true;
                }
                case ArenaSurfaceType.Right:
                {
                    var spacing = GetWallAnchorSpacing(surfaceType);
                    snappedBallCenter.x = BoardRect.xMax - radius;
                    snappedBallCenter.y = SnapToWallAnchor(rawBallCenter.y, BoardRect.yMin, BoardRect.yMax, spacing);
                    wallNormal = Vector2.left;
                    return true;
                }
            }

            return false;
        }

        public bool TryProjectAimToWall(Vector2 direction, out ArenaSurfaceType wallSide, out Vector2 projectedPoint)
        {
            wallSide = ArenaSurfaceType.Top;
            projectedPoint = CurrentLaunchPoint;

            var safeDirection = direction.sqrMagnitude <= 0.0001f ? Vector2.up : direction.normalized;
            if (safeDirection.y <= 0.0001f)
                return false;

            var origin = CurrentLaunchPoint;
            var radius = config.ball.radius;
            var topY = BoardRect.yMax - radius;
            var leftX = BoardRect.xMin + radius;
            var rightX = BoardRect.xMax - radius;
            var bestT = float.MaxValue;

            var topT = (topY - origin.y) / safeDirection.y;
            if (topT > 0f)
            {
                bestT = topT;
                wallSide = ArenaSurfaceType.Top;
                projectedPoint = new Vector2(origin.x + safeDirection.x * topT, topY);
            }

            if (safeDirection.x < -0.0001f)
            {
                var leftT = (leftX - origin.x) / safeDirection.x;
                if (leftT > 0f && leftT < bestT)
                {
                    bestT = leftT;
                    wallSide = ArenaSurfaceType.Left;
                    projectedPoint = new Vector2(leftX, origin.y + safeDirection.y * leftT);
                }
            }

            if (safeDirection.x > 0.0001f)
            {
                var rightT = (rightX - origin.x) / safeDirection.x;
                if (rightT > 0f && rightT < bestT)
                {
                    bestT = rightT;
                    wallSide = ArenaSurfaceType.Right;
                    projectedPoint = new Vector2(rightX, origin.y + safeDirection.y * rightT);
                }
            }

            return bestT < float.MaxValue;
        }

        public WallAimPoint FindNearestWallAimPoint(ArenaSurfaceType wallSide, Vector2 projectedPoint)
        {
            WallAimPoint nearest = null;
            var bestDistance = float.MaxValue;
            foreach (var aimPoint in wallAimPoints)
            {
                if (aimPoint.wallSide != wallSide)
                    continue;

                var distance = GetWallAxisDistance(projectedPoint, aimPoint.position, wallSide);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = aimPoint;
                }
            }

            return nearest;
        }

        public float GetAimSnapRadius(ArenaSurfaceType wallSide)
        {
            return GetWallAnchorSpacing(wallSide) * Mathf.Max(0.1f, config.aim.wallAimSnapFactor);
        }

        public float GetAimReleaseRadius(ArenaSurfaceType wallSide)
        {
            return GetWallAnchorSpacing(wallSide) * Mathf.Max(config.aim.wallAimSnapFactor, config.aim.wallAimReleaseFactor);
        }

        public float GetWallAxisDistance(Vector2 from, Vector2 to, ArenaSurfaceType wallSide)
        {
            return wallSide == ArenaSurfaceType.Top ? Mathf.Abs(from.x - to.x) : Mathf.Abs(from.y - to.y);
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
                var colliderSize = surfaceType == ArenaSurfaceType.Top
                    ? new Vector2(spacing + colliderOverlap, thickness)
                    : new Vector2(thickness, spacing + colliderOverlap);
                var visualSize = surfaceType == ArenaSurfaceType.Top
                    ? new Vector2(Mathf.Max(0.12f, spacing - visualGap), thickness * 0.86f)
                    : new Vector2(thickness * 0.86f, Mathf.Max(0.12f, spacing - visualGap));

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
            var subdivisions = Mathf.Max(1, config.arena.wallPointSubdivisions);
            return baseCount * subdivisions;
        }

        float GetWallAnchorSpacing(ArenaSurfaceType surfaceType)
        {
            var span = surfaceType == ArenaSurfaceType.Top ? BoardRect.width : BoardRect.height;
            return span / GetWallAnchorCount(surfaceType);
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

            var wrappedIndex = Mathf.Abs(index) % pattern.Length;
            return Mathf.Clamp(pattern[wrappedIndex], -1f, 1f);
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
                var position = wallSide switch
                {
                    ArenaSurfaceType.Top => new Vector2(anchor, BoardRect.yMax - radius),
                    ArenaSurfaceType.Left => new Vector2(BoardRect.xMin + radius, anchor),
                    ArenaSurfaceType.Right => new Vector2(BoardRect.xMax - radius, anchor),
                    _ => Vector2.zero
                };
                var normal = wallSide switch
                {
                    ArenaSurfaceType.Top => Vector2.down,
                    ArenaSurfaceType.Left => Vector2.right,
                    ArenaSurfaceType.Right => Vector2.left,
                    _ => Vector2.zero
                };

                wallAimPoints.Add(new WallAimPoint
                {
                    id = $"{wallSide}_{index:000}",
                    position = position,
                    wallSide = wallSide,
                    normal = normal,
                    priority = index
                });
            }
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
                Player.AddGold(CurrentEnemy.RewardGold);
                Player.RestoreToFullHealth();
            }

            Player.RegisterKillAndTryLevelUp();
            boardManager.AdvanceBlockProgression();
            buffManager.GenerateChoices();
            ChangeState(RoundState.BuffChoose);
        }

        void PrepareNextRound()
        {
            boardManager.AdvanceBlockProgression();
            boardManager.ShuffleBlocks(CurrentLaunchPoint);
            ballController.PlaceAt(CurrentLaunchPoint);
            RefreshLaunchCounter();
            UpdateLaunchMarker();
            ChangeState(RoundState.Aim);
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
            if (launchMarker == null)
                return;

            launchMarker.position = CurrentLaunchPoint;
        }

        public void TrySelectBuff(int index)
        {
            if (State != RoundState.BuffChoose)
                return;

            if (!buffManager.TryApplyChoice(index))
                return;

            enemyEncounterIndex += 1;
            SpawnEnemy(enemyEncounterIndex);
            PrepareNextRound();
        }

        public void DebugShuffleBoard()
        {
            if (State == RoundState.GameOver)
                return;

            boardManager.ShuffleBlocks(CurrentLaunchPoint);
            UpdateLaunchMarker();
        }

        public void DebugAddGold(int amount)
        {
            Player.AddGold(amount);
        }

        public void ToggleAimMode()
        {
            config.aim.currentAimMode = config.aim.currentAimMode == InputAimMode.PCMouseAimClick
                ? InputAimMode.MobileDragConfirm
                : InputAimMode.PCMouseAimClick;

            if (State == RoundState.Aim)
                launcher?.CancelAim();
        }

        public void DebugKillEnemy()
        {
            if (CurrentEnemy == null || State == RoundState.GameOver || State == RoundState.BuffChoose)
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
            if (ballController != null)
                ballController.StopImmediately();
        }

        void RefreshLaunchCounter()
        {
            if (ballController == null)
                return;

            ballController.SetLaunchCounter(RemainingLaunchesForEnemy, MaxLaunchesPerEnemy);
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

            if (ballController != null)
                ballController.SetLaunchCounterVisible(newState == RoundState.Aim);

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
                normal =
                {
                    background = panelTexture,
                    textColor = Color.white
                }
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
