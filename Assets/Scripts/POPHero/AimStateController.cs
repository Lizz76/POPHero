using UnityEngine;

namespace POPHero
{
    public class AimLockContext
    {
        public bool throwReady;
        public bool hasLockedAim;
        public bool aimLockedInput;
        public Vector2 lockCursor;
        public float lastAimAngle;
        public Vector2 lockedAimDirection = Vector2.up;
        public TrajectoryPreviewResult lockedPreview;
        public TrajectoryPreviewResult previousPreview;
    }

    public static class TrajectoryRecalcGate
    {
        public static bool ShouldRecalculate(
            Vector2 lockCursor,
            Vector2 cursorPos,
            float lastAimAngle,
            float currentAngle,
            float recalcDistanceThreshold,
            float recalcAngleThreshold)
        {
            return Vector2.Distance(lockCursor, cursorPos) >= recalcDistanceThreshold ||
                   Mathf.Abs(Mathf.DeltaAngle(lastAimAngle, currentAngle)) >= recalcAngleThreshold;
        }

        public static bool ShouldHold(
            Vector2 lockCursor,
            Vector2 cursorPos,
            float lastAimAngle,
            float currentAngle,
            float holdDistanceThreshold,
            float holdAngleThreshold)
        {
            return Vector2.Distance(lockCursor, cursorPos) <= holdDistanceThreshold &&
                   Mathf.Abs(Mathf.DeltaAngle(lastAimAngle, currentAngle)) <= holdAngleThreshold;
        }
    }

    public class AimStateController
    {
        readonly AimLockContext context = new();

        PopHeroGame game;
        TrajectoryPredictor trajectoryPredictor;

        public AimLockContext Context => context;

        public void Initialize(PopHeroGame owner, TrajectoryPredictor predictor)
        {
            game = owner;
            trajectoryPredictor = predictor;
            Reset();
        }

        public void Reset()
        {
            context.throwReady = false;
            context.hasLockedAim = false;
            context.aimLockedInput = false;
            context.lockCursor = Vector2.zero;
            context.lastAimAngle = 90f;
            context.lockedAimDirection = Vector2.up;
            context.lockedPreview = null;
            context.previousPreview = null;
        }

        public bool BeginInput(Vector2 cursorWorld)
        {
            context.aimLockedInput = true;
            return UpdateLockedAim(cursorWorld, true);
        }

        public void EndInput()
        {
            context.aimLockedInput = false;
        }

        public bool UpdateLockedAim(Vector2 cursorWorld, bool forceAccept)
        {
            if (game == null || trajectoryPredictor == null)
                return false;

            var origin = game.CurrentLaunchPoint;
            var candidateDirection = ClampAimDirection(cursorWorld - origin);
            if (candidateDirection.sqrMagnitude <= 0.0001f)
                return false;

            var candidateAngle = Mathf.Atan2(candidateDirection.y, candidateDirection.x) * Mathf.Rad2Deg;
            if (candidateAngle < 0f)
                candidateAngle += 360f;

            if (!context.hasLockedAim)
                return AcceptCandidate(cursorWorld, candidateDirection, candidateAngle);

            if (!forceAccept)
            {
                if (TrajectoryRecalcGate.ShouldHold(
                        context.lockCursor,
                        cursorWorld,
                        context.lastAimAngle,
                        candidateAngle,
                        game.GetAimHoldDistanceThreshold(),
                        game.GetAimHoldAngleThreshold()))
                {
                    return context.throwReady;
                }

                if (!TrajectoryRecalcGate.ShouldRecalculate(
                        context.lockCursor,
                        cursorWorld,
                        context.lastAimAngle,
                        candidateAngle,
                        game.GetAimRecalcDistanceThreshold(),
                        game.GetAimRecalcAngleThreshold()))
                {
                    return context.throwReady;
                }
            }

            return AcceptCandidate(cursorWorld, candidateDirection, candidateAngle);
        }

        bool AcceptCandidate(Vector2 cursorWorld, Vector2 candidateDirection, float candidateAngle)
        {
            var preview = trajectoryPredictor.BuildPreview(
                game.CurrentLaunchPoint,
                candidateDirection,
                game.config.ball.previewSegments,
                game.config.ball.previewDistance);

            if (preview == null || !preview.HasValidPath)
            {
                context.throwReady = false;
                return false;
            }

            context.previousPreview = context.lockedPreview;
            context.lockCursor = cursorWorld;
            context.lastAimAngle = candidateAngle;
            context.lockedAimDirection = candidateDirection;
            context.lockedPreview = preview;
            context.throwReady = true;
            context.hasLockedAim = true;
            return true;
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
