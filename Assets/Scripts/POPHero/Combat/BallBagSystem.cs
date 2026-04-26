using System;
using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public static class DefaultBallCatalog
    {
        public const string Normal = "normal";
        public const string Attack = "attack";
        public const string Defense = "defense";
        public const string Multiplier = "multiplier";
        public const string Lightning = "lightning";
        public const string Burst = "burst";
        public const string Pierce = "pierce";

        public static List<BallDefinition> CreateDefaults()
        {
            return new List<BallDefinition>
            {
                Create(Normal, "普通弹球", BlockRarity.White, "攻击、防御、倍率收益均为 x1.0。", 1f, 1f, 1f, true, true, BallSpecialType.None),
                Create(Attack, "攻击弹球", BlockRarity.Blue, "攻击方块收益 x1.5，防御方块收益 x0.8。", 1.5f, 0.8f, 1f, true, true, BallSpecialType.None),
                Create(Defense, "防御弹球", BlockRarity.Blue, "防御方块收益 x1.7，攻击方块收益 x0.8。", 0.8f, 1.7f, 1f, true, true, BallSpecialType.None),
                Create(Multiplier, "倍率弹球", BlockRarity.Blue, "倍率方块收益 x1.8，攻击方块收益 x0.7。", 0.7f, 1f, 1.8f, true, true, BallSpecialType.None),
                Create(Lightning, "雷电弹球", BlockRarity.Purple, "单次收益 x0.9。命中时有概率连锁触发附近方块。", 0.9f, 0.9f, 0.9f, false, true, BallSpecialType.Lightning, 0.3f, 1f, 0.5f, 3f),
                Create(Burst, "爆裂弹球", BlockRarity.Purple, "防御收益 x0.8。发射结束时按命中次数追加伤害。", 1f, 0.8f, 1f, false, true, BallSpecialType.Burst, 5f, 5f, 2f, 0f),
                Create(Pierce, "穿透弹球", BlockRarity.Purple, "单次收益 x0.9。前 3 次命中穿透并以 80% 收益触发。", 0.9f, 0.9f, 0.9f, true, true, BallSpecialType.Pierce, 3f, 0.8f, 0f, 0f)
            };
        }

        static BallDefinition Create(
            string id,
            string displayName,
            BlockRarity rarity,
            string description,
            float attackMultiplier,
            float shieldMultiplier,
            float multiplierMultiplier,
            bool isInitial,
            bool isBattleReward,
            BallSpecialType specialType,
            float valueA = 0f,
            float valueB = 0f,
            float valueC = 0f,
            float valueD = 0f)
        {
            return new BallDefinition
            {
                id = id,
                displayName = displayName,
                rarity = rarity,
                description = description,
                attackMultiplier = attackMultiplier,
                shieldMultiplier = shieldMultiplier,
                multiplierMultiplier = multiplierMultiplier,
                isInitial = isInitial,
                isBattleReward = isBattleReward,
                isShop = false,
                specialType = specialType,
                valueA = valueA,
                valueB = valueB,
                valueC = valueC,
                valueD = valueD
            };
        }
    }

    public sealed class BallBagManager
    {
        readonly PlayerBallCollection playerBalls = new();
        readonly BallBagState bagState = new();
        readonly List<BallDefinition> initialFallbacks = new();
        PopHeroGame game;
        int serial;

        public PlayerBallCollection PlayerBalls => playerBalls;
        public BallBagState State => bagState;
        public PlayerBallInstance CurrentBall => bagState.currentBall;
        public BallDefinition CurrentDefinition => bagState.currentBall?.definition;
        public BallDefinition ActiveRoundDefinition => bagState.activeRoundBall?.definition ?? CurrentDefinition;
        public int DrawPileCount => bagState.drawPile.Count;
        public int UsedPileCount => bagState.usedPile.Count;
        public bool CanDiscard => (game == null || game.State == RoundState.Aim) && bagState.currentBall != null && bagState.discardsRemaining > 0;

        public void Initialize(PopHeroGame owner)
        {
            game = owner;
            serial = 0;
            playerBalls.Clear();
            bagState.Clear();
            initialFallbacks.Clear();
            initialFallbacks.AddRange(DefaultBallCatalog.CreateDefaults());
            GrantInitialBalls();
            StartBattleBag();
        }

        public void StartBattleBag()
        {
            bagState.Clear();
            bagState.drawPile.AddRange(playerBalls.Balls);
            Shuffle(bagState.drawPile);
            DrawNextBall();
            bagState.discardsRemaining = 1;
        }

        public void BeginActionWindow()
        {
            EnsureCurrentBall();
            bagState.discardsRemaining = 1;
        }

        public void BeginRound()
        {
            EnsureCurrentBall();
            bagState.activeRoundBall = bagState.currentBall;
        }

        public void FinishRoundAndDrawNext()
        {
            if (bagState.currentBall != null)
                bagState.usedPile.Add(bagState.currentBall);

            bagState.currentBall = null;
            bagState.activeRoundBall = null;
            DrawNextBall();
            bagState.discardsRemaining = 1;
        }

        public bool TryDiscardCurrent(out string message)
        {
            message = string.Empty;
            if (!CanDiscard)
            {
                message = "当前不能弃球。";
                return false;
            }

            var discarded = bagState.currentBall;
            bagState.usedPile.Add(discarded);
            bagState.currentBall = null;
            bagState.discardsRemaining = 0;
            DrawNextBall();
            message = discarded?.definition != null ? $"已弃掉 {discarded.definition.displayName}。" : "已弃掉当前弹球。";
            return true;
        }

        public PlayerBallInstance AddBall(BallDefinition definition)
        {
            return playerBalls.Add(definition, serial++);
        }

        public void ForceCurrentForTests(BallDefinition definition)
        {
            var instance = AddBall(definition);
            bagState.currentBall = instance;
            bagState.discardsRemaining = 1;
        }

        void GrantInitialBalls()
        {
            AddInitial(DefaultBallCatalog.Normal);
            AddInitial(DefaultBallCatalog.Normal);
            AddInitial(DefaultBallCatalog.Attack);
            AddInitial(DefaultBallCatalog.Defense);
            AddInitial(DefaultBallCatalog.Multiplier);
            AddInitial(DefaultBallCatalog.Pierce);
        }

        void AddInitial(string id)
        {
            var definition = FindDefinition(id) ?? initialFallbacks.Find(ball => string.Equals(ball.id, id, StringComparison.OrdinalIgnoreCase));
            if (definition != null)
                AddBall(definition);
        }

        BallDefinition FindDefinition(string id)
        {
            if (game?.Tables != null && game.Tables.GetBallDef(id) is { } configured)
                return configured;

            return null;
        }

        void EnsureCurrentBall()
        {
            if (bagState.currentBall != null)
                return;

            DrawNextBall();
        }

        void DrawNextBall()
        {
            if (bagState.drawPile.Count == 0 && bagState.usedPile.Count > 0)
            {
                bagState.drawPile.AddRange(bagState.usedPile);
                bagState.usedPile.Clear();
                Shuffle(bagState.drawPile);
            }

            if (bagState.drawPile.Count == 0)
                return;

            var lastIndex = bagState.drawPile.Count - 1;
            bagState.currentBall = bagState.drawPile[lastIndex];
            bagState.drawPile.RemoveAt(lastIndex);
        }

        static void Shuffle<T>(IList<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }

    public sealed class BallRewardService
    {
        readonly List<BallRewardOption> activeOptions = new();
        PopHeroGame game;

        public IReadOnlyList<BallRewardOption> ActiveOptions => activeOptions;

        public void Initialize(PopHeroGame owner)
        {
            game = owner;
            activeOptions.Clear();
        }

        public void GenerateOptions(int count)
        {
            activeOptions.Clear();
            var source = game?.Tables?.GetBattleRewardBallDefs();
            var pool = source != null ? new List<BallDefinition>(source) : DefaultBallCatalog.CreateDefaults();
            if (pool.Count == 0)
                pool.AddRange(DefaultBallCatalog.CreateDefaults());

            Shuffle(pool);
            var optionCount = Mathf.Max(1, count);
            for (var index = 0; index < optionCount; index++)
            {
                var definition = pool[index % pool.Count];
                activeOptions.Add(new BallRewardOption
                {
                    index = index,
                    definition = definition,
                    displayName = definition.displayName,
                    description = definition.description,
                    rarityText = FormatRarity(definition.rarity),
                    color = RarityColor(definition.rarity)
                });
            }
        }

        public bool TryClaimOption(int index, out PlayerBallInstance addedBall, out string failReason)
        {
            addedBall = null;
            failReason = string.Empty;
            if (game?.BallBag == null)
            {
                failReason = "弹球袋尚未初始化。";
                return false;
            }

            if (index < 0 || index >= activeOptions.Count)
            {
                failReason = "弹球奖励索引无效。";
                return false;
            }

            addedBall = game.BallBag.AddBall(activeOptions[index].definition);
            activeOptions.Clear();
            return addedBall != null;
        }

        public void Clear()
        {
            activeOptions.Clear();
        }

        static void Shuffle<T>(IList<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        static string FormatRarity(BlockRarity rarity)
        {
            return rarity switch
            {
                BlockRarity.White => "普通",
                BlockRarity.Blue => "精良",
                BlockRarity.Purple => "稀有",
                BlockRarity.Gold => "传说",
                _ => rarity.ToString()
            };
        }

        static Color RarityColor(BlockRarity rarity)
        {
            return rarity switch
            {
                BlockRarity.White => new Color(0.94f, 0.96f, 1f, 1f),
                BlockRarity.Blue => new Color(0.42f, 0.72f, 1f, 1f),
                BlockRarity.Purple => new Color(0.78f, 0.46f, 1f, 1f),
                BlockRarity.Gold => new Color(1f, 0.82f, 0.34f, 1f),
                _ => Color.white
            };
        }
    }

    public static class BallEffectCalculator
    {
        public static int ScaleAdditive(float value, BallDefinition ball, BoardBlockType blockType, float extraMultiplier = 1f)
        {
            return Mathf.Max(0, Mathf.RoundToInt(value * GetBlockMultiplier(ball, blockType) * Mathf.Max(0f, extraMultiplier)));
        }

        public static float ScaleMultiplier(float value, BallDefinition ball, float extraMultiplier = 1f)
        {
            var scaled = 1f + (value - 1f) * GetBlockMultiplier(ball, BoardBlockType.AttackMultiply) * Mathf.Max(0f, extraMultiplier);
            return Mathf.Max(0f, scaled);
        }

        public static int BurstDamage(BallDefinition ball, int hitCount)
        {
            if (ball == null || ball.specialType != BallSpecialType.Burst)
                return 0;

            var baseDamage = Mathf.RoundToInt(ball.valueA <= 0f ? 5f : ball.valueA);
            var hitsPerStep = Mathf.Max(1, Mathf.RoundToInt(ball.valueB <= 0f ? 5f : ball.valueB));
            var damagePerStep = Mathf.RoundToInt(ball.valueC <= 0f ? 2f : ball.valueC);
            return Mathf.Max(0, baseDamage + Mathf.FloorToInt(Mathf.Max(0, hitCount) / (float)hitsPerStep) * damagePerStep);
        }

        public static int PierceCount(BallDefinition ball)
        {
            return ball != null && ball.specialType == BallSpecialType.Pierce ? Mathf.Max(0, Mathf.RoundToInt(ball.valueA <= 0f ? 3f : ball.valueA)) : 0;
        }

        public static float PierceYieldMultiplier(BallDefinition ball)
        {
            return ball != null && ball.specialType == BallSpecialType.Pierce ? Mathf.Max(0f, ball.valueB <= 0f ? 0.8f : ball.valueB) : 1f;
        }

        static float GetBlockMultiplier(BallDefinition ball, BoardBlockType blockType)
        {
            if (ball == null)
                return 1f;

            return blockType switch
            {
                BoardBlockType.AttackAdd => Mathf.Max(0f, ball.attackMultiplier),
                BoardBlockType.Shield => Mathf.Max(0f, ball.shieldMultiplier),
                BoardBlockType.AttackMultiply => Mathf.Max(0f, ball.multiplierMultiplier),
                _ => 1f
            };
        }
    }
}
