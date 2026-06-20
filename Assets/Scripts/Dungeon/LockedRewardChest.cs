using DG.Tweening;
using EchoDungeon.Core;
using EchoDungeon.Data;
using EchoDungeon.Player;
using EchoDungeon.UI;
using UnityEngine;

namespace EchoDungeon.Dungeon
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Renderer))]
    public sealed class LockedRewardChest : MonoBehaviour
    {
        private static readonly Color LockedColor = new Color(1f, 0.78f, 0.22f, 1f);
        private static readonly Color OpenedColor = new Color(0.3f, 0.32f, 0.36f, 1f);

        [SerializeField] private string draftTitle = "Locked Cache";
        [SerializeField] private string draftSubtitle = "Spend 1 key to claim 1 of 3 premium perks";

        private bool opened;
        private bool draftOpen;
        private int sourceRoomIndex;
        private PlayerInventory nearbyInventory;
        private Material chestMaterial;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            chestMaterial = GetComponent<Renderer>()?.material;
            TweenUtility.StartPulse(chestMaterial, LockedColor, 0.45f, 0.7f, this, gameObject);
        }

        private void Update()
        {
            if (opened || draftOpen || nearbyInventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            if (GameInput.WasInteractPressed())
            {
                TryOpen(nearbyInventory);
            }
        }

        public void Configure(int roomIndex, string title, string subtitle)
        {
            sourceRoomIndex = Mathf.Max(0, roomIndex);
            if (!string.IsNullOrWhiteSpace(title))
            {
                draftTitle = title;
            }

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                draftSubtitle = subtitle;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
            if (inventory == null || opened)
            {
                return;
            }

            nearbyInventory = inventory;
            if (inventory.HasItem(PlayerInventory.KeyId, 1))
            {
                GameEvents.RaiseInteractionPrompt("Unlock Cache (1 Key)");
                GameEvents.RaiseNotification("Press E to unlock the cache (1 key)");
            }
            else
            {
                GameEvents.RaiseInteractionPrompt("Need 1 Key");
                GameEvents.RaiseNotification("Cache locked - you need 1 key");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
            if (inventory != null && inventory == nearbyInventory)
            {
                nearbyInventory = null;
                GameEvents.ClearInteractionPrompt();
            }
        }

        private void TryOpen(PlayerInventory inventory)
        {
            if (inventory == null || opened || draftOpen)
            {
                return;
            }

            if (!inventory.TryConsume(PlayerInventory.KeyId, 1))
            {
                GameEvents.RaiseNotification("You need a key to open this cache");
                return;
            }

            var choices = UpgradeCatalog.BuildDraft(sourceRoomIndex, true, 3);
            if (choices.Count == 0)
            {
                inventory.AddItem(PlayerInventory.KeyId, 1);
                GameEvents.RaiseNotification("No premium reward is available right now");
                return;
            }

            draftOpen = true;
            nearbyInventory = null;
            GameEvents.ClearInteractionPrompt();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.RewardSelection);
            }

            PlayerStats stats = inventory.GetComponent<PlayerStats>();
            HUDController hud = FindFirstObjectByType<HUDController>();
            if (hud == null || !hud.ShowPerkDraft(
                draftTitle,
                draftSubtitle,
                choices,
                choice => CompleteOpen(stats, choice)))
            {
                draftOpen = false;
                inventory.AddItem(PlayerInventory.KeyId, 1);
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetState(GameState.Playing);
                }

                GameEvents.RaiseNotification("Perk selection UI is missing");
            }
        }

        private void CompleteOpen(PlayerStats stats, PerkChoice choice)
        {
            draftOpen = false;
            opened = true;

            if (stats != null && choice != null)
            {
                UpgradeEffectApplier.TryApply(stats, choice);
            }

            DOTween.Kill(this);
            if (chestMaterial != null)
            {
                chestMaterial.DOColor(OpenedColor, 0.18f).SetTarget(this).SetLink(gameObject);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Playing);
            }

            if (choice != null)
            {
                GameEvents.RaiseNotification("Cache reward: " + choice.Title);
            }
        }
    }
}
