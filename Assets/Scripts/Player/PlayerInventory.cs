using System.Collections;
using EchoDungeon.Core;
using EchoDungeon.Data;
using UnityEngine;

namespace EchoDungeon.Player
{
    public sealed class PlayerInventory : MonoBehaviour
    {
        public const string HealthPotionId = "item_health_potion";
        public const string SpeedBoostId = "item_speed_boost";
        public const string KeyId = "item_key";

        [Header("Consumable Tuning")]
        [SerializeField] private float potionHealAmount = 35f;
        [SerializeField] private float speedBoostAmount = 3f;
        [SerializeField] private float speedBoostDuration = 6f;

        private int healthPotionCount;
        private int speedBoostCount;
        private int keyCount;

        private PlayerStats stats;
        private PlayerHealth health;
        private Coroutine speedBoostRoutine;

        private void Awake()
        {
            stats = GetComponent<PlayerStats>();
            health = GetComponent<PlayerHealth>();
            RaiseInventoryChanged();
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            if (GameInput.WasPotionPressed())
            {
                TryUseHealthPotion();
            }

            if (GameInput.WasSpeedBoostPressed())
            {
                TryUseSpeedBoost();
            }
        }

        public void AddItem(string itemId, int amount, bool showNotification = true)
        {
            if (amount <= 0)
            {
                return;
            }

            if (!TryAddKnownItem(itemId, amount))
            {
                return;
            }

            RaiseInventoryChanged();
            if (showNotification)
            {
                GameEvents.RaiseNotification("Picked: " + GetDisplayName(itemId) + " x" + amount);
            }
        }

        public int GetAmount(string itemId)
        {
            if (itemId == HealthPotionId)
            {
                return healthPotionCount;
            }

            if (itemId == SpeedBoostId)
            {
                return speedBoostCount;
            }

            if (itemId == KeyId)
            {
                return keyCount;
            }

            return 0;
        }

        public bool HasItem(string itemId, int amount = 1)
        {
            return GetAmount(itemId) >= Mathf.Max(1, amount);
        }

        public bool TryConsume(string itemId, int amount)
        {
            int requestedAmount = Mathf.Max(1, amount);
            if (!HasItem(itemId, requestedAmount))
            {
                return false;
            }

            TryAddKnownItem(itemId, -requestedAmount);
            RaiseInventoryChanged();
            return true;
        }

        private void TryUseHealthPotion()
        {
            if (!TryConsume(HealthPotionId, 1))
            {
                GameEvents.RaiseNotification("No health potion");
                return;
            }

            if (health != null)
            {
                health.Heal(potionHealAmount);
            }

            GameEvents.RaiseNotification("Health potion used");
        }

        private void TryUseSpeedBoost()
        {
            if (!TryConsume(SpeedBoostId, 1))
            {
                GameEvents.RaiseNotification("No speed boost");
                return;
            }

            if (speedBoostRoutine != null)
            {
                StopCoroutine(speedBoostRoutine);
            }

            speedBoostRoutine = StartCoroutine(SpeedBoostRoutine());
        }

        private IEnumerator SpeedBoostRoutine()
        {
            if (stats == null)
            {
                yield break;
            }

            stats.ApplyUpgrade(UpgradeType.MoveSpeed, speedBoostAmount);
            GameEvents.RaiseNotification("Speed boost active");
            yield return new WaitForSeconds(speedBoostDuration);
            stats.ApplyUpgrade(UpgradeType.MoveSpeed, -speedBoostAmount);
            GameEvents.RaiseNotification("Speed boost ended");
            speedBoostRoutine = null;
        }

        private bool TryAddKnownItem(string itemId, int amount)
        {
            if (itemId == HealthPotionId)
            {
                healthPotionCount = Mathf.Max(0, healthPotionCount + amount);
                return true;
            }

            if (itemId == SpeedBoostId)
            {
                speedBoostCount = Mathf.Max(0, speedBoostCount + amount);
                return true;
            }

            if (itemId == KeyId)
            {
                keyCount = Mathf.Max(0, keyCount + amount);
                return true;
            }

            return false;
        }

        private void RaiseInventoryChanged()
        {
            GameEvents.RaiseInventoryChanged(
                healthPotionCount,
                speedBoostCount,
                keyCount);
        }

        private string GetDisplayName(string itemId)
        {
            if (itemId == HealthPotionId)
            {
                return "Health Potion";
            }

            if (itemId == SpeedBoostId)
            {
                return "Speed Boost";
            }

            if (itemId == KeyId)
            {
                return "Dungeon Key";
            }

            return itemId;
        }
    }
}
