using System;
using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    [CreateAssetMenu(fileName = "PopHeroPrototypeConfig", menuName = "POPHero/Prototype Config")]
    public class PopHeroPrototypeConfig : ScriptableObject
    {
        public ArenaSettings arena = new();
        public BallSettings ball = new();
        public AimSettings aim = new();
        public PlayerSettings player = new();
        public BoardSettings board = new();
        public BlockRewardSettings blockRewards = new();
        public StickerSettings stickers = new();
        public ModSettings mods = new();
        public ShopSettings shop = new();
        public IntermissionSettings intermission = new();
        public EnemyProgressionSettings enemies = new();
        public DebugSettings debug = new();

        public static PopHeroPrototypeConfig CreateRuntimeDefault()
        {
            var config = CreateInstance<PopHeroPrototypeConfig>();
            config.name = "Runtime POPHero Config";
            return config;
        }
    }

    [Serializable]
    public class ArenaSettings
    {
        public Vector2 boardCenter = new(0f, -1.6f);
        public Vector2 boardSize = new(10.6f, 10.8f);
        public float wallThickness = 0.45f;
        public float wallStoneUnitLength = 0.62f;
        public int wallPointSubdivisions = 3;
        public float wallStoneVisualGap = 0.08f;
        public float wallStoneColliderOverlap = 0.04f;
        public float wallStoneColorVariance = 0.18f;
        public float topPanelHeight = 3f;
        public float launchLineOffset = 0.42f;
        public float bottomTriggerHeight = 0.3f;
        public float cameraSize = 8.9f;
        public Color backgroundColor = new(0.08f, 0.08f, 0.08f, 1f);
        public Color topPanelColor = new(0.16f, 0.09f, 0.09f, 1f);
        public Color boardBackgroundColor = new(0.05f, 0.05f, 0.06f, 1f);
        public Color boardFrameColor = new(0.44f, 0.27f, 0.13f, 1f);
        public Color wallColor = new(0.71f, 0.47f, 0.24f, 1f);
        public Color launchGuideColor = new(0.2f, 0.85f, 0.45f, 0.28f);
        public Color safeZoneColor = new(0.18f, 0.55f, 0.88f, 0.16f);
        public Color enemyPanelAccent = new(0.72f, 0.07f, 0.17f, 1f);
    }

    [Serializable]
    public class BallSettings
    {
        public float radius = 0.15f;
        public float speed = 12f;
        public float accelerationPerBounce = 1.5f;
        public float maxSpeed = 24f;
        public float maxFlightDuration = 18f;
        public float outOfBoundsRecoveryPadding = 0.8f;
        public float minAimAngle = 18f;
        public float maxAimAngle = 162f;
        public int previewSegments = 30;
        public float previewDistance = 100f;
        public float previewHitEpsilon = 0.01f;
        public float previewMinHitGap = 0.02f;
        public float previewLineStartWidth = 0.08f;
        public float previewLineEndWidth = 0.05f;
        public float hitCooldown = 0.05f;
        public float baseTrailTime = 0.14f;
        public float boostTrailTime = 0.26f;
        public float boostDuration = 0.18f;
        public float baseTrailWidth = 0.14f;
        public float boostTrailWidth = 0.24f;
        public Color color = new(0.92f, 0.95f, 1f, 1f);
        public Color previewColor = new(0.04f, 1f, 0.2f, 0.92f);
    }

    [Serializable]
    public class AimSettings
    {
        public InputAimMode currentAimMode = InputAimMode.PCMouseAimClick;
        public float dragStartRadius = 0.95f;
        public float inputLockStartRadius = 0.95f;
        public float inputRecalcDistance = 0.34f;
        public float inputReleaseDistance = 0.18f;
        public float inputRecalcAngle = 2.8f;
        public float inputHoldAngle = 1.25f;
        public float aimAssistBonus = 0.2f;
        public bool showTrajectoryMemory = false;
        public float wallAimSnapFactor = 0.45f;
        public float wallAimReleaseFactor = 0.9f;
    }

    [Serializable]
    public class PlayerSettings
    {
        public int maxHp = 100;
        public int currentHp = 100;
        public int startShield = 0;
        public int startGold = 0;
    }

    [Serializable]
    public class BoardSettings
    {
        public Vector2 blockSize = new(1.18f, 1.18f);
        public int attackAddCount = 6;
        public int attackMultiplyCount = 2;
        public int shieldCount = 3;
        public int startingVisibleBlockCount = 1;
        public int visibleBlockIncreasePerRound = 0;
        public float minRotationAngle = -45f;
        public float maxRotationAngle = 45f;
        public float rotationStep = 15f;
        public bool keepLabelUpright = true;
        public List<int> attackAddValues = new() { 8, 10, 12, 15 };
        public List<float> attackMultiplyValues = new() { 1.2f, 1.4f, 1.6f };
        public List<int> shieldValues = new() { 5, 8, 10 };
        public float sidePadding = 0.85f;
        public float topPadding = 1.25f;
        public float bottomPadding = 2.1f;
        public float launchSafeWidth = 2.8f;
        public float launchSafeHeight = 1.8f;
        public int perBlockPlacementTries = 50;
        public int shuffleRetryCount = 8;
        public Color attackAddColor = new(0.38f, 0.18f, 0.86f, 1f);
        public Color attackMultiplyColor = new(0.2f, 0.52f, 0.95f, 1f);
        public Color shieldColor = new(0.16f, 0.75f, 0.34f, 1f);
        public Color labelColor = new(0.97f, 0.99f, 1f, 1f);
    }

    [Serializable]
    public class StickerSettings
    {
        public int baseInventoryCapacity = 6;
        public int defaultSocketsPerCard = 2;
        public int unlockedSocketsPerCard = 1;
        public int maxSocketsPerCard = 3;
    }

    [Serializable]
    public class ModSettings
    {
        public int modUseCap = 6;
    }

    [Serializable]
    public class ShopSettings
    {
        public int stickerSkipMoney = 6;
        public int stickerRerollMoney = 4;
        public int shopRerollMoney = 3;
        public int blockRemovalCost = 18;
    }

    [Serializable]
    public class IntermissionSettings
    {
        public int rewardChoiceCount = 3;
        public int shopStickerSlots = 3;
        public int shopModSlots = 1;
        public int shopGrowthSlots = 1;
    }

    [Serializable]
    public class BlockRewardSettings
    {
        public int initialChoiceCount = 3;
        public int rewardChoiceCount = 3;
        public int maxActiveBlocks = 10;
        public int maxReserveBlocks = 3;
        public List<RarityOddsStage> rarityOdds = new()
        {
            new RarityOddsStage { minimumKills = 0, white = 75, blue = 20, purple = 5, gold = 0 },
            new RarityOddsStage { minimumKills = 3, white = 55, blue = 30, purple = 12, gold = 3 },
            new RarityOddsStage { minimumKills = 6, white = 40, blue = 35, purple = 18, gold = 7 },
            new RarityOddsStage { minimumKills = 10, white = 28, blue = 35, purple = 25, gold = 12 },
            new RarityOddsStage { minimumKills = 15, white = 20, blue = 32, purple = 30, gold = 18 }
        };

        public RarityValueTable attackValues = new() { white = 8f, blue = 14f, purple = 22f, gold = 34f };
        public RarityValueTable shieldValues = new() { white = 6f, blue = 11f, purple = 18f, gold = 28f };
        public RarityValueTable multiplierValues = new() { white = 1.2f, blue = 1.5f, purple = 1.9f, gold = 2.5f };
    }

    [Serializable]
    public class RarityOddsStage
    {
        public int minimumKills;
        public float white;
        public float blue;
        public float purple;
        public float gold;
    }

    [Serializable]
    public class RarityValueTable
    {
        public float white;
        public float blue;
        public float purple;
        public float gold;

        public float Get(BlockRarity rarity)
        {
            return rarity switch
            {
                BlockRarity.White => white,
                BlockRarity.Blue => blue,
                BlockRarity.Purple => purple,
                BlockRarity.Gold => gold,
                _ => white
            };
        }
    }

    [Serializable]
    public class EnemyProgressionSettings
    {
        public List<EnemyTemplate> templates = new()
        {
            new EnemyTemplate { displayName = "苔石偶像", maxHp = 80, rewardGold = 20, rewardHeal = 8, attackDamage = 8, color = new Color(0.95f, 0.38f, 0.38f, 1f) },
            new EnemyTemplate { displayName = "铁皮信使", maxHp = 140, rewardGold = 30, rewardHeal = 10, attackDamage = 12, color = new Color(0.99f, 0.52f, 0.25f, 1f) },
            new EnemyTemplate { displayName = "尖刺图腾", maxHp = 220, rewardGold = 45, rewardHeal = 12, attackDamage = 18, color = new Color(0.95f, 0.74f, 0.24f, 1f) },
            new EnemyTemplate { displayName = "狂战祭司", maxHp = 320, rewardGold = 60, rewardHeal = 15, attackDamage = 25, color = new Color(0.9f, 0.37f, 0.79f, 1f) },
            new EnemyTemplate { displayName = "深渊领主", maxHp = 450, rewardGold = 80, rewardHeal = 18, attackDamage = 35, color = new Color(0.52f, 0.48f, 1f, 1f) }
        };

        public int endlessHpGrowth = 90;
        public int endlessGoldGrowth = 14;
        public int endlessHealGrowth = 3;
        public int endlessAttackGrowth = 5;
        public int maxLaunchesPerEnemy = 6;
    }

    [Serializable]
    public class EnemyTemplate
    {
        public string displayName = "敌人";
        public int maxHp = 80;
        public int rewardGold = 20;
        public int rewardHeal = 8;
        public int attackDamage = 8;
        public Color color = new(0.95f, 0.38f, 0.38f, 1f);
    }

    [Serializable]
    public class DebugSettings
    {
        public bool showSpawnSafeZone = true;
        public bool showTrajectoryComparison = true;
        public bool showActualHitMarkers = true;
    }
}
