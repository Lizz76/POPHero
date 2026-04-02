using UnityEngine;

namespace POPHero
{
    public class PlayerLauncher : MonoBehaviour
    {
        PopHeroGame game;
        BallController ballController;
        TrajectoryPredictor trajectoryPredictor;
        IAimService aimStateController;
        Camera mainCamera;
        LineRenderer aimLine;
        LineRenderer memoryLine;
        bool isDragging;
        readonly IAimInputStrategy pcAimInputStrategy = new PcAimInputStrategy();
        readonly IAimInputStrategy mobileAimInputStrategy = new MobileAimInputStrategy();

        public AimLockContext AimContext => aimStateController?.Context;

        public void Initialize(PopHeroGame owner, BallController ball, TrajectoryPredictor predictor)
        {
            game = owner;
            ballController = ball;
            trajectoryPredictor = predictor;
            mainCamera = Camera.main;
            aimStateController = new AimStateController();
            aimStateController.Initialize(game, trajectoryPredictor);

            aimLine = BuildLineRenderer("AimPreviewLine", game.config.ball.previewColor, game.config.ball.previewLineStartWidth, game.config.ball.previewLineEndWidth, 500);
            memoryLine = BuildLineRenderer("AimMemoryLine", new Color(0.22f, 0.78f, 1f, 0.35f), game.config.ball.previewLineStartWidth * 0.72f, game.config.ball.previewLineEndWidth * 0.72f, 499);
        }

        void Update()
        {
            if (game == null || !game.CanSimulate())
                return;

            mainCamera ??= Camera.main;
            if (game.State != RoundState.Aim)
            {
                if (aimLine.enabled || memoryLine.enabled || isDragging)
                    CancelAim();
                return;
            }

            GetCurrentInputStrategy().Tick(this);
        }

        public void CancelAim()
        {
            isDragging = false;
            aimStateController?.Reset();
            aimLine.enabled = false;
            aimLine.positionCount = 0;
            memoryLine.enabled = false;
            memoryLine.positionCount = 0;
            game?.ClearAimPreview();
        }

        IAimInputStrategy GetCurrentInputStrategy()
        {
            return game.CurrentAimMode == InputAimMode.MobileDragConfirm ? mobileAimInputStrategy : pcAimInputStrategy;
        }

        internal void TickPcAimInput()
        {
            if (mainCamera == null || Input.touchCount > 0)
                return;

            var worldPoint = GetWorldPoint(Input.mousePosition);
            UpdateAimPreview(worldPoint, false);

            if (Input.GetMouseButtonDown(0) && AimContext != null && AimContext.throwReady)
                LaunchCurrentAim();
        }

        internal void TickMobileAimInput()
        {
            if (mainCamera == null)
                return;

            if (Input.touchCount > 0)
            {
                HandleMobileTouchInput();
                return;
            }

            HandleMobileMouseInput();
        }

        void HandleMobileTouchInput()
        {
            var touch = Input.GetTouch(0);
            var worldPoint = GetWorldPoint(touch.position);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    if (ShouldStartDrag(worldPoint))
                        BeginDrag(worldPoint);
                    else if (CanConfirmAim())
                        LaunchCurrentAim();
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (isDragging)
                        UpdateAimPreview(worldPoint, false);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (isDragging)
                        EndDrag(worldPoint);
                    break;
            }
        }

        void HandleMobileMouseInput()
        {
            var worldPoint = GetWorldPoint(Input.mousePosition);
            if (Input.GetMouseButtonDown(0))
            {
                if (ShouldStartDrag(worldPoint))
                    BeginDrag(worldPoint);
                else if (CanConfirmAim())
                    LaunchCurrentAim();
            }

            if (isDragging && Input.GetMouseButton(0))
                UpdateAimPreview(worldPoint, false);

            if (isDragging && Input.GetMouseButtonUp(0))
                EndDrag(worldPoint);
        }

        void BeginDrag(Vector2 worldPoint)
        {
            isDragging = true;
            UpdateAimPreview(worldPoint, true);
        }

        void EndDrag(Vector2 worldPoint)
        {
            UpdateAimPreview(worldPoint, false);
            isDragging = false;
            aimStateController?.EndInput();
        }

        void LaunchCurrentAim()
        {
            var context = AimContext;
            if (context == null || !context.throwReady)
                return;

            game.TryLaunchBall(context.lockedAimDirection, context.lockedPreview);
        }

        void UpdateAimPreview(Vector2 worldPoint, bool beginInput)
        {
            if (aimStateController == null)
                return;

            var hasAim = beginInput
                ? aimStateController.BeginInput(worldPoint)
                : aimStateController.UpdateLockedAim(worldPoint, false);
            if (!hasAim || AimContext == null || AimContext.lockedPreview == null)
            {
                ClearCurrentAim();
                return;
            }

            DrawLine(aimLine, AimContext.lockedPreview, true);
            DrawLine(memoryLine, AimContext.previousPreview, game.ModManager.ShowTrajectoryMemory() || game.config.aim.showTrajectoryMemory);
            game.ApplyPreviewResult(AimContext.lockedPreview);
        }

        void ClearCurrentAim()
        {
            aimLine.enabled = false;
            aimLine.positionCount = 0;
            memoryLine.enabled = false;
            memoryLine.positionCount = 0;
            game.ClearAimPreview();
        }

        bool ShouldStartDrag(Vector2 worldPoint)
        {
            var radius = Mathf.Max(game.config.aim.dragStartRadius, game.config.aim.inputLockStartRadius, game.config.ball.radius * 3f);
            return Vector2.Distance(worldPoint, game.CurrentLaunchPoint) <= radius;
        }

        bool CanConfirmAim()
        {
            return !isDragging && AimContext != null && AimContext.throwReady;
        }

        Vector2 GetWorldPoint(Vector3 screenPosition)
        {
            var worldPoint = mainCamera.ScreenToWorldPoint(screenPosition);
            return new Vector2(worldPoint.x, worldPoint.y);
        }

        LineRenderer BuildLineRenderer(string objectName, Color color, float startWidth, float endWidth, int sortingOrder)
        {
            var lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(transform, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.alignment = LineAlignment.TransformZ;
            line.numCapVertices = 6;
            line.numCornerVertices = 4;
            line.startWidth = startWidth;
            line.endWidth = endWidth;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = color;
            line.endColor = color;
            line.sortingLayerName = "Default";
            line.sortingOrder = sortingOrder;
            line.enabled = false;
            return line;
        }

        static void DrawLine(LineRenderer line, TrajectoryPreviewResult preview, bool visible)
        {
            if (line == null)
                return;

            if (!visible || preview == null || !preview.HasValidPath)
            {
                line.enabled = false;
                line.positionCount = 0;
                return;
            }

            line.enabled = true;
            line.positionCount = preview.pathPoints.Count;
            for (var index = 0; index < preview.pathPoints.Count; index++)
                line.SetPosition(index, preview.pathPoints[index]);
        }
    }
}
