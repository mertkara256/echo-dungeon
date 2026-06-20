using UnityEngine;

namespace EchoDungeon.Enemy
{
    public sealed class MeleeEnemy : EnemyBase
    {
        protected override void ApplyArchetypeDefaults()
        {
            maxHealth = 42f;
            moveSpeed = 3.6f;
            damage = 11f;
            detectionRange = 13f;
            attackRange = 1.8f;
            attackCooldown = 1.1f;
            patrolRadius = 4f;
            scoreReward = 12;
            isRanged = false;
            bodyColor = new Color(0.9f, 0.3f, 0.3f, 1f);
        }
    }
}
