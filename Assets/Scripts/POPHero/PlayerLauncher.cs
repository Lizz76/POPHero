using UnityEngine;

namespace POPHero
{
    public class PlayerLauncher : MonoBehaviour
    {
        PopHeroGame game;
        BallController ballController;
        TrajectoryPredictor trajectoryPredictor;
        LineRenderer aimLine;
        Camera mainCamera;
        TrajectoryPreviewResult currentPreview;
        WallAimPoint currentLockedAimPoint;
        bool isDragging;
        bool aimLocked;
        bool hasValidAimDirection;
        Vector2 currentAimDirection = Vector2.up;

        public void Initialize(PopHeroGame owner, BallController ball, TrajectoryPredictor predictor)
        {
            game = owner;
            ballController = ball;
            trajectoryPredictor = predictor;
            mainCamera = Camera.main;
            aimLine = gameObject.AddComponent<LineRenderer>();
            aimLine.useWorldSpace = true;
            aimLine.alignment = LineAlignment.TransformZ;
            aimLine.numCapVertices = 6;
            aimLine.numCornerVertices = 4;
            aimLine.startWidth = game.config.ball.previewLineStartWidth;
            aimLine.endWidth = game.config.ball.previewLineEndWidth;
            aimLine.material = new Material(Shader.Find("Sprites/Default"));
            aimLine.startColor = game.config.ball.previewColor;
            aimLine.endColor = game.config.ball.previewColor;
            aimLine.sortingLayerName = "Default";
            aimLine.sortingOrder = 500;
            aimLine.enabled = false;
        }

        void Update()
        {
            if (game == null || !game.CanSimulate())
                return;

            mainCamera ??= Camera.main;
            if (game.State != RoundState.Aim)
            {
                if (aimLine.enabled || isDragging || aimLocked || hasValidAimDirection)
                    CancelAim();
                return;
            }

            switch (game.CurrentAimMode)
            {
                case InputAimMode.MobileDragConfirm:
                    HandleMobileAimInput();
                    break;
                case InputAimMode.PCMouseAimClick:
                default:
                    HandlePcAimInput();
                    break;
            }
        }

        public void CancelAim()
        {
            isDragging = false;
            aimLocked = false;
            hasValidAimDirection = false;
            currentPreview = null;
            currentLockedAimPoint = null;
            aimLine.enabled = false;
            aimLine.positionCount = 0;
            game?.ClearAimPreview();
        }

        void HandlePcAimInput()
        {
            if (Input.touchCount > 0 || mainCamera == null)
                return;

            var worldPoint = GetWorldPoint(Input.mousePosition);
            UpdateAimPreview(worldPoint, true);

            if (Input.GetMouseButtonDown(0) && hasValidAimDirection)
                LaunchCurrentAim();
        }

        void HandleMobileAimInput()
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
                    {
                        BeginDrag(worldPoint);
                    }
                    else if (CanConfirmAim())
                    {
                        LaunchCurrentAim();
                    }
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
                {
                    BeginDrag(worldPoint);
                }
                else if (CanConfirmAim())
                {
                    LaunchCurrentAim();
                }
            }

            if (isDragging && Input.GetMouseButton(0))
                UpdateAimPreview(worldPoint, false);

