using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public enum BallFlightEventType
    {
        None,
        BlockHit,
        WallBounce,
        BottomHit,
        Recovery,
        Terminated
    }

    public enum BallFlightTerminationReason
    {
        None,
        HitBottom,
        MaxDistance,
        MaxBounces,
        MaxDuration,
        InvalidState,
        FailedRecovery,
        StepLimit
    }

    public sealed class BallFlightState
    {
        public Vector2 position;
        public Vector2 direction = Vector2.up;
        public float speed;
        public float elapsedTime;
        public float traveledDistance;
        public int bounceCount;
        public Collider2D ignoredCollider;
        public Collider2D secondaryIgnoredCollider;
        public WallHitMemory previousWallHit;
        public int repeatedCornerCount;
        public Collider2D recoveryCollider;
        public int recoveryCount;
        public Collider2D lastGameplayCollider;
        public Vector2 lastGameplayHitPoint;
        public bool hasLastGameplayHit;
        public bool isTerminated;
        public BallFlightTerminationReason terminationReason;

        public static BallFlightState Create(Vector2 origin, Vector2 launchDirection, float initialSpeed)
        {
            return new BallFlightState
            {
                position = origin,
                direction = launchDirection.sqrMagnitude <= 0.0001f ? Vector2.up : launchDirection.normalized,
                speed = Mathf.Max(0.01f, initialSpeed),
                terminationReason = BallFlightTerminationReason.None
            };
        }

        public void Terminate(BallFlightTerminationReason reason)
        {
            isTerminated = true;
            terminationReason = reason;
        }
    }

    public struct BallFlightRunOptions
    {
        public float distanceBudget;
        public float maxTotalDistance;
        public float maxDuration;
        public int maxBounces;
        public int maxSteps;
        public bool includeStartPoint;
    }

    public struct BallFlightEvent
    {
        public BallFlightEventType eventType;
        public Vector2 point;
        public Vector2 normal;
        public Vector2 nextDirection;
        public float nextSpeed;
        public float travelCost;
        public Collider2D collider;
        public BoardBlock block;
        public ArenaSurfaceMarker marker;
        public BallFlightTerminationReason terminationReason;
        public bool isRecovery;
    }

    public sealed class BallFlightResult
    {
        public readonly List<Vector3> pathPoints = new();
        public readonly List<BallFlightEvent> events = new();
        public readonly List<BoardBlock> hitBlocks = new();
        public BallFlightTerminationReason terminationReason;
        public Vector2 finalPosition;
        public Vector2 finalDirection;
        public float finalSpeed;
        public int bounceCount;
        public bool hitBottom;
        public bool stepLimitReached;
    }

    public sealed class BallFlightSimulator
    {
        readonly PopHeroGame game;
        readonly BallController ball;

        public BallFlightSimulator(PopHeroGame owner, BallController ballController)
        {
            game = owner;
            ball = ballController;
        }

        public BallFlightResult Simulate(BallFlightState state, BallFlightRunOptions options)
        {
            var result = new BallFlightResult();
            if (state == null || game == null)
            {
                result.terminationReason = BallFlightTerminationReason.InvalidState;
                return result;
            }

            var epsilon = Mathf.Max(0.001f, game.config.ball.previewHitEpsilon);
            var remainingDistance = Mathf.Max(0f, options.distanceBudget);
            var maxSteps = Mathf.Max(1, options.maxSteps);
            if (options.includeStartPoint)
                AddPathPoint(result, state.position);

            for (var stepIndex = 0; stepIndex < maxSteps && remainingDistance > epsilon && !state.isTerminated; stepIndex++)
            {
                if (!ValidateState(state, result))
                    break;

                ClampRemainingBudgets(state, options, ref remainingDistance);
                if (remainingDistance <= epsilon)
                    break;

                if (TryRecoverOutOfBounds(state, result))
                    continue;

                if (game.BounceStepSolver == null ||
                    !game.BounceStepSolver.TryCastStep(state.position, state.direction, remainingDistance, state.ignoredCollider, state.secondaryIgnoredCollider, out var step))
                {
                    MoveWithoutHit(state, result, remainingDistance, options);
                    remainingDistance = 0f;
                    break;
                }

                var segmentStart = state.position;
                var cornerResolved = game.BounceStepSolver.TryResolveCornerBounce(state.previousWallHit, step, out var cornerBounce);
                if (cornerResolved)
                {
                    step.hitPoint = cornerBounce.safePoint;
                    step.hitNormal = cornerBounce.combinedNormal;
                    step.travelDistance = Mathf.Max(step.travelDistance, Vector2.Distance(segmentStart, step.hitPoint));
                    state.repeatedCornerCount += 1;
                }
                else
                {
                    state.repeatedCornerCount = 0;
                }

                state.position = step.hitPoint;
                AddPathPoint(result, state.position);

                var travelCost = step.isRecoveryStep
                    ? Mathf.Max(epsilon, game.config.ball.sameColliderMinTravel)
                    : Mathf.Max(step.travelDistance, epsilon);
                ConsumeTravel(state, travelCost);
                remainingDistance = Mathf.Max(0f, remainingDistance - travelCost);

                if (step.marker != null && step.marker.surfaceType == ArenaSurfaceType.Bottom)
                {
                    AddEvent(result, state, step, BallFlightEventType.BottomHit, travelCost, BallFlightTerminationReason.HitBottom);
                    state.Terminate(BallFlightTerminationReason.HitBottom);
                    result.hitBottom = true;
                    break;
                }

                UpdateRecoveryMemory(state, step);
                var reflectDirection = Vector2.Reflect(state.direction, step.hitNormal).normalized;
                if (reflectDirection.sqrMagnitude <= 0.0001f)
                    reflectDirection = state.direction.sqrMagnitude > 0.0001f ? -state.direction.normalized : Vector2.down;

                var eventType = ResolveEventType(state, step);
                if (!step.isRecoveryStep)
                {
                    state.speed = Mathf.Min(game.config.ball.maxSpeed, state.speed + game.config.ball.accelerationPerBounce);
                    state.bounceCount += 1;
                }

                state.direction = reflectDirection;
                AddEvent(result, state, step, eventType, travelCost, BallFlightTerminationReason.None);
                PushAwayFromSurface(state, epsilon, step, cornerResolved, cornerBounce);
                UpdateIgnoreMemory(state, step, cornerResolved, cornerBounce);

                if (ShouldTerminateForBounceLimit(state, options))
                    break;
                if (ShouldTerminateForBudgets(state, options))
                    break;
            }

            if (!state.isTerminated && remainingDistance > epsilon)
                result.stepLimitReached = true;

            result.finalPosition = state.position;
            result.finalDirection = state.direction;
            result.finalSpeed = state.speed;
            result.bounceCount = state.bounceCount;
            result.terminationReason = state.terminationReason;
            return result;
        }

        bool ValidateState(BallFlightState state, BallFlightResult result)
        {
            if (!float.IsNaN(state.position.x) && !float.IsNaN(state.position.y) &&
                !float.IsNaN(state.direction.x) && !float.IsNaN(state.direction.y))
                return true;

            state.Terminate(BallFlightTerminationReason.InvalidState);
            result.terminationReason = state.terminationReason;
            AddEvent(result, state, default, BallFlightEventType.Terminated, 0f, state.terminationReason);
            return false;
        }

        void ClampRemainingBudgets(BallFlightState state, BallFlightRunOptions options, ref float remainingDistance)
        {
            if (options.maxTotalDistance > 0f)
                remainingDistance = Mathf.Min(remainingDistance, Mathf.Max(0f, options.maxTotalDistance - state.traveledDistance));

            if (options.maxDuration > 0f)
            {
                var timeLeft = Mathf.Max(0f, options.maxDuration - state.elapsedTime);
                if (timeLeft <= 0f)
                {
                    state.Terminate(BallFlightTerminationReason.MaxDuration);
                    remainingDistance = 0f;
                    return;
                }

                remainingDistance = Mathf.Min(remainingDistance, state.speed * timeLeft);
            }
        }

        bool TryRecoverOutOfBounds(BallFlightState state, BallFlightResult result)
        {
            var padding = Mathf.Max(0.2f, game.config.ball.outOfBoundsRecoveryPadding);
            var radius = GetBallRadius();
            var position = state.position;
            var normal = Vector2.zero;
            var safePoint = position;

            if (position.y <= game.CurrentBottomBoundaryY - padding)
            {
                state.Terminate(BallFlightTerminationReason.HitBottom);
                AddEvent(result, state, default, BallFlightEventType.BottomHit, 0f, BallFlightTerminationReason.HitBottom);
                result.hitBottom = true;
                return true;
            }

            if (position.x <= game.BoardRect.xMin - padding)
            {
                normal = Vector2.right;
                safePoint = new Vector2(game.BoardRect.xMin + radius + game.config.ball.interiorPushOutPadding, position.y);
            }
            else if (position.x >= game.BoardRect.xMax + padding)
            {
                normal = Vector2.left;
                safePoint = new Vector2(game.BoardRect.xMax - radius - game.config.ball.interiorPushOutPadding, position.y);
            }
            else if (position.y >= game.BoardRect.yMax + padding)
            {
                normal = Vector2.down;
                safePoint = new Vector2(position.x, game.BoardRect.yMax - radius - game.config.ball.interiorPushOutPadding);
            }

            if (normal.sqrMagnitude <= 0.0001f)
                return false;

            state.position = safePoint;
            state.direction = Vector2.Reflect(state.direction, normal).normalized;
            if (state.direction.sqrMagnitude <= 0.0001f)
                state.direction = -normal;
            state.previousWallHit.Clear();
            state.ignoredCollider = null;
            state.secondaryIgnoredCollider = null;
            AddPathPoint(result, state.position);
            AddEvent(result, state, new TrajectoryCastStep { hitPoint = safePoint, hitNormal = normal }, BallFlightEventType.Recovery, 0f, BallFlightTerminationReason.None);
            return true;
        }

        void MoveWithoutHit(BallFlightState state, BallFlightResult result, float distance, BallFlightRunOptions options)
        {
            state.position += state.direction * Mathf.Max(0f, distance);
            ConsumeTravel(state, Mathf.Max(0f, distance));
            AddPathPoint(result, state.position);
            ShouldTerminateForBudgets(state, options);
        }

        void ConsumeTravel(BallFlightState state, float distance)
        {
            state.traveledDistance += Mathf.Max(0f, distance);
            state.elapsedTime += state.speed <= 0.001f ? 0f : Mathf.Max(0f, distance) / state.speed;
        }

        void UpdateRecoveryMemory(BallFlightState state, TrajectoryCastStep step)
        {
            if (step.isRecoveryStep)
            {
                state.recoveryCount = state.recoveryCollider == step.collider ? state.recoveryCount + 1 : 1;
                state.recoveryCollider = step.collider;
            }
            else
            {
                state.recoveryCount = 0;
                state.recoveryCollider = null;
            }
        }

        BallFlightEventType ResolveEventType(BallFlightState state, TrajectoryCastStep step)
        {
            if (step.isRecoveryStep)
                return BallFlightEventType.Recovery;

            if (step.block != null && ShouldEmitGameplayHit(state, step))
                return BallFlightEventType.BlockHit;

            return BallFlightEventType.WallBounce;
        }

        bool ShouldEmitGameplayHit(BallFlightState state, TrajectoryCastStep step)
        {
            var minHitGap = Mathf.Max(game.config.ball.previewHitEpsilon, game.config.ball.previewMinHitGap);
            if (state.hasLastGameplayHit &&
                state.lastGameplayCollider == step.collider &&
                Vector2.Distance(state.lastGameplayHitPoint, step.hitPoint) < minHitGap)
                return false;

            state.hasLastGameplayHit = true;
            state.lastGameplayCollider = step.collider;
            state.lastGameplayHitPoint = step.hitPoint;
            return true;
        }

        void PushAwayFromSurface(BallFlightState state, float epsilon, TrajectoryCastStep step, bool cornerResolved, CornerBounceResult cornerBounce)
        {
            var pushDistance = state.repeatedCornerCount >= 2
                ? Mathf.Max(epsilon * 2f, GetBallRadius() * 0.16f)
                : cornerResolved
                    ? Mathf.Max(epsilon * 1.5f, GetBallRadius() * 0.1f)
                    : step.isRecoveryStep
                        ? Mathf.Max(epsilon * 1.5f, GetBallRadius() * 0.12f)
                        : epsilon;

            state.position += state.direction * pushDistance;
            if (!step.isRecoveryStep || state.recoveryCount < Mathf.Max(1, game.config.ball.interiorRepeatLimit))
                return;

            state.position += state.direction * Mathf.Max(epsilon * 2f, GetBallRadius() * 0.18f);
            state.recoveryCount = 0;
            state.recoveryCollider = null;
        }

        void UpdateIgnoreMemory(BallFlightState state, TrajectoryCastStep step, bool cornerResolved, CornerBounceResult cornerBounce)
        {
            if (cornerResolved)
            {
                state.ignoredCollider = cornerBounce.ignoredColliderA;
                state.secondaryIgnoredCollider = cornerBounce.ignoredColliderB;
                state.previousWallHit.Clear();
                return;
            }

            state.ignoredCollider = step.collider;
            state.secondaryIgnoredCollider = null;

            if (step.isRecoveryStep)
            {
                state.previousWallHit.Clear();
                return;
            }

            if (step.marker != null && IsReflectiveWall(step.marker.surfaceType))
                state.previousWallHit.Set(step.marker.surfaceType, step.hitPoint, step.hitNormal, step.collider);
            else
                state.previousWallHit.Clear();
        }

        bool ShouldTerminateForBounceLimit(BallFlightState state, BallFlightRunOptions options)
        {
            if (options.maxBounces <= 0 || state.bounceCount < options.maxBounces)
                return false;

            state.Terminate(BallFlightTerminationReason.MaxBounces);
            return true;
        }

        bool ShouldTerminateForBudgets(BallFlightState state, BallFlightRunOptions options)
        {
            if (options.maxTotalDistance > 0f && state.traveledDistance >= options.maxTotalDistance)
            {
                state.Terminate(BallFlightTerminationReason.MaxDistance);
                return true;
            }

            if (options.maxDuration > 0f && state.elapsedTime >= options.maxDuration)
            {
                state.Terminate(BallFlightTerminationReason.MaxDuration);
                return true;
            }

            return false;
        }

        void AddEvent(BallFlightResult result, BallFlightState state, TrajectoryCastStep step, BallFlightEventType eventType, float travelCost, BallFlightTerminationReason terminationReason)
        {
            var flightEvent = new BallFlightEvent
            {
                eventType = eventType,
                point = step.hitPoint,
                normal = step.hitNormal,
                nextDirection = state.direction,
                nextSpeed = state.speed,
                travelCost = travelCost,
                collider = step.collider,
                block = step.block,
                marker = step.marker,
                terminationReason = terminationReason,
                isRecovery = step.isRecoveryStep
            };

            result.events.Add(flightEvent);
            if (eventType == BallFlightEventType.BlockHit && step.block != null)
                result.hitBlocks.Add(step.block);
        }

        static void AddPathPoint(BallFlightResult result, Vector2 point)
        {
            var worldPoint = new Vector3(point.x, point.y, 0f);
            if (result.pathPoints.Count == 0 || Vector3.Distance(result.pathPoints[result.pathPoints.Count - 1], worldPoint) >= 0.001f)
                result.pathPoints.Add(worldPoint);
        }

        float GetBallRadius()
        {
            if (ball != null)
                return Mathf.Max(0.01f, ball.BallRadiusWorld);

            return game != null ? Mathf.Max(0.01f, game.config.ball.radius) : 0.01f;
        }

        static bool IsReflectiveWall(ArenaSurfaceType surfaceType)
        {
            return surfaceType == ArenaSurfaceType.Left ||
                   surfaceType == ArenaSurfaceType.Right ||
                   surfaceType == ArenaSurfaceType.Top;
        }
    }
}
