using UnityEngine;

namespace POPHero
{
    public sealed class BounceStepSolver : IBounceStepSolver
    {
        readonly RaycastHit2D[] castHits = new RaycastHit2D[32];
        readonly Collider2D[] overlapHits = new Collider2D[24];
        readonly PopHeroGame game;
        readonly BallController ball;

        public BounceStepSolver(PopHeroGame owner, BallController ballController)
        {
            game = owner;
            ball = ballController;
        }

        public bool TryCastStep(Vector2 origin, Vector2 direction, float maxDistance, Collider2D ignoredCollider, Collider2D secondaryIgnoredCollider, out TrajectoryCastStep step)
        {
            step = default;
            if (game == null || maxDistance <= 0.0001f)
                return false;

            var safeDirection = direction.sqrMagnitude <= 0.0001f ? Vector2.up : direction.normalized;
            if (TryResolveEmbeddedRecovery(origin, safeDirection, ignoredCollider, secondaryIgnoredCollider, out var embeddedRecovery))
            {
                step = new TrajectoryCastStep
                {
                    collider = embeddedRecovery.ignoredCollider,
                    block = embeddedRecovery.ignoredCollider != null ? embeddedRecovery.ignoredCollider.GetComponent<BoardBlock>() : null,
                    marker = embeddedRecovery.ignoredCollider != null ? embeddedRecovery.ignoredCollider.GetComponent<ArenaSurfaceMarker>() : null,
                    hitPoint = embeddedRecovery.safePoint,
                    hitNormal = embeddedRecovery.recoveryNormal,
                    travelDistance = 0f,
                    isRecoveryStep = true
                };
                return true;
            }

            var hit = FindNearestHit(origin, safeDirection, maxDistance, ignoredCollider, secondaryIgnoredCollider);
            if (hit.collider == null)
                return false;

            var hitPoint = hit.centroid;
            var hitNormal = hit.normal;
            var marker = hit.collider.GetComponent<ArenaSurfaceMarker>();
            if (marker != null && marker.surfaceType != ArenaSurfaceType.Bottom && game.TryGetWallSnap(marker.surfaceType, hitPoint, out var snappedPoint, out var snappedNormal))
            {
                hitPoint = snappedPoint;
                hitNormal = snappedNormal;
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

        public bool TryResolveCornerBounce(WallHitMemory previousWallHit, TrajectoryCastStep step, out CornerBounceResult result)
        {
            result = default;
            if (!previousWallHit.hasValue || step.marker == null)
                return false;

            var currentSurface = step.marker.surfaceType;
            if (!IsReflectiveWall(previousWallHit.surfaceType) || !IsReflectiveWall(currentSurface) || !ArePerpendicularWalls(previousWallHit.surfaceType, currentSurface))
                return false;

            var epsilon = Mathf.Max(0.001f, game.config.ball.previewHitEpsilon);
            var cornerGap = Mathf.Max(epsilon * 2f, game.config.ball.cornerHitGap);
            if (Vector2.Distance(previousWallHit.hitPoint, step.hitPoint) > cornerGap)
                return false;

            var shortTravelThreshold = Mathf.Max(epsilon * 2f, game.config.ball.cornerHitGap * 0.5f);
            if (step.travelDistance > shortTravelThreshold)
                return false;

            var combinedNormal = (previousWallHit.hitNormal + step.hitNormal).normalized;
            if (combinedNormal.sqrMagnitude <= 0.0001f)
                return false;

            result = new CornerBounceResult
            {
                safePoint = GetCornerSafePoint(previousWallHit.surfaceType, currentSurface, step.hitPoint),
                combinedNormal = combinedNormal,
                ignoredColliderA = previousWallHit.collider,
                ignoredColliderB = step.collider
            };
            return true;
        }

        RaycastHit2D FindNearestHit(Vector2 origin, Vector2 direction, float distance, Collider2D ignoredCollider, Collider2D secondaryIgnoredCollider)
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

                if (ball?.BallCollider != null && hit.collider == ball.BallCollider)
                    continue;

                if (ignoredCollider != null && hit.collider == ignoredCollider)
                    continue;

                if (secondaryIgnoredCollider != null && hit.collider == secondaryIgnoredCollider)
                    continue;

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    bestHit = hit;
                }
            }

            return bestHit;
        }

        bool TryResolveEmbeddedRecovery(Vector2 origin, Vector2 direction, Collider2D ignoredCollider, Collider2D secondaryIgnoredCollider, out EmbeddedRecoveryResult result)
        {
            result = default;
            var filter = new ContactFilter2D();
            filter.NoFilter();
            filter.useTriggers = true;
            var overlapCount = Physics2D.OverlapCircle(origin, Mathf.Max(0.01f, GetBallRadius() * 0.95f), filter, overlapHits);
            if (overlapCount <= 0)
                return false;

            var bestMetric = float.MaxValue;
            var found = false;
            for (var index = 0; index < overlapCount; index++)
            {
                var collider = overlapHits[index];
                if (collider == null)
                    continue;

                if (ball?.BallCollider != null && collider == ball.BallCollider)
                    continue;

                if (ignoredCollider != null && collider == ignoredCollider)
                    continue;

                if (secondaryIgnoredCollider != null && collider == secondaryIgnoredCollider)
                    continue;

                var marker = collider.GetComponent<ArenaSurfaceMarker>();
                if (marker != null && marker.surfaceType == ArenaSurfaceType.Bottom)
                    continue;

                if (!TryBuildRecovery(origin, direction, collider, marker, out var safePoint, out var normal, out var metric))
                    continue;

                if (metric >= bestMetric)
                    continue;

                bestMetric = metric;
                result = new EmbeddedRecoveryResult
                {
                    safePoint = safePoint,
                    recoveryNormal = normal,
                    ignoredCollider = collider
                };
                found = true;
            }

            return found;
        }

