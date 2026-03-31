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
                var hit = FindNearestHit(currentOrigin, currentDirection, remainingDistance, epsilon);
                if (hit.collider == null)
                {
                    result.pathPoints.Add(ToPoint(currentOrigin + currentDirection * remainingDistance));
                    result.finalDirection = currentDirection;
                    break;
                }

                var hitPoint = hit.centroid;
                if (hasPreviousHitPoint && Vector2.Distance(previousHitPoint, hitPoint) < minHitGap)
                    break;

                result.pathPoints.Add(ToPoint(hitPoint));
                previousHitPoint = hitPoint;
                hasPreviousHitPoint = true;
                remainingDistance -= Mathf.Max(epsilon, hit.distance);

                var block = hit.collider.GetComponent<BoardBlock>();
                if (block != null)
                {
                    ApplyPredictedBlockEffect(block, ref predictedAttack, ref predictedShield);
                    if (highlightedBlocks.Add(block))
                        result.hitBlocks.Add(block);
                }

                var marker = hit.collider.GetComponent<ArenaSurfaceMarker>();
                if (marker != null && marker.surfaceType == ArenaSurfaceType.Bottom)
                {
                    result.hitBottom = true;
                    result.finalDirection = currentDirection;
                    break;
                }

                var reflectDirection = Vector2.Reflect(currentDirection, hit.normal).normalized;
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
            var hitCount = Physics2D.CircleCast(origin, game.config.ball.radius * 0.96f, direction, filter, castHits, distance);
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

        static Vector3 ToPoint(Vector2 point)
        {
            return new Vector3(point.x, point.y, 0f);
        }
    }
}
