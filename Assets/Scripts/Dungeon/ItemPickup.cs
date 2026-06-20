using DG.Tweening;
using EchoDungeon.Core;
using EchoDungeon.Player;
using UnityEngine;

namespace EchoDungeon.Dungeon
{
    [RequireComponent(typeof(Collider))]
    public sealed class ItemPickup : MonoBehaviour
    {
        private static readonly Color PotionColor = new Color(0.9f, 0.2f, 0.2f, 1f);
        private static readonly Color SpeedColor = new Color(0.2f, 0.8f, 1f, 1f);
        private static readonly Color KeyColor = new Color(1f, 0.82f, 0.2f, 1f);

        [SerializeField] private string itemId = PlayerInventory.HealthPotionId;
        [SerializeField] private int amount = 1;

        private bool collected;
        private Material pulseMaterial;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            pulseMaterial = GetComponent<Renderer>()?.material;
            ApplyVisualColor();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected)
            {
                return;
            }

            PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
            if (inventory == null)
            {
                return;
            }

            Collect(inventory);
        }

        public void SetupDefaultPotion(int count)
        {
            itemId = PlayerInventory.HealthPotionId;
            amount = Mathf.Max(1, count);
            ApplyVisualColor();
        }

        public void SetupDefaultSpeedBoost(int count)
        {
            itemId = PlayerInventory.SpeedBoostId;
            amount = Mathf.Max(1, count);
            ApplyVisualColor();
        }

        public void SetupDefaultKey(int count)
        {
            itemId = PlayerInventory.KeyId;
            amount = Mathf.Max(1, count);
            ApplyVisualColor();
        }

        private void Collect(PlayerInventory inventory)
        {
            if (inventory == null)
            {
                return;
            }

            collected = true;
            DOTween.Kill(this);
            inventory.AddItem(itemId, amount);
            Destroy(gameObject);
        }

        private void ApplyVisualColor()
        {
            Color baseColor = PotionColor;
            if (itemId == PlayerInventory.SpeedBoostId)
            {
                baseColor = SpeedColor;
            }
            else if (itemId == PlayerInventory.KeyId)
            {
                baseColor = KeyColor;
            }

            TweenUtility.StartPulse(pulseMaterial, baseColor, 0.45f, 0.55f, this, gameObject);
        }
    }
}
