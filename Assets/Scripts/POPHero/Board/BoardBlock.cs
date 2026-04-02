using UnityEngine;

namespace POPHero
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(ArenaSurfaceMarker))]
    public abstract class BoardBlock : MonoBehaviour
    {
        public string blockId;
        public BoardBlockType blockType;
        public Vector2 position;
        public Vector2 size;
        public float valueA;
        public float valueB;
        public bool canReflect = true;
        public BlockCardState CardState { get; private set; }

        protected PopHeroGame game;

        SpriteRenderer spriteRenderer;
        TextMesh label;
        MeshRenderer labelRenderer;
        Color baseFillColor;
        Color baseLabelColor;
        float pulseScale = 1f;
        float rotationAngle;
        bool keepLabelUpright;
        BlockVisualState currentVisualState = BlockVisualState.Default;

        public void Initialize(PopHeroGame owner, BlockCardState cardState, Vector2 worldPosition, Vector2 blockSize, float rotationZ, bool keepTextUpright, Color fillColor, PhysicsMaterial2D bounceMaterial)
        {
            game = owner;
            CardState = cardState;
            blockId = cardState.id;
            blockType = cardState.baseBlockType;
            position = worldPosition;
            size = blockSize;
            valueA = cardState.baseValueA;
            valueB = cardState.baseValueB;
            rotationAngle = rotationZ;
            keepLabelUpright = keepTextUpright;
            baseFillColor = fillColor;
            baseLabelColor = owner.config.board.labelColor;

            transform.position = worldPosition;
            transform.localScale = new Vector3(blockSize.x, blockSize.y, 1f);
            transform.rotation = Quaternion.Euler(0f, 0f, rotationAngle);

            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = PrototypeVisualFactory.SquareSprite;
            spriteRenderer.sortingOrder = 20;

            var collider = GetComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.sharedMaterial = bounceMaterial;

            var surfaceMarker = GetComponent<ArenaSurfaceMarker>();
            surfaceMarker.surfaceType = ArenaSurfaceType.Block;

            EnsureLabel(baseLabelColor);
            RefreshLabel();
            SetVisualState(BlockVisualState.Default);
        }

        public void HandleBallHit(BallController ball)
        {
            OnBallHit(ball);
            pulseScale = 1.12f;
        }

        public void SetVisualState(BlockVisualState state)
        {
            currentVisualState = state;
            ApplyVisualState();
        }

        protected abstract void OnBallHit(BallController ball);
        protected abstract string GetLabelText();

        void EnsureLabel(Color color)
        {
            if (label != null)
                return;

            label = PrototypeVisualFactory.CreateTextObject("Label", transform, string.Empty, color, 30, 0.09f);
            labelRenderer = label.GetComponent<MeshRenderer>();
            label.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            label.transform.localRotation = keepLabelUpright
                ? Quaternion.Euler(0f, 0f, -rotationAngle)
                : Quaternion.identity;
        }

        void ApplyVisualState()
        {
            if (spriteRenderer == null)
                return;

            var fillColor = currentVisualState switch
            {
                BlockVisualState.Highlight => Color.Lerp(baseFillColor, Color.white, 0.35f),
                BlockVisualState.Dim => ScaleColor(baseFillColor, 0.68f),
                _ => baseFillColor
            };
            var labelColor = currentVisualState switch
            {
                BlockVisualState.Highlight => Color.white,
                BlockVisualState.Dim => ScaleColor(baseLabelColor, 0.74f),
                _ => baseLabelColor
            };

            spriteRenderer.color = fillColor;
            spriteRenderer.sortingOrder = currentVisualState == BlockVisualState.Highlight ? 22 : 20;

            if (label != null)
                label.color = labelColor;
            if (labelRenderer != null)
                labelRenderer.sortingOrder = currentVisualState == BlockVisualState.Highlight ? 32 : 30;
        }

        protected void RefreshLabel()
        {
            if (label == null)
                return;

            label.text = GetLabelText();
        }

        public void RefreshFromCard()
        {
            if (CardState == null)
                return;

            blockType = CardState.baseBlockType;
            valueA = CardState.baseValueA;
            valueB = CardState.baseValueB;
            RefreshLabel();
        }

        static Color ScaleColor(Color color, float factor)
        {
            return new Color(color.r * factor, color.g * factor, color.b * factor, color.a);
        }

        void Update()
        {
            pulseScale = Mathf.Lerp(pulseScale, 1f, 10f * Time.deltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(size.x, size.y, 1f) * pulseScale, 12f * Time.deltaTime);
        }
    }
}
