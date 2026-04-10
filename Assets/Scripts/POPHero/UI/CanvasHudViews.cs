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

    public sealed class CanvasBlockRowView
    {
        readonly RectTransform root;
        readonly Image background;
        readonly TMP_Text indexText;
        readonly Button typeButton;
        readonly TMP_Text typeLabel;
        readonly RectTransform stickerRoot;
        readonly RectTransform socketRoot;
        readonly List<Button> stickerButtons = new();
        readonly List<TMP_Text> stickerLabels = new();
        readonly List<Image> stickerImages = new();
        readonly List<Button> socketButtons = new();
        readonly List<TMP_Text> socketLabels = new();
        readonly List<Image> socketImages = new();

        public GameObject gameObject => root.gameObject;

        CanvasBlockRowView(RectTransform root, Image background, TMP_Text indexText, Button typeButton, TMP_Text typeLabel, RectTransform stickerRoot, RectTransform socketRoot)
        {
            this.root = root;
            this.background = background;
            this.indexText = indexText;
            this.typeButton = typeButton;
            this.typeLabel = typeLabel;
            this.stickerRoot = stickerRoot;
            this.socketRoot = socketRoot;
        }

        public static CanvasBlockRowView Create(Transform parent)
        {
            var root = CanvasUiFactory.Node("BlockRow", parent);
            var layoutElement = root.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 38f;
            layoutElement.preferredHeight = 38f;
            layoutElement.flexibleWidth = 1f;
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            var background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.12f, 0.14f, 0.2f, 0.94f);

            var index = CanvasUiFactory.Text("Index", root, 18, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            index.rectTransform.gameObject.AddComponent<LayoutElement>().preferredWidth = 36f;

            var typeButton = CanvasUiFactory.Button("TypeButton", root, "ATK", new Color(0.28f, 0.36f, 0.52f, 1f), Color.white, 18);
            var typeLayout = typeButton.gameObject.AddComponent<LayoutElement>();
            typeLayout.preferredWidth = 34f;
            typeLayout.preferredHeight = 28f;
            var typeLabel = typeButton.GetComponentInChildren<TMP_Text>();

            var stickerRoot = CanvasUiFactory.Node("StickerRoot", root);
            stickerRoot.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var stickerLayout = stickerRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            stickerLayout.spacing = 4f;
            stickerLayout.childAlignment = TextAnchor.MiddleLeft;
            stickerLayout.childForceExpandHeight = false;
            stickerLayout.childForceExpandWidth = false;

            var socketRoot = CanvasUiFactory.Node("SocketRoot", root);
            var socketLayout = socketRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            socketLayout.spacing = 4f;
            socketLayout.childAlignment = TextAnchor.MiddleRight;
            socketLayout.childForceExpandHeight = false;
            socketLayout.childForceExpandWidth = false;

            return new CanvasBlockRowView(root, background, index, typeButton, typeLabel, stickerRoot, socketRoot);
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
            typeButton.gameObject.SetActive(false);
            SetStickerCount(0);
            SetSocketCount(0);
        }

        public void SetType(string icon, Color color, Action action)
        {
            typeButton.gameObject.SetActive(true);
            typeButton.GetComponent<Image>().color = color;
            typeLabel.text = icon;
            typeButton.onClick.RemoveAllListeners();
            if (action != null)
                typeButton.onClick.AddListener(() => action());
        }

        public void SetTypeTooltip(string title, string body, Color color, CanvasHudController controller)
        {
            AttachTooltip(typeButton.gameObject, title, body, color, controller);
        }

        public void SetStickerCount(int count)
        {
            while (stickerButtons.Count < count)
            {
                var button = CanvasUiFactory.Button("Sticker", stickerRoot, "", new Color(0.24f, 0.28f, 0.38f, 1f), Color.white, 14);
                var layout = button.gameObject.AddComponent<LayoutElement>();
                layout.preferredWidth = 24f;
                layout.preferredHeight = 24f;
                stickerButtons.Add(button);
                stickerLabels.Add(button.GetComponentInChildren<TMP_Text>());
                stickerImages.Add(button.GetComponent<Image>());
            }

            for (var i = 0; i < stickerButtons.Count; i++)
                stickerButtons[i].gameObject.SetActive(i < count);
        }

        public void SetSticker(int index, string text, Color color)
        {
            stickerImages[index].color = color;
            stickerLabels[index].text = text;
            stickerButtons[index].interactable = false;
        }

        public void SetStickerTooltip(int index, string title, string body, Color color, CanvasHudController controller)
        {
            AttachTooltip(stickerButtons[index].gameObject, title, body, color, controller);
        }

        public void SetSocketCount(int count)
        {
            while (socketButtons.Count < count)
            {
                var button = CanvasUiFactory.Button("Socket", socketRoot, "+", new Color(0.32f, 0.36f, 0.42f, 1f), Color.white, 14);
                var layout = button.gameObject.AddComponent<LayoutElement>();
                layout.preferredWidth = 22f;
                layout.preferredHeight = 22f;
                socketButtons.Add(button);
                socketLabels.Add(button.GetComponentInChildren<TMP_Text>());
                socketImages.Add(button.GetComponent<Image>());
            }

            for (var i = 0; i < socketButtons.Count; i++)
                socketButtons[i].gameObject.SetActive(i < count);
        }

        public void SetSocket(int index, string text, Color color, Action action)
        {
            socketImages[index].color = color;
            socketLabels[index].text = text;
            socketButtons[index].onClick.RemoveAllListeners();
            if (action != null)
                socketButtons[index].onClick.AddListener(() => action());
        }

        public void SetSocketTooltip(int index, string title, string body, Color color, CanvasHudController controller)
        {
            AttachTooltip(socketButtons[index].gameObject, title, body, color, controller);
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
}
