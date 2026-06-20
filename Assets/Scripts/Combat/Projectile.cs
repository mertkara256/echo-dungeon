using EchoDungeon.Core.Interfaces;
using EchoDungeon.Enemy;
using EchoDungeon.Player;
using UnityEngine;

namespace EchoDungeon.Combat
{
    [RequireComponent(typeof(Collider))]
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField] private bool isPlayerOwned = true;
        [SerializeField] private float speed = 20f;
        [SerializeField] private float damage = 8f;
        [SerializeField] private float lifeTime = 2f;

        private Vector3 direction;
        private float endLifeTime;
        private GameObject owner;
        private bool initialized;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnEnable()
        {
            if (!initialized)
            {
                return;
            }

            endLifeTime = Time.time + lifeTime;
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            transform.position += direction * speed * Time.deltaTime;

            if (Time.time >= endLifeTime)
            {
                Despawn();
            }
        }

        public void Launch(
            Vector3 launchDirection,
            float launchSpeed,
            float launchDamage,
            float launchLifeTime,
            GameObject launchOwner,
            bool ownerIsPlayer)
        {
            direction = launchDirection.normalized;
            speed = launchSpeed;
            damage = launchDamage;
            lifeTime = launchLifeTime;
            owner = launchOwner;
            isPlayerOwned = ownerIsPlayer;
            initialized = true;
            endLifeTime = Time.time + lifeTime;

            TrailRenderer trail = GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.Clear();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!initialized)
            {
                return;
            }

            if (other.isTrigger)
            {
                return;
            }

            GameObject otherRoot = other.transform.root.gameObject;

            if (owner != null && otherRoot == owner)
            {
                return;
            }

            if (isPlayerOwned && otherRoot.GetComponentInParent<PlayerHealth>() != null)
            {
                return;
            }

            if (!isPlayerOwned && otherRoot.GetComponentInParent<EnemyBase>() != null)
            {
                return;
            }

            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage, owner);
            }

            Despawn();
        }

        private void Despawn()
        {
            initialized = false;
            Destroy(gameObject);
        }
    }
}
