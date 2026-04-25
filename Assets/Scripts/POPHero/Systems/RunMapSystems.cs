using System;
using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public sealed class RunMapManager : IRunMapService
    {
        readonly List<MapNodeState> nodes = new();
        readonly List<MapEventChoiceState> currentEventChoices = new();
        readonly Dictionary<string, MapNodeState> nodesById = new(StringComparer.OrdinalIgnoreCase);

        PopHeroGame game;
        MapConfigDef config;

        public IReadOnlyList<MapNodeState> Nodes => nodes;
        public IReadOnlyList<MapEventChoiceState> CurrentEventChoices => currentEventChoices;
        public MapNodeState CurrentNode { get; private set; }
        public string LastFeedback { get; private set; } = string.Empty;
        public bool HasMap => nodes.Count > 0;

        public void Initialize(PopHeroGame owner)
        {
            game = owner;
            Reset();
        }

        public void Reset()
        {
            nodes.Clear();
            nodesById.Clear();
            currentEventChoices.Clear();
            CurrentNode = null;
            LastFeedback = string.Empty;
        }

        public void GenerateNewMap()
        {
            Reset();
            config = SanitizeConfig(game?.Tables?.GetRunMapConfig());
            var floorCount = Mathf.Max(2, config.floorCount);
            var floors = new List<List<MapNodeState>>();

            for (var floor = 0; floor < floorCount; floor++)
            {
                var isBossFloor = floor == floorCount - 1;
                var count = isBossFloor
                    ? 1
                    : UnityEngine.Random.Range(config.minNodesPerFloor, config.maxNodesPerFloor + 1);
                var floorNodes = new List<MapNodeState>();

                for (var index = 0; index < count; index++)
                {
                    var node = CreateNode(floor, index, count, isBossFloor);
                    nodes.Add(node);
                    nodesById[node.id] = node;
                    floorNodes.Add(node);
                }

                floors.Add(floorNodes);
            }

            LinkFloors(floors);
            foreach (var node in floors[0])
                node.status = MapNodeStatus.Available;

            LastFeedback = "选择一个可达节点继续前进。";
        }

        public bool TrySelectNode(string nodeId, out MapNodeState node, out string failReason)
        {
            failReason = string.Empty;
            node = null;
            if (!nodesById.TryGetValue(nodeId ?? string.Empty, out node))
            {
                failReason = "没有找到地图节点。";
                LastFeedback = failReason;
                return false;
            }

            if (!node.IsSelectable)
            {
                failReason = "该节点当前不可达。";
                LastFeedback = failReason;
                return false;
            }

            CurrentNode = node;
            node.status = MapNodeStatus.Current;
            currentEventChoices.Clear();
            if (node.kind == MapNodeKind.Event)
                BuildCurrentEventChoices();

            LastFeedback = $"已进入：{GetNodeKindName(node.kind)}";
            return true;
        }

        public bool TryCompleteCurrentNode(out bool completedBoss, out string failReason)
        {
            completedBoss = false;
            failReason = string.Empty;
            if (CurrentNode == null)
            {
                failReason = "当前没有正在处理的地图节点。";
                LastFeedback = failReason;
                return false;
            }

            CurrentNode.status = MapNodeStatus.Completed;
            completedBoss = CurrentNode.kind == MapNodeKind.Boss;
            foreach (var nextId in CurrentNode.nextNodeIds)
            {
                if (nodesById.TryGetValue(nextId, out var next) && next.status == MapNodeStatus.Locked)
                    next.status = MapNodeStatus.Available;
            }

            CurrentNode = null;
            currentEventChoices.Clear();
            LastFeedback = completedBoss ? "Boss 已击败，路线完成。" : "节点完成，选择下一步路线。";
            return true;
        }

        public MapNodeState FindNode(string nodeId)
        {
            return nodesById.TryGetValue(nodeId ?? string.Empty, out var node) ? node : null;
        }

        static MapConfigDef SanitizeConfig(MapConfigDef source)
        {
            source ??= new MapConfigDef();
            var minNodes = Mathf.Max(1, source.minNodesPerFloor);
            var maxNodes = Mathf.Max(minNodes, source.maxNodesPerFloor);
            return new MapConfigDef
            {
                id = string.IsNullOrWhiteSpace(source.id) ? "default" : source.id,
                floorCount = Mathf.Max(2, source.floorCount),
                minNodesPerFloor = minNodes,
                maxNodesPerFloor = maxNodes,
                extraConnectionChance = Mathf.Clamp01(source.extraConnectionChance),
                battleWeight = Mathf.Max(0, source.battleWeight),
                shopWeight = Mathf.Max(0, source.shopWeight),
                workbenchWeight = Mathf.Max(0, source.workbenchWeight),
                restWeight = Mathf.Max(0, source.restWeight),
                eventWeight = Mathf.Max(0, source.eventWeight),
                bossEnemyIndex = source.bossEnemyIndex
            };
        }

        MapNodeState CreateNode(int floor, int index, int count, bool isBossFloor)
        {
            var x = count <= 1 ? 0.5f : (index + 1f) / (count + 1f);
            var y = config.floorCount <= 1 ? 0.5f : floor / (float)(config.floorCount - 1);
            var kind = isBossFloor
                ? MapNodeKind.Boss
                : floor == 0
                    ? MapNodeKind.Battle
                    : RollNodeKind();
            return new MapNodeState
            {
                id = $"map_{floor:00}_{index:00}",
                floor = floor,
                kind = kind,
                status = MapNodeStatus.Locked,
                normalizedPosition = new Vector2(x, y),
                enemyIndex = ResolveEnemyIndex(floor, kind)
            };
        }

        void LinkFloors(IReadOnlyList<List<MapNodeState>> floors)
        {
            for (var floor = 0; floor < floors.Count - 1; floor++)
            {
                var current = floors[floor];
                var next = floors[floor + 1];
                var incoming = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var node in current)
                {
                    var primary = next[UnityEngine.Random.Range(0, next.Count)];
                    AddConnection(node, primary);
                    incoming.Add(primary.id);

                    foreach (var candidate in next)
                    {
                        if (candidate == primary || node.nextNodeIds.Contains(candidate.id))
                            continue;
                        if (UnityEngine.Random.value <= config.extraConnectionChance)
                        {
                            AddConnection(node, candidate);
                            incoming.Add(candidate.id);
                        }
                    }
                }

                foreach (var node in next)
                {
                    if (incoming.Contains(node.id))
                        continue;
                    var source = current[UnityEngine.Random.Range(0, current.Count)];
                    AddConnection(source, node);
                }
            }
        }

        static void AddConnection(MapNodeState from, MapNodeState to)
        {
            if (from != null && to != null && !from.nextNodeIds.Contains(to.id))
                from.nextNodeIds.Add(to.id);
        }

        MapNodeKind RollNodeKind()
        {
            var battle = Mathf.Max(0, config.battleWeight);
            var shop = Mathf.Max(0, config.shopWeight);
            var workbench = Mathf.Max(0, config.workbenchWeight);
            var rest = Mathf.Max(0, config.restWeight);
            var mapEvent = Mathf.Max(0, config.eventWeight);
            var total = battle + shop + workbench + rest + mapEvent;
            if (total <= 0)
                return MapNodeKind.Battle;

            var roll = UnityEngine.Random.Range(0, total);
            if (roll < battle)
                return MapNodeKind.Battle;
            roll -= battle;
            if (roll < shop)
                return MapNodeKind.Shop;
            roll -= shop;
            if (roll < workbench)
                return MapNodeKind.Workbench;
            roll -= workbench;
            if (roll < rest)
                return MapNodeKind.Rest;

            return MapNodeKind.Event;
        }

        int ResolveEnemyIndex(int floor, MapNodeKind kind)
        {
            var templateCount = Mathf.Max(1, game?.Config?.enemies?.templates?.Count ?? 1);
            if (kind == MapNodeKind.Boss)
            {
                var configured = config.bossEnemyIndex;
                return configured >= 0 ? Mathf.Clamp(configured, 0, templateCount - 1) : templateCount - 1;
            }

            return Mathf.Clamp(floor, 0, templateCount - 1);
        }

        void BuildCurrentEventChoices()
        {
            currentEventChoices.Clear();
            currentEventChoices.AddRange(CreateDefaultEventChoices());
        }

        public static List<MapEventChoiceState> CreateDefaultEventChoices()
        {
            return new List<MapEventChoiceState>
            {
                new()
                {
                    index = 0,
                    actionType = MapEventActionType.GainGold,
                    title = "旧货箱",
                    description = "打开路边补给箱，获得 12 金币。",
                    buttonText = "拿走金币",
                    intValue = 12
                },
                new()
                {
                    index = 1,
                    actionType = MapEventActionType.TakeDamageUnlockSocket,
                    title = "危险训练",
                    description = "受到 10 点伤害，随机解锁 1 个方块槽位。",
                    buttonText = "接受训练",
                    intValue = 10
                },
                new()
                {
                    index = 2,
                    actionType = MapEventActionType.OpenWorkbench,
                    title = "临时工坊",
                    description = "进入一次免费方块操作，可以删除或替换方块。",
                    buttonText = "进入工坊",
                    profileId = "map_workbench"
                },
                new()
                {
                    index = 3,
                    actionType = MapEventActionType.Heal,
                    title = "临时营火",
                    description = "恢复 30% 最大生命，不会超过生命上限。",
                    buttonText = "休息回血",
                    healPercent = MapHealingRules.DefaultHealPercent
                }
            };
        }

        public static string GetNodeKindName(MapNodeKind kind)
        {
            return kind switch
            {
                MapNodeKind.Battle => "战斗",
                MapNodeKind.Shop => "商店",
                MapNodeKind.Workbench => "工坊",
                MapNodeKind.Rest => "休息",
                MapNodeKind.Event => "事件",
                MapNodeKind.Boss => "Boss",
                _ => "节点"
            };
        }
    }
}
