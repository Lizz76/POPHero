using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace POPHero
{
    public sealed class CanvasMapEdgeGraphic : MaskableGraphic
    {
        Sprite lineSprite;
        Vector2 from;
        Vector2 to;
        float thickness = 8f;

        public override Texture mainTexture => lineSprite != null ? lineSprite.texture : s_WhiteTexture;

        public void SetLine(Vector2 fromPosition, Vector2 toPosition, float lineThickness, Color lineColor, Sprite sprite)
        {
            from = fromPosition;
            to = toPosition;
            thickness = Mathf.Max(1f, lineThickness);
            color = lineColor;
            lineSprite = sprite;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var delta = to - from;
            if (delta.sqrMagnitude <= 0.001f)
                return;

            var normal = new Vector2(-delta.y, delta.x).normalized * (thickness * 0.5f);
            var uv = lineSprite != null ? DataUtility.GetOuterUV(lineSprite) : new Vector4(0f, 0f, 1f, 1f);
            var vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = from - normal;
            vertex.uv0 = new Vector2(uv.x, uv.y);
            vh.AddVert(vertex);

            vertex.position = from + normal;
            vertex.uv0 = new Vector2(uv.x, uv.w);
            vh.AddVert(vertex);

            vertex.position = to + normal;
            vertex.uv0 = new Vector2(uv.z, uv.w);
            vh.AddVert(vertex);

            vertex.position = to - normal;
            vertex.uv0 = new Vector2(uv.z, uv.y);
            vh.AddVert(vertex);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }
    }

    public sealed class CanvasMapNodeView
    {
        readonly RectTransform root;
        readonly RectTransform buttonRoot;
        readonly RectTransform iconRoot;
        readonly RectTransform statusRoot;
        readonly Image glow;
        readonly Image background;
        readonly Image icon;
        readonly Image statusImage;
        readonly TMP_Text fallbackLabel;
        readonly TMP_Text statusLabel;
        readonly TMP_Text titleLabel;
        readonly Button button;
        readonly Outline outline;
        readonly CanvasPointerRelay pointerRelay;

        public RectTransform Rect => root;
        public GameObject gameObject => root.gameObject;

        CanvasMapNodeView(
            RectTransform root,
            RectTransform buttonRoot,
            RectTransform iconRoot,
            RectTransform statusRoot,
            Image glow,
            Image background,
            Image icon,
            Image statusImage,
            TMP_Text fallbackLabel,
            TMP_Text statusLabel,
            TMP_Text titleLabel,
            Button button,
            Outline outline,
            CanvasPointerRelay pointerRelay)
        {
            this.root = root;
            this.buttonRoot = buttonRoot;
            this.iconRoot = iconRoot;
            this.statusRoot = statusRoot;
            this.glow = glow;
            this.background = background;
            this.icon = icon;
            this.statusImage = statusImage;
            this.fallbackLabel = fallbackLabel;
            this.statusLabel = statusLabel;
            this.titleLabel = titleLabel;
            this.button = button;
            this.outline = outline;
            this.pointerRelay = pointerRelay;
        }

        public static CanvasMapNodeView Create(Transform parent)
        {
            var root = CanvasUiFactory.Node("MapNode", parent);
            root.sizeDelta = new Vector2(110f, 92f);

            var glowRoot = CanvasUiFactory.Node("Glow", root);
            glowRoot.anchorMin = new Vector2(0.5f, 0.5f);
            glowRoot.anchorMax = new Vector2(0.5f, 0.5f);
            glowRoot.pivot = new Vector2(0.5f, 0.5f);
            glowRoot.sizeDelta = new Vector2(82f, 82f);
            glowRoot.anchoredPosition = new Vector2(0f, 8f);
            var glow = glowRoot.gameObject.AddComponent<Image>();
            glow.raycastTarget = false;

            var buttonRoot = CanvasUiFactory.Node("Button", root);
            buttonRoot.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRoot.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRoot.pivot = new Vector2(0.5f, 0.5f);
            buttonRoot.sizeDelta = new Vector2(62f, 62f);
            buttonRoot.anchoredPosition = new Vector2(0f, 8f);
            var background = buttonRoot.gameObject.AddComponent<Image>();
            var button = buttonRoot.gameObject.AddComponent<Button>();
            var outline = buttonRoot.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(2f, -2f);
            outline.enabled = false;

            var iconRoot = CanvasUiFactory.Node("Icon", buttonRoot);
            iconRoot.anchorMin = new Vector2(0.2f, 0.2f);
            iconRoot.anchorMax = new Vector2(0.8f, 0.8f);
            iconRoot.offsetMin = Vector2.zero;
            iconRoot.offsetMax = Vector2.zero;
            var icon = iconRoot.gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var fallback = CanvasUiFactory.Text("FallbackLabel", buttonRoot, 24, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            fallback.rectTransform.anchorMin = Vector2.zero;
            fallback.rectTransform.anchorMax = Vector2.one;
            fallback.rectTransform.offsetMin = Vector2.zero;
            fallback.rectTransform.offsetMax = Vector2.zero;
            fallback.raycastTarget = false;
            fallback.enableAutoSizing = true;
            fallback.fontSizeMin = 11f;
            fallback.fontSizeMax = 24f;

            var statusRoot = CanvasUiFactory.Node("Status", root);
            statusRoot.anchorMin = new Vector2(0.5f, 0.5f);
            statusRoot.anchorMax = new Vector2(0.5f, 0.5f);
            statusRoot.pivot = new Vector2(0.5f, 0.5f);
            statusRoot.sizeDelta = new Vector2(32f, 32f);
            statusRoot.anchoredPosition = new Vector2(30f, 36f);
            var statusImage = statusRoot.gameObject.AddComponent<Image>();
            statusImage.raycastTarget = false;
            var statusLabel = CanvasUiFactory.Text("Label", statusRoot, 12, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            statusLabel.rectTransform.anchorMin = Vector2.zero;
            statusLabel.rectTransform.anchorMax = Vector2.one;
            statusLabel.rectTransform.offsetMin = Vector2.zero;
            statusLabel.rectTransform.offsetMax = Vector2.zero;
            statusLabel.raycastTarget = false;
            statusLabel.enableAutoSizing = true;
            statusLabel.fontSizeMin = 6f;
            statusLabel.fontSizeMax = 12f;

            var title = CanvasUiFactory.Text("Title", root, 15, new Color(0.88f, 0.92f, 1f, 1f), TextAlignmentOptions.Center, FontStyles.Bold);
            title.rectTransform.anchorMin = new Vector2(0f, 0f);
            title.rectTransform.anchorMax = new Vector2(1f, 0f);
            title.rectTransform.pivot = new Vector2(0.5f, 0f);
            title.rectTransform.offsetMin = new Vector2(0f, 0f);
            title.rectTransform.offsetMax = new Vector2(0f, 24f);
            title.raycastTarget = false;
            title.enableAutoSizing = true;
            title.fontSizeMin = 10f;
            title.fontSizeMax = 15f;

            var pointerRelay = buttonRoot.gameObject.AddComponent<CanvasPointerRelay>();
            return new CanvasMapNodeView(root, buttonRoot, iconRoot, statusRoot, glow, background, icon, statusImage, fallback, statusLabel, title, button, outline, pointerRelay);
        }

        public void Set(MapNodeCardModel model, MapVisualSettings visuals, Action action)
        {
            var isBoss = model.Kind == MapNodeKind.Boss;
            var nodeSize = isBoss ? 72f : 60f;
            buttonRoot.sizeDelta = new Vector2(nodeSize, nodeSize);
            glow.rectTransform.sizeDelta = new Vector2(nodeSize + 22f, nodeSize + 22f);
            statusRoot.anchoredPosition = new Vector2(nodeSize * 0.43f, 8f + nodeSize * 0.43f);
            iconRoot.anchorMin = isBoss ? new Vector2(0.16f, 0.16f) : new Vector2(0.2f, 0.2f);
            iconRoot.anchorMax = isBoss ? new Vector2(0.84f, 0.84f) : new Vector2(0.8f, 0.8f);

            var accent = ResolveNodeColor(model, visuals);
            var locked = model.Status == MapNodeStatus.Locked;
            var completed = model.Status == MapNodeStatus.Completed;
            var current = model.Status == MapNodeStatus.Current;

            glow.sprite = visuals?.selectableGlowSprite ?? PrototypeVisualFactory.CircleSprite;
            glow.color = model.CanSelect
                ? WithAlpha(accent, 0.32f)
                : current
                    ? WithAlpha(visuals?.currentEdgeColor ?? Color.yellow, 0.34f)
                    : Color.clear;
            glow.enabled = model.CanSelect || current;

            background.sprite = visuals?.nodeBackgroundSprite ?? PrototypeVisualFactory.CircleSprite;
            background.color = locked
                ? WithAlpha(visuals?.lockedNodeColor ?? accent, 0.82f)
                : completed
                    ? WithAlpha(visuals?.completedNodeColor ?? accent, 0.9f)
                    : accent;

            icon.sprite = ResolveIcon(model.Kind, visuals);
            icon.enabled = icon.sprite != null;
            icon.color = locked ? new Color(0.58f, 0.62f, 0.7f, 0.7f) : Color.white;
            fallbackLabel.gameObject.SetActive(icon.sprite == null);
            fallbackLabel.text = ResolveFallbackText(model.Kind);
            fallbackLabel.color = locked ? new Color(0.72f, 0.76f, 0.84f, 0.82f) : Color.white;

            titleLabel.text = RunMapManager.GetNodeKindName(model.Kind);
            titleLabel.color = locked ? new Color(0.55f, 0.6f, 0.68f, 1f) : new Color(0.9f, 0.94f, 1f, 1f);

            SetStatusBadge(model, visuals);

            outline.enabled = model.CanSelect || current;
            outline.effectColor = current ? visuals?.currentEdgeColor ?? Color.yellow : WithAlpha(accent, 0.92f);
            outline.effectDistance = current ? new Vector2(4f, -4f) : new Vector2(2f, -2f);

            button.interactable = model.CanSelect;
            var colors = button.colors;
            colors.normalColor = background.color;
            colors.highlightedColor = Color.Lerp(background.color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(background.color, Color.black, 0.18f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = background.color;
            button.colors = colors;

            button.onClick.RemoveAllListeners();
            if (action != null)
                button.onClick.AddListener(() => action());
        }

        public void SetHover(Action entered, Action exited)
        {
            pointerRelay.Entered = entered;
            pointerRelay.Exited = exited;
        }

        void SetStatusBadge(MapNodeCardModel model, MapVisualSettings visuals)
        {
            Sprite sprite = null;
            var label = string.Empty;
            var color = Color.white;

            switch (model.Status)
            {
                case MapNodeStatus.Completed:
                    sprite = visuals?.completedMarkSprite;
                    label = "OK";
                    color = visuals?.completedNodeColor ?? new Color(0.34f, 0.68f, 0.45f, 1f);
                    break;
                case MapNodeStatus.Locked:
                    sprite = visuals?.lockedMarkSprite;
                    label = "LOCK";
                    color = visuals?.lockedNodeColor ?? new Color(0.33f, 0.36f, 0.43f, 1f);
                    break;
                case MapNodeStatus.Current:
                    label = "...";
                    color = visuals?.currentEdgeColor ?? new Color(1f, 0.88f, 0.38f, 1f);
                    break;
                case MapNodeStatus.Available:
                    label = "GO";
                    color = ResolveNodeColor(model, visuals);
                    break;
            }

            var show = !string.IsNullOrEmpty(label) || sprite != null;
            statusRoot.gameObject.SetActive(show);
            if (!show)
                return;

            statusImage.sprite = sprite ?? PrototypeVisualFactory.CircleSprite;
            statusImage.color = color;
            statusLabel.gameObject.SetActive(sprite == null);
            statusLabel.text = label;
            statusLabel.color = Color.white;
            statusLabel.fontSize = label.Length > 2 ? 8 : 12;
        }

        static Color ResolveNodeColor(MapNodeCardModel model, MapVisualSettings visuals)
        {
            if (model.Status == MapNodeStatus.Locked)
                return visuals?.lockedNodeColor ?? new Color(0.33f, 0.36f, 0.43f, 1f);
            if (model.Status == MapNodeStatus.Completed)
                return visuals?.completedNodeColor ?? new Color(0.34f, 0.68f, 0.45f, 1f);
            return model.AccentColor;
        }

        static Sprite ResolveIcon(MapNodeKind kind, MapVisualSettings visuals)
        {
            return kind switch
            {
                MapNodeKind.Battle => visuals?.battleIconSprite,
                MapNodeKind.Shop => visuals?.shopIconSprite,
                MapNodeKind.Workbench => visuals?.workbenchIconSprite,
                MapNodeKind.Rest => visuals?.eventIconSprite,
                MapNodeKind.Event => visuals?.eventIconSprite,
                MapNodeKind.Boss => visuals?.bossIconSprite,
                _ => null
            };
        }

        static string ResolveFallbackText(MapNodeKind kind)
        {
            return kind switch
            {
                MapNodeKind.Battle => "ATK",
                MapNodeKind.Shop => "$",
                MapNodeKind.Workbench => "W",
                MapNodeKind.Rest => "HP",
                MapNodeKind.Event => "?",
                MapNodeKind.Boss => "BOSS",
                _ => "?"
            };
        }

        static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }

    public sealed class CanvasMapRouteView : MonoBehaviour
    {
        readonly List<CanvasMapNodeView> nodeViews = new();
        readonly List<CanvasMapEdgeGraphic> edgeViews = new();
        readonly Dictionary<string, MapNodeCardModel> nodeById = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, Vector2> positionById = new(StringComparer.OrdinalIgnoreCase);

        RectTransform root;
        RectTransform edgeLayer;
        RectTransform nodeLayer;
        RectTransform detailPanel;
        Image background;
        TMP_Text detailTitle;
        TMP_Text detailMeta;
        TMP_Text detailBody;
        MapPanelModel currentModel;
        MapVisualSettings currentVisuals;
        Action<string> selectNode;

        public static CanvasMapRouteView Create(Transform parent)
        {
            var root = CanvasUiFactory.Node("RouteView", parent);
            Stretch(root);
            var view = root.gameObject.AddComponent<CanvasMapRouteView>();
            view.EnsureBuilt();
            return view;
        }

        void Awake() => EnsureBuilt();

        void OnRectTransformDimensionsChange()
        {
            if (currentModel != null && isActiveAndEnabled)
                Rebuild();
        }

        public void Set(MapPanelModel model, MapVisualSettings visuals, Action<string> onSelectNode)
        {
            currentModel = model;
            currentVisuals = visuals;
            selectNode = onSelectNode;
            Rebuild();
        }

        void EnsureBuilt()
        {
            root ??= transform as RectTransform;
            if (root == null)
                return;

            background ??= root.GetComponent<Image>() ?? root.gameObject.AddComponent<Image>();
            background.raycastTarget = false;

            edgeLayer ??= FindOrCreateRect("EdgeLayer", root);
            SetFill(edgeLayer, 0f, 0f, 0f, 154f);
            edgeLayer.SetAsFirstSibling();

            nodeLayer ??= FindOrCreateRect("NodeLayer", root);
            SetFill(nodeLayer, 0f, 0f, 0f, 154f);

            detailPanel ??= FindOrCreateRect("DetailPanel", root);
            SetBottomStretch(detailPanel, 16f, 16f, 16f, 122f);
            var detailImage = detailPanel.GetComponent<Image>() ?? detailPanel.gameObject.AddComponent<Image>();
            detailImage.color = new Color(0.09f, 0.11f, 0.17f, 0.92f);

            detailTitle ??= FindText(detailPanel, "Title");
            if (detailTitle == null)
            {
                detailTitle = CanvasUiFactory.Text("Title", detailPanel, 20, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
                SetTopStretch(detailTitle.rectTransform, 14f, 14f, 10f, 28f);
            }

            detailMeta ??= FindText(detailPanel, "Meta");
            if (detailMeta == null)
            {
                detailMeta = CanvasUiFactory.Text("Meta", detailPanel, 14, new Color(0.7f, 0.82f, 1f, 1f), TextAlignmentOptions.Left, FontStyles.Bold);
                SetTopStretch(detailMeta.rectTransform, 14f, 14f, 40f, 24f);
            }

            detailBody ??= FindText(detailPanel, "Body");
            if (detailBody == null)
            {
                detailBody = CanvasUiFactory.Text("Body", detailPanel, 15, new Color(0.84f, 0.87f, 0.94f, 1f), TextAlignmentOptions.TopLeft);
                detailBody.enableWordWrapping = true;
                SetFill(detailBody.rectTransform, 14f, 14f, 68f, 12f);
            }
        }

        void Rebuild()
        {
            EnsureBuilt();
            if (root == null || currentModel == null)
                return;

            var visuals = currentVisuals;
            background.sprite = visuals?.backgroundSprite ?? PrototypeVisualFactory.SquareSprite;
            background.color = visuals?.backgroundTint ?? new Color(0.06f, 0.08f, 0.13f, 0.96f);

            nodeById.Clear();
            positionById.Clear();

            var nodes = currentModel.Nodes ?? Array.Empty<MapNodeCardModel>();
            var routeRect = nodeLayer != null ? nodeLayer.rect : root.rect;
            var width = Mathf.Max(routeRect.width, root.sizeDelta.x, 760f);
            var height = Mathf.Max(routeRect.height, root.sizeDelta.y - 154f, 360f);

            for (var index = 0; index < nodes.Count; index++)
            {
                var node = nodes[index];
                nodeById[node.NodeId] = node;
                positionById[node.NodeId] = ToLocalPosition(node.NormalizedPosition, width, height);
            }

            RebuildEdges(nodes, visuals);
            RebuildNodes(nodes, visuals);
            ShowDefaultDetails();
        }

        void RebuildEdges(IReadOnlyList<MapNodeCardModel> nodes, MapVisualSettings visuals)
        {
            var edgeIndex = 0;
            for (var index = 0; index < nodes.Count; index++)
            {
                var fromNode = nodes[index];
                if (fromNode.NextNodeIds == null || !positionById.TryGetValue(fromNode.NodeId, out var fromPosition))
                    continue;

                for (var nextIndex = 0; nextIndex < fromNode.NextNodeIds.Count; nextIndex++)
                {
                    var toId = fromNode.NextNodeIds[nextIndex];
                    if (!positionById.TryGetValue(toId, out var toPosition) || !nodeById.TryGetValue(toId, out var toNode))
                        continue;

                    var edge = EnsureEdge(edgeIndex++);
                    edge.gameObject.SetActive(true);
                    edge.SetLine(fromPosition, toPosition, 7f, ResolveEdgeColor(fromNode, toNode, visuals), visuals?.edgeSprite);
                }
            }

            for (var index = edgeIndex; index < edgeViews.Count; index++)
                edgeViews[index].gameObject.SetActive(false);
        }

        void RebuildNodes(IReadOnlyList<MapNodeCardModel> nodes, MapVisualSettings visuals)
        {
            while (nodeViews.Count < nodes.Count)
                nodeViews.Add(CanvasMapNodeView.Create(nodeLayer));

            for (var index = 0; index < nodeViews.Count; index++)
            {
                var active = index < nodes.Count;
                var view = nodeViews[index];
                view.gameObject.SetActive(active);
                if (!active)
                    continue;

                var node = nodes[index];
                view.Rect.anchoredPosition = positionById[node.NodeId];
                var capturedNode = node;
                var capturedId = node.NodeId;
                view.Set(node, visuals, node.CanSelect ? () => selectNode?.Invoke(capturedId) : null);
                view.SetHover(() => ShowNodeDetails(capturedNode), ShowDefaultDetails);
            }
        }

        CanvasMapEdgeGraphic EnsureEdge(int index)
        {
            while (edgeViews.Count <= index)
            {
                var rect = CanvasUiFactory.Node("Edge", edgeLayer);
                Stretch(rect);
                var edge = rect.gameObject.AddComponent<CanvasMapEdgeGraphic>();
                edgeViews.Add(edge);
            }

            return edgeViews[index];
        }

        void ShowNodeDetails(MapNodeCardModel node)
        {
            detailTitle.text = node.Title;
            detailTitle.color = node.AccentColor;
            detailMeta.text = $"{node.KindText}  {node.StatusText}";
            detailBody.text = node.Description;
        }

        void ShowDefaultDetails()
        {
            if (detailTitle == null)
                return;

            detailTitle.text = "路线详情";
            detailTitle.color = Color.white;
            detailMeta.text = currentModel?.FeedbackText ?? string.Empty;
            detailBody.text = "发光节点可以进入；灰色节点会在完成前置路线后解锁。悬停节点可查看具体去向。";
        }

        static Vector2 ToLocalPosition(Vector2 normalized, float width, float height)
        {
            var xPadding = Mathf.Clamp(width * 0.12f, 64f, 130f);
            var topPadding = Mathf.Clamp(height * 0.1f, 44f, 74f);
            var bottomPadding = Mathf.Clamp(height * 0.11f, 50f, 82f);
            var x = Mathf.Lerp(-width * 0.5f + xPadding, width * 0.5f - xPadding, Mathf.Clamp01(normalized.x));
            var y = Mathf.Lerp(-height * 0.5f + bottomPadding, height * 0.5f - topPadding, Mathf.Clamp01(normalized.y));
            return new Vector2(x, y);
        }

        static Color ResolveEdgeColor(MapNodeCardModel from, MapNodeCardModel to, MapVisualSettings visuals)
        {
            if (from.Status == MapNodeStatus.Current || to.Status == MapNodeStatus.Current)
                return visuals?.currentEdgeColor ?? new Color(1f, 0.88f, 0.38f, 1f);
            if (from.Status == MapNodeStatus.Completed && to.Status == MapNodeStatus.Available)
                return visuals?.availableEdgeColor ?? new Color(0.72f, 0.86f, 1f, 0.96f);
            if (from.Status == MapNodeStatus.Completed)
                return visuals?.completedEdgeColor ?? new Color(0.34f, 0.68f, 0.45f, 0.88f);
            return visuals?.lockedEdgeColor ?? new Color(0.23f, 0.26f, 0.32f, 0.72f);
        }

        static RectTransform FindOrCreateRect(string name, RectTransform parent)
        {
            var existing = parent.Find(name) as RectTransform;
            return existing != null ? existing : CanvasUiFactory.Node(name, parent);
        }

        static TMP_Text FindText(RectTransform parent, string name)
        {
            var child = parent.Find(name);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        static void SetFill(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        static void SetTopStretch(RectTransform rect, float left, float right, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -(top + height));
            rect.offsetMax = new Vector2(-right, -top);
        }

        static void SetBottomStretch(RectTransform rect, float left, float right, float bottom, float height)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, bottom + height);
        }
    }
}
