using System;
using System.Collections;
using System.Collections.Generic;
using EchoDungeon.Combat;
using EchoDungeon.Core;
using EchoDungeon.Enemy;
using EchoDungeon.Player;
using UnityEngine;

namespace EchoDungeon.Bosses
{
    [RequireComponent(typeof(BossHealth))]
    public sealed class BossController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.4f;
        [SerializeField] private float turnSpeed = 10f;
        [SerializeField] private float stopDistance = 2.2f;

        [Header("Difficulty")]
        [SerializeField] private float healthScalePerDifficulty = 0.06f;
        [SerializeField] private float moveSpeedScalePerDifficulty = 0.02f;

        [Header("Phase Thresholds")]
        [SerializeField, Range(0.4f, 0.95f)] private float phaseTwoThreshold = 0.7f;
        [SerializeField, Range(0.1f, 0.6f)] private float phaseThreeThreshold = 0.4f;
        [SerializeField, Range(0.01f, 0.3f)] private float finalPhaseThreshold = 0.15f;

        [Header("Vulnerable Window")]
        [SerializeField] private float vulnerableWindowDuration = 1.2f;

        [Header("Projectile")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private string projectilePrefabName = "CorruptedGuardianProjectile";
        [SerializeField] private float projectileDamage = 14f;
        [SerializeField] private float projectileSpeed = 19f;
        [SerializeField] private float projectileLifetime = 3f;
        [SerializeField] private float projectileScale = 0.38f;
        [SerializeField] private Color projectileColor = new Color(0.85f, 0.25f, 0.95f, 1f);
        [SerializeField] private float projectileSingleCooldown = 3.2f;
        [SerializeField] private float projectileTripleCooldown = 5.4f;
        [SerializeField] private float projectileWindupDuration = 0.28f;
        [SerializeField] private float projectileTripleSpreadAngle = 16f;

        [Header("Melee Slam")]
        [SerializeField] private float meleeAttackRange = 3.6f;
        [SerializeField] private float slamRadius = 4.8f;
        [SerializeField] private float slamDamage = 24f;
        [SerializeField] private float slamCooldown = 2.8f;
        [SerializeField] private float slamWindupDuration = 0.85f;
        [SerializeField] private float slamRecoveryDuration = 0.32f;
        [SerializeField] private Color slamTelegraphColor = new Color(0.9f, 0.18f, 0.18f, 0.82f);

        [Header("Dash")]
        [SerializeField] private float dashDamage = 20f;
        [SerializeField] private float dashDistance = 8f;
        [SerializeField] private float dashDuration = 0.4f;
        [SerializeField] private float dashCooldown = 4.4f;
        [SerializeField] private float dashWindupDuration = 0.2f;
        [SerializeField] private float dashHitRadius = 1.4f;

        [Header("Hazard Area")]
        [SerializeField] private float areaCooldown = 6.6f;
        [SerializeField] private int hazardCount = 2;
        [SerializeField] private float hazardRadius = 2.2f;
        [SerializeField] private float hazardDamage = 11f;
        [SerializeField] private float hazardTickInterval = 0.55f;
        [SerializeField] private float hazardDuration = 5f;
        [SerializeField] private float hazardWarningDuration = 0.65f;
        [SerializeField] private Color hazardWarningColor = new Color(1f, 0.25f, 0.2f, 0.8f);
        [SerializeField] private Color hazardColor = new Color(0.6f, 0.05f, 0.08f, 0.85f);

        [Header("Minions")]
        [SerializeField] private GameObject minionPrefab;
        [SerializeField] private int minionsPerWave = 5;
        [SerializeField] private float minionCooldown = 8f;
        [SerializeField] private float minionSpawnRadius = 4.5f;
        [SerializeField] private float minionMaxHealth = 1f;
        [SerializeField] private float minionMoveSpeed = 7.4f;
        [SerializeField] private float minionDamage = 4f;
        [SerializeField] private float minionDetectionRange = 20f;
        [SerializeField] private float minionAttackRange = 1.25f;
        [SerializeField] private float minionAttackCooldown = 0.7f;
        [SerializeField] private int minionScoreReward = 4;
        [SerializeField] private float minionScale = 0.72f;
        [SerializeField] private Color minionColor = new Color(0.76f, 0.18f, 0.62f, 1f);

        private readonly List<GameObject> activeHazards = new List<GameObject>();
        private readonly List<EnemyBase> activeMinions = new List<EnemyBase>();

        private BossHealth health;
        private Transform playerTransform;
        private PlayerHealth playerHealth;
        private Coroutine actionRoutine;
        private BossPhase currentPhase = BossPhase.Phase1;
        private float baseMoveSpeed;
        private float baseProjectileDamage;
        private float baseSlamDamage;
        private float baseDashDamage;
        private float baseHazardDamage;
        private int roomDifficultyLevel;
        private bool isBusy;
        private bool inVulnerableWindow;
        private bool alternateTripleVolley;
        private float nextFinalComboTime;
        private float nextProjectileSingleTime;
        private float nextProjectileTripleTime;
        private float nextMeleeTime;
        private float nextDashTime;
        private float nextAreaTime;
        private float nextMinionTime;

        public event Action<BossController> BossDefeated;

        public BossPhase CurrentPhase
        {
            get { return currentPhase; }
        }

        private void Awake()
        {
            baseMoveSpeed = moveSpeed;
            baseProjectileDamage = projectileDamage;
            baseSlamDamage = slamDamage;
            baseDashDamage = dashDamage;
            baseHazardDamage = hazardDamage;
            CacheReferences();
        }

        private void OnEnable()
        {
            CacheReferences();
            if (health != null)
            {
                health.Died -= HandleBossDied;
                health.Died += HandleBossDied;
                health.HealthChanged -= HandleHealthChanged;
                health.HealthChanged += HandleHealthChanged;
                RefreshPhase(health.HealthRatio, true);
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleBossDied;
                health.HealthChanged -= HandleHealthChanged;
            }
        }

        private void Update()
        {
            if (health == null || health.IsDead)
            {
                return;
            }

            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            if (playerTransform == null || playerHealth == null || playerHealth.IsDead)
            {
                return;
            }

            if (isBusy || inVulnerableWindow)
            {
                FacePlayer();
                return;
            }

            RunPhaseLogic();
        }

        public void Setup(Transform player, int difficultyLevel)
        {
            CacheReferences();
            playerTransform = player;
            playerHealth = player != null ? player.GetComponent<PlayerHealth>() : null;
            roomDifficultyLevel = difficultyLevel;
            currentPhase = BossPhase.Phase1;
            alternateTripleVolley = false;
            nextFinalComboTime = 0f;
            nextProjectileSingleTime = 0f;
            nextProjectileTripleTime = 0f;
            nextMeleeTime = 0f;
            nextDashTime = 0f;
            nextAreaTime = 0f;
            nextMinionTime = 0f;

            DayScaling dayScaling = DayProgression.CurrentScaling;
            moveSpeed = baseMoveSpeed
                * (1f + Mathf.Max(0, difficultyLevel) * moveSpeedScalePerDifficulty)
                * dayScaling.MoveSpeedMultiplier;
            gameObject.name = "Corrupted Guardian";

            if (health != null)
            {
                float healthScale = (1f + Mathf.Max(0, difficultyLevel) * healthScalePerDifficulty)
                    * dayScaling.HealthMultiplier;
                health.ApplyMaxHealthScale(healthScale, true);
                RefreshPhase(health.HealthRatio, true);
            }

            ApplyDamageScale(dayScaling.DamageMultiplier);
            GameEvents.RaiseNotification("Corrupted Guardian awakened");
        }

        public void FacePlayer()
        {
            if (playerTransform != null)
            {
                FaceTowards(playerTransform.position);
            }
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
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        private void RunPhaseLogic()
        {
            float distance = DistanceToPlayer();
            float cadence = GetCadenceMultiplier();

            switch (currentPhase)
            {
                case BossPhase.Phase1:
                    RunPhaseOne(distance, cadence);
                    break;
                case BossPhase.Phase2:
                    RunPhaseTwo(distance, cadence);
                    break;
                case BossPhase.Phase3:
                    RunPhaseThree(distance, cadence);
                    break;
                default:
                    RunFinalPhase(distance, cadence);
                    break;
            }
        }

        private void RunPhaseOne(float distance, float cadence)
        {
            if (distance <= meleeAttackRange && CanUseMelee())
            {
                BeginAction(ExecuteMeleeAttack(cadence));
                return;
            }

            MoveTowardsPlayer();
        }

        private void RunPhaseTwo(float distance, float cadence)
        {
            if (distance <= meleeAttackRange && CanUseMelee())
            {
                BeginAction(ExecuteMeleeAttack(cadence));
                return;
            }

            if (alternateTripleVolley && CanUseTripleProjectile())
            {
                alternateTripleVolley = false;
                BeginAction(FireTripleProjectile(cadence));
                return;
            }

            if (CanUseSingleProjectile())
            {
                alternateTripleVolley = true;
                BeginAction(FireSingleProjectile(cadence));
                return;
            }

            MoveTowardsPlayer();
        }

        private void RunPhaseThree(float distance, float cadence)
        {
            if (CanUseMinions())
            {
                BeginAction(SpawnMinionWave(cadence));
                return;
            }

            if (CanUseAreaAttack())
            {
                BeginAction(ExecuteAreaAttack(cadence));
                return;
            }

            if (distance > meleeAttackRange * 1.15f && CanUseDash())
            {
                BeginAction(ExecuteDash(cadence));
                return;
            }

            if (CanUseTripleProjectile())
            {
                BeginAction(FireTripleProjectile(cadence));
                return;
            }

            if (distance <= meleeAttackRange && CanUseMelee())
            {
                BeginAction(ExecuteMeleeAttack(cadence));
                return;
            }

            MoveTowardsPlayer();
        }

        private void RunFinalPhase(float distance, float cadence)
        {
            if (Time.time >= nextFinalComboTime
                && CanUseMelee()
                && CanUseDash()
                && CanUseTripleProjectile())
            {
                nextFinalComboTime = Time.time + 6f / Mathf.Max(0.75f, cadence);
                BeginAction(FinalComboRoutine(cadence));
                return;
            }

            if (distance <= meleeAttackRange && CanUseMelee())
            {
                BeginAction(FinalSingleAttackRoutine(ExecuteMeleeAttack(cadence)));
                return;
            }

            if (CanUseDash() && distance > stopDistance)
            {
                BeginAction(FinalSingleAttackRoutine(ExecuteDash(cadence)));
                return;
            }

            if (CanUseTripleProjectile())
            {
                BeginAction(FinalSingleAttackRoutine(FireTripleProjectile(cadence)));
                return;
            }

            MoveTowardsPlayer(1.1f);
        }

        private IEnumerator FinalComboRoutine(float cadence)
        {
            yield return ExecuteMeleeAttack(cadence);
            yield return VulnerableWindowRoutine(vulnerableWindowDuration);

            yield return ExecuteDash(cadence);
            yield return VulnerableWindowRoutine(vulnerableWindowDuration);

            yield return FireTripleProjectile(cadence);
            yield return VulnerableWindowRoutine(vulnerableWindowDuration + 0.2f);
        }

        private IEnumerator FinalSingleAttackRoutine(IEnumerator attackRoutine)
        {
            yield return attackRoutine;
            yield return VulnerableWindowRoutine(vulnerableWindowDuration);
        }

        private IEnumerator ExecuteMeleeAttack(float cadence)
        {
            nextMeleeTime = Time.time + Mathf.Max(0.25f, slamCooldown / Mathf.Max(0.5f, cadence));
            GameObject telegraph = CreateSlamTelegraph(transform.position, transform.parent);

            float elapsed = 0f;
            while (elapsed < slamWindupDuration)
            {
                elapsed += Time.deltaTime;
                FacePlayer();
                yield return null;
            }

            if (telegraph != null)
            {
                Destroy(telegraph);
            }

            TryDamagePlayerInRadius(slamRadius, slamDamage);
            yield return new WaitForSeconds(slamRecoveryDuration / Mathf.Max(0.75f, cadence));
        }

        private GameObject CreateSlamTelegraph(Vector3 worldPosition, Transform parent)
        {
            GameObject telegraph = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            telegraph.name = "BossSlamTelegraph";
            telegraph.transform.position = worldPosition + Vector3.up * 0.03f;
            telegraph.transform.localScale = new Vector3(slamRadius * 2f, 0.03f, slamRadius * 2f);
            telegraph.transform.SetParent(parent, true);

            Collider collider = telegraph.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            Renderer renderer = telegraph.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = slamTelegraphColor;
            }

            return telegraph;
        }

        private IEnumerator FireSingleProjectile(float cadence)
        {
            if (playerTransform == null)
            {
                yield break;
            }

            nextProjectileSingleTime = Time.time + Mathf.Max(0.2f, projectileSingleCooldown / Mathf.Max(0.5f, cadence));
            yield return new WaitForSeconds(projectileWindupDuration / Mathf.Max(0.75f, cadence));

            Vector3 spawnPosition = transform.position + Vector3.up * 1.25f + transform.forward * 1.2f;
            Vector3 direction = (playerTransform.position + Vector3.up * 0.8f - spawnPosition).normalized;
            LaunchProjectile(spawnPosition, direction);
        }

        private IEnumerator FireTripleProjectile(float cadence)
        {
            if (playerTransform == null)
            {
                yield break;
            }

            nextProjectileTripleTime = Time.time + Mathf.Max(0.25f, projectileTripleCooldown / Mathf.Max(0.5f, cadence));
            yield return new WaitForSeconds((projectileWindupDuration + 0.08f) / Mathf.Max(0.75f, cadence));

            Vector3 spawnPosition = transform.position + Vector3.up * 1.2f + transform.forward * 1.15f;
            Vector3 baseDirection = (playerTransform.position + Vector3.up * 0.75f - spawnPosition).normalized;
            LaunchProjectile(spawnPosition, Quaternion.AngleAxis(-projectileTripleSpreadAngle, Vector3.up) * baseDirection);
            LaunchProjectile(spawnPosition, baseDirection);
            LaunchProjectile(spawnPosition, Quaternion.AngleAxis(projectileTripleSpreadAngle, Vector3.up) * baseDirection);
        }

        private void LaunchProjectile(Vector3 spawnPosition, Vector3 direction)
        {
            Projectile projectile = projectilePrefab != null
                ? ProjectileFactory.CreateFromPrefab(projectilePrefab, spawnPosition, direction)
                : ProjectileFactory.CreateSphereProjectile(
                    projectilePrefabName,
                    spawnPosition,
                    direction,
                    projectileScale,
                    projectileColor);
            if (projectile == null)
            {
                return;
            }

            projectile.Launch(
                direction,
                projectileSpeed,
                projectileDamage,
                projectileLifetime,
                gameObject,
                false);
        }

        private IEnumerator ExecuteDash(float cadence)
        {
            if (playerTransform == null)
            {
                yield break;
            }

            nextDashTime = Time.time + Mathf.Max(0.35f, dashCooldown / Mathf.Max(0.5f, cadence));

            float windup = dashWindupDuration / Mathf.Max(0.8f, cadence);
            float elapsedWindup = 0f;
            while (elapsedWindup < windup)
            {
                elapsedWindup += Time.deltaTime;
                FacePlayer();
                yield return null;
            }

            Vector3 start = transform.position;
            Vector3 targetPosition = playerTransform.position;
            targetPosition.y = start.y;
            Vector3 direction = targetPosition - start;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.01f)
            {
                direction = transform.forward;
            }

            direction.Normalize();
            Vector3 dashEnd = start + direction * dashDistance;
            bool appliedDamage = false;
            float timer = 0f;
            float duration = Mathf.Max(0.06f, dashDuration / Mathf.Max(0.8f, cadence));

            while (timer < duration)
            {
                timer += Time.deltaTime;
                transform.position = Vector3.Lerp(start, dashEnd, Mathf.Clamp01(timer / duration));
                FaceTowards(dashEnd);

                if (!appliedDamage && IsPlayerInRadius(dashHitRadius))
                {
                    playerHealth.TakeDamage(dashDamage, gameObject);
                    appliedDamage = true;
                }

                yield return null;
            }
        }