            if (isDragging && Input.GetMouseButtonUp(0))
                EndDrag(worldPoint);
        }

        void BeginDrag(Vector2 worldPoint)
        {
            isDragging = true;
            aimLocked = false;
            UpdateAimPreview(worldPoint, false);
        }

        void EndDrag(Vector2 worldPoint)
        {
            UpdateAimPreview(worldPoint, false);
            isDragging = false;
            aimLocked = hasValidAimDirection;
        }

        void LaunchCurrentAim()
        {
            if (!hasValidAimDirection)
                return;

            game.TryLaunchBall(currentAimDirection, currentPreview);
        }

        void UpdateAimPreview(Vector2 worldPoint, bool lockAimAfterUpdate)
        {
            var origin = game.CurrentLaunchPoint;
            var rawDirection = ClampAimDirection(worldPoint - origin);
            if (!game.TryProjectAimToWall(rawDirection, out var projectedWallSide, out var projectedPoint))
            {
                ClearCurrentAim();
                return;
            }

            currentLockedAimPoint = ResolveLockedAimPoint(projectedWallSide, projectedPoint);
            if (currentLockedAimPoint == null)
            {
                ClearCurrentAim();
                return;
            }

            currentAimDirection = ClampAimDirection(currentLockedAimPoint.position - origin);
            hasValidAimDirection = currentAimDirection.sqrMagnitude > 0.001f;
            aimLocked = (lockAimAfterUpdate || aimLocked) && hasValidAimDirection;

            if (!hasValidAimDirection)
            {
                ClearCurrentAim();
                return;
            }

            var preview = trajectoryPredictor.BuildPreview(origin, currentAimDirection, game.config.ball.previewSegments, game.config.ball.previewDistance);
            if (!preview.HasValidPath)
            {
                currentPreview = null;
                aimLine.enabled = false;
                aimLine.positionCount = 0;
                game.ClearAimPreview();
                return;
            }

            currentPreview = preview;
            aimLine.enabled = true;
            aimLine.positionCount = preview.pathPoints.Count;
            for (var i = 0; i < preview.pathPoints.Count; i++)
                aimLine.SetPosition(i, preview.pathPoints[i]);

            game.ApplyPreviewResult(preview);
        }

        WallAimPoint ResolveLockedAimPoint(ArenaSurfaceType projectedWallSide, Vector2 projectedPoint)
        {
            var nearestPoint = game.FindNearestWallAimPoint(projectedWallSide, projectedPoint);
            if (nearestPoint == null)
                return currentLockedAimPoint;

            if (currentLockedAimPoint == null)
                return nearestPoint;

            if (currentLockedAimPoint.wallSide == projectedWallSide)
            {
                var distanceToCurrent = game.GetWallAxisDistance(projectedPoint, currentLockedAimPoint.position, projectedWallSide);
                if (distanceToCurrent <= game.GetAimReleaseRadius(projectedWallSide))
                    return currentLockedAimPoint;
            }

            var distanceToNearest = game.GetWallAxisDistance(projectedPoint, nearestPoint.position, projectedWallSide);
            if (distanceToNearest <= game.GetAimSnapRadius(projectedWallSide))
                return nearestPoint;

            return currentLockedAimPoint.wallSide == projectedWallSide ? currentLockedAimPoint : nearestPoint;
        }

        void ClearCurrentAim()
        {
            aimLocked = false;
            hasValidAimDirection = false;
            currentPreview = null;
            currentLockedAimPoint = null;
            aimLine.enabled = false;
            aimLine.positionCount = 0;
            game.ClearAimPreview();
        }

        bool ShouldStartDrag(Vector2 worldPoint)
        {
            return Vector2.Distance(worldPoint, game.CurrentLaunchPoint) <= Mathf.Max(game.config.aim.dragStartRadius, game.config.ball.radius * 3f);
        }

        bool CanConfirmAim()
        {
            return !isDragging && aimLocked && hasValidAimDirection;
        }

        Vector2 GetWorldPoint(Vector3 screenPosition)
        {
            var worldPoint = mainCamera.ScreenToWorldPoint(screenPosition);
            return new Vector2(worldPoint.x, worldPoint.y);
        }

        Vector2 ClampAimDirection(Vector2 rawDirection)
        {
            if (rawDirection.sqrMagnitude <= 0.0001f)
                rawDirection = Vector2.up;

            var angle = Mathf.Atan2(rawDirection.y, rawDirection.x) * Mathf.Rad2Deg;
            if (angle < 0f)
                angle += 360f;

            angle = Mathf.Clamp(angle, game.config.ball.minAimAngle, game.config.ball.maxAimAngle);
            var radians = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;
        }
    }
}
