using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class BallController : MonoBehaviour
    {
        readonly Dictionary<int, float> recentHits = new();

        PopHeroGame game;
        Rigidbody2D body;
        CircleCollider2D circleCollider;
        TrailRenderer trailRenderer;
        TextMesh launchCounterLabel;
        bool isFlying;
        float currentSpeed;
        float trailBoostTimer;
        Vector2 lastMoveDirection = Vector2.up;

        public bool IsFlying => isFlying;
        public CircleCollider2D BallCollider => circleCollider;
        public Vector2 Position => body != null ? body.position : (Vector2)transform.position;

        public void Initialize(PopHeroGame owner, Rigidbody2D rigidbody2D, CircleCollider2D collider2D, TrailRenderer runtimeTrail)
        {
            game = owner;
            body = rigidbody2D;
            circleCollider = collider2D;
            trailRenderer = runtimeTrail;
            ConfigureTrail(0f, 0f, false);
            BuildLaunchCounter();
        }

        public void PlaceAt(Vector2 worldPosition)
        {
            isFlying = false;
            currentSpeed = 0f;
            trailBoostTimer = 0f;
            lastMoveDirection = Vector2.up;
            recentHits.Clear();
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.isKinematic = true;
            body.position = worldPosition;
            transform.position = worldPosition;
            ConfigureTrail(0f, 0f, false);
            trailRenderer?.Clear();
        }

        public void Launch(Vector2 direction, float speed)
        {
            recentHits.Clear();
            isFlying = true;
            currentSpeed = Mathf.Clamp(speed, 0.1f, game.config.ball.maxSpeed);
            lastMoveDirection = direction.sqrMagnitude <= 0.001f ? Vector2.up : direction.normalized;
            body.isKinematic = false;
            body.velocity = lastMoveDirection * currentSpeed;
            ConfigureTrail(game.config.ball.baseTrailTime, game.config.ball.baseTrailWidth, true);
            trailRenderer?.Clear();
            BoostTrail();
        }

        public void StopImmediately()
        {
            isFlying = false;
            currentSpeed = 0f;
            trailBoostTimer = 0f;
            if (body == null)
                return;

            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.isKinematic = true;
            ConfigureTrail(0f, 0f, false);
            trailRenderer?.Clear();
        }

        public void SetLaunchCounter(int remaining, int maximum)
        {
            if (launchCounterLabel == null)
                BuildLaunchCounter();

            launchCounterLabel.text = $"{Mathf.Max(0, remaining)}/{Mathf.Max(1, maximum)}";
        }

        public void SetLaunchCounterVisible(bool isVisible)
        {
            if (launchCounterLabel == null)
                BuildLaunchCounter();

            launchCounterLabel.gameObject.SetActive(isVisible);
        }

        void Update()
        {
            UpdateTrailVisual();
        }

        void FixedUpdate()
        {
            if (game == null || !game.CanSimulate() || !isFlying)
                return;

            if (body.velocity.sqrMagnitude <= 0.001f)
            {
                body.velocity = lastMoveDirection.normalized * currentSpeed;
                return;
            }

            lastMoveDirection = body.velocity.normalized;
            body.velocity = lastMoveDirection * currentSpeed;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (game == null || game.State != RoundState.BallFlying || !isFlying)
                return;

            if (!CanTrigger(collision.collider))
                return;

            var block = collision.collider.GetComponent<BoardBlock>();
            var marker = collision.collider.GetComponent<ArenaSurfaceMarker>();
            var isReflectiveCollision = block != null || (marker != null && marker.surfaceType != ArenaSurfaceType.Bottom);
            if (!isReflectiveCollision)
                return;

            if (block != null)
                block.HandleBallHit(this);

            var contactNormal = collision.contactCount > 0 ? collision.GetContact(0).normal : -lastMoveDirection;
            var baseDirection = lastMoveDirection.sqrMagnitude > 0.001f
                ? lastMoveDirection
                : (body.velocity.sqrMagnitude > 0.001f ? body.velocity.normalized : Vector2.up);
            var reflectDirection = Vector2.Reflect(baseDirection, contactNormal).normalized;
            if (reflectDirection.sqrMagnitude <= 0.0001f)
                reflectDirection = baseDirection.normalized;

            currentSpeed = Mathf.Min(game.config.ball.maxSpeed, currentSpeed + game.config.ball.accelerationPerBounce);
            lastMoveDirection = reflectDirection;
            body.velocity = reflectDirection * currentSpeed;
            BoostTrail();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (game == null || game.State != RoundState.BallFlying || !isFlying)
                return;

            var marker = other.GetComponent<ArenaSurfaceMarker>();
            if (marker == null || marker.surfaceType != ArenaSurfaceType.Bottom)
                return;

            var landingPoint = other.ClosestPoint(transform.position);
            landingPoint.x = Mathf.Clamp(landingPoint.x, game.BoardRect.xMin + game.config.ball.radius, game.BoardRect.xMax - game.config.ball.radius);
            landingPoint.y = game.LaunchY;
            StopImmediately();
            game.OnBallReturned(landingPoint);
        }

        bool CanTrigger(Collider2D collider2D)
        {
            var instanceId = collider2D.GetInstanceID();
            if (recentHits.TryGetValue(instanceId, out var lastHitTime) && Time.time - lastHitTime < game.config.ball.hitCooldown)
                return false;

            recentHits[instanceId] = Time.time;
            return true;
        }

        void BuildLaunchCounter()
        {
            if (launchCounterLabel != null)
                return;

            launchCounterLabel = PrototypeVisualFactory.CreateTextObject("LaunchCounter", transform, "0/0", Color.white, 42, 0.06f, FontStyle.Bold);
            var offsetY = circleCollider != null ? circleCollider.radius + 0.34f : 0.4f;
            launchCounterLabel.transform.localPosition = new Vector3(0f, -offsetY, 0f);
            launchCounterLabel.gameObject.SetActive(false);
        }

        void BoostTrail()
        {
            if (trailRenderer == null)
                return;

            trailBoostTimer = Mathf.Max(trailBoostTimer, game.config.ball.boostDuration);
            UpdateTrailVisual();
        }

        void UpdateTrailVisual()
        {
            if (trailRenderer == null)
                return;

            if (!isFlying)
            {
                ConfigureTrail(0f, 0f, false);
                return;
            }

            trailBoostTimer = Mathf.Max(0f, trailBoostTimer - Time.deltaTime);
            var boostRatio = game.config.ball.boostDuration <= 0.001f
                ? 0f
                : Mathf.Clamp01(trailBoostTimer / game.config.ball.boostDuration);
            var time = Mathf.Lerp(game.config.ball.baseTrailTime, game.config.ball.boostTrailTime, boostRatio);
            var width = Mathf.Lerp(game.config.ball.baseTrailWidth, game.config.ball.boostTrailWidth, boostRatio);
            ConfigureTrail(time, width, true);
        }

        void ConfigureTrail(float trailTime, float width, bool emitting)
        {
            if (trailRenderer == null)
                return;

            trailRenderer.time = trailTime;
            trailRenderer.startWidth = width;
            trailRenderer.endWidth = width * 0.12f;
            trailRenderer.emitting = emitting;
        }
    }
}
