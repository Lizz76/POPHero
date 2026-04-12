using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace POPHero
{
    public class BlockCellView : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image backgroundImage;
        [SerializeField] Image iconImage;
        [SerializeField] TMP_Text fallbackLabel;

        Sprite defaultBackgroundSprite;
        Sprite defaultIconSprite;
        bool defaultsCaptured;

        public static BlockCellView Create(Transform parent, BlockCellView prefab = null)
        {
            if (prefab != null)
                return UnityEngine.Object.Instantiate(prefab, parent, false);

            return CreateFallbackObject(parent);
        }

        public static BlockCellView CreateFallbackObject(Transform parent = null)
        {
            var button = CanvasUiFactory.Button("BlockCellView", parent, string.Empty, new Color(0.28f, 0.36f, 0.52f, 1f), Color.white, 12);
            var root = button.GetComponent<RectTransform>();
            var layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 40f;
            layout.preferredHeight = 28f;

            var view = button.gameObject.AddComponent<BlockCellView>();
            view.button = button;
            view.backgroundImage = button.GetComponent<Image>();
            view.fallbackLabel = button.GetComponentInChildren<TMP_Text>(true);
            if (view.fallbackLabel != null)
            {
                view.fallbackLabel.alignment = TextAlignmentOptions.Center;
                view.fallbackLabel.fontSize = 11;
                view.fallbackLabel.fontStyle = FontStyles.Bold;
            }

            var iconRoot = CanvasUiFactory.Node("Icon", root);
            iconRoot.anchorMin = new Vector2(0.18f, 0.18f);
            iconRoot.anchorMax = new Vector2(0.82f, 0.82f);
            iconRoot.offsetMin = Vector2.zero;
            iconRoot.offsetMax = Vector2.zero;
            view.iconImage = iconRoot.gameObject.AddComponent<Image>();
            view.iconImage.preserveAspect = true;
            view.iconImage.raycastTarget = false;

            view.EnsureReferences();
            return view;
        }

        void Awake()
        {
            EnsureReferences();
            CaptureDefaults();
        }

        void OnValidate()
        {
            EnsureReferences();
            if (fallbackLabel != null && fallbackLabel.font == null)
                fallbackLabel.font = PrototypeVisualFactory.GetCjkTmpFontAsset() ?? TMP_Settings.defaultFontAsset;
        }

        internal void SetVisual(BlockCardState cardState, BlockVisualPresentation visual, Action action)
        {
            EnsureReferences();
            CaptureDefaults();

            backgroundImage.sprite = visual.BackgroundSprite ?? defaultBackgroundSprite ?? PrototypeVisualFactory.SquareSprite;
            backgroundImage.color = visual.BackgroundTint;

            iconImage.sprite = visual.IconSprite ?? defaultIconSprite ?? GetRuntimeFallbackIcon(cardState?.baseBlockType ?? BoardBlockType.Hybrid);
            iconImage.color = visual.IconTint;
            iconImage.enabled = iconImage.sprite != null;

            var showFallback = !iconImage.enabled && !string.IsNullOrWhiteSpace(visual.FallbackIconText);
            fallbackLabel.gameObject.SetActive(showFallback);
            if (showFallback)
            {
                fallbackLabel.text = visual.FallbackIconText;
                fallbackLabel.color = visual.IconTint;
            }

            button.onClick.RemoveAllListeners();
            if (action != null)
                button.onClick.AddListener(() => action());
        }

        internal void SetPlaceholder(string label, Color color, Action action)
        {
            SetVisual(
                null,
                new BlockVisualPresentation(
                    null,
                    color,
                    null,
                    Color.white,
                    label),
                action);
        }

        void EnsureReferences()
        {
            button ??= GetComponent<Button>();
            backgroundImage ??= GetComponent<Image>();
            fallbackLabel ??= GetComponentInChildren<TMP_Text>(true);
            if (fallbackLabel != null && fallbackLabel.font == null)
                fallbackLabel.font = PrototypeVisualFactory.GetCjkTmpFontAsset() ?? TMP_Settings.defaultFontAsset;

            if (iconImage == null)
            {
                var iconTransform = transform.Find("Icon");
            if (iconTransform != null)
                iconImage = iconTransform.GetComponent<Image>();
            }

            DisableLegacyBadgeChild();
        }

        void CaptureDefaults()
        {
            if (defaultsCaptured)
                return;

            defaultBackgroundSprite = backgroundImage != null ? backgroundImage.sprite : null;
            defaultIconSprite = iconImage != null ? iconImage.sprite : null;
            defaultsCaptured = true;
        }

        void DisableLegacyBadgeChild()
        {
            var badgeTransform = transform.Find("RarityBadge");
            if (badgeTransform != null)
                badgeTransform.gameObject.SetActive(false);
        }

        static Sprite GetRuntimeFallbackIcon(BoardBlockType blockType)
        {
            return blockType switch
            {
                BoardBlockType.AttackAdd => PrototypeVisualFactory.AttackIconSprite,
                BoardBlockType.Shield => PrototypeVisualFactory.ShieldIconSprite,
                BoardBlockType.AttackMultiply => PrototypeVisualFactory.MultiplierIconSprite,
                _ => PrototypeVisualFactory.SquareSprite
            };
        }
    }
}
