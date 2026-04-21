using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public class TrajectoryPredictor : MonoBehaviour
    {
        PopHeroGame game;
        BallController ball;
        BallFlightSimulator flightSimulator;

        public void Initialize(PopHeroGame owner, BallController ballController)
        {
            game = owner;
            ball = ballController;
            flightSimulator = new BallFlightSimulator(owner, ballController);
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
            var predictedAttack = 0;
            var predictedShield = 0;
            var previewDistanceBudget = GetPreviewDistanceBudget(maxDistance);
            var previewBounceBudget = GetPreviewBounceBudget(maxBounces, previewDistanceBudget);
            var simulationState = BallFlightState.Create(origin, direction, game.config.ball.speed);
            var simulationResult = flightSimulator.Simulate(simulationState, new BallFlightRunOptions
            {
                // Preview should try to cover the full legal flight, not stop at an arbitrary
                // visual cap and mislead the live simulation.
                distanceBudget = previewDistanceBudget,
                maxTotalDistance = previewDistanceBudget,
                maxDuration = Mathf.Max(0.1f, game.config.ball.maxFlightDuration),
                maxBounces = previewBounceBudget,
                maxSteps = Mathf.Max(128, previewBounceBudget * 6),
                includeStartPoint = true
            });

            result.pathPoints.AddRange(simulationResult.pathPoints);
            result.hitBottom = simulationResult.hitBottom;
            result.finalDirection = simulationResult.finalDirection;
            result.bounceCount = simulationResult.bounceCount;

            foreach (var flightEvent in simulationResult.events)
            {
                if (flightEvent.eventType != BallFlightEventType.BlockHit || flightEvent.block == null)
                    continue;

                ApplyPredictedBlockEffect(flightEvent.block, ref predictedAttack, ref predictedShield);
                result.hitBlocks.Add(flightEvent.block);
            }

            result.predictedAttackScore = predictedAttack;
            result.predictedShieldGain = predictedShield;
            return result;
        }

        float GetPreviewDistanceBudget(float requestedMaxDistance)
        {
            var configuredBudget = Mathf.Max(1f, requestedMaxDistance);
            if (game == null || game.config == null)
                return configuredBudget;

            var fullFlightBudget = Mathf.Max(game.config.ball.speed, game.config.ball.maxSpeed) *
                                   Mathf.Max(0.1f, game.config.ball.maxFlightDuration);
            return Mathf.Max(configuredBudget, fullFlightBudget);
        }

        int GetPreviewBounceBudget(int requestedMaxBounces, float distanceBudget)
        {
            var configuredBudget = Mathf.Max(1, requestedMaxBounces);
            if (game == null)
                return configuredBudget;

            var arenaSpan = Mathf.Max(1f, Mathf.Max(game.BoardRect.width, game.BoardRect.height));
            var adaptiveBudget = Mathf.CeilToInt(distanceBudget / arenaSpan) * 4;
            return Mathf.Max(configuredBudget, 64, adaptiveBudget);
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
