using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class BallController : MonoBehaviour
    {
        readonly Dictionary<int, float> recentHits = new();
        readonly List<Vector3> actualPathPoints = new();
        readonly List<SpriteRenderer> hitMarkers = new();

        PopHeroGame game;
        Rigidbody2D body;
        CircleCollider2D circleCollider;
        TrailRenderer trailRenderer;
        TrajectoryPredictor trajectoryPredictor;
        TextMesh launchCounterLabel;
        LineRenderer debugPreviewLine;
        LineRenderer debugActualLine;
        Transform debugMarkerRoot;
        bool isFlying;
        float currentSpeed;
        float flightTimer;
        float trailBoostTimer;
        Vector2 lastMoveDirection = Vector2.up;

        public bool IsFlying => isFlying;
        public CircleCollider2D BallCollider => circleCollider;
        public Vector2 Position => body != null ? body.position : (Vector2)transform.position;
        public float BallRadiusWorld => circleCollider != null
            ? circleCollider.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y), 1f)
            : (game != null ? game.config.ball.radius : 0.15f);

        public void Initialize(PopHeroGame owner, Rigidbody2D rigidbody2D, CircleCollider2D collider2D, TrailRenderer runtimeTrail)
        {
            game = owner;
            body = rigidbody2D;
            circleCollider = collider2D;
            trailRenderer = runtimeTrail;
            ConfigureTrail(0f, 0f, false);
            BuildLaunchCounter();
            BuildDebugVisualization();
        }

        public void SetTrajectoryPredictor(TrajectoryPredictor predictor)
        {
            trajectoryPredictor = predictor;
        }

        public void PlaceAt(Vector2 worldPosition)
        {
            isFlying = false;
            currentSpeed = 0f;
            flightTimer = 0f;
            trailBoostTimer = 0f;
            lastMoveDirection = Vector2.up;
            recentHits.Clear();
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.isKinematic = true;
            body.position = worldPosition;
            transform.position = worldPosition;
            ConfigureTrail(0f, 0f, false);
            trailRenderer?.Clear();
            ResetDebugTrajectories();
        }

        public void Launch(Vector2 direction, float speed, TrajectoryPreviewResult preview = null)
        {
            recentHits.Clear();
            isFlying = true;
            currentSpeed = Mathf.Clamp(speed, 0.1f, game.config.ball.maxSpeed);
            flightTimer = 0f;
            lastMoveDirection = direction.sqrMagnitude <= 0.001f ? Vector2.up : direction.normalized;
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.isKinematic = true;
            ConfigureTrail(game.config.ball.baseTrailTime, game.config.ball.baseTrailWidth, true);
            trailRenderer?.Clear();
            PrepareDebugTrajectories(preview);
            BoostTrail();
        }

        public void StopImmediately()
        {
            isFlying = false;
            currentSpeed = 0f;
            flightTimer = 0f;
            trailBoostTimer = 0f;
            if (body == null)
                return;

            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.isKinematic = true;
            ConfigureTrail(0f, 0f, false);
            trailRenderer?.Clear();
        }

        public void SetLaunchCounter(int remaining, int maximum)
        {
            if (launchCounterLabel == null)
                BuildLaunchCounter();

            launchCounterLabel.text = $"{Mathf.Max(0, remaining)}/{Mathf.Max(1, maximum)}";
        }

        public void SetLaunchCounterVisible(bool isVisible)
        {
            if (launchCounterLabel == null)
                BuildLaunchCounter();

            launchCounterLabel.gameObject.SetActive(isVisible);
        }

        void Update()
        {
            if (isFlying)
                flightTimer += Time.deltaTime;

            UpdateTrailVisual();
            CheckFlightFailSafe();
        }

        void FixedUpdate()
        {
            if (game == null || !game.CanSimulate() || !isFlying)
                return;

            AdvanceFlight(Time.fixedDeltaTime);
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (trajectoryPredictor != null)
                return;

            if (game == null || game.State != RoundState.BallFlying || !isFlying)
                return;

            if (!CanTrigger(collision.collider))
                return;

            var block = collision.collider.GetComponent<BoardBlock>();
            var marker = collision.collider.GetComponent<ArenaSurfaceMarker>();
            var isReflectiveCollision = block != null || (marker != null && marker.surfaceType != ArenaSurfaceType.Bottom);
            if (!isReflectiveCollision)
                return;

            if (block != null)
                block.HandleBallHit(this);

            var baseDirection = lastMoveDirection.sqrMagnitude > 0.001f
                ? lastMoveDirection
                : (body.velocity.sqrMagnitude > 0.001f ? body.velocity.normalized : Vector2.up);
            var contactNormal = collision.contactCount > 0 ? collision.GetContact(0).normal : -baseDirection;
            var impactCenter = body.position;
            if (marker != null && marker.surfaceType != ArenaSurfaceType.Bottom)
            {
                if (game.TryGetWallSnap(marker.surfaceType, impactCenter, out var snappedCenter, out var snappedNormal))
                {
                    impactCenter = snappedCenter;
                    contactNormal = snappedNormal;
                    body.position = snappedCenter;
                    transform.position = snappedCenter;
                }
            }

            var reflectDirection = Vector2.Reflect(baseDirection, contactNormal).normalized;
            if (reflectDirection.sqrMagnitude <= 0.0001f)
                reflectDirection = baseDirection.normalized;

            currentSpeed = Mathf.Min(game.config.ball.maxSpeed, currentSpeed + game.config.ball.accelerationPerBounce);
            lastMoveDirection = reflectDirection;
            body.velocity = reflectDirection * currentSpeed;
            BoostTrail();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (trajectoryPredictor != null)
                return;

            if (game == null || game.State != RoundState.BallFlying || !isFlying)
                return;

            var marker = other.GetComponent<ArenaSurfaceMarker>();
            if (marker == null || marker.surfaceType != ArenaSurfaceType.Bottom)
                return;

            ReturnToBottom(other.ClosestPoint(transform.position));
        }

        void CheckFlightFailSafe()
        {
            if (game == null || game.State != RoundState.BallFlying || !isFlying || body == null)
                return;

            var position = body.position;
            if (float.IsNaN(position.x) || float.IsNaN(position.y))
            {
                ReturnToBottom(game.CurrentLaunchPoint);
                return;
            }

            var recoveryPadding = Mathf.Max(0.2f, game.config.ball.outOfBoundsRecoveryPadding);
            var hardBottomY = game.BoardRect.yMin - recoveryPadding;
            var hardTopY = game.BoardRect.yMax + recoveryPadding * 2f;
            var hardLeftX = game.BoardRect.xMin - recoveryPadding * 2f;
            var hardRightX = game.BoardRect.xMax + recoveryPadding * 2f;

            if (position.y <= hardBottomY || position.y >= hardTopY || position.x <= hardLeftX || position.x >= hardRightX)
            {
                ReturnToBottom(position);
                return;
            }

            if (flightTimer >= Mathf.Max(3f, game.config.ball.maxFlightDuration))
                ReturnToBottom(position);
        }

        void ReturnToBottom(Vector2 rawLandingPoint)
        {
            if (game == null || game.State != RoundState.BallFlying || !isFlying)
                return;

            var landingPoint = rawLandingPoint;
            landingPoint.x = Mathf.Clamp(landingPoint.x, game.BoardRect.xMin + game.config.ball.radius, game.BoardRect.xMax - game.config.ball.radius);
            landingPoint.y = game.LaunchY;
            StopImmediately();
            game.OnBallReturned(landingPoint);
        }

        void AdvanceFlight(float deltaTime)
        {
            var epsilon = Mathf.Max(0.001f, game.config.ball.previewHitEpsilon);
            var remainingDistance = Mathf.Max(0f, currentSpeed * deltaTime);
            var currentPosition = body.position;
            var currentDirection = lastMoveDirection.sqrMagnitude > 0.001f ? lastMoveDirection.normalized : Vector2.up;
            var loopGuard = 0;
            Collider2D ignoredCollider = null;
            Collider2D secondaryIgnoredCollider = null;
            WallHitMemory previousWallHit = default;
            var repeatedCornerCount = 0;
            Collider2D recoveryCollider = null;
            var recoveryCount = 0;

            while (remainingDistance > epsilon && loopGuard < 16 && isFlying)
            {
                loopGuard += 1;
                var segmentStart = currentPosition;

                if (game.BounceStepSolver == null || !game.BounceStepSolver.TryCastStep(currentPosition, currentDirection, remainingDistance, ignoredCollider, secondaryIgnoredCollider, out var step))
                {
                    currentPosition += currentDirection * remainingDistance;
                    remainingDistance = 0f;
                    break;
                }

                var cornerResolved = game.BounceStepSolver.TryResolveCornerBounce(previousWallHit, step, out var cornerBounce);
                if (cornerResolved)
                {
                    step.hitPoint = cornerBounce.safePoint;
                    step.hitNormal = cornerBounce.combinedNormal;
                    step.travelDistance = Mathf.Max(step.travelDistance, Vector2.Distance(segmentStart, step.hitPoint));
                    repeatedCornerCount += 1;
                }
                else
                {
                    repeatedCornerCount = 0;
                }

                currentPosition = step.hitPoint;
                RecordActualPoint(currentPosition, true);

                if (!step.isRecoveryStep && step.block != null && CanTrigger(step.collider))
                    step.block.HandleBallHit(this);

                if (step.marker != null && step.marker.surfaceType == ArenaSurfaceType.Bottom)
                {
                    body.position = currentPosition;
                    transform.position = currentPosition;
                    ReturnToBottom(currentPosition);
                    return;
                }

                if (step.isRecoveryStep)
                {
                    recoveryCount = recoveryCollider == step.collider ? recoveryCount + 1 : 1;
                    recoveryCollider = step.collider;
                }
                else
                {
                    recoveryCount = 0;
                    recoveryCollider = null;
                }

                if (!step.isRecoveryStep)
                    currentSpeed = Mathf.Min(game.config.ball.maxSpeed, currentSpeed + game.config.ball.accelerationPerBounce);
                currentDirection = Vector2.Reflect(currentDirection, step.hitNormal).normalized;
                if (currentDirection.sqrMagnitude <= 0.0001f)
                    currentDirection = lastMoveDirection.sqrMagnitude > 0.001f ? -lastMoveDirection.normalized : Vector2.down;

                lastMoveDirection = currentDirection;
                if (!step.isRecoveryStep)
                    BoostTrail();
                var travelCost = step.isRecoveryStep
                    ? Mathf.Max(epsilon, game.config.ball.sameColliderMinTravel)
                    : Mathf.Max(step.travelDistance, epsilon);
                remainingDistance = Mathf.Max(0f, remainingDistance - travelCost);
                var pushDistance = repeatedCornerCount >= 2
                    ? Mathf.Max(epsilon * 2f, BallRadiusWorld * 0.16f)
                    : cornerResolved
                        ? Mathf.Max(epsilon * 1.5f, BallRadiusWorld * 0.1f)
                        : step.isRecoveryStep
                            ? Mathf.Max(epsilon * 1.5f, BallRadiusWorld * 0.12f)
                            : epsilon;
                currentPosition += currentDirection * pushDistance;
                if (cornerResolved)
                {
                    ignoredCollider = cornerBounce.ignoredColliderA;
                    secondaryIgnoredCollider = cornerBounce.ignoredColliderB;
                    previousWallHit.Clear();
                }
                else if (step.isRecoveryStep)
                {
                    ignoredCollider = step.collider;
                    secondaryIgnoredCollider = null;
                    previousWallHit.Clear();
                    if (recoveryCount >= Mathf.Max(1, game.config.ball.interiorRepeatLimit))
                    {
                        currentPosition += currentDirection * Mathf.Max(epsilon * 2f, BallRadiusWorld * 0.18f);
                        recoveryCount = 0;
                        recoveryCollider = null;
                    }
                }
                else
                {
                    ignoredCollider = step.collider;
                    secondaryIgnoredCollider = null;
                    if (step.marker != null &&
                        (step.marker.surfaceType == ArenaSurfaceType.Top || step.marker.surfaceType == ArenaSurfaceType.Left || step.marker.surfaceType == ArenaSurfaceType.Right))
                    {
                        previousWallHit.Set(step.marker.surfaceType, step.hitPoint, step.hitNormal, step.collider);
                    }
                    else
                    {
                        previousWallHit.Clear();
                    }
                }
            }

            body.position = currentPosition;
            transform.position = currentPosition;
            lastMoveDirection = currentDirection;
            RecordActualPoint(currentPosition, false);
        }

        bool CanTrigger(Collider2D collider2D)
        {
            var instanceId = collider2D.GetInstanceID();
            if (recentHits.TryGetValue(instanceId, out var lastHitTime) && Time.time - lastHitTime < game.config.ball.hitCooldown)
                return false;

            recentHits[instanceId] = Time.time;
            return true;
        }

        void BuildLaunchCounter()
        {
            if (launchCounterLabel != null)
                return;

            launchCounterLabel = PrototypeVisualFactory.CreateTextObject("LaunchCounter", transform, "0/0", Color.white, 42, 0.06f, FontStyle.Bold);
            var offsetY = circleCollider != null ? circleCollider.radius + 0.34f : 0.4f;
            launchCounterLabel.transform.localPosition = new Vector3(0f, -offsetY, 0f);
            launchCounterLabel.gameObject.SetActive(false);
        }

        void BuildDebugVisualization()
        {
            debugPreviewLine = BuildDebugLine("PreviewDebugLine", new Color(0.08f, 1f, 0.32f, 0.78f), 0.055f, 501);
            debugActualLine = BuildDebugLine("ActualDebugLine", new Color(1f, 0.28f, 0.24f, 0.92f), 0.07f, 502);
            debugMarkerRoot = new GameObject("ActualHitMarkers").transform;
            debugMarkerRoot.SetParent(transform.parent, false);
        }

        LineRenderer BuildDebugLine(string objectName, Color color, float width, int sortingOrder)
        {
            var lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(transform.parent, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.alignment = LineAlignment.TransformZ;
            line.numCapVertices = 6;
            line.numCornerVertices = 4;
            line.startWidth = width;
            line.endWidth = width * 0.92f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = color;
            line.endColor = color;
            line.sortingLayerName = "Default";
            line.sortingOrder = sortingOrder;
            line.enabled = false;
            return line;
        }

        void PrepareDebugTrajectories(TrajectoryPreviewResult preview)
        {
            ResetDebugTrajectories();
            if (!IsTrajectoryDebugEnabled())
                return;

            if (preview != null && preview.HasValidPath)
            {
                debugPreviewLine.enabled = true;
                debugPreviewLine.positionCount = preview.pathPoints.Count;
                for (var index = 0; index < preview.pathPoints.Count; index++)
                    debugPreviewLine.SetPosition(index, preview.pathPoints[index]);
            }

            actualPathPoints.Add(transform.position);
            RefreshActualDebugLine();
        }

        void ResetDebugTrajectories()
        {
            actualPathPoints.Clear();
            if (debugPreviewLine != null)
            {
                debugPreviewLine.enabled = false;
                debugPreviewLine.positionCount = 0;
            }

            if (debugActualLine != null)
            {
                debugActualLine.enabled = false;
                debugActualLine.positionCount = 0;
            }

            foreach (var marker in hitMarkers)
                marker.gameObject.SetActive(false);
        }

        void RecordActualPoint(Vector2 worldPoint, bool createMarker)
        {
            if (!IsTrajectoryDebugEnabled())
                return;

            var point = new Vector3(worldPoint.x, worldPoint.y, 0f);
            if (actualPathPoints.Count == 0 || Vector3.Distance(actualPathPoints[actualPathPoints.Count - 1], point) >= 0.05f)
            {
                actualPathPoints.Add(point);
                RefreshActualDebugLine();
            }

            if (createMarker && game.config.debug.showActualHitMarkers)
                ActivateHitMarker(point);
        }

        void RefreshActualDebugLine()
        {
            if (debugActualLine == null)
                return;

            debugActualLine.enabled = actualPathPoints.Count >= 2;
            debugActualLine.positionCount = actualPathPoints.Count;
            for (var index = 0; index < actualPathPoints.Count; index++)
                debugActualLine.SetPosition(index, actualPathPoints[index]);
        }

        void ActivateHitMarker(Vector3 worldPoint)
        {
            SpriteRenderer marker;
            foreach (var existingMarker in hitMarkers)
            {
                if (!existingMarker.gameObject.activeSelf)
                {
                    marker = existingMarker;
                    marker.gameObject.SetActive(true);
                    marker.transform.position = worldPoint;
                    return;
                }
            }

            var markerObject = PrototypeVisualFactory.CreateSpriteObject("HitMarker", debugMarkerRoot, PrototypeVisualFactory.CircleSprite, new Color(1f, 0.34f, 0.26f, 0.95f), 503, Vector2.one * 0.14f);
            marker = markerObject.GetComponent<SpriteRenderer>();
            markerObject.transform.position = worldPoint;
            hitMarkers.Add(marker);
        }

        bool IsTrajectoryDebugEnabled()
        {
            return game != null && game.config != null && game.config.debug.showTrajectoryComparison;
        }

        void BoostTrail()
        {
            if (trailRenderer == null)
                return;

            trailBoostTimer = Mathf.Max(trailBoostTimer, game.config.ball.boostDuration);
            UpdateTrailVisual();
        }

        void UpdateTrailVisual()
        {
            if (trailRenderer == null)
                return;

            if (!isFlying)
            {
                ConfigureTrail(0f, 0f, false);
                return;
            }

            trailBoostTimer = Mathf.Max(0f, trailBoostTimer - Time.deltaTime);
            var boostRatio = game.config.ball.boostDuration <= 0.001f
                ? 0f
                : Mathf.Clamp01(trailBoostTimer / game.config.ball.boostDuration);
            var time = Mathf.Lerp(game.config.ball.baseTrailTime, game.config.ball.boostTrailTime, boostRatio);
            var width = Mathf.Lerp(game.config.ball.baseTrailWidth, game.config.ball.boostTrailWidth, boostRatio);
            ConfigureTrail(time, width, true);
        }

        void ConfigureTrail(float trailTime, float width, bool emitting)
        {
            if (trailRenderer == null)
                return;

            trailRenderer.time = trailTime;
            trailRenderer.startWidth = width;
            trailRenderer.endWidth = width * 0.12f;
            trailRenderer.emitting = emitting;
        }
    }
}
