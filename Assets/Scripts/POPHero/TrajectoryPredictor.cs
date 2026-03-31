using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public class TrajectoryPredictor : MonoBehaviour
    {
        readonly RaycastHit2D[] castHits = new RaycastHit2D[32];

        PopHeroGame game;
        BallController ball;

        public void Initialize(PopHeroGame owner, BallController ballController)
        {
            game = owner;
            ball = ballController;
        }

        public bool TryCastStep(Vector2 origin, Vector2 direction, float maxDistance, out TrajectoryCastStep step)
        {
            step = default;
            if (game == null || maxDistance <= 0.0001f)
                return false;

            var safeDirection = direction.sqrMagnitude <= 0.0001f ? Vector2.up : direction.normalized;
            var epsilon = Mathf.Max(0.001f, game.config.ball.previewHitEpsilon);
            var hit = FindNearestHit(origin, safeDirection, maxDistance, epsilon);
            if (hit.collider == null)
                return false;

            var hitPoint = hit.centroid;
            var hitNormal = hit.normal;
            var marker = hit.collider.GetComponent<ArenaSurfaceMarker>();
            if (marker != null && marker.surfaceType != ArenaSurfaceType.Bottom)
            {
                if (game.TryGetWallSnap(marker.surfaceType, hitPoint, out var snappedPoint, out var snappedNormal))
                {
                    hitPoint = snappedPoint;
                    hitNormal = snappedNormal;
                }
            }

            step = new TrajectoryCastStep
            {
                collider = hit.collider,
                block = hit.collider.GetComponent<BoardBlock>(),
                marker = marker,
                hitPoint = hitPoint,
                hitNormal = hitNormal.sqrMagnitude <= 0.0001f ? -safeDirection : hitNormal.normalized,
                travelDistance = Mathf.Max(0f, Vector2.Distance(origin, hitPoint))
            };
            return true;
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
            var predictedAttack = 0;
            var predictedShield = 0;

            result.pathPoints.Add(ToPoint(origin));
            result.finalDirection = currentDirection;

            for (var bounceIndex = 0; bounceIndex < Mathf.Max(1, maxBounces) && remainingDistance > epsilon; bounceIndex++)
            {
                if (!TryCastStep(currentOrigin, currentDirection, remainingDistance, out var step))
                {
                    result.pathPoints.Add(ToPoint(currentOrigin + currentDirection * remainingDistance));
                    result.finalDirection = currentDirection;
                    break;
                }

                var hitPoint = step.hitPoint;
                if (hasPreviousHitPoint && Vector2.Distance(previousHitPoint, hitPoint) < minHitGap)
                    break;

                result.pathPoints.Add(ToPoint(hitPoint));
                previousHitPoint = hitPoint;
                hasPreviousHitPoint = true;
                remainingDistance -= Mathf.Max(epsilon, step.travelDistance);

                var block = step.block;
                if (block != null)
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

                result.bounceCount += 1;
                currentOrigin = hitPoint + reflectDirection * epsilon;
                currentDirection = reflectDirection;
                result.finalDirection = reflectDirection;
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

        RaycastHit2D FindNearestHit(Vector2 origin, Vector2 direction, float distance, float epsilon)
        {
            var filter = new ContactFilter2D();
            filter.NoFilter();
            filter.useTriggers = true;
            var hitCount = Physics2D.CircleCast(origin, GetBallRadius(), direction, filter, castHits, distance);
            var bestDistance = float.MaxValue;
            RaycastHit2D bestHit = default;

            for (var i = 0; i < hitCount; i++)
            {
                var hit = castHits[i];
                if (hit.collider == null)
                    continue;

                if (ball.BallCollider != null && hit.collider == ball.BallCollider)
                    continue;

                var marker = hit.collider.GetComponent<ArenaSurfaceMarker>();
                var isBottom = marker != null && marker.surfaceType == ArenaSurfaceType.Bottom;
                if (!isBottom && hit.distance < epsilon)
                    continue;

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    bestHit = hit;
                }
            }

            return bestHit;
        }

        float GetBallRadius()
        {
            if (ball != null)
                return Mathf.Max(0.01f, ball.BallRadiusWorld);

            return Mathf.Max(0.01f, game.config.ball.radius);
        }

        static Vector3 ToPoint(Vector2 point)
        {
            return new Vector3(point.x, point.y, 0f);
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
    }
}
