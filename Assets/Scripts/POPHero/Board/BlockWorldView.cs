using UnityEngine;

namespace POPHero
{
    public class BlockWorldView : MonoBehaviour
    {
        const int BackgroundSortingOrder = 20;
        const int IconSortingOrder = 28;

        [SerializeField] SpriteRenderer backgroundRenderer;
        [SerializeField] SpriteRenderer iconRenderer;
        [SerializeField] Animator iconAnimator;
        [SerializeField] TextMesh fallbackIconLabel;

        MeshRenderer fallbackIconRenderer;
        Sprite defaultBackgroundSprite;
        Sprite defaultIconSprite;
        Color baseBackgroundColor = Color.white;
        Color baseIconColor = Color.white;
        float rotationAngle;
        bool keepFallbackLabelUpright;
        bool defaultsCaptured;
        BlockVisualState currentVisualState = BlockVisualState.Default;

        public static BlockWorldView CreateFallbackObject(string objectName, Transform parent = null)
        {
            var go = new GameObject(objectName);
            if (parent != null)
                go.transform.SetParent(parent, false);

            go.AddComponent<SpriteRenderer>();
            var view = go.AddComponent<BlockWorldView>();
            view.EnsureReferences(true);
            return view;
        }

        void Awake()
        {
            EnsureReferences(true);
            CaptureDefaults();
        }

        void OnValidate()
        {
            EnsureReferences(false);
            if (fallbackIconLabel != null && fallbackIconLabel.font == null)
                fallbackIconLabel.font = PrototypeVisualFactory.GetCjkRuntimeFont();
        }

        public void Configure(float rotationZ, bool keepTextUpright)
        {
            rotationAngle = rotationZ;
            keepFallbackLabelUpright = keepTextUpright;
            if (fallbackIconLabel != null)
            {
                fallbackIconLabel.transform.localRotation = keepFallbackLabelUpright
                    ? Quaternion.Euler(0f, 0f, -rotationAngle)
                    : Quaternion.identity;
            }
        }

        internal void Apply(BlockCardState cardState, BlockVisualPresentation visual)
        {
            EnsureReferences(true);
            CaptureDefaults();

            backgroundRenderer.sprite = visual.BackgroundSprite ?? defaultBackgroundSprite ?? PrototypeVisualFactory.SquareSprite;
            iconRenderer.sprite = visual.IconSprite ?? defaultIconSprite ?? GetRuntimeFallbackIcon(cardState?.baseBlockType ?? BoardBlockType.Hybrid);

            baseBackgroundColor = visual.BackgroundTint;
            baseIconColor = visual.IconTint;

            iconRenderer.enabled = iconRenderer.sprite != null;

            if (fallbackIconLabel != null)
            {
                var showFallback = !iconRenderer.enabled && !string.IsNullOrWhiteSpace(visual.FallbackIconText);
                fallbackIconLabel.gameObject.SetActive(showFallback);
                if (showFallback)
                {
                    fallbackIconLabel.text = visual.FallbackIconText;
                    fallbackIconLabel.color = baseIconColor;
                }
            }

            ApplyVisualState();
        }

        public void SetVisualState(BlockVisualState state)
        {
            currentVisualState = state;
            ApplyVisualState();
        }

        public void PlayIconHitAnimation()
        {
            if (iconAnimator == null)
                return;

            iconAnimator.ResetTrigger("Hit");
            iconAnimator.SetTrigger("Hit");
        }

        void EnsureReferences(bool createIfMissing)
        {
            backgroundRenderer ??= GetComponent<SpriteRenderer>();
            if (backgroundRenderer == null && createIfMissing)
                backgroundRenderer = gameObject.AddComponent<SpriteRenderer>();

            iconRenderer = FindOrCreateSpriteRenderer(iconRenderer, "Icon", createIfMissing, new Vector3(0f, 0f, 0f), new Vector3(0.56f, 0.56f, 1f), IconSortingOrder);
            if (iconAnimator == null && iconRenderer != null)
                iconAnimator = iconRenderer.GetComponent<Animator>();
            DisableLegacyBadgeChild();

            if (fallbackIconLabel == null)
            {
                var fallbackTransform = transform.Find("IconFallback");
                if (fallbackTransform != null)
                    fallbackIconLabel = fallbackTransform.GetComponent<TextMesh>();
            }

            if (fallbackIconLabel == null && createIfMissing)
            {
                fallbackIconLabel = PrototypeVisualFactory.CreateTextObject("IconFallback", transform, string.Empty, Color.white, IconSortingOrder + 1, 0.08f);
                fallbackIconLabel.transform.localPosition = new Vector3(0f, -0.02f, -0.02f);
            }

            if (fallbackIconLabel != null)
            {
                fallbackIconRenderer = fallbackIconLabel.GetComponent<MeshRenderer>();
                if (fallbackIconLabel.font == null)
                    fallbackIconLabel.font = PrototypeVisualFactory.GetCjkRuntimeFont();
                Configure(rotationAngle, keepFallbackLabelUpright);
            }

            if (backgroundRenderer != null)
            {
                backgroundRenderer.sortingLayerName = "Default";
                backgroundRenderer.sortingOrder = BackgroundSortingOrder;
            }
        }

        SpriteRenderer FindOrCreateSpriteRenderer(SpriteRenderer current, string childName, bool createIfMissing, Vector3 localPosition, Vector3 localScale, int sortingOrder)
        {
            if (current == null)
            {
                var child = transform.Find(childName);
                if (child != null)
                    current = child.GetComponent<SpriteRenderer>();
            }

            if (current == null && createIfMissing)
            {
                var childObject = new GameObject(childName);
                childObject.transform.SetParent(transform, false);
                childObject.transform.localPosition = localPosition;
                childObject.transform.localScale = localScale;
                current = childObject.AddComponent<SpriteRenderer>();
            }

            if (current != null)
            {
                current.sortingLayerName = "Default";
                current.sortingOrder = sortingOrder;
            }

            return current;
        }

        void CaptureDefaults()
        {
            if (defaultsCaptured)
                return;

            defaultBackgroundSprite = backgroundRenderer != null ? backgroundRenderer.sprite : null;
            defaultIconSprite = iconRenderer != null ? iconRenderer.sprite : null;
            defaultsCaptured = true;
        }

        void ApplyVisualState()
        {
            if (backgroundRenderer == null)
                return;

            backgroundRenderer.color = TintForState(baseBackgroundColor);
            backgroundRenderer.sortingOrder = currentVisualState == BlockVisualState.Highlight ? BackgroundSortingOrder + 2 : BackgroundSortingOrder;

            if (iconRenderer != null)
            {
                iconRenderer.color = TintForState(baseIconColor);
                iconRenderer.sortingOrder = currentVisualState == BlockVisualState.Highlight ? IconSortingOrder + 2 : IconSortingOrder;
            }

            if (fallbackIconLabel != null)
                fallbackIconLabel.color = TintForState(baseIconColor);
            if (fallbackIconRenderer != null)
                fallbackIconRenderer.sortingOrder = currentVisualState == BlockVisualState.Highlight ? IconSortingOrder + 3 : IconSortingOrder + 1;
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

        Color TintForState(Color baseColor)
        {
            return currentVisualState switch
            {
                BlockVisualState.Dim => WithAlpha(baseColor, baseColor.a * 0.44f),
                _ => WithAlpha(baseColor, baseColor.a)
            };
        }

        static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        }
    }
}
