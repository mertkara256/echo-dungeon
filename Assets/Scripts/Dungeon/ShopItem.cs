using DG.Tweening;
using EchoDungeon.Core;
using EchoDungeon.Data;
using EchoDungeon.Player;
using TMPro;
using UnityEngine;

namespace EchoDungeon.Dungeon
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Renderer))]
    public sealed class ShopItem : MonoBehaviour
    {
        [Header("Shop Item")]
        [SerializeField] private ShopItemType itemType = ShopItemType.HealthPotion;
        [SerializeField] private string itemName = "Health Potion";
        [SerializeField] private string description = "+25 HP";
        [SerializeField] private int price = 10;

        [Header("Visual")]
        [SerializeField] private Color availableColor = new Color(1f, 0.73f, 0.24f, 1f);
        [SerializeField] private Color purchasedColor = new Color(0.25f, 0.28f, 0.3f, 1f);
        [SerializeField] private TextMeshPro priceLabel;

        private static readonly Color LabelColor = new Color(0.95f, 0.94f, 0.86f, 1f);

        private bool purchased;
        private PlayerStats nearbyPlayer;
        private Material itemMaterial;
        private Camera mainCamera;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            itemMaterial = GetComponent<Renderer>()?.material;
            mainCamera = Camera.main;
            TweenUtility.StartPulse(itemMaterial, availableColor, 0.35f, 0.65f, this, gameObject);
        }

        private void Update()
        {
            if (purchased || nearbyPlayer == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            if (GameInput.WasInteractPressed())
            {
                TryPurchase(nearbyPlayer);
            }
        }

        private void LateUpdate()
        {
            if (priceLabel == null)
            {
                return;
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    return;
                }
            }

            priceLabel.transform.rotation = Quaternion.LookRotation(priceLabel.transform.position - mainCamera.transform.position);
        }

        public void Configure(ShopItemType type, int itemPrice)
        {
            itemType = type;
            price = Mathf.Max(0, itemPrice);
            ApplyTypeDefaults();
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerStats stats = other.GetComponentInParent<PlayerStats>();
            if (stats == null || purchased)
            {
                return;
            }

            nearbyPlayer = stats;
            GameEvents.RaiseInteractionPrompt(GetInteractionLabel());
            GameEvents.RaiseNotification(itemName + " - " + price + " coins - " + description);
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerStats stats = other.GetComponentInParent<PlayerStats>();
            if (stats != null && stats == nearbyPlayer)
            {
                nearbyPlayer = null;
                GameEvents.ClearInteractionPrompt();
            }
        }

        private string GetInteractionLabel()
        {
            return itemName + " - " + price + " Coins";
        }

        private void TryPurchase(PlayerStats stats)
        {
            if (stats == null || purchased)
            {
                return;
            }

            if (CoinManager.Instance == null || !CoinManager.Instance.SpendCoins(price))
            {
                GameEvents.RaiseNotification("Not enough coins");
                return;
            }

            string feedback = ApplyPurchaseEffect(stats);
            purchased = true;
            nearbyPlayer = null;
            GameEvents.ClearInteractionPrompt();
            GameEvents.RaiseNotification(feedback);
            Debug.Log(feedback);

            DOTween.Kill(this);
            if (itemMaterial != null)
            {
                itemMaterial.DOColor(purchasedColor, 0.12f).SetTarget(this).SetLink(gameObject);
            }

            Destroy(gameObject, 0.2f);
        }

        private string ApplyPurchaseEffect(PlayerStats stats)
        {
            switch (itemType)
            {
                case ShopItemType.HealthPotion:
                    PlayerInventory healthInventory = stats.GetComponent<PlayerInventory>();
                    if (healthInventory != null)
                    {
                        healthInventory.AddItem(PlayerInventory.HealthPotionId, 1, false);
                    }

                    return "Health Potion added to inventory!";
                case ShopItemType.SpeedPotion:
                    PlayerInventory speedInventory = stats.GetComponent<PlayerInventory>();
                    if (speedInventory != null)
                    {
                        speedInventory.AddItem(PlayerInventory.SpeedBoostId, 1, false);
                    }

                    return "Speed Potion added to inventory!";
                case ShopItemType.StasisChargePack:
                    PlayerSpellCaster stasisCaster = stats.GetComponent<PlayerSpellCaster>();
                    if (stasisCaster != null)
                    {
                        stasisCaster.GrantStasisCharges(3);
                    }

                    return "Stop charges added: +3";
                case ShopItemType.PoisonChargePack:
                    PlayerSpellCaster poisonCaster = stats.GetComponent<PlayerSpellCaster>();
                    if (poisonCaster != null)
                    {
                        poisonCaster.GrantPoisonCharges(3);
                    }

                    return "Poison charges added: +3";
                case ShopItemType.RandomUpgrade:
                    return "Random upgrade gained: " + ApplyRandomUpgrade(stats);
                default:
                    return itemName + " purchased!";
            }
        }

        private static string ApplyRandomUpgrade(PlayerStats stats)
        {
            int roll = Random.Range(0, 5);
            switch (roll)
            {
                case 0:
                    stats.ApplyUpgrade(UpgradeType.MaxHealth, 10f);
                    return "+10 Max Health";
                case 1:
                    stats.ApplyUpgrade(UpgradeType.MoveSpeed, 1f);
                    return "+1 Move Speed";
                case 2:
                    stats.ApplyUpgrade(UpgradeType.Damage, 2f);
                    return "+10% Attack Damage";
                case 3:
                    stats.ApplyUpgrade(UpgradeType.AttackRate, 0.2f);
                    return "+10% Fire Rate";
                default:
                    stats.ApplyUpgrade(UpgradeType.Armor, 1f);
                    return "+1 Armor";
            }
        }

        private void ApplyTypeDefaults()
        {
            switch (itemType)
            {
                case ShopItemType.HealthPotion:
                    itemName = "Health Potion";
                    description = "Adds 1 health potion";
                    availableColor = new Color(0.9f, 0.2f, 0.2f, 1f);
                    break;
                case ShopItemType.SpeedPotion:
                    itemName = "Speed Potion";
                    description = "Adds 1 speed boost";
                    availableColor = new Color(0.2f, 0.8f, 1f, 1f);
                    break;
                case ShopItemType.StasisChargePack:
                    itemName = "Stop x3";
                    description = "Adds 3 Stop spell charges";
                    availableColor = new Color(0.24f, 0.9f, 1f, 1f);
                    break;
                case ShopItemType.PoisonChargePack:
                    itemName = "Poison x3";
                    description = "Adds 3 Poison spell charges";
                    availableColor = new Color(0.36f, 0.92f, 0.34f, 1f);
                    break;
                case ShopItemType.RandomUpgrade:
                    itemName = "Random Upgrade";
                    description = "Gain a random perk";
                    availableColor = new Color(0.62f, 0.43f, 0.98f, 1f);
                    break;
            }

            if (itemMaterial != null)
            {
                TweenUtility.StartPulse(itemMaterial, availableColor, 0.35f, 0.65f, this, gameObject);
            }

            EnsurePriceLabel();
        }

        private void EnsurePriceLabel()
        {
            if (priceLabel == null)
            {
                priceLabel = GetComponentInChildren<TextMeshPro>(true);
            }

            if (priceLabel == null)
            {
                Debug.LogWarning("ShopItem: Price label is missing on " + name + ".");
                return;
            }

            priceLabel.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            priceLabel.transform.localScale = Vector3.one;
            priceLabel.alignment = TextAlignmentOptions.Center;
            priceLabel.fontSize = 0.85f;
            priceLabel.textWrappingMode = TextWrappingModes.NoWrap;
            priceLabel.rectTransform.sizeDelta = new Vector2(2.6f, 0.8f);
            priceLabel.text = itemName + "\n" + price + "c";
            priceLabel.color = LabelColor;
        }
    }
}
