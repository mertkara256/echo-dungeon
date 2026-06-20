using System.Collections.Generic;
using EchoDungeon.Enemy;
using UnityEngine;

namespace EchoDungeon.Player
{
    public sealed class StasisFieldSpell : GroundSpellField
    {
        [SerializeField] private float refreshInterval = 0.15f;

        private float nextRefreshTime;
        private readonly Collider[] overlapResults = new Collider[48];

        protected override void OnInitialized()
        {
            nextRefreshTime = Time.time;
        }

        protected override void ApplyEffect()
        {
            if (Time.time < nextRefreshTime)
            {
                return;
            }

            nextRefreshTime = Time.time + Mathf.Max(0.05f, refreshInterval);
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, Radius, overlapResults, ~0, QueryTriggerInteraction.Collide);
            HashSet<EnemyBase> affected = new HashSet<EnemyBase>();

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = overlapResults[i];
                if (hit == null)
                {
                    continue;
                }

                EnemyBase enemy = hit.GetComponentInParent<EnemyBase>();
                if (enemy == null || enemy.IsDead || !affected.Add(enemy))
                {
                    continue;
                }

                enemy.ApplySpellFreeze(RemainingDuration + 0.05f);
            }
        }
    }
}
