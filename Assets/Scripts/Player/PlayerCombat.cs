using EchoDungeon.Combat;
using EchoDungeon.Core;
using UnityEngine;

namespace EchoDungeon.Player
{
    [RequireComponent(typeof(PlayerStats))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [Header("Ranged Attack")]
        [SerializeField] private float projectileLifeTime = 6f;
        [SerializeField] private Vector3 projectileSpawnOffset = new Vector3(0f, 1f, 1.05f);
        [SerializeField] private float projectileVisualScale = 0.45f;
        [SerializeField] private string projectileName = "PlayerProjectile";
        [SerializeField] private Color projectileColor = new Color(0.25f, 0.85f, 1f, 1f);
        [SerializeField] private float rangedDamageMultiplier = 1f;
        [SerializeField] private float echoBarrageSpreadAngle = 11f;

        private PlayerStats stats;
        private PlayerMeleeAttack meleeAttack;
        private float nextRangedTime;
        private Camera mainCamera;
        private bool echoBarrageEnabled;

        private void Awake()
        {
            stats = GetComponent<PlayerStats>();
            meleeAttack = GetComponent<PlayerMeleeAttack>();
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            if (meleeAttack == null)
            {
                meleeAttack = GetComponent<PlayerMeleeAttack>();
            }

            if (meleeAttack != null && (meleeAttack.IsAttackInProgress || GameInput.WasSecondaryFirePressed()))
            {
                return;
            }

            if (GameInput.IsPrimaryFireHeld())
            {
                TryRangedAttack();
            }
        }

        public void EnableEchoBarrage()
        {
            echoBarrageEnabled = true;
        }

        private void TryRangedAttack()
        {
            float attackInterval = 1f / Mathf.Max(0.1f, stats.AttackRate);
            if (Time.time < nextRangedTime)
            {
                return;
            }

            nextRangedTime = Time.time + attackInterval;

            Vector3 spawnPosition = transform.position
                + Vector3.up * projectileSpawnOffset.y
                + transform.forward * projectileSpawnOffset.z
                + transform.right * projectileSpawnOffset.x;
            Vector3 fireDirection = GetAimDirection(spawnPosition);

            if (echoBarrageEnabled)
            {
                LaunchProjectile(spawnPosition, Quaternion.AngleAxis(-echoBarrageSpreadAngle, Vector3.up) * fireDirection);
                LaunchProjectile(spawnPosition, fireDirection);
                LaunchProjectile(spawnPosition, Quaternion.AngleAxis(echoBarrageSpreadAngle, Vector3.up) * fireDirection);
                return;
            }

            LaunchProjectile(spawnPosition, fireDirection);
        }

        private void LaunchProjectile(Vector3 spawnPosition, Vector3 fireDirection)
        {
            Projectile projectile = ProjectileFactory.CreateSphereProjectile(
                projectileName,
                spawnPosition,
                fireDirection,
                projectileVisualScale,
                projectileColor,
                true);
            if (projectile != null)
            {
                projectile.Launch(
                    fireDirection,
                    stats.ProjectileSpeed,
                    stats.Damage * rangedDamageMultiplier,
                    projectileLifeTime,
                    gameObject,
                    true);
            }
        }

        private Vector3 GetAimDirection(Vector3 fromPosition)
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    return transform.forward;
                }
            }

            Ray ray = mainCamera.ScreenPointToRay(GameInput.PointerPosition);
            Plane aimPlane = new Plane(Vector3.up, new Vector3(0f, fromPosition.y, 0f));

            if (aimPlane.Raycast(ray, out float enter))
            {
                Vector3 targetPoint = ray.GetPoint(enter);
                targetPoint.y = fromPosition.y;
                Vector3 direction = (targetPoint - fromPosition).normalized;

                if (direction.sqrMagnitude > 0.0001f)
                {
                    return direction;
                }
            }

            return transform.forward;
        }
    }
}
