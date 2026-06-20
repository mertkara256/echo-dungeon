using UnityEngine;

namespace EchoDungeon.Combat
{
    public static class ProjectileFactory
    {
        public static Projectile CreateSphereProjectile(
            string projectileName,
            Vector3 position,
            Vector3 direction,
            float scale,
            Color color,
            bool addTrail = false)
        {
            GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = projectileName;
            projectileObject.transform.SetPositionAndRotation(position, SafeLookRotation(direction));
            projectileObject.transform.localScale = Vector3.one * scale;

            Renderer renderer = projectileObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }

            if (addTrail)
            {
                TrailRenderer trail = projectileObject.AddComponent<TrailRenderer>();
                trail.time = 0.34f;
                trail.startWidth = scale * 0.6f;
                trail.endWidth = 0f;
                trail.minVertexDistance = 0.02f;
            }

            EnsureProjectileComponents(projectileObject);
            return projectileObject.GetComponent<Projectile>();
        }

        public static Projectile CreateFromPrefab(
            GameObject projectilePrefab,
            Vector3 position,
            Vector3 direction)
        {
            if (projectilePrefab == null)
            {
                return null;
            }

            GameObject projectileObject = Object.Instantiate(
                projectilePrefab,
                position,
                SafeLookRotation(direction));
            projectileObject.SetActive(true);
            EnsureProjectileComponents(projectileObject);
            return projectileObject.GetComponent<Projectile>();
        }

        public static void EnsureProjectileComponents(GameObject projectileObject)
        {
            if (projectileObject == null)
            {
                return;
            }

            Collider collider = projectileObject.GetComponent<Collider>();
            if (collider == null)
            {
                collider = projectileObject.AddComponent<SphereCollider>();
            }

            collider.isTrigger = true;

            Rigidbody body = projectileObject.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = projectileObject.AddComponent<Rigidbody>();
            }

            body.useGravity = false;
            body.isKinematic = true;

            if (projectileObject.GetComponent<Projectile>() == null)
            {
                projectileObject.AddComponent<Projectile>();
            }
        }

        private static Quaternion SafeLookRotation(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return Quaternion.identity;
            }

            return Quaternion.LookRotation(direction.normalized);
        }
    }
}