        bool TryBuildRecovery(Vector2 origin, Vector2 direction, Collider2D collider, ArenaSurfaceMarker marker, out Vector2 safePoint, out Vector2 normal, out float metric)
        {
            var radius = GetBallRadius();
            var padding = Mathf.Max(0.005f, game.config.ball.interiorPushOutPadding);
            safePoint = origin;
            normal = direction.sqrMagnitude <= 0.0001f ? Vector2.up : -direction.normalized;
            metric = float.MaxValue;

            if (marker != null)
            {
                switch (marker.surfaceType)
                {
                    case ArenaSurfaceType.Left:
                        normal = Vector2.right;
                        safePoint = new Vector2(game.BoardRect.xMin + radius + padding, origin.y);
                        metric = Mathf.Abs(origin.x - game.BoardRect.xMin);
                        return true;
                    case ArenaSurfaceType.Right:
                        normal = Vector2.left;
                        safePoint = new Vector2(game.BoardRect.xMax - radius - padding, origin.y);
                        metric = Mathf.Abs(game.BoardRect.xMax - origin.x);
                        return true;
                    case ArenaSurfaceType.Top:
                        normal = Vector2.down;
                        safePoint = new Vector2(origin.x, game.BoardRect.yMax - radius - padding);
                        metric = Mathf.Abs(game.BoardRect.yMax - origin.y);
                        return true;
                }
            }

            var bounds = collider.bounds;
            if (!bounds.Contains(origin))
            {
                var closestPoint = collider.ClosestPoint(origin);
                if (Vector2.Distance(origin, closestPoint) > Mathf.Max(0.005f, game.config.ball.sameColliderMinTravel))
                    return false;
            }

            var center = (Vector2)bounds.center;
            var extents = bounds.extents;
            var local = origin - center;
            var penetrationX = extents.x - Mathf.Abs(local.x);
            var penetrationY = extents.y - Mathf.Abs(local.y);

            if (penetrationX <= penetrationY)
            {
                var sign = Mathf.Sign(Mathf.Approximately(local.x, 0f) ? -direction.x : local.x);
                if (Mathf.Approximately(sign, 0f))
                    sign = 1f;
                normal = new Vector2(sign, 0f);
                var surfaceX = center.x + extents.x * sign;
                safePoint = new Vector2(surfaceX + sign * (radius + padding), origin.y);
                metric = Mathf.Abs(penetrationX);
                return true;
            }

            var ySign = Mathf.Sign(Mathf.Approximately(local.y, 0f) ? -direction.y : local.y);
            if (Mathf.Approximately(ySign, 0f))
                ySign = 1f;
            normal = new Vector2(0f, ySign);
            var surfaceY = center.y + extents.y * ySign;
            safePoint = new Vector2(origin.x, surfaceY + ySign * (radius + padding));
            metric = Mathf.Abs(penetrationY);
            return true;
        }

        float GetBallRadius()
        {
            if (ball != null)
                return Mathf.Max(0.01f, ball.BallRadiusWorld);

            return Mathf.Max(0.01f, game.config.ball.radius);
        }

        static bool IsReflectiveWall(ArenaSurfaceType surfaceType)
        {
            return surfaceType == ArenaSurfaceType.Top || surfaceType == ArenaSurfaceType.Left || surfaceType == ArenaSurfaceType.Right;
        }

        static bool ArePerpendicularWalls(ArenaSurfaceType first, ArenaSurfaceType second)
        {
            return (first == ArenaSurfaceType.Top && (second == ArenaSurfaceType.Left || second == ArenaSurfaceType.Right)) ||
                   (second == ArenaSurfaceType.Top && (first == ArenaSurfaceType.Left || first == ArenaSurfaceType.Right));
        }

        Vector2 GetCornerSafePoint(ArenaSurfaceType first, ArenaSurfaceType second, Vector2 fallbackPoint)
        {
            var radius = GetBallRadius();
            var x = fallbackPoint.x;
            var y = fallbackPoint.y;

            if (first == ArenaSurfaceType.Left || second == ArenaSurfaceType.Left)
                x = game.BoardRect.xMin + radius;
            else if (first == ArenaSurfaceType.Right || second == ArenaSurfaceType.Right)
                x = game.BoardRect.xMax - radius;

            if (first == ArenaSurfaceType.Top || second == ArenaSurfaceType.Top)
                y = game.BoardRect.yMax - radius;

            return new Vector2(x, y);
        }
    }
}
