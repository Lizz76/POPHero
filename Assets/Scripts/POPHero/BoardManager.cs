using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public class BoardManager : MonoBehaviour
    {
        class BlockBlueprint
        {
            public string id;
            public BoardBlockType type;
            public float valueA;
            public float valueB;
        }

        readonly List<BlockBlueprint> blueprints = new();
        readonly List<BoardBlock> activeBlocks = new();

        PopHeroGame game;
        Transform blockRoot;
        PhysicsMaterial2D bounceMaterial;
        int blueprintId;
        int visibleBlockCount;
        GameObject safeZoneMarker;
        Rect currentSafeZone;

        public IReadOnlyList<BoardBlock> ActiveBlocks => activeBlocks;
        public Rect CurrentSafeZone => currentSafeZone;
        public int BlueprintCount => blueprints.Count;
        public int VisibleBlockCount => Mathf.Clamp(visibleBlockCount, 0, blueprints.Count);

        public void Initialize(PopHeroGame owner, Transform runtimeRoot, PhysicsMaterial2D material)
        {
            game = owner;
            blockRoot = runtimeRoot;
            bounceMaterial = material;
            CreateInitialBlueprints();
            ResetBlockProgression();
            BuildSafeZoneMarker();
        }

        public void ShuffleBlocks(Vector2 launchPoint)
        {
            RefreshVisibleBlockCount();
            ClearActiveBlocks();
            currentSafeZone = BuildSafeZone(launchPoint);
            UpdateSafeZoneMarker();

            var positions = BuildCandidatePositions(currentSafeZone);
            Shuffle(positions);

            var count = Mathf.Min(positions.Count, VisibleBlockCount);
            var selectedBlueprints = SelectBlueprintsForCurrentLevel(count);
            for (var i = 0; i < Mathf.Min(count, selectedBlueprints.Count); i++)
                CreateRuntimeBlock(selectedBlueprints[i], positions[i]);

            ClearPreviewState();
        }

        public void AddAttackToAll(int amount)
        {
            foreach (var blueprint in blueprints)
            {
                if (blueprint.type == BoardBlockType.AttackAdd)
                    blueprint.valueA += Mathf.Max(0, amount);
            }
        }

        public void AddShieldToAll(int amount)
        {
            foreach (var blueprint in blueprints)
            {
                if (blueprint.type == BoardBlockType.Shield)
                    blueprint.valueA += Mathf.Max(0, amount);
            }
        }

        public void AddMultiplierToAll(float amount)
        {
            foreach (var blueprint in blueprints)
            {
                if (blueprint.type == BoardBlockType.AttackMultiply)
                    blueprint.valueA += Mathf.Max(0f, amount);
            }
        }

        public void UpgradeRandomAttackBlock(int upgradedValue)
        {
            var candidates = blueprints.FindAll(block => block.type == BoardBlockType.AttackAdd);
            if (candidates.Count == 0)
                return;

            var target = candidates[Random.Range(0, candidates.Count)];
            target.valueA = Mathf.Max(target.valueA, upgradedValue);
        }

        public void UpgradeRandomShieldBlock(int upgradedValue)
        {
            var candidates = blueprints.FindAll(block => block.type == BoardBlockType.Shield);
            if (candidates.Count == 0)
                return;

            var target = candidates[Random.Range(0, candidates.Count)];
            target.valueA = Mathf.Max(target.valueA, upgradedValue);
        }

        public void AddRandomAttackBlock(int attackValue)
        {
            blueprints.Add(new BlockBlueprint
            {
                id = $"Block_{blueprintId++}",
                type = BoardBlockType.AttackAdd,
                valueA = Mathf.Max(1, attackValue),
                valueB = 0f
            });
            RefreshVisibleBlockCount();
        }

        public void ResetBlockProgression()
        {
            RefreshVisibleBlockCount();
        }

        public void AdvanceBlockProgression()
        {
            RefreshVisibleBlockCount();
        }

        public void ApplyPreviewState(IReadOnlyCollection<BoardBlock> hitBlocks)
        {
            if (activeBlocks.Count == 0)
                return;

            if (hitBlocks == null)
            {
                ClearPreviewState();
                return;
            }

            var highlightedIds = new HashSet<int>();
            foreach (var block in hitBlocks)
            {
                if (block != null)
                    highlightedIds.Add(block.GetInstanceID());
            }

            foreach (var block in activeBlocks)
            {
                if (block == null)
                    continue;

                block.SetVisualState(highlightedIds.Contains(block.GetInstanceID())
                    ? BlockVisualState.Highlight
                    : BlockVisualState.Dim);
            }
        }

        public void ClearPreviewState()
        {
            foreach (var block in activeBlocks)
            {
                if (block != null)
                    block.SetVisualState(BlockVisualState.Default);
            }
        }

        void RefreshVisibleBlockCount()
        {
            var availableCount = game?.Player != null ? game.Player.AvailableBlockCount : 1;
            visibleBlockCount = Mathf.Clamp(availableCount, 1, Mathf.Max(1, blueprints.Count));
        }

        void CreateInitialBlueprints()
        {
            blueprints.Clear();
            blueprintId = 0;

            for (var i = 0; i < game.config.board.attackAddCount; i++)
                blueprints.Add(CreateBlueprint(BoardBlockType.AttackAdd));

            for (var i = 0; i < game.config.board.attackMultiplyCount; i++)
                blueprints.Add(CreateBlueprint(BoardBlockType.AttackMultiply));

            for (var i = 0; i < game.config.board.shieldCount; i++)
                blueprints.Add(CreateBlueprint(BoardBlockType.Shield));
        }

        List<BlockBlueprint> SelectBlueprintsForCurrentLevel(int count)
        {
            var selected = new List<BlockBlueprint>();
            if (count <= 0 || blueprints.Count == 0)
                return selected;

            var level = game.Player != null ? game.Player.Level : 0;
            var attacks = GetShuffledBlueprints(BoardBlockType.AttackAdd);
            var shields = GetShuffledBlueprints(BoardBlockType.Shield);
            var multipliers = GetShuffledBlueprints(BoardBlockType.AttackMultiply);

            if (attacks.Count > 0)
                selected.Add(TakeFirst(attacks));

            if (level <= 0)
                return selected;

            var multiplierCount = 0;
            var shieldCount = 0;
            var multiplierLimit = GetMultiplierLimit(level);
            var shieldLimit = GetShieldLimit(level);

            while (selected.Count < count)
            {
                var next = PickWeightedBlueprint(level, attacks, shields, multipliers, shieldCount < shieldLimit, multiplierCount < multiplierLimit)
                    ?? TakeFallbackBlueprint(attacks, shields, multipliers, shieldCount < shieldLimit, multiplierCount < multiplierLimit);
                if (next == null)
                    break;

                selected.Add(next);
                if (next.type == BoardBlockType.Shield)
                    shieldCount += 1;
                else if (next.type == BoardBlockType.AttackMultiply)
                    multiplierCount += 1;
            }

            return selected;
        }

        List<BlockBlueprint> GetShuffledBlueprints(BoardBlockType type)
        {
            var list = blueprints.FindAll(blueprint => blueprint.type == type);
            Shuffle(list);
            return list;
        }

        BlockBlueprint PickWeightedBlueprint(int level, List<BlockBlueprint> attacks, List<BlockBlueprint> shields, List<BlockBlueprint> multipliers, bool allowShield, bool allowMultiplier)
        {
            var attackWeight = attacks.Count > 0 ? 6 : 0;
            var shieldWeight = allowShield && shields.Count > 0 ? 3 : 0;
            var multiplierWeight = allowMultiplier && multipliers.Count > 0
                ? (level >= 7 ? 2 : 1)
                : 0;
            var total = attackWeight + shieldWeight + multiplierWeight;
            if (total <= 0)
                return null;

            var roll = Random.Range(0, total);
            if (roll < attackWeight)
                return TakeFirst(attacks);

            roll -= attackWeight;
            if (roll < shieldWeight)
                return TakeFirst(shields);

            return TakeFirst(multipliers);
        }

        BlockBlueprint TakeFallbackBlueprint(List<BlockBlueprint> attacks, List<BlockBlueprint> shields, List<BlockBlueprint> multipliers, bool allowShield, bool allowMultiplier)
        {
            if (attacks.Count > 0)
                return TakeFirst(attacks);
            if (allowShield && shields.Count > 0)
                return TakeFirst(shields);
            if (allowMultiplier && multipliers.Count > 0)
                return TakeFirst(multipliers);
            if (shields.Count > 0)
                return TakeFirst(shields);
            if (multipliers.Count > 0)
                return TakeFirst(multipliers);
            return null;
        }

        static BlockBlueprint TakeFirst(List<BlockBlueprint> list)
        {
            if (list == null || list.Count == 0)
                return null;

            var item = list[0];
            list.RemoveAt(0);
            return item;
        }

        static int GetShieldLimit(int level)
        {
            if (level <= 0)
                return 0;
            if (level <= 2)
                return 1;
            if (level <= 5)
                return 2;
            return 3;
        }

        static int GetMultiplierLimit(int level)
        {
            if (level <= 2)
                return 0;
            if (level <= 5)
                return 1;
            if (level <= 8)
                return 2;
            return 3;
        }

        BlockBlueprint CreateBlueprint(BoardBlockType type)
        {
            return new BlockBlueprint
            {
                id = $"Block_{blueprintId++}",
                type = type,
                valueA = type switch
                {
                    BoardBlockType.AttackAdd => RandomFrom(game.config.board.attackAddValues),
                    BoardBlockType.AttackMultiply => RandomFrom(game.config.board.attackMultiplyValues),
                    _ => RandomFrom(game.config.board.shieldValues)
                },
                valueB = 0f
            };
        }

        void CreateRuntimeBlock(BlockBlueprint blueprint, Vector2 position)
        {
            var go = new GameObject(blueprint.id);
            go.transform.SetParent(blockRoot, false);

            BoardBlock block = blueprint.type switch
            {
                BoardBlockType.AttackAdd => go.AddComponent<AttackAddBlock>(),
                BoardBlockType.AttackMultiply => go.AddComponent<AttackMultiplyBlock>(),
                _ => go.AddComponent<ShieldBlock>()
            };

            block.Initialize(
                game,
                blueprint.id,
                blueprint.type,
                position,
                game.config.board.blockSize,
                blueprint.valueA,
                blueprint.valueB,
                GetRandomRotation(),
                game.config.board.keepLabelUpright,
                GetColor(blueprint.type),
                bounceMaterial);
            activeBlocks.Add(block);
        }

        Rect BuildSafeZone(Vector2 launchPoint)
        {
            var board = game.BoardRect;
            var width = game.config.board.launchSafeWidth;
            var height = game.config.board.launchSafeHeight;
            var minX = Mathf.Clamp(launchPoint.x - width * 0.5f, board.xMin, board.xMax - width);
            return new Rect(minX, board.yMin + 0.2f, width, height);
        }

        List<Vector2> BuildCandidatePositions(Rect safeZone)
        {
            var positions = new List<Vector2>();
            var board = game.BoardRect;
            var size = game.config.board.blockSize;
            var halfSize = size * 0.5f;
            var minX = board.xMin + game.config.board.sidePadding + halfSize.x;
            var maxX = board.xMax - game.config.board.sidePadding - halfSize.x;
            var minY = board.yMin + game.config.board.bottomPadding + halfSize.y;
            var maxY = board.yMax - game.config.board.topPadding - halfSize.y;

            var spacingX = size.x + 0.45f;
            var spacingY = size.y + 0.5f;
            var columns = Mathf.Max(4, Mathf.FloorToInt((maxX - minX) / spacingX) + 1);
            var rows = Mathf.Max(4, Mathf.FloorToInt((maxY - minY) / spacingY) + 1);

            for (var y = 0; y < rows; y++)
            {
                var tY = rows == 1 ? 0.5f : y / (rows - 1f);
                var posY = Mathf.Lerp(minY, maxY, tY);
                for (var x = 0; x < columns; x++)
                {
                    var tX = columns == 1 ? 0.5f : x / (columns - 1f);
                    var posX = Mathf.Lerp(minX, maxX, tX);
                    var point = new Vector2(posX, posY);
                    if (point.x > safeZone.xMin - halfSize.x &&
                        point.x < safeZone.xMax + halfSize.x &&
                        point.y > safeZone.yMin - halfSize.y &&
                        point.y < safeZone.yMax + halfSize.y)
                        continue;

                    positions.Add(point);
                }
            }

            return positions;
        }

        void BuildSafeZoneMarker()
        {
            safeZoneMarker = PrototypeVisualFactory.CreateSpriteObject("LaunchSafeZone", transform, PrototypeVisualFactory.SquareSprite, game.config.arena.safeZoneColor, 4, Vector2.one);
            safeZoneMarker.SetActive(game.config.debug.showSpawnSafeZone);
        }

        void UpdateSafeZoneMarker()
        {
            if (safeZoneMarker == null)
                return;

            safeZoneMarker.SetActive(game.config.debug.showSpawnSafeZone);
            safeZoneMarker.transform.position = currentSafeZone.center;
            safeZoneMarker.transform.localScale = new Vector3(currentSafeZone.width, currentSafeZone.height, 1f);
        }

        void ClearActiveBlocks()
        {
            foreach (var block in activeBlocks)
            {
                if (block != null)
                    Destroy(block.gameObject);
            }

            activeBlocks.Clear();
        }

        Color GetColor(BoardBlockType type)
        {
            return type switch
            {
                BoardBlockType.AttackAdd => game.config.board.attackAddColor,
                BoardBlockType.AttackMultiply => game.config.board.attackMultiplyColor,
                _ => game.config.board.shieldColor
            };
        }

        static void Shuffle<T>(IList<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var swapIndex = Random.Range(0, i + 1);
                (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
            }
        }

        static float RandomFrom(IReadOnlyList<int> values)
        {
            if (values == null || values.Count == 0)
                return 1f;
            return values[Random.Range(0, values.Count)];
        }

        static float RandomFrom(IReadOnlyList<float> values)
        {
            if (values == null || values.Count == 0)
                return 1f;
            return values[Random.Range(0, values.Count)];
        }

        float GetRandomRotation()
        {
            var min = game.config.board.minRotationAngle;
            var max = game.config.board.maxRotationAngle;
            var step = Mathf.Max(0.001f, game.config.board.rotationStep);
            if (Mathf.Approximately(min, max))
                return min;

            var randomAngle = Random.Range(min, max);
            return Mathf.Round(randomAngle / step) * step;
        }
    }
}
