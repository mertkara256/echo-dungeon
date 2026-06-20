using UnityEngine;

namespace EchoDungeon.Enemy
{
    public sealed class TankEnemy : EnemyBase
    {
        protected override void ApplyArchetypeDefaults()
        {
            maxHealth = 85f;
            moveSpeed = 2f;
            damage = 18f;
            detectionRange = 12f;
            attackRange = 2.3f;
            attackCooldown = 1.4f;
            patrolRadius = 3f;
            scoreReward = 22;
            isRanged = false;
            bodyColor = new Color(0.45f, 0.2f, 0.1f, 1f);
        }
    }
}
