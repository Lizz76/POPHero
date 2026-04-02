using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public class TrajectoryPredictor : MonoBehaviour
    {
        PopHeroGame game;
        BallController ball;

        public void Initialize(PopHeroGame owner, BallController ballController)
        {
            game = owner;
            ball = ballController;
        }

        public bool TryCastStep(Vector2 origin, Vector2 direction, float maxDistance, out TrajectoryCastStep step)
        {
            return TryCastStep(origin, direction, maxDistance, null, null, out step);
        }

        public bool TryCastStep(Vector2 origin, Vector2 direction, float maxDistance, Collider2D ignoredCollider, out TrajectoryCastStep step)
        {
            return TryCastStep(origin, direction, maxDistance, ignoredCollider, null, out step);
        }

        public bool TryCastStep(Vector2 origin, Vector2 direction, float maxDistance, Collider2D ignoredCollider, Collider2D secondaryIgnoredCollider, out TrajectoryCastStep step)
        {
            step = default;
            return game != null && game.BounceStepSolver != null &&
                   game.BounceStepSolver.TryCastStep(origin, direction, maxDistance, ignoredCollider, secondaryIgnoredCollider, out step);
        }

        public bool TryResolveCornerBounce(WallHitMemory previousWallHit, TrajectoryCastStep step, out CornerBounceResult result)
        {
            result = default;
            return game != null && game.BounceStepSolver != null &&
                   game.BounceStepSolver.TryResolveCornerBounce(previousWallHit, step, out result);
        }

        public TrajectoryPreviewResult BuildPreview(Vector2 origin, Vector2 direction, int maxBounces, float maxDistance)
        {
            var result = new TrajectoryPreviewResult();
            var highlightedBlocks = new HashSet<BoardBlock>();
            var currentOrigin = origin;
            var currentDirection = direction.sqrMagnitude <= 0.0001f ? Vector2.up : direction.normalized;
            var remainingDistance = Mathf.Max(1f, maxDistance);
            var epsilon = Mathf.Max(0.001f, game.config.ball.previewHitEpsilon);
            var minHitGap = Mathf.Max(epsilon, game.config.ball.previewMinHitGap);
            var previousHitPoint = Vector2.zero;
            var hasPreviousHitPoint = false;
            Collider2D previousCollider = null;
            Collider2D ignoredCollider = null;
            Collider2D secondaryIgnoredCollider = null;
            WallHitMemory previousWallHit = default;
            var predictedAttack = 0;
            var predictedShield = 0;
            Collider2D recoveryCollider = null;
            var recoveryCount = 0;

            result.pathPoints.Add(ToPoint(origin));
            result.finalDirection = currentDirection;

            for (var bounceIndex = 0; bounceIndex < Mathf.Max(1, maxBounces) && remainingDistance > epsilon; bounceIndex++)
            {
                if (!TryCastStep(currentOrigin, currentDirection, remainingDistance, ignoredCollider, secondaryIgnoredCollider, out var step))
                {
                    result.pathPoints.Add(ToPoint(currentOrigin + currentDirection * remainingDistance));
                    result.finalDirection = currentDirection;
                    break;
                }

                var cornerResolved = TryResolveCornerBounce(previousWallHit, step, out var cornerBounce);
                if (cornerResolved)
                {
                    step.hitPoint = cornerBounce.safePoint;
                    step.hitNormal = cornerBounce.combinedNormal;
                    step.travelDistance = Mathf.Max(step.travelDistance, Vector2.Distance(currentOrigin, step.hitPoint));
                }

                var hitPoint = step.hitPoint;
                if (hasPreviousHitPoint &&
                    previousCollider == step.collider &&
                    Vector2.Distance(previousHitPoint, hitPoint) < minHitGap)
                    break;

                result.pathPoints.Add(ToPoint(hitPoint));
                previousHitPoint = hitPoint;
                hasPreviousHitPoint = true;
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

                var travelCost = step.isRecoveryStep
                    ? Mathf.Max(epsilon, game.config.ball.sameColliderMinTravel)
                    : Mathf.Max(epsilon, step.travelDistance);
                remainingDistance -= travelCost;

                var block = step.block;
                if (!step.isRecoveryStep && block != null)
                {
                    ApplyPredictedBlockEffect(block, ref predictedAttack, ref predictedShield);
                    if (highlightedBlocks.Add(block))
                        result.hitBlocks.Add(block);
                }

                if (step.marker != null && step.marker.surfaceType == ArenaSurfaceType.Bottom)
                {
                    result.hitBottom = true;
                    result.finalDirection = currentDirection;
                    break;
                }

                var reflectDirection = Vector2.Reflect(currentDirection, step.hitNormal).normalized;
                if (reflectDirection.sqrMagnitude <= 0.0001f)
                    break;

                if (!step.isRecoveryStep)
                    result.bounceCount += 1;
                currentOrigin = hitPoint + reflectDirection * epsilon;
                currentDirection = reflectDirection;
                result.finalDirection = reflectDirection;
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
                        currentOrigin = hitPoint + reflectDirection * Mathf.Max(epsilon * 2f, GetBallRadius() * 0.18f);
                        recoveryCount = 0;
                        recoveryCollider = null;
                    }
                }
                else
                {
                    ignoredCollider = step.collider;
                    secondaryIgnoredCollider = null;
                    if (step.marker != null && IsReflectiveWall(step.marker.surfaceType))
                        previousWallHit.Set(step.marker.surfaceType, hitPoint, step.hitNormal, step.collider);
                    else
                        previousWallHit.Clear();
                }

                previousCollider = step.collider;
            }

            result.predictedAttackScore = predictedAttack;
            result.predictedShieldGain = predictedShield;
            return result;
        }

        void ApplyPredictedBlockEffect(BoardBlock block, ref int predictedAttack, ref int predictedShield)
        {
            switch (block.blockType)
            {
                case BoardBlockType.AttackAdd:
                    predictedAttack += Mathf.Max(0, Mathf.RoundToInt(block.valueA));
                    break;
                case BoardBlockType.AttackMultiply:
                    if (predictedAttack > 0 && block.valueA > 0f)
                        predictedAttack = Mathf.Max(0, Mathf.RoundToInt(predictedAttack * block.valueA));
                    break;
                case BoardBlockType.Shield:
                    predictedShield += Mathf.Max(0, Mathf.RoundToInt(block.valueA));
                    break;
            }
        }

        static Vector3 ToPoint(Vector2 point)
        {
            return new Vector3(point.x, point.y, 0f);
        }

        float GetBallRadius()
        {
            if (ball != null)
                return Mathf.Max(0.01f, ball.BallRadiusWorld);

            return game != null
                ? Mathf.Max(0.01f, game.config.ball.radius)
                : 0.01f;
        }

        static bool IsReflectiveWall(ArenaSurfaceType surfaceType)
        {
            return surfaceType == ArenaSurfaceType.Left ||
                   surfaceType == ArenaSurfaceType.Right ||
                   surfaceType == ArenaSurfaceType.Top;
        }
    }

    public struct TrajectoryCastStep
    {
        public Collider2D collider;
        public BoardBlock block;
        public ArenaSurfaceMarker marker;
        public Vector2 hitPoint;
        public Vector2 hitNormal;
        public float travelDistance;
        public bool isRecoveryStep;
    }
}
