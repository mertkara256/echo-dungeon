using System.Collections;
using EchoDungeon.Core;
using EchoDungeon.Core.Interfaces;
using UnityEngine;

namespace EchoDungeon.Player
{
    [RequireComponent(typeof(PlayerStats))]
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float invulnerabilityDuration = 0.25f;
        [SerializeField] private Color damageFlashColor = new Color(1f, 0.2f, 0.2f, 1f);
        [SerializeField] private float damageFlashDuration = 0.1f;

        private PlayerStats stats;
        private float currentHealth;
        private float lastDamageTime = -999f;
        private Renderer[] cachedRenderers;
        private Color[] defaultColors;
        private Coroutine damageFlashRoutine;

        public bool IsDead { get; private set; }

        public float CurrentHealth
        {
            get { return currentHealth; }
        }

        private void Awake()
        {
            stats = GetComponent<PlayerStats>();
            CacheVisualData();
            currentHealth = stats.MaxHealth;
            GameEvents.RaisePlayerHealthChanged(currentHealth, stats.MaxHealth);
        }

        private void OnEnable()
        {
            if (stats != null)
            {
                stats.StatsChanged += OnStatsChanged;
            }
        }

        private void OnDisable()
        {
            if (stats != null)
            {
                stats.StatsChanged -= OnStatsChanged;
            }

            if (damageFlashRoutine != null)
            {
                StopCoroutine(damageFlashRoutine);
                damageFlashRoutine = null;
            }

            RestoreDefaultColors();
        }

        public void TakeDamage(float amount, GameObject instigator = null)
        {
            if (IsDead)
            {
                return;
            }

            if (Time.time - lastDamageTime < invulnerabilityDuration)
            {
                return;
            }

            float mitigatedDamage = Mathf.Max(1f, amount - stats.Armor);
            currentHealth = Mathf.Clamp(currentHealth - mitigatedDamage, 0f, stats.MaxHealth);
            lastDamageTime = Time.time;

            GameEvents.RaisePlayerDamaged(mitigatedDamage);
            GameEvents.RaisePlayerHealthChanged(currentHealth, stats.MaxHealth);
            TriggerDamageFlash();

            if (currentHealth <= 0f)
            {
                IsDead = true;
                GameEvents.RaisePlayerDied();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead)
            {
                return;
            }

            currentHealth = Mathf.Clamp(currentHealth + amount, 0f, stats.MaxHealth);
            GameEvents.RaisePlayerHealthChanged(currentHealth, stats.MaxHealth);
        }

        public void ResetHealthToMax()
        {
            IsDead = false;
            currentHealth = stats.MaxHealth;
            GameEvents.RaisePlayerHealthChanged(currentHealth, stats.MaxHealth);
        }

        public void SetCurrentHealth(float health)
        {
            currentHealth = Mathf.Clamp(health, 0f, stats.MaxHealth);
            IsDead = currentHealth <= 0f;
            GameEvents.RaisePlayerHealthChanged(currentHealth, stats.MaxHealth);
        }

        private void OnStatsChanged()
        {
            if (IsDead)
            {
                return;
            }

            currentHealth = Mathf.Clamp(currentHealth, 0f, stats.MaxHealth);
            GameEvents.RaisePlayerHealthChanged(currentHealth, stats.MaxHealth);
        }

        private void CacheVisualData()
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

        private void TriggerDamageFlash()
        {
            if (cachedRenderers == null || cachedRenderers.Length == 0)
            {
                return;
            }

            if (damageFlashRoutine != null)
            {
                StopCoroutine(damageFlashRoutine);
            }

            damageFlashRoutine = StartCoroutine(DamageFlashRoutine());
        }

        private IEnumerator DamageFlashRoutine()
        {
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] != null)
                {
                    cachedRenderers[i].material.color = damageFlashColor;
                }
            }

            yield return new WaitForSeconds(damageFlashDuration);
            RestoreDefaultColors();
            damageFlashRoutine = null;
        }

        private void RestoreDefaultColors()
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
