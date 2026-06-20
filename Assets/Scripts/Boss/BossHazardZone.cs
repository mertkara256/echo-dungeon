using System.Collections;
using System.Collections.Generic;
using EchoDungeon.Player;
using UnityEngine;

namespace EchoDungeon.Bosses
{
    [RequireComponent(typeof(Collider))]
    public sealed class BossHazardZone : MonoBehaviour
    {
        private readonly Dictionary<PlayerHealth, float> nextTickTimes = new Dictionary<PlayerHealth, float>();

        private float damage;
        private float tickInterval;
        private float duration;
        private GameObject owner;
        private bool initialized;

        public void Initialize(GameObject damageOwner, float damagePerTick, float interval, float activeDuration)
        {
            owner = damageOwner;
            damage = damagePerTick;
            tickInterval = Mathf.Max(0.1f, interval);
            duration = Mathf.Max(0.2f, activeDuration);
            initialized = true;

            Collider collider = GetComponent<Collider>();
            collider.isTrigger = true;
            StartCoroutine(LifeRoutine());
        }

        private void OnTriggerStay(Collider other)
        {
            if (!initialized)
            {
                return;
            }

            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null || playerHealth.IsDead)
            {
                return;
            }

            float now = Time.time;
            if (!nextTickTimes.TryGetValue(playerHealth, out float nextTick))
            {
                nextTick = 0f;
            }

            if (now < nextTick)
            {
                return;
            }

            nextTickTimes[playerHealth] = now + tickInterval;
            playerHealth.TakeDamage(damage, owner);
        }

        private IEnumerator LifeRoutine()
        {
            yield return new WaitForSeconds(duration);
            Destroy(gameObject);
        }
    }
}
