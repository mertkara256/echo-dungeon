using System;
using System.Collections;
using DG.Tweening;
using EchoDungeon.Combat;
using EchoDungeon.Core;
using EchoDungeon.Core.Interfaces;
using EchoDungeon.Player;
using UnityEngine;

namespace EchoDungeon.Enemy
{
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        [Header("Core Stats")]
        [SerializeField] protected float maxHealth = 40f;
        [SerializeField] protected float moveSpeed = 3f;
        [SerializeField] protected float damage = 10f;
        [SerializeField] protected float detectionRange = 14f;
        [SerializeField] protected float attackRange = 1.8f;
        [SerializeField] protected float attackCooldown = 1f;
        [SerializeField] protected float patrolRadius = 4f;
        [SerializeField] protected int scoreReward = 15;

        [Header("Ranged")]
        [SerializeField] protected bool isRanged;
        [SerializeField] protected float projectileSpeed = 16f;
        [SerializeField] protected float projectileLifeTime = 2f;
        [SerializeField] protected string projectileName = "EnemyProjectile";
        [SerializeField] protected float projectileVisualScale = 0.28f;
        [SerializeField] protected Color projectileColor = new Color(1f, 0.4f, 0.18f, 1f);

        [Header("Visual")]
        [SerializeField] protected Color bodyColor = Color.red;
        [SerializeField] private Color hitFlashColor = Color.white;
        [SerializeField] private float hitFlashDuration = 0.08f;
        [SerializeField] private float hitPauseDuration = 0.035f;

        [Header("Attack Animation")]
        [SerializeField] private float attackAnimDuration = 0.3f;
        [SerializeField] private float lungePeakTime = 0.35f;
        [SerializeField] private float lungeDistance = 0.6f;
        [SerializeField] private float attackScalePunch = 1.25f;

        [Header("Death")]
        [SerializeField] private float deathFallDuration = 0.2f;
        [SerializeField] private float deathFallAngle = 82f;
        [SerializeField] private float deathDespawnDelay = 0.08f;

        private float templateMaxHealth;
        private float templateMoveSpeed;
        private float templateDamage;
        private float templateDetectionRange;
        private float templateAttackRange;
        private float templateAttackCooldown;
        private float templatePatrolRadius;
        private int templateScoreReward;
        private float templateProjectileSpeed;

        private enum EnemyState
        {
            Idle,
            Patrol,
            Chase,
            Attack,
            Dead
        }

        private EnemyState currentState;
        private Transform playerTransform;
        private PlayerHealth playerHealth;
        private float currentHealth;
        private float idleTimer;
        private float nextAttackTime;
        private float spellFrozenUntil;
        private bool initialized;
        private bool deathProcessed;
        private Vector3 spawnPoint;
        private Vector3 patrolTarget;

        private bool isPlayingAttackAnim;
        private float attackAnimTimer;
        private Vector3 attackAnimStartPos;
        private Vector3 attackAnimLungeDir;
        private Vector3 originalScale;
        private bool hasForcedAggro;
        private bool hasPatrolTarget;

        private Renderer cachedRenderer;
        private Collider[] cachedColliders;

        public event Action<EnemyBase> Died;

        public bool IsDead { get; private set; }
        public bool IsAttacking { get { return isPlayingAttackAnim; } }
        public bool IsSpellFrozen { get { return Time.time < spellFrozenUntil; } }

        public float DistanceToPlayer
        {
            get
            {
                if (playerTransform == null)
                {
                    return float.MaxValue;
                }

                Vector3 a = transform.position;
                Vector3 b = playerTransform.position;
                a.y = 0f;
                b.y = 0f;
                return Vector3.Distance(a, b);
            }
        }

        protected Transform PlayerTransform
        {
            get { return playerTransform; }
        }

        public int ScoreReward
        {
            get { return scoreReward; }
        }

        private void Awake()
        {
            ApplyArchetypeDefaults();
            CaptureTemplateStats();
            CacheReferences();
            RefreshVisuals();
        }

        private void Update()
        {
            if (!initialized || IsDead)
            {
                return;
            }

            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            if (IsSpellFrozen)
            {
                return;
            }

            UpdateAttackAnimation();
            TickState();
        }

        private void OnDisable()
        {
            DOTween.Kill(cachedRenderer);
            RestoreBodyColor();
        }

        protected virtual void ApplyArchetypeDefaults()
        {
        }

