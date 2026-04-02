using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public class TrajectoryPreviewResult
    {
        public readonly List<Vector3> pathPoints = new();
        public readonly List<BoardBlock> hitBlocks = new();
        public int predictedAttackScore;
        public int predictedShieldGain;
        public bool hitBottom;
        public Vector2 finalDirection = Vector2.up;
        public int bounceCount;

        public bool HasValidPath => pathPoints.Count >= 2;
    }
}
