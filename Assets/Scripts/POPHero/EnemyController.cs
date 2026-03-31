using UnityEngine;

namespace POPHero
{
    public class EnemyController : MonoBehaviour
    {
        const float HpBarWidth = 2.5f;
        const float HpBarHeight = 0.16f;

        EnemyData currentEnemy;
        SpriteRenderer bodyRenderer;
        SpriteRenderer coreRenderer;
        SpriteRenderer hpFillRenderer;
        SpriteRenderer hpPreviewRenderer;
        TextMesh nameLabel;
        TextMesh intentLabel;
        TextMesh hpLabel;
        Color baseColor = Color.white;
        float flashTimer;
        int pendingDamagePreview;

        public EnemyData CurrentEnemy => currentEnemy;

        public void Initialize(PopHeroGame owner)
        {
            bodyRenderer = PrototypeVisualFactory.CreateSpriteObject("EnemyBody", transform, PrototypeVisualFactory.SquareSprite, Color.white, 10, new Vector2(2.2f, 2.2f)).GetComponent<SpriteRenderer>();
            coreRenderer = PrototypeVisualFactory.CreateSpriteObject("EnemyCore", transform, PrototypeVisualFactory.CircleSprite, new Color(1f, 1f, 1f, 0.2f), 11, Vector2.one * 0.72f).GetComponent<SpriteRenderer>();
            coreRenderer.transform.localPosition = new Vector3(0f, 0.12f, 0f);

            var hpBack = PrototypeVisualFactory.CreateSpriteObject("HpBack", transform, PrototypeVisualFactory.SquareSprite, new Color(0f, 0f, 0f, 0.55f), 12, new Vector2(2.8f, 0.3f)).GetComponent<SpriteRenderer>();
            hpBack.transform.localPosition = new Vector3(0f, -1.8f, 0f);

            hpFillRenderer = PrototypeVisualFactory.CreateSpriteObject("HpFill", transform, PrototypeVisualFactory.SquareSprite, new Color(0.98f, 0.92f, 0.72f, 1f), 13, new Vector2(HpBarWidth, HpBarHeight)).GetComponent<SpriteRenderer>();
            hpFillRenderer.transform.localPosition = new Vector3(0f, -1.8f, -0.02f);
            hpPreviewRenderer = PrototypeVisualFactory.CreateSpriteObject("HpPreview", transform, PrototypeVisualFactory.SquareSprite, new Color(0.56f, 0.16f, 0.18f, 0.92f), 14, new Vector2(HpBarWidth, HpBarHeight)).GetComponent<SpriteRenderer>();
            hpPreviewRenderer.transform.localPosition = new Vector3(0f, -1.8f, -0.015f);

            nameLabel = PrototypeVisualFactory.CreateTextObject("EnemyName", transform, "敌人", Color.white, 15, 0.11f);
            nameLabel.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            intentLabel = PrototypeVisualFactory.CreateTextObject("EnemyIntent", transform, "攻击 0", new Color(1f, 0.78f, 0.34f, 1f), 16, 0.085f);
            intentLabel.transform.localPosition = new Vector3(0f, 2.15f, 0f);
            hpLabel = PrototypeVisualFactory.CreateTextObject("EnemyHp", transform, "0/0", Color.white, 15, 0.08f, FontStyle.Normal);
            hpLabel.transform.localPosition = new Vector3(0f, -2.2f, 0f);
        }

        public void SetEnemy(EnemyData enemyData)
        {
            currentEnemy = enemyData;
            pendingDamagePreview = 0;
            baseColor = enemyData.AccentColor;
            Refresh();
        }

        public void SetPreviewDamage(int pendingDamage)
        {
            pendingDamagePreview = Mathf.Max(0, pendingDamage);
            RefreshHpBar();
        }

        public void ClearPreviewDamage()
        {
            pendingDamagePreview = 0;
            RefreshHpBar();
        }

        public void Refresh()
        {
            if (currentEnemy == null)
                return;

            bodyRenderer.color = baseColor;
            nameLabel.text = currentEnemy.DisplayName;
            intentLabel.text = currentEnemy.CurrentHp > 0 ? $"攻击 {currentEnemy.AttackDamage}" : string.Empty;
            coreRenderer.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.08f, 0.24f, currentEnemy.CurrentHp / (float)currentEnemy.MaxHp));
            RefreshHpBar();
        }

        public void PlayHitFeedback(bool wasKillingBlow)
        {
            flashTimer = wasKillingBlow ? 0.32f : 0.14f;
            if (wasKillingBlow)
                bodyRenderer.color = Color.white;
        }

        void RefreshHpBar()
        {
            if (currentEnemy == null)
                return;

            var displayHp = Mathf.Max(0, currentEnemy.CurrentHp - pendingDamagePreview);
            var realRatio = currentEnemy.MaxHp <= 0 ? 0f : currentEnemy.CurrentHp / (float)currentEnemy.MaxHp;
            var previewRatio = currentEnemy.MaxHp <= 0 ? 0f : displayHp / (float)currentEnemy.MaxHp;
            var previewLossRatio = Mathf.Max(0f, realRatio - previewRatio);

            hpLabel.text = $"{displayHp}/{currentEnemy.MaxHp}";
            UpdateBar(hpFillRenderer, realRatio, new Color(0.98f, 0.92f, 0.72f, 1f));
            UpdateBar(hpPreviewRenderer, previewLossRatio, new Color(0.56f, 0.16f, 0.18f, 0.92f), previewRatio);
            hpPreviewRenderer.enabled = pendingDamagePreview > 0 && previewLossRatio > 0.001f;
        }

        void UpdateBar(SpriteRenderer renderer, float ratio, Color color, float startRatio = 0f)
        {
            ratio = Mathf.Clamp01(ratio);
            startRatio = Mathf.Clamp01(startRatio);
            renderer.color = color;
            renderer.enabled = ratio > 0.001f;
            if (!renderer.enabled)
                return;

            var width = HpBarWidth * ratio;
            renderer.transform.localScale = new Vector3(width, HpBarHeight, 1f);
            renderer.transform.localPosition = new Vector3(-HpBarWidth * 0.5f + HpBarWidth * startRatio + width * 0.5f, -1.8f, renderer.transform.localPosition.z);
        }

        void Update()
        {
            if (flashTimer <= 0f)
                return;

            flashTimer -= Time.deltaTime;
            var t = Mathf.Clamp01(flashTimer / 0.32f);
            bodyRenderer.color = Color.Lerp(baseColor, Color.white, t);
        }
    }
}
