using System;
using EchoDungeon.Data;
using UnityEngine;

namespace EchoDungeon.Player
{
    public sealed class PlayerStats : MonoBehaviour
    {
        [Header("Base Stats")]
        [SerializeField] private float baseMaxHealth = 100f;
        [SerializeField] private float baseMoveSpeed = 6f;
        [SerializeField] private float baseDamage = 18f;
        [SerializeField] private float baseAttackRate = 2f;
        [SerializeField] private float baseProjectileSpeed = 9.5f;
        [SerializeField] private float baseArmor = 0f;

        public event Action StatsChanged;

        public float MaxHealth { get; private set; }
        public float MoveSpeed { get; private set; }
        public float Damage { get; private set; }
        public float AttackRate { get; private set; }
        public float ProjectileSpeed { get; private set; }
        public float Armor { get; private set; }

        private void Awake()
        {
            ResetToDefaults();
        }

        public void ResetToDefaults()
        {
            MaxHealth = baseMaxHealth;
            MoveSpeed = baseMoveSpeed;
            Damage = baseDamage;
            AttackRate = baseAttackRate;
            ProjectileSpeed = baseProjectileSpeed;
            Armor = baseArmor;

            NotifyStatsChanged();
        }

        public void ApplyUpgrade(UpgradeType upgradeType, float amount)
        {
            switch (upgradeType)
            {
                case UpgradeType.MaxHealth:
                    MaxHealth = Mathf.Max(10f, MaxHealth + amount);
                    break;
                case UpgradeType.Damage:
                    Damage = Mathf.Max(1f, Damage + amount);
                    break;
                case UpgradeType.AttackRate:
                    AttackRate = Mathf.Clamp(AttackRate + amount, 0.3f, 15f);
                    break;
                case UpgradeType.MoveSpeed:
                    MoveSpeed = Mathf.Clamp(MoveSpeed + amount, 2f, 20f);
                    break;
                case UpgradeType.ProjectileSpeed:
                    ProjectileSpeed = Mathf.Clamp(ProjectileSpeed + amount, 5f, 60f);
                    break;
                case UpgradeType.Armor:
                    Armor = Mathf.Clamp(Armor + amount, 0f, 200f);
                    break;
            }

            NotifyStatsChanged();
        }

        public void SetFromSave(
            float maxHealth,
            float moveSpeed,
            float damage,
            float attackRate,
            float projectileSpeed,
            float armor)
        {
            MaxHealth = Mathf.Max(10f, maxHealth);
            MoveSpeed = Mathf.Clamp(moveSpeed, 2f, 20f);
            Damage = Mathf.Max(1f, damage);
            AttackRate = Mathf.Clamp(attackRate, 0.3f, 15f);
            ProjectileSpeed = Mathf.Clamp(projectileSpeed, 5f, 60f);
            Armor = Mathf.Clamp(armor, 0f, 200f);

            NotifyStatsChanged();
        }

        private void NotifyStatsChanged()
        {
            if (StatsChanged != null)
            {
                StatsChanged.Invoke();
            }
        }
    }
}