        private IEnumerator ExecuteAreaAttack(float cadence)
        {
            if (playerTransform == null)
            {
                yield break;
            }

            nextAreaTime = Time.time + Mathf.Max(0.35f, areaCooldown / Mathf.Max(0.5f, cadence));

            int count = Mathf.Max(1, hazardCount);
            List<Vector3> positions = new List<Vector3>(count);
            for (int i = 0; i < count; i++)
            {
                Vector2 scatter = UnityEngine.Random.insideUnitCircle * 2.4f;
                Vector3 targetPosition = playerTransform.position + new Vector3(scatter.x, 0f, scatter.y);
                targetPosition.y = transform.position.y;
                positions.Add(targetPosition);
                SpawnHazardDisk("BossHazardWarning", targetPosition, hazardWarningColor, transform.parent, false);
            }

            yield return new WaitForSeconds(hazardWarningDuration / Mathf.Max(0.8f, cadence));

            for (int i = 0; i < positions.Count; i++)
            {
                GameObject hazard = SpawnHazardDisk("BossHazardArea", positions[i], hazardColor, transform.parent, true);
                if (hazard == null)
                {
                    continue;
                }

                BossHazardZone zone = hazard.AddComponent<BossHazardZone>();
                zone.Initialize(gameObject, hazardDamage, hazardTickInterval, hazardDuration);
                activeHazards.Add(hazard);
            }
        }

