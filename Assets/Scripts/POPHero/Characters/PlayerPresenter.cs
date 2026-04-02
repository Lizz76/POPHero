using UnityEngine;

namespace POPHero
{
    public class PlayerPresenter : MonoBehaviour
    {
        const float HpBarWidth = 2.2f;
        const float HpBarHeight = 0.15f;

        SpriteRenderer bodyRenderer;
        SpriteRenderer coreRenderer;
        SpriteRenderer hpFillRenderer;
        TextMesh nameLabel;
        TextMesh hpLabel;
        Color baseColor = new(0.28f, 0.72f, 0.96f, 1f);
        float flashTimer;
        int snapshotHp = -1;
        int snapshotMaxHp = -1;

        public void Initialize()
        {
            bodyRenderer = PrototypeVisualFactory.CreateSpriteObject("HeroBody", transform, PrototypeVisualFactory.SquareSprite, baseColor, 10, new Vector2(1.7f, 1.9f)).GetComponent<SpriteRenderer>();
            coreRenderer = PrototypeVisualFactory.CreateSpriteObject("HeroCore", transform, PrototypeVisualFactory.CircleSprite, new Color(1f, 1f, 1f, 0.24f), 11, Vector2.one * 0.58f).GetComponent<SpriteRenderer>();
            coreRenderer.transform.localPosition = new Vector3(0f, 0.06f, 0f);

            var hpBack = PrototypeVisualFactory.CreateSpriteObject("HpBack", transform, PrototypeVisualFactory.SquareSprite, new Color(0f, 0f, 0f, 0.55f), 12, new Vector2(2.45f, 0.28f)).GetComponent<SpriteRenderer>();
            hpBack.transform.localPosition = new Vector3(0f, -1.48f, 0f);

            hpFillRenderer = PrototypeVisualFactory.CreateSpriteObject("HpFill", transform, PrototypeVisualFactory.SquareSprite, new Color(0.58f, 0.94f, 0.7f, 1f), 13, new Vector2(HpBarWidth, HpBarHeight)).GetComponent<SpriteRenderer>();
            hpFillRenderer.transform.localPosition = new Vector3(0f, -1.48f, -0.02f);

            nameLabel = PrototypeVisualFactory.CreateTextObject("HeroName", transform, "\u4e3b\u89d2", Color.white, 15, 0.095f);
            nameLabel.transform.localPosition = new Vector3(0f, 1.38f, 0f);
            hpLabel = PrototypeVisualFactory.CreateTextObject("HeroHp", transform, "0/0", Color.white, 15, 0.075f, FontStyle.Normal);
            hpLabel.transform.localPosition = new Vector3(0f, -1.84f, 0f);
        }

        public void SetHpSnapshot(int currentHp, int maxHp)
        {
            snapshotHp = Mathf.Max(0, currentHp);
            snapshotMaxHp = Mathf.Max(1, maxHp);
            UpdateDisplayedHp(snapshotHp, snapshotMaxHp);
        }

        public void Refresh(PlayerData player)
        {
            if (player == null)
                return;

            snapshotHp = -1;
            snapshotMaxHp = -1;
            bodyRenderer.color = baseColor;
            coreRenderer.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.08f, 0.26f, player.CurrentHp / (float)Mathf.Max(1, player.MaxHp)));
            UpdateDisplayedHp(player.CurrentHp, player.MaxHp);
        }

        public void PlayHitFeedback(bool heavy = false)
        {
            flashTimer = heavy ? 0.3f : 0.14f;
            bodyRenderer.color = Color.white;
        }

        void UpdateDisplayedHp(int currentHp, int maxHp)
        {
            hpLabel.text = $"{currentHp}/{maxHp}";
            UpdateBar(Mathf.Clamp01(currentHp / (float)Mathf.Max(1, maxHp)));
        }

        void UpdateBar(float ratio)
        {
            var width = HpBarWidth * Mathf.Clamp01(ratio);
            hpFillRenderer.enabled = width > 0.001f;
            if (!hpFillRenderer.enabled)
                return;

            hpFillRenderer.transform.localScale = new Vector3(width, HpBarHeight, 1f);
            hpFillRenderer.transform.localPosition = new Vector3(-HpBarWidth * 0.5f + width * 0.5f, -1.48f, -0.02f);
        }

        void Update()
        {
            if (flashTimer <= 0f)
                return;

            flashTimer -= Time.deltaTime;
            var t = Mathf.Clamp01(flashTimer / 0.3f);
            bodyRenderer.color = Color.Lerp(baseColor, Color.white, t);
        }
    }
}
