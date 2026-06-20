using UnityEngine;

namespace EchoDungeon.Enemy
{
    public sealed class RangedEnemy : EnemyBase
    {
        protected override void ApplyArchetypeDefaults()
        {
            maxHealth = 30f;
            moveSpeed = 2.8f;
            damage = 9f;
            detectionRange = 17f;
            attackRange = 8.5f;
            attackCooldown = 1.45f;
            patrolRadius = 5f;
            scoreReward = 15;
            isRanged = true;
            projectileSpeed = 18f;
            bodyColor = new Color(0.85f, 0.75f, 0.2f, 1f);
        }
    }
}
