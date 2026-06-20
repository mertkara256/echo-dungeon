using UnityEngine;

namespace EchoDungeon.Enemy
{
    public sealed class EliteRangedEnemy : EnemyBase
    {
        [SerializeField] private float projectileSpreadAngle = 11f;

        protected override void ApplyArchetypeDefaults()
        {
            maxHealth = 58f;
            moveSpeed = 2.6f;
            damage = 10f;
            detectionRange = 24f;
            attackRange = 16f;
            attackCooldown = 0.82f;
            patrolRadius = 5.5f;
            scoreReward = 24;
            isRanged = true;
            projectileSpeed = 22f;
            bodyColor = new Color(1f, 0.48f, 0.16f, 1f);
        }

        protected override void FireProjectileAttack()
        {
            if (!CanDetectPlayer() || PlayerTransform == null)
            {
                return;
            }

            Vector3 spawnPosition = transform.position + Vector3.up * 1.05f + transform.forward * 0.9f;
            Vector3 centerDirection = (PlayerTransform.position + Vector3.up * 0.7f - spawnPosition).normalized;

            LaunchProjectile(spawnPosition, centerDirection);
            LaunchProjectile(spawnPosition, Quaternion.AngleAxis(projectileSpreadAngle, Vector3.up) * centerDirection);
            LaunchProjectile(spawnPosition, Quaternion.AngleAxis(-projectileSpreadAngle, Vector3.up) * centerDirection);
        }
    }
}
