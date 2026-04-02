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
        int snapshotHp = -1;
        int snapshotMaxHp = -1;

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

            nameLabel = PrototypeVisualFactory.CreateTextObject("EnemyName", transform, "\u654c\u4eba", Color.white, 15, 0.11f);
            nameLabel.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            intentLabel = PrototypeVisualFactory.CreateTextObject("EnemyIntent", transform, "\u653b\u51fb 0", new Color(1f, 0.78f, 0.34f, 1f), 16, 0.085f);
            intentLabel.transform.localPosition = new Vector3(0f, 2.15f, 0f);
            hpLabel = PrototypeVisualFactory.CreateTextObject("EnemyHp", transform, "0/0", Color.white, 15, 0.08f, FontStyle.Normal);
            hpLabel.transform.localPosition = new Vector3(0f, -2.2f, 0f);
        }

        public void SetEnemy(EnemyData enemyData)
        {
            currentEnemy = enemyData;
            snapshotHp = -1;
            snapshotMaxHp = -1;
            baseColor = enemyData.AccentColor;
            Refresh();
        }

        public void SetPreviewDamage(int pendingDamage)
        {
            // 飞行中的伤害只显示在左侧累计面板，不再驱动敌人血条或数字。
        }

        public void ClearPreviewDamage(bool refreshDisplay = true)
        {
            // 兼容旧调用，当前不再维护飞行中的敌人虚血显示。
        }

        public void SetHpSnapshot(int hp, int maxHp)
        {
            snapshotHp = Mathf.Max(0, hp);
            snapshotMaxHp = Mathf.Max(1, maxHp);
            RefreshHpBar();
        }

        public void Refresh()
        {
            if (currentEnemy == null)
                return;

            snapshotHp = -1;
            snapshotMaxHp = -1;
            bodyRenderer.color = baseColor;
            nameLabel.text = currentEnemy.DisplayName;
            intentLabel.text = currentEnemy.CurrentHp > 0 ? $"\u653b\u51fb {currentEnemy.AttackDamage}" : string.Empty;
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

            var baseHp = snapshotHp >= 0 ? snapshotHp : currentEnemy.CurrentHp;
            var maxHp = snapshotMaxHp > 0 ? snapshotMaxHp : currentEnemy.MaxHp;
            var displayHp = Mathf.Max(0, baseHp);
            var realRatio = maxHp <= 0 ? 0f : displayHp / (float)maxHp;

            hpLabel.text = $"{displayHp}/{maxHp}";
            UpdateBar(hpFillRenderer, realRatio, new Color(0.98f, 0.92f, 0.72f, 1f));
            hpPreviewRenderer.enabled = false;
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
