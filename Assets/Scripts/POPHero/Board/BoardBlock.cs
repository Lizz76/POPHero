using UnityEngine;

namespace POPHero
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(ArenaSurfaceMarker))]
    [RequireComponent(typeof(BlockWorldView))]
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

        BlockWorldView worldView;
        float pulseScale = 1f;
        float rotationAngle;
        bool keepFallbackLabelUpright;

        public void Initialize(PopHeroGame owner, BlockCardState cardState, Vector2 worldPosition, Vector2 blockSize, float rotationZ, bool keepTextUpright, PhysicsMaterial2D bounceMaterial)
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
            keepFallbackLabelUpright = keepTextUpright;

            transform.position = worldPosition;
            transform.localScale = new Vector3(blockSize.x, blockSize.y, 1f);
            transform.rotation = Quaternion.Euler(0f, 0f, rotationAngle);

            var collider = GetComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.sharedMaterial = bounceMaterial;

            var surfaceMarker = GetComponent<ArenaSurfaceMarker>();
            surfaceMarker.surfaceType = ArenaSurfaceType.Block;

            worldView = GetComponent<BlockWorldView>() ?? gameObject.AddComponent<BlockWorldView>();
            worldView.Configure(rotationAngle, keepFallbackLabelUpright);
            RefreshVisuals();
            SetVisualState(BlockVisualState.Default);
        }

        public void HandleBallHit(BallController ball)
        {
            OnBallHit(ball);
            pulseScale = 1.12f;
        }

        public void SetVisualState(BlockVisualState state)
        {
            worldView?.SetVisualState(state);
        }

        protected abstract void OnBallHit(BallController ball);

        public void RefreshFromCard()
        {
            if (CardState == null)
                return;

            blockType = CardState.baseBlockType;
            valueA = CardState.baseValueA;
            valueB = CardState.baseValueB;
            RefreshVisuals();
        }

        void RefreshVisuals()
        {
            if (CardState == null || game == null)
                return;

            worldView ??= GetComponent<BlockWorldView>() ?? gameObject.AddComponent<BlockWorldView>();
            worldView.Configure(rotationAngle, keepFallbackLabelUpright);
            worldView.Apply(CardState, BlockPresentationUtility.GetBlockVisual(game.config.board, CardState));
        }

        void Update()
        {
            pulseScale = Mathf.Lerp(pulseScale, 1f, 10f * Time.deltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(size.x, size.y, 1f) * pulseScale, 12f * Time.deltaTime);
        }
    }
}
