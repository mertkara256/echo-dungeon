using System.Collections.Generic;
using EchoDungeon.Core.Interfaces;
using UnityEngine;

namespace EchoDungeon.Player
{
    public sealed class PoisonFieldSpell : GroundSpellField
    {
        [SerializeField] private float tickInterval = 0.5f;
        [SerializeField] private float damagePerTick = 5f;

        private float nextTickTime;
        private readonly Collider[] overlapResults = new Collider[64];

        public void ConfigureDamage(float interval, float tickDamage)
        {
            tickInterval = Mathf.Max(0.1f, interval);
            damagePerTick = Mathf.Max(1f, tickDamage);
        }

        protected override void OnInitialized()
        {
            nextTickTime = Time.time;
        }

        protected override void ApplyEffect()
        {
            if (Time.time < nextTickTime)
            {
                return;
            }

            nextTickTime = Time.time + Mathf.Max(0.1f, tickInterval);
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, Radius, overlapResults, ~0, QueryTriggerInteraction.Collide);
            HashSet<IDamageable> affected = new HashSet<IDamageable>();

            for (int i = 0; i < hitCount; i++)
            {
                if (!TryGetDamageable(overlapResults[i], out IDamageable damageable))
                {
                    continue;
                }

                if (damageable.IsDead || !affected.Add(damageable))
                {
                    continue;
                }

                damageable.TakeDamage(damagePerTick, Caster);
            }
        }
    }
}
