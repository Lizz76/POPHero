using UnityEngine;

namespace POPHero
{
    public class PopHeroGame : MonoBehaviour
    {
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

            CreateWall(parent, "WallTop", new Vector2(BoardRect.center.x, BoardRect.yMax + config.arena.wallThickness * 0.5f), new Vector2(BoardRect.width + config.arena.wallThickness * 2f, config.arena.wallThickness), ArenaSurfaceType.Top);
            CreateWall(parent, "WallLeft", new Vector2(BoardRect.xMin - config.arena.wallThickness * 0.5f, BoardRect.center.y), new Vector2(config.arena.wallThickness, BoardRect.height + config.arena.wallThickness), ArenaSurfaceType.Left);
            CreateWall(parent, "WallRight", new Vector2(BoardRect.xMax + config.arena.wallThickness * 0.5f, BoardRect.center.y), new Vector2(config.arena.wallThickness, BoardRect.height + config.arena.wallThickness), ArenaSurfaceType.Right);

            var bottomLine = PrototypeVisualFactory.CreateSpriteObject("BottomLine", parent, PrototypeVisualFactory.SquareSprite, new Color(0.93f, 0.66f, 0.18f, 0.36f), 7, new Vector2(BoardRect.width, 0.14f));
            bottomLine.transform.position = new Vector3(BoardRect.center.x, BoardRect.yMin + 0.02f, 0f);
            var bottomTrigger = bottomLine.AddComponent<BoxCollider2D>();
            bottomTrigger.isTrigger = true;
            bottomTrigger.size = new Vector2(BoardRect.width, config.arena.bottomTriggerHeight);
            var marker = bottomLine.AddComponent<ArenaSurfaceMarker>();
            marker.surfaceType = ArenaSurfaceType.Bottom;
        }

        void CreateWall(Transform parent, string objectName, Vector2 position, Vector2 scale, ArenaSurfaceType surfaceType)
        {
            var wall = PrototypeVisualFactory.CreateSpriteObject(objectName, parent, PrototypeVisualFactory.SquareSprite, config.arena.wallColor, 8, scale);
            wall.transform.position = position;
            var collider = wall.AddComponent<BoxCollider2D>();
            collider.sharedMaterial = bounceMaterial;
            var marker = wall.AddComponent<ArenaSurfaceMarker>();
            marker.surfaceType = surfaceType;
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

        public void TryLaunchBall(Vector2 direction)
        {
            if (State != RoundState.Aim || direction.sqrMagnitude <= 0.001f || RemainingLaunchesForEnemy <= 0)
                return;

            RemainingLaunchesForEnemy = Mathf.Max(0, RemainingLaunchesForEnemy - 1);
            RefreshLaunchCounter();
            roundController.BeginRound();
            ChangeState(RoundState.BallFlying);
            RefreshPendingDamagePreview();
            ballController.Launch(direction, config.ball.speed);
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
                Player.Heal(CurrentEnemy.RewardHeal);
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
