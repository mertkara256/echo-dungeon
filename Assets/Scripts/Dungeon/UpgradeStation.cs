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
    public sealed class UpgradeStation : MonoBehaviour
    {
        private static readonly Color ActiveColor = new Color(0.35f, 0.7f, 1f, 1f);
        private static readonly Color UsedColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        private bool used;
        private bool draftOpen;
        private int sourceRoomIndex;
        private PlayerStats nearbyPlayer;
        private Material stationMaterial;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            stationMaterial = GetComponent<Renderer>()?.material;
            TweenUtility.StartPulse(stationMaterial, ActiveColor, 0.35f, 0.7f, this, gameObject);
        }

        private void Update()
        {
            if (used || draftOpen || nearbyPlayer == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            if (GameInput.WasInteractPressed())
            {
                OpenDraft(nearbyPlayer);
            }
        }

        public void Configure(int roomIndex)
        {
            sourceRoomIndex = Mathf.Max(0, roomIndex);
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerStats playerStats = other.GetComponentInParent<PlayerStats>();
            if (playerStats == null || used)
            {
                return;
            }

            nearbyPlayer = playerStats;
            GameEvents.RaiseInteractionPrompt("Draft Perk");
            GameEvents.RaiseNotification("Press E to draft 1 of 3 perks");
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerStats playerStats = other.GetComponentInParent<PlayerStats>();
            if (playerStats != null && playerStats == nearbyPlayer)
            {
                nearbyPlayer = null;
                GameEvents.ClearInteractionPrompt();
            }
        }

        private void OpenDraft(PlayerStats stats)
        {
            if (stats == null || used || draftOpen)
            {
                return;
            }

            var choices = UpgradeCatalog.BuildDraft(sourceRoomIndex, false, 3);
            if (choices.Count == 0)
            {
                GameEvents.RaiseNotification("No perk choices are available");
                return;
            }

            draftOpen = true;
            GameEvents.ClearInteractionPrompt();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.RewardSelection);
            }

            HUDController hud = FindFirstObjectByType<HUDController>();
            if (hud == null || !hud.ShowPerkDraft(
                "Choose a Perk",
                "Pick 1 of 3 upgrades for this run",
                choices,
                choice => ApplySelectedUpgrade(stats, choice)))
            {
                draftOpen = false;
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetState(GameState.Playing);
                }

                GameEvents.RaiseNotification("Perk selection UI is missing");
            }
        }

        private void ApplySelectedUpgrade(PlayerStats stats, PerkChoice choice)
        {
            draftOpen = false;
            used = true;
            nearbyPlayer = null;

            if (stats != null && choice != null)
            {
                UpgradeEffectApplier.TryApply(stats, choice);
            }

            DOTween.Kill(this);
            if (stationMaterial != null)
            {
                stationMaterial.DOColor(UsedColor, 0.15f).SetTarget(this).SetLink(gameObject);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Playing);
            }

            if (choice != null)
            {
                GameEvents.RaiseNotification("Perk gained: " + choice.Title);
            }
        }
    }
}