        public void Initialize(Transform player, int difficultyLevel, bool bossVariant)
        {
            playerTransform = player;
            playerHealth = player != null ? player.GetComponent<PlayerHealth>() : null;

            RestoreTemplateStats();
            ApplyDifficulty(difficultyLevel, bossVariant);
            CacheReferences();

            currentHealth = maxHealth;
            nextAttackTime = 0f;
            spellFrozenUntil = 0f;
            IsDead = false;
            initialized = true;
            deathProcessed = false;
            spawnPoint = transform.position;
            hasForcedAggro = false;
            hasPatrolTarget = false;
            isPlayingAttackAnim = false;
            originalScale = transform.localScale;

            EnableColliders(true);
            RefreshVisuals();

            SetState(EnemyState.Idle);
        }

        public void ApplySpellFreeze(float duration)
        {
            if (IsDead || !initialized || duration <= 0f)
            {
                return;
            }

            spellFrozenUntil = Mathf.Max(spellFrozenUntil, Time.time + duration);
        }

        public void ApplyRuntimeProfile(
            float? maxHealthOverride = null,
            float? moveSpeedOverride = null,
            float? damageOverride = null,
            float? detectionRangeOverride = null,
            float? attackRangeOverride = null,
            float? attackCooldownOverride = null,
            float? patrolRadiusOverride = null,
            int? scoreRewardOverride = null,
            float? projectileSpeedOverride = null,
            Color? bodyColorOverride = null,
            Vector3? localScaleOverride = null,
            bool refillHealth = true)
        {
            if (maxHealthOverride.HasValue)
            {
                maxHealth = Mathf.Max(1f, maxHealthOverride.Value);
            }

            if (moveSpeedOverride.HasValue)
            {
                moveSpeed = Mathf.Max(0.1f, moveSpeedOverride.Value);
            }

            if (damageOverride.HasValue)
            {
                damage = Mathf.Max(1f, damageOverride.Value);
            }

            if (detectionRangeOverride.HasValue)
            {
                detectionRange = Mathf.Max(0.5f, detectionRangeOverride.Value);
            }

            if (attackRangeOverride.HasValue)
            {
                attackRange = Mathf.Max(0.1f, attackRangeOverride.Value);
            }

            if (attackCooldownOverride.HasValue)
            {
                attackCooldown = Mathf.Max(0.05f, attackCooldownOverride.Value);
            }

            if (patrolRadiusOverride.HasValue)
            {
                patrolRadius = Mathf.Max(0f, patrolRadiusOverride.Value);
            }

            if (scoreRewardOverride.HasValue)
            {
                scoreReward = Mathf.Max(0, scoreRewardOverride.Value);
            }

            if (projectileSpeedOverride.HasValue)
            {
                projectileSpeed = Mathf.Max(0.5f, projectileSpeedOverride.Value);
            }

            if (bodyColorOverride.HasValue)
            {
                bodyColor = bodyColorOverride.Value;
            }

            if (localScaleOverride.HasValue)
            {
                transform.localScale = localScaleOverride.Value;
            }

            CaptureTemplateStats();

            if (initialized && refillHealth)
            {
                currentHealth = maxHealth;
            }

            RefreshVisuals();
        }

        public void ForceChase()
        {
            SetState(EnemyState.Chase);
        }

        private void SetState(EnemyState state)
        {
            currentState = state;

            switch (currentState)
            {
                case EnemyState.Idle:
                    idleTimer = UnityEngine.Random.Range(0.45f, 1.2f);
                    break;
                case EnemyState.Patrol:
                    ClearPatrolTarget();
                    break;
                case EnemyState.Dead:
                    EnableColliders(false);
                    break;
            }
        }

        private void TickState()
        {
            switch (currentState)
            {
                case EnemyState.Idle:
                    TickIdleState();
                    break;
                case EnemyState.Patrol:
                    TickPatrolState();
                    break;
                case EnemyState.Chase:
                    TickChaseState();
                    break;
                case EnemyState.Attack:
                    TickAttackState();
                    break;
            }
        }

        private void TickIdleState()
        {
            if (CanDetectPlayer())
            {
                SetState(EnemyState.Chase);
                return;
            }

            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
            {
                SetState(EnemyState.Patrol);
            }
        }

        private void TickPatrolState()
        {
            if (CanDetectPlayer())
            {
                SetState(EnemyState.Chase);
                return;
            }

            MoveTowards(GetPatrolPoint(), 0.7f);
        }

        private void TickChaseState()
        {
            if (!CanDetectPlayer())
            {
                SetState(EnemyState.Patrol);
                return;
            }

            if (IsPlayerInAttackRange())
            {
                SetState(EnemyState.Attack);
                return;
            }

            MoveTowardsPlayer();
        }

        private void TickAttackState()
        {
            if (IsAttacking)
            {
                return;
            }

            if (!CanDetectPlayer())
            {
                SetState(EnemyState.Patrol);
                return;
            }

            if (!IsPlayerInAttackRange())
            {
                SetState(EnemyState.Chase);
                return;
            }

            FaceTowardsPlayer();
            TryAttack();
        }

