using System;
using EchoDungeon.Core.Interfaces;
using UnityEngine;

namespace EchoDungeon.Bosses
{
    [RequireComponent(typeof(Collider))]
    public sealed class BossHealth : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 480f;
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.45f, 0.45f, 1f);
        [SerializeField] private float hitFlashDuration = 0.08f;

        private float baseMaxHealth;
        private float currentHealth;
        private bool isDead;
        private Renderer[] cachedRenderers;
        private Color[] defaultColors;
        private Coroutine flashRoutine;

        public event Action<float, float> HealthChanged;
        public event Action Died;

        public bool IsDead
        {
            get { return isDead; }
        }

        public float CurrentHealth
        {
            get { return currentHealth; }
        }

        public float MaxHealth
        {
            get { return maxHealth; }
        }

        public float HealthRatio
        {
            get { return maxHealth <= 0.01f ? 0f : currentHealth / maxHealth; }
        }

        private void Awake()
        {
            baseMaxHealth = maxHealth;
            CacheRenderers();
            ResetHealthToMax();
        }

        private void OnDisable()
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
            }

            RestoreColors();
        }

        public void ApplyMaxHealthScale(float scale, bool refill)
        {
            maxHealth = Mathf.Max(100f, baseMaxHealth * Mathf.Max(0.25f, scale));

            if (refill)
            {
                currentHealth = maxHealth;
            }
            else
            {
                currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            }

            isDead = currentHealth <= 0f;
            RaiseHealthChanged();
        }

        public void ResetHealthToMax()
        {
            isDead = false;
            currentHealth = maxHealth;
            RaiseHealthChanged();
        }

        public void TakeDamage(float amount, GameObject instigator = null)
        {
            if (isDead)
            {
                return;
            }

            currentHealth = Mathf.Clamp(currentHealth - Mathf.Max(1f, amount), 0f, maxHealth);
            PlayHitFlash();
            RaiseHealthChanged();

            if (currentHealth <= 0f)
            {
                isDead = true;
                Died?.Invoke();
            }
        }

        private void RaiseHealthChanged()
        {
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void CacheRenderers()
        {
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            defaultColors = new Color[cachedRenderers.Length];

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] != null)
                {
                    defaultColors[i] = cachedRenderers[i].material.color;
                }
            }
        }

        private void PlayHitFlash()
        {
            if (cachedRenderers == null || cachedRenderers.Length == 0)
            {
                return;
            }

            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }

            flashRoutine = StartCoroutine(HitFlashRoutine());
        }

        private System.Collections.IEnumerator HitFlashRoutine()
        {
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] != null)
                {
                    cachedRenderers[i].material.color = hitFlashColor;
                }
            }

            yield return new WaitForSeconds(hitFlashDuration);
            RestoreColors();
            flashRoutine = null;
        }

        private void RestoreColors()
        {
            if (cachedRenderers == null || defaultColors == null)
            {
                return;
            }

            int count = Mathf.Min(cachedRenderers.Length, defaultColors.Length);
            for (int i = 0; i < count; i++)
            {
                if (cachedRenderers[i] != null)
                {
                    cachedRenderers[i].material.color = defaultColors[i];
                }
            }
        }
    }
}