        private GameObject SpawnHazardDisk(string objectName, Vector3 worldPosition, Color color, Transform parent, bool keepCollider)
        {
            GameObject disk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disk.name = objectName;
            disk.transform.position = worldPosition + Vector3.up * 0.04f;
            disk.transform.localScale = new Vector3(hazardRadius * 2f, 0.04f, hazardRadius * 2f);
            disk.transform.SetParent(parent, true);

            Renderer renderer = disk.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }

            Collider collider = disk.GetComponent<Collider>();
            if (collider != null && !keepCollider)
            {
                collider.enabled = false;
            }

            if (!keepCollider)
            {
                Destroy(disk, hazardWarningDuration + 0.1f);
            }

            return disk;
        }

        private IEnumerator SpawnMinionWave(float cadence)
        {
            if (playerTransform == null)
            {
                yield break;
            }

            nextMinionTime = Time.time + Mathf.Max(0.5f, minionCooldown / Mathf.Max(0.5f, cadence));

            for (int i = 0; i < Mathf.Max(1, minionsPerWave); i++)
            {
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle.normalized
                    * UnityEngine.Random.Range(2f, minionSpawnRadius);
                Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 1f, randomCircle.y);

                EnemyBase minion = SpawnMinion(spawnPosition);
                if (minion == null)
                {
                    continue;
                }

                ConfigureMinion(minion);
                minion.ForceAggroPlayer();
                minion.ForceChase();
                minion.Died += HandleMinionDied;
                activeMinions.Add(minion);
                yield return new WaitForSeconds(0.08f);
            }
        }

        private EnemyBase SpawnMinion(Vector3 position)
        {
            if (minionPrefab == null)
            {
                Debug.LogWarning("BossController: Minion prefab is missing.");
                return null;
            }

            GameObject minionObject = Instantiate(minionPrefab, position, Quaternion.identity, transform.parent);
            EnemyBase instance = minionObject.GetComponent<EnemyBase>();
            if (instance == null)
            {
                Debug.LogWarning("BossController: Minion prefab does not have an EnemyBase component.");
                return null;
            }

            instance.Initialize(playerTransform, roomDifficultyLevel, false);
            return instance;
        }

        private void ConfigureMinion(EnemyBase minion)
        {
            if (minion == null)
            {
                return;
            }

            EnemyCoinDrop coinDrop = minion.GetComponent<EnemyCoinDrop>();
            if (coinDrop != null)
            {
                coinDrop.Configure(0f, 1, 1, null);
            }

            DayScaling dayScaling = DayProgression.CurrentScaling;
            minion.ApplyRuntimeProfile(
                maxHealthOverride: minionMaxHealth * dayScaling.HealthMultiplier,
                moveSpeedOverride: minionMoveSpeed * dayScaling.MoveSpeedMultiplier,
                damageOverride: minionDamage * dayScaling.DamageMultiplier,
                detectionRangeOverride: minionDetectionRange,
                attackRangeOverride: minionAttackRange,
                attackCooldownOverride: minionAttackCooldown,
                scoreRewardOverride: minionScoreReward,
                bodyColorOverride: minionColor,
                localScaleOverride: Vector3.one * Mathf.Max(0.2f, minionScale),
                refillHealth: true);
        }

        private void HandleMinionDied(EnemyBase minion)
        {
            if (minion != null)
            {
                minion.Died -= HandleMinionDied;
            }

            activeMinions.Remove(minion);
        }

        private void ClearActiveMinions()
        {
            for (int i = activeMinions.Count - 1; i >= 0; i--)
            {
                EnemyBase minion = activeMinions[i];
                if (minion == null)
                {
                    continue;
                }

                minion.Died -= HandleMinionDied;
                Destroy(minion.gameObject);
            }

            activeMinions.Clear();
        }

        private void ClearHazards()
        {
            for (int i = activeHazards.Count - 1; i >= 0; i--)
            {
                if (activeHazards[i] != null)
                {
                    Destroy(activeHazards[i]);
                }
            }

            activeHazards.Clear();
        }

        private bool CanUseMelee()
        {
            return Time.time >= nextMeleeTime;
        }

        private bool CanUseSingleProjectile()
        {
            return Time.time >= nextProjectileSingleTime;
        }

        private bool CanUseTripleProjectile()
        {
            return Time.time >= nextProjectileTripleTime;
        }

        private bool CanUseDash()
        {
            return Time.time >= nextDashTime;
        }

        private bool CanUseAreaAttack()
        {
            return Time.time >= nextAreaTime;
        }

        private bool CanUseMinions()
        {
            return Time.time >= nextMinionTime;
        }

        private IEnumerator VulnerableWindowRoutine(float duration)
        {
            inVulnerableWindow = true;
            GameEvents.RaiseNotification("Corrupted Guardian staggered");
            yield return new WaitForSeconds(Mathf.Clamp(duration, 1f, 2f));
            inVulnerableWindow = false;
        }

        private void MoveTowardsPlayer(float speedMultiplier = 1f)
        {
            if (playerTransform == null)
            {
                return;
            }

            Vector3 current = transform.position;
            Vector3 target = playerTransform.position;
            current.y = 0f;
            target.y = 0f;

            if (Vector3.Distance(current, target) <= stopDistance)
            {
                FacePlayer();
                return;
            }

            Vector3 moveDirection = (target - current).normalized;
            transform.position += moveDirection * moveSpeed * speedMultiplier * Time.deltaTime;
            FaceTowards(playerTransform.position);
        }

        private float DistanceToPlayer()
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

        private bool IsPlayerInRadius(float radius)
        {
            if (playerHealth == null || playerHealth.IsDead)
            {
                return false;
            }

            Vector3 playerFlat = playerHealth.transform.position;
            Vector3 bossFlat = transform.position;
            playerFlat.y = 0f;
            bossFlat.y = 0f;
            return Vector3.Distance(playerFlat, bossFlat) <= radius;
        }

        private void TryDamagePlayerInRadius(float radius, float amount)
        {
            if (IsPlayerInRadius(radius))
            {
                playerHealth.TakeDamage(amount, gameObject);
            }
        }

        private float GetCadenceMultiplier()
        {
            switch (currentPhase)
            {
                case BossPhase.Phase2:
                    return 1.12f;
                case BossPhase.Phase3:
                    return 1.22f;
                case BossPhase.Final:
                    return 1.35f;
                default:
                    return 1f;
            }
        }

        private void BeginAction(IEnumerator routine)
        {
            if (routine == null || actionRoutine != null)
            {
                return;
            }

            actionRoutine = StartCoroutine(ActionRoutine(routine));
        }

        private IEnumerator ActionRoutine(IEnumerator routine)
        {
            isBusy = true;
            yield return routine;
            isBusy = false;
            actionRoutine = null;
        }

        private void HandleHealthChanged(float current, float max)
        {
            float ratio = max <= 0.01f ? 0f : current / max;
            RefreshPhase(ratio, false);
        }

        private void RefreshPhase(float healthRatio, bool silent)
        {
            BossPhase nextPhase = EvaluatePhase(healthRatio);
            if (nextPhase == currentPhase)
            {
                return;
            }

            currentPhase = nextPhase;
            if (!silent)
            {
                HandlePhaseChanged(currentPhase);
            }
        }

        private BossPhase EvaluatePhase(float ratio)
        {
            if (ratio <= finalPhaseThreshold)
            {
                return BossPhase.Final;
            }

            if (ratio <= phaseThreeThreshold)
            {
                return BossPhase.Phase3;
            }

            if (ratio <= phaseTwoThreshold)
            {
                return BossPhase.Phase2;
            }

            return BossPhase.Phase1;
        }

        private void HandlePhaseChanged(BossPhase phase)
        {
            if (phase == BossPhase.Phase2)
            {
                GameEvents.RaiseNotification("Corrupted Guardian entered Phase 2");
            }
            else if (phase == BossPhase.Phase3)
            {
                GameEvents.RaiseNotification("Corrupted Guardian entered Phase 3");
            }
            else if (phase == BossPhase.Final)
            {
                GameEvents.RaiseNotification("Corrupted Guardian is enraged");
            }
        }

        private void HandleBossDied()
        {
            StopAllCoroutines();
            isBusy = false;
            inVulnerableWindow = false;
            ClearActiveMinions();
            ClearHazards();

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            BossDefeated?.Invoke(this);
            GameEvents.RaiseNotification("Corrupted Guardian defeated");
            Destroy(gameObject, 1.25f);
        }

        private void CacheReferences()
        {
            health = GetComponent<BossHealth>();
        }

        private void ApplyDamageScale(float damageMultiplier)
        {
            float scale = Mathf.Max(0.01f, damageMultiplier);
            projectileDamage = baseProjectileDamage * scale;
            slamDamage = baseSlamDamage * scale;
            dashDamage = baseDashDamage * scale;
            hazardDamage = baseHazardDamage * scale;
        }
    }
}