        public bool CanDetectPlayer()
        {
            return playerTransform != null && !IsDead && (hasForcedAggro || DistanceToPlayer <= detectionRange);
        }

        public void ForceAggroPlayer()
        {
            hasForcedAggro = true;
        }

        public bool IsPlayerInAttackRange()
        {
            return playerTransform != null && !IsDead && DistanceToPlayer <= attackRange;
        }

        public void MoveTowardsPlayer()
        {
            if (playerTransform == null)
            {
                return;
            }

            MoveTowards(playerTransform.position);
        }

        public void MoveTowards(Vector3 worldPosition, float speedMultiplier = 1f)
        {
            Vector3 target = worldPosition;
            target.y = transform.position.y;

            Vector3 direction = target - transform.position;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector3 step = direction.normalized * moveSpeed * speedMultiplier * Time.deltaTime;
            transform.position += step;
            FaceTowards(worldPosition);
        }

        public void FaceTowardsPlayer()
        {
            if (playerTransform == null)
            {
                return;
            }

            FaceTowards(playerTransform.position);
        }

        public void FaceTowards(Vector3 worldPosition)
        {
            Vector3 direction = worldPosition - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 14f * Time.deltaTime);
        }

        public Vector3 GetPatrolPoint()
        {
            if (!hasPatrolTarget || Vector3.Distance(transform.position, patrolTarget) < 0.5f)
            {
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * patrolRadius;
                patrolTarget = spawnPoint + new Vector3(randomCircle.x, 0f, randomCircle.y);
                hasPatrolTarget = true;
            }

            return patrolTarget;
        }

        public void ClearPatrolTarget()
        {
            hasPatrolTarget = false;
        }

        public bool TryAttack()
        {
            if (Time.time < nextAttackTime || isPlayingAttackAnim)
            {
                return false;
            }

            nextAttackTime = Time.time + attackCooldown;

            if (isRanged)
            {
                FireProjectileAttack();
                StartAttackAnimation();
                return true;
            }

            if (playerHealth != null && IsPlayerInAttackRange())
            {
                playerHealth.TakeDamage(damage, gameObject);
                StartAttackAnimation();
                return true;
            }

            return false;
        }

        private void StartAttackAnimation()
        {
            isPlayingAttackAnim = true;
            attackAnimTimer = 0f;
            attackAnimStartPos = transform.position;
            originalScale = transform.localScale;

            if (playerTransform != null)
            {
                Vector3 dir = playerTransform.position - transform.position;
                dir.y = 0f;
                attackAnimLungeDir = dir.sqrMagnitude > 0.001f ? dir.normalized : transform.forward;
            }
            else
            {
                attackAnimLungeDir = transform.forward;
            }
        }

        private void UpdateAttackAnimation()
        {
            if (!isPlayingAttackAnim)
            {
                return;
            }

            attackAnimTimer += Time.deltaTime;
            float t = Mathf.Clamp01(attackAnimTimer / Mathf.Max(0.01f, attackAnimDuration));

            // Lunge curve: peaks at lungePeakTime then returns
            float lungeCurve;
            if (t < lungePeakTime)
            {
                lungeCurve = Mathf.Sin(t / lungePeakTime * Mathf.PI * 0.5f);
            }
            else
            {
                float returnT = (t - lungePeakTime) / (1f - lungePeakTime);
                lungeCurve = Mathf.Cos(returnT * Mathf.PI * 0.5f);
            }

            // Scale punch curve: peak at midpoint
            float scaleCurve = Mathf.Sin(t * Mathf.PI);

            float currentLunge = isRanged ? -lungeDistance * 0.5f : lungeDistance;
            transform.position = attackAnimStartPos + attackAnimLungeDir * currentLunge * lungeCurve;

            float scaleMultiplier = 1f + (attackScalePunch - 1f) * scaleCurve;
            transform.localScale = originalScale * scaleMultiplier;

            if (t >= 1f)
            {
                transform.position = attackAnimStartPos;
                transform.localScale = originalScale;
                isPlayingAttackAnim = false;
            }
        }

        public void TakeDamage(float amount, GameObject instigator = null)
        {
            if (IsDead || !initialized)
            {
                return;
            }

            currentHealth -= Mathf.Max(1f, amount);
            PlayHitFeedback(instigator, amount);
            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            if (deathProcessed)
            {
                return;
            }

            deathProcessed = true;
            IsDead = true;
            SetState(EnemyState.Dead);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(scoreReward);
            }

            if (Died != null)
            {
                Died.Invoke(this);
            }

            StartCoroutine(FallAndDespawnRoutine());
        }

        private IEnumerator FallAndDespawnRoutine()
        {
            Quaternion startRotation = transform.rotation;
            float sideSign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            Quaternion endRotation = Quaternion.AngleAxis(sideSign * deathFallAngle, transform.forward) * startRotation;

            float timer = 0f;
            float duration = Mathf.Max(0.01f, deathFallDuration);

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / duration);
                transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);
                yield return null;
            }

            transform.rotation = endRotation;

            if (deathDespawnDelay > 0f)
            {
                yield return new WaitForSeconds(deathDespawnDelay);
            }

            DespawnNow();
        }

        private void DespawnNow()
        {
            Destroy(gameObject);
        }

        protected virtual void FireProjectileAttack()
        {
            if (playerTransform == null)
            {
                return;
            }

            Vector3 spawnPosition = transform.position + Vector3.up + transform.forward * 0.8f;
            Vector3 direction = (playerTransform.position + Vector3.up * 0.7f - spawnPosition).normalized;
            LaunchProjectile(spawnPosition, direction);
        }

        protected void LaunchProjectile(Vector3 spawnPosition, Vector3 direction)
        {
            Projectile projectile = ProjectileFactory.CreateSphereProjectile(
                projectileName,
                spawnPosition,
                direction,
                projectileVisualScale,
                projectileColor);
            if (projectile != null)
            {
                projectile.Launch(
                    direction,
                    projectileSpeed,
                    damage,
                    projectileLifeTime,
                    gameObject,
                    false);
            }
        }

        private void ApplyDifficulty(int difficultyLevel, bool bossVariant)
        {
            float roomMultiplier = 1f + difficultyLevel * 0.08f;
            DayScaling dayScaling = DayProgression.CurrentScaling;

            maxHealth *= roomMultiplier;
            damage *= 1f + difficultyLevel * 0.05f;
            moveSpeed *= 1f + difficultyLevel * 0.01f;
            scoreReward = Mathf.RoundToInt(scoreReward * roomMultiplier);

            if (bossVariant)
            {
                maxHealth *= 3f;
                damage *= 1.8f;
                moveSpeed *= 1.15f;
                transform.localScale = transform.localScale * 1.6f;
                scoreReward = Mathf.RoundToInt(scoreReward * 2f);
            }

            maxHealth *= dayScaling.HealthMultiplier;
            damage *= dayScaling.DamageMultiplier;
            moveSpeed *= dayScaling.MoveSpeedMultiplier;
        }

        private void CaptureTemplateStats()
        {
            templateMaxHealth = maxHealth;
            templateMoveSpeed = moveSpeed;
            templateDamage = damage;
            templateDetectionRange = detectionRange;
            templateAttackRange = attackRange;
            templateAttackCooldown = attackCooldown;
            templatePatrolRadius = patrolRadius;
            templateScoreReward = scoreReward;
            templateProjectileSpeed = projectileSpeed;
        }

        private void RestoreTemplateStats()
        {
            maxHealth = templateMaxHealth;
            moveSpeed = templateMoveSpeed;
            damage = templateDamage;
            detectionRange = templateDetectionRange;
            attackRange = templateAttackRange;
            attackCooldown = templateAttackCooldown;
            patrolRadius = templatePatrolRadius;
            scoreReward = templateScoreReward;
            projectileSpeed = templateProjectileSpeed;
        }

        private void CacheReferences()
        {
            cachedRenderer = GetComponentInChildren<Renderer>();
            cachedColliders = GetComponentsInChildren<Collider>();
        }

        private void RefreshVisuals()
        {
            if (cachedRenderer != null)
            {
                cachedRenderer.material.color = bodyColor;
            }
        }

        private void RestoreBodyColor()
        {
            if (cachedRenderer != null)
            {
                cachedRenderer.material.color = bodyColor;
            }
        }

        private void PlayHitFeedback(GameObject instigator, float damageAmount)
        {
            if (cachedRenderer != null)
            {
                DOTween.Kill(cachedRenderer);
                cachedRenderer.material.color = hitFlashColor;
                cachedRenderer.material
                    .DOColor(bodyColor, hitFlashDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true)
                    .SetTarget(cachedRenderer)
                    .SetLink(gameObject);
            }

            if (instigator != null && instigator.GetComponent<PlayerStats>() != null)
            {
                float intensity = Mathf.Clamp01(damageAmount / Mathf.Max(10f, maxHealth * 0.35f));
                GameEvents.RaisePlayerHitLanded(intensity);
                GameEvents.RaiseHitPauseRequested(hitPauseDuration);
            }
        }

        private void EnableColliders(bool enabled)
        {
            if (cachedColliders == null)
            {
                return;
            }

            for (int i = 0; i < cachedColliders.Length; i++)
            {
                cachedColliders[i].enabled = enabled;
            }
        }
    }
}
