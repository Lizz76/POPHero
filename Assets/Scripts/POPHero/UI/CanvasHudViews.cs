using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace POPHero
{
    static class CanvasUiFactory
    {
        static TMP_FontAsset cachedFont;
        static TMP_FontAsset Font => cachedFont ??= PrototypeVisualFactory.GetCjkTmpFontAsset() ?? TMP_Settings.defaultFontAsset;

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        public static TMP_Text Text(string name, Transform parent, int size, Color color, TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
        {
            var rect = Node(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = Font;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.fontStyle = style;
            return text;
        }

        public static Button Button(string name, Transform parent, string label, Color fill, Color textColor, int fontSize)
        {
            var rect = Node(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = fill;
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = fill;
            colors.highlightedColor = fill * 1.08f;
            colors.pressedColor = fill * 0.92f;
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(fill.r * 0.45f, fill.g * 0.45f, fill.b * 0.45f, 0.7f);
            button.colors = colors;
            var text = Text("Label", rect, fontSize, textColor, TextAlignmentOptions.Center, FontStyles.Bold);
            text.text = label;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return button;
        }
    }

    public sealed class CanvasPointerRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action Entered;
        public Action Exited;
        public void OnPointerEnter(PointerEventData eventData) => Entered?.Invoke();
        public void OnPointerExit(PointerEventData eventData) => Exited?.Invoke();
    }

    public sealed class CanvasStickerDragRelay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Func<bool> BeginDrag;
        public Action Drag;
        public Action EndDrag;

        bool dragging;

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = BeginDrag?.Invoke() == true;
            if (!dragging)
                return;

            Drag?.Invoke();
            eventData.Use();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging)
                return;

            Drag?.Invoke();
            eventData.Use();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging)
                return;

            dragging = false;
            EndDrag?.Invoke();
            eventData.Use();
        }
    }

    public sealed class CanvasSocketDropRelay : MonoBehaviour, IDropHandler
    {
        public Action Dropped;

        public void OnDrop(PointerEventData eventData)
        {
            Dropped?.Invoke();
            eventData.Use();
        }
    }

    public sealed class CanvasBlockRowView
    {
        readonly RectTransform root;
        readonly Image background;
        readonly TMP_Text indexText;
        readonly BlockCellView typeCell;
        readonly RectTransform socketRoot;
        readonly BlockCellView socketCellPrefab;
        readonly List<BlockCellView> socketCells = new();

        public GameObject gameObject => root.gameObject;

        CanvasBlockRowView(RectTransform root, Image background, TMP_Text indexText, BlockCellView typeCell, RectTransform socketRoot, BlockCellView socketCellPrefab)
        {
            this.root = root;
            this.background = background;
            this.indexText = indexText;
            this.typeCell = typeCell;
            this.socketRoot = socketRoot;
            this.socketCellPrefab = socketCellPrefab;
        }

        public static CanvasBlockRowView Create(Transform parent, BlockCellView blockCellPrefab = null)
        {
            var root = CanvasUiFactory.Node("BlockRow", parent);
            var layoutElement = root.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 72f;
            layoutElement.preferredHeight = 72f;
            layoutElement.flexibleWidth = 1f;
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            var background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.12f, 0.14f, 0.2f, 0.94f);

            var index = CanvasUiFactory.Text("Index", root, 20, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            var indexLayout = index.rectTransform.gameObject.AddComponent<LayoutElement>();
            indexLayout.minWidth = 42f;
            indexLayout.preferredWidth = 42f;

            var typeCell = BlockCellView.Create(root, blockCellPrefab);
            var typeCellLayout = typeCell.GetComponent<LayoutElement>() ?? typeCell.gameObject.AddComponent<LayoutElement>();
            typeCellLayout.minWidth = 58f;
            typeCellLayout.preferredWidth = 58f;
            typeCellLayout.minHeight = 44f;
            typeCellLayout.preferredHeight = 44f;

            var socketRoot = CanvasUiFactory.Node("SocketRoot", root);
            var socketLayout = socketRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            socketLayout.spacing = 6f;
            socketLayout.childAlignment = TextAnchor.MiddleLeft;
            socketLayout.childForceExpandHeight = false;
            socketLayout.childForceExpandWidth = false;

            return new CanvasBlockRowView(root, background, index, typeCell, socketRoot, blockCellPrefab);
        }

        public void SetSelection(bool selected)
        {
            background.color = selected
                ? new Color(0.34f, 0.29f, 0.08f, 0.96f)
                : new Color(0.12f, 0.14f, 0.2f, 0.94f);
        }

        public void SetIndex(int value) => indexText.text = value.ToString("00");

        public void SetEmpty()
        {
            typeCell.gameObject.SetActive(false);
            SetSocketCount(0);
        }

        internal void SetTypeVisual(BlockCardState cardState, BlockVisualPresentation visual, Action action)
        {
            typeCell.SetVisual(cardState, visual, action);
        }

        public void SetTypePlaceholder(string label, Color color, Action action)
        {
            typeCell.SetPlaceholder(label, color, action);
        }

        public void SetTypeTooltip(string title, string body, Color color, CanvasHudController controller)
        {
            AttachTooltip(typeCell.gameObject, title, body, color, controller);
        }

        public void SetSocketCount(int count)
        {
            while (socketCells.Count < count)
            {
                var cell = BlockCellView.Create(socketRoot, socketCellPrefab);
                var layout = cell.GetComponent<LayoutElement>() ?? cell.gameObject.AddComponent<LayoutElement>();
                layout.minWidth = 58f;
                layout.preferredWidth = 58f;
                layout.minHeight = 44f;
                layout.preferredHeight = 44f;
                socketCells.Add(cell);
            }

            for (var i = 0; i < socketCells.Count; i++)
                socketCells[i].gameObject.SetActive(i < count);
        }

        public void SetSocket(int index, string fallbackText, Sprite iconSprite, Color color, Action action, Action dropAction = null)
        {
            var cell = socketCells[index];
            cell.SetCustom(null, color, iconSprite, Color.white, fallbackText, action);

            var dropRelay = cell.gameObject.GetComponent<CanvasSocketDropRelay>() ??
                cell.gameObject.AddComponent<CanvasSocketDropRelay>();
            dropRelay.Dropped = dropAction;
        }

        public void SetSocketTooltip(int index, string title, string body, Color color, CanvasHudController controller)
        {
            AttachTooltip(socketCells[index].gameObject, title, body, color, controller);
        }

        static void AttachTooltip(GameObject go, string title, string body, Color color, CanvasHudController controller)
        {
            var relay = go.GetComponent<CanvasPointerRelay>() ?? go.AddComponent<CanvasPointerRelay>();
            relay.Entered = () => controller.SetTooltip(title, body, color);
            relay.Exited = controller.ClearTooltip;
        }
    }

    public sealed class CanvasCardView
    {
        readonly RectTransform root;
        readonly Image background;
        readonly TMP_Text title;
        readonly TMP_Text subtitle;
        readonly TMP_Text meta;
        readonly TMP_Text desc;
        readonly Button button;
        readonly TMP_Text buttonLabel;

        public GameObject gameObject => root.gameObject;

        CanvasCardView(RectTransform root, Image background, TMP_Text title, TMP_Text subtitle, TMP_Text meta, TMP_Text desc, Button button, TMP_Text buttonLabel)
        {
            this.root = root;
            this.background = background;
            this.title = title;
            this.subtitle = subtitle;
            this.meta = meta;
            this.desc = desc;
            this.button = button;
            this.buttonLabel = buttonLabel;
        }

        public static CanvasCardView Create(Transform parent)
        {
            var root = CanvasUiFactory.Node("Card", parent);
            var layoutElement = root.gameObject.AddComponent<LayoutElement>();
            layoutElement.minWidth = 220f;
            layoutElement.preferredWidth = 250f;
            layoutElement.flexibleWidth = 1f;
            layoutElement.minHeight = 220f;
            layoutElement.preferredHeight = 230f;
            var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.14f, 0.16f, 0.24f, 0.96f);

            var title = CanvasUiFactory.Text("Title", root, 24, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            var subtitle = CanvasUiFactory.Text("Subtitle", root, 18, new Color(0.88f, 0.9f, 1f, 1f), TextAlignmentOptions.Left, FontStyles.Bold);
            var meta = CanvasUiFactory.Text("Meta", root, 18, new Color(0.96f, 0.84f, 0.48f, 1f), TextAlignmentOptions.Left);
            var desc = CanvasUiFactory.Text("Description", root, 18, new Color(0.86f, 0.88f, 0.94f, 1f), TextAlignmentOptions.TopLeft);
            desc.enableWordWrapping = true;
            desc.overflowMode = TextOverflowModes.Ellipsis;
            desc.rectTransform.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            var button = CanvasUiFactory.Button("ActionButton", root, "Select", new Color(0.3f, 0.52f, 0.94f, 1f), Color.white, 20);
            button.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;

            return new CanvasCardView(root, background, title, subtitle, meta, desc, button, button.GetComponentInChildren<TMP_Text>());
        }

        public void Set(string titleValue, string subtitleValue, string metaValue, string descValue, string buttonValue, Color accent)
        {
            title.text = titleValue;
            subtitle.text = subtitleValue;
            meta.text = metaValue;
            meta.gameObject.SetActive(!string.IsNullOrWhiteSpace(metaValue));
            desc.text = descValue;
            buttonLabel.text = buttonValue;
            title.color = accent;
            background.color = new Color(accent.r * 0.18f + 0.12f, accent.g * 0.18f + 0.12f, accent.b * 0.18f + 0.16f, 0.98f);
        }

        public void SetInteractable(bool interactable) => button.interactable = interactable;

        public void SetAction(Action action)
        {
            button.onClick.RemoveAllListeners();
            if (action != null)
                button.onClick.AddListener(() => action());
        }
    }

    public sealed class CanvasStickerCellView
    {
        readonly RectTransform root;
        readonly Image background;
        readonly Button button;
        readonly Image icon;
        readonly TMP_Text fallbackLabel;
        readonly CanvasGroup canvasGroup;

        public GameObject gameObject => root.gameObject;

        CanvasStickerCellView(RectTransform root, Image background, Button button, Image icon, TMP_Text fallbackLabel, CanvasGroup canvasGroup)
        {
            this.root = root;
            this.background = background;
            this.button = button;
            this.icon = icon;
            this.fallbackLabel = fallbackLabel;
            this.canvasGroup = canvasGroup;
        }

        public static CanvasStickerCellView Create(Transform parent)
        {
            var root = CanvasUiFactory.Node("StickerCell", parent);
            var layoutElement = root.gameObject.AddComponent<LayoutElement>();
            layoutElement.minWidth = 88f;
            layoutElement.minHeight = 88f;
            layoutElement.preferredWidth = 88f;
            layoutElement.preferredHeight = 88f;

            var background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.12f, 0.14f, 0.2f, 0.94f);
            var canvasGroup = root.gameObject.AddComponent<CanvasGroup>();

            var button = root.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = background.color;
            colors.highlightedColor = new Color(0.18f, 0.22f, 0.3f, 0.98f);
            colors.pressedColor = new Color(0.1f, 0.12f, 0.16f, 0.98f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.12f, 0.14f, 0.2f, 0.4f);
            button.colors = colors;

            var iconRoot = CanvasUiFactory.Node("Icon", root);
            iconRoot.anchorMin = new Vector2(0.5f, 0.5f);
            iconRoot.anchorMax = new Vector2(0.5f, 0.5f);
            iconRoot.pivot = new Vector2(0.5f, 0.5f);
            iconRoot.sizeDelta = new Vector2(44f, 44f);
            var icon = iconRoot.gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.enabled = false;

            var label = CanvasUiFactory.Text("FallbackLabel", root, 24, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = new Vector2(1f, 1f);
            label.rectTransform.offsetMin = new Vector2(10f, 10f);
            label.rectTransform.offsetMax = new Vector2(-10f, -10f);
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;

            return new CanvasStickerCellView(root, background, button, icon, label, canvasGroup);
        }

        public void Set(string fallbackText, Sprite iconSprite, Color accent, Action action)
        {
            background.color = new Color(accent.r * 0.1f + 0.1f, accent.g * 0.1f + 0.11f, accent.b * 0.1f + 0.14f, 0.96f);
            icon.sprite = iconSprite;
            icon.enabled = iconSprite != null;
            icon.color = Color.white;
            fallbackLabel.text = fallbackText;
            fallbackLabel.color = accent;
            fallbackLabel.gameObject.SetActive(iconSprite == null);

            button.onClick.RemoveAllListeners();
            if (action != null)
                button.onClick.AddListener(() => action());
        }

        public void SetInteractable(bool interactable) => button.interactable = interactable;

        public void SetDraggingVisual(bool dragging)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = dragging ? 0.45f : 1f;
        }

        public void SetDragHandlers(Func<bool> beginDrag, Action drag, Action endDrag)
        {
            var relay = root.gameObject.GetComponent<CanvasStickerDragRelay>() ?? root.gameObject.AddComponent<CanvasStickerDragRelay>();
            relay.BeginDrag = beginDrag;
            relay.Drag = drag;
            relay.EndDrag = endDrag;
        }

        public void SetTooltip(string titleValue, string bodyValue, Color color, CanvasHudController controller)
        {
            var relay = root.gameObject.GetComponent<CanvasPointerRelay>() ?? root.gameObject.AddComponent<CanvasPointerRelay>();
            relay.Entered = () => controller.SetTooltip(titleValue, bodyValue, color);
            relay.Exited = controller.ClearTooltip;
        }
    }

    public sealed class CanvasListEntryView
    {
        readonly RectTransform root;
        readonly Image background;
        readonly Button button;
        readonly TMP_Text title;
        readonly TMP_Text tag;
        readonly TMP_Text desc;

        public GameObject gameObject => root.gameObject;

        CanvasListEntryView(RectTransform root, Image background, Button button, TMP_Text title, TMP_Text tag, TMP_Text desc)
        {
            this.root = root;
            this.background = background;
            this.button = button;
            this.title = title;
            this.tag = tag;
            this.desc = desc;
        }

        public static CanvasListEntryView Create(Transform parent)
        {
            var root = CanvasUiFactory.Node("Entry", parent);
            var layoutElement = root.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 56f;
            layoutElement.preferredHeight = 60f;
            layoutElement.flexibleWidth = 1f;
            var background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.12f, 0.14f, 0.2f, 0.94f);
            var button = root.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = background.color;
            colors.highlightedColor = new Color(0.18f, 0.22f, 0.3f, 0.98f);
            colors.pressedColor = new Color(0.1f, 0.12f, 0.16f, 0.98f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.12f, 0.14f, 0.2f, 0.4f);
            button.colors = colors;

            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var title = CanvasUiFactory.Text("Title", root, 20, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            title.rectTransform.gameObject.AddComponent<LayoutElement>().preferredWidth = 160f;
            var tag = CanvasUiFactory.Text("Tag", root, 18, new Color(0.96f, 0.82f, 0.44f, 1f), TextAlignmentOptions.Left, FontStyles.Bold);
            tag.rectTransform.gameObject.AddComponent<LayoutElement>().preferredWidth = 100f;
            var desc = CanvasUiFactory.Text("Description", root, 18, new Color(0.85f, 0.88f, 0.94f, 1f), TextAlignmentOptions.Left);
            desc.enableWordWrapping = false;
            desc.overflowMode = TextOverflowModes.Ellipsis;
            desc.rectTransform.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            return new CanvasListEntryView(root, background, button, title, tag, desc);
        }

        public void Set(string titleValue, string tagValue, string descValue, Color accent, Action action)
        {
            title.text = titleValue;
            tag.text = tagValue;
            desc.text = descValue;
            tag.color = accent;
            background.color = new Color(accent.r * 0.12f + 0.1f, accent.g * 0.12f + 0.12f, accent.b * 0.12f + 0.16f, 0.94f);
            button.onClick.RemoveAllListeners();
            if (action != null)
                button.onClick.AddListener(() => action());
        }

        public void SetInteractable(bool interactable) => button.interactable = interactable;

        public void SetTooltip(string titleValue, string bodyValue, Color color, CanvasHudController controller)
        {
            var relay = root.gameObject.GetComponent<CanvasPointerRelay>() ?? root.gameObject.AddComponent<CanvasPointerRelay>();
            relay.Entered = () => controller.SetTooltip(titleValue, bodyValue, color);
            relay.Exited = controller.ClearTooltip;
        }
    }

    public sealed class CanvasBlockOperationEntryView
    {
        readonly RectTransform root;
        readonly Image background;
        readonly Button mainButton;
        readonly TMP_Text title;
        readonly TMP_Text meta;
        readonly TMP_Text desc;
        readonly Button sideButton;
        readonly TMP_Text sideButtonLabel;

        public GameObject gameObject => root.gameObject;

        CanvasBlockOperationEntryView(RectTransform root, Image background, Button mainButton, TMP_Text title, TMP_Text meta, TMP_Text desc, Button sideButton, TMP_Text sideButtonLabel)
        {
            this.root = root;
            this.background = background;
            this.mainButton = mainButton;
            this.title = title;
            this.meta = meta;
            this.desc = desc;
            this.sideButton = sideButton;
            this.sideButtonLabel = sideButtonLabel;
        }

        public static CanvasBlockOperationEntryView Create(Transform parent)
        {
            var root = CanvasUiFactory.Node("BlockOperationEntry", parent);
            var layoutElement = root.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 74f;
            layoutElement.preferredHeight = 80f;
            layoutElement.flexibleWidth = 1f;

            var background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.12f, 0.14f, 0.2f, 0.94f);

            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;

            var mainRoot = CanvasUiFactory.Node("MainRoot", root);
            var mainLayoutElement = mainRoot.gameObject.AddComponent<LayoutElement>();
            mainLayoutElement.flexibleWidth = 1f;
            var mainButton = mainRoot.gameObject.AddComponent<Button>();
            mainButton.transition = Selectable.Transition.None;

            var mainLayout = mainRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            mainLayout.spacing = 4f;
            mainLayout.childAlignment = TextAnchor.UpperLeft;
            mainLayout.childControlWidth = true;
            mainLayout.childControlHeight = false;
            mainLayout.childForceExpandWidth = true;
            mainLayout.childForceExpandHeight = false;

            var title = CanvasUiFactory.Text("Title", mainRoot, 20, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            var meta = CanvasUiFactory.Text("Meta", mainRoot, 17, new Color(0.96f, 0.82f, 0.44f, 1f), TextAlignmentOptions.Left, FontStyles.Bold);
            var desc = CanvasUiFactory.Text("Description", mainRoot, 16, new Color(0.85f, 0.88f, 0.94f, 1f), TextAlignmentOptions.Left);
            desc.enableWordWrapping = false;
            desc.overflowMode = TextOverflowModes.Ellipsis;

            var sideButton = CanvasUiFactory.Button("SideButton", root, "删除", new Color(0.58f, 0.18f, 0.2f, 1f), Color.white, 18);
            var sideLayoutElement = sideButton.gameObject.AddComponent<LayoutElement>();
            sideLayoutElement.minWidth = 92f;
            sideLayoutElement.preferredWidth = 92f;
            sideLayoutElement.minHeight = 40f;
            sideLayoutElement.preferredHeight = 40f;

            return new CanvasBlockOperationEntryView(root, background, mainButton, title, meta, desc, sideButton, sideButton.GetComponentInChildren<TMP_Text>());
        }

        public void Set(string titleValue, string metaValue, string descValue, Color accent, bool selected, Action mainAction, Action sideAction, string sideLabel)
        {
            title.text = titleValue ?? string.Empty;
            meta.text = metaValue ?? string.Empty;
            desc.text = descValue ?? string.Empty;

            var baseColor = new Color(accent.r * 0.1f + 0.1f, accent.g * 0.1f + 0.12f, accent.b * 0.1f + 0.16f, 0.94f);
            background.color = selected
                ? new Color(0.34f, 0.29f, 0.08f, 0.96f)
                : baseColor;
            meta.color = accent;

            mainButton.onClick.RemoveAllListeners();
            if (mainAction != null)
                mainButton.onClick.AddListener(() => mainAction());

            sideButton.onClick.RemoveAllListeners();
            if (sideAction != null)
                sideButton.onClick.AddListener(() => sideAction());

            sideButton.gameObject.SetActive(sideAction != null);
            if (sideButtonLabel != null)
                sideButtonLabel.text = sideLabel ?? string.Empty;
        }

        public void SetTooltip(string titleValue, string bodyValue, Color color, CanvasHudController controller)
        {
            var relay = root.gameObject.GetComponent<CanvasPointerRelay>() ?? root.gameObject.AddComponent<CanvasPointerRelay>();
            relay.Entered = () => controller.SetTooltip(titleValue, bodyValue, color);
            relay.Exited = controller.ClearTooltip;
        }
    }
}
