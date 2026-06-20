using System;
using System.Collections;
using System.Collections.Generic;
using EchoDungeon.Core;
using EchoDungeon.Data;
using EchoDungeon.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EchoDungeon.UI
{
    public sealed class HUDController : MonoBehaviour
    {
        [Header("Theme")]
        [SerializeField] private Color primaryTextColor = new Color(0.92f, 0.9f, 0.84f, 1f);
        [SerializeField] private Color mutedTextColor = new Color(0.62f, 0.66f, 0.72f, 1f);
        [SerializeField] private Color hpAccentColor = new Color(0.82f, 0.12f, 0.16f, 1f);
        [SerializeField] private Color goldAccentColor = new Color(1f, 0.73f, 0.24f, 1f);
        [SerializeField] private Color upgradeAccentColor = new Color(0.62f, 0.43f, 0.98f, 1f);

        [Header("Main HUD")]
        [SerializeField] private HealthBarUI healthBar;
        [SerializeField] private SpellCooldownBarUI stasisCooldownBar;
        [SerializeField] private SpellCooldownBarUI poisonCooldownBar;
        [SerializeField] private RoomProgressUI roomProgress;
        [SerializeField] private InteractionPromptUI interactionPrompt;
        [SerializeField] private Image damageOverlay;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI scoreValueText;
        [SerializeField] private TextMeshProUGUI coinValueText;
        [SerializeField] private TextMeshProUGUI potionCountText;
        [SerializeField] private TextMeshProUGUI boostCountText;
        [SerializeField] private TextMeshProUGUI keyCountText;
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private TextMeshProUGUI stateText;

        [Header("Groups")]
        [SerializeField] private CanvasGroup notificationGroup;
        [SerializeField] private CanvasGroup stateGroup;

        [Header("Perk Draft")]
        [SerializeField] private GameObject perkDraftOverlay;
        [SerializeField] private CanvasGroup perkDraftGroup;
        [SerializeField] private TextMeshProUGUI perkDraftTitleText;
        [SerializeField] private TextMeshProUGUI perkDraftSubtitleText;
        [SerializeField] private PerkCard[] perkCards = new PerkCard[3];

        [Header("Day Transition")]
        [SerializeField] private GameObject dayTransitionOverlay;
        [SerializeField] private CanvasGroup dayTransitionGroup;
        [SerializeField] private TextMeshProUGUI dayTransitionTitleText;
        [SerializeField] private TextMeshProUGUI dayTransitionSubtitleText;

        private PlayerSpellCaster playerSpellCaster;
        private IReadOnlyList<PerkChoice> activeDraftChoices;
        private Action<PerkChoice> activeDraftSelected;
        private Coroutine notificationRoutine;
        private Coroutine damageOverlayRoutine;
        private Coroutine statePulseRoutine;
        private Coroutine dayTransitionRoutine;

        [Serializable]
        private sealed class PerkCard
        {
            [SerializeField] private Button button;
            [SerializeField] private Image background;
            [SerializeField] private TextMeshProUGUI titleText;
            [SerializeField] private TextMeshProUGUI descriptionText;
            [SerializeField] private TextMeshProUGUI hotkeyText;

            public Button Button { get { return button; } }
            public Image Background { get { return background; } }
            public TextMeshProUGUI TitleText { get { return titleText; } }
            public TextMeshProUGUI DescriptionText { get { return descriptionText; } }
            public TextMeshProUGUI HotkeyText { get { return hotkeyText; } }

            public bool IsReady
            {
                get
                {
                    return button != null
                        && background != null
                        && titleText != null
                        && descriptionText != null
                        && hotkeyText != null;
                }
            }
        }

        private void Awake()
        {
            WirePerkButtons();
            HidePerkDraftOverlay();

            if (damageOverlay != null)
            {
                damageOverlay.color = Color.clear;
            }

            if (notificationGroup != null)
            {
                notificationGroup.alpha = 0f;
                notificationGroup.blocksRaycasts = false;
            }

            if (stateGroup != null)
            {
                stateGroup.alpha = 0f;
                stateGroup.blocksRaycasts = false;
            }

            if (dayTransitionOverlay != null)
            {
                dayTransitionOverlay.SetActive(false);
            }
        }

        private void Start()
        {
            SyncPlayerVitals();
            SyncInventoryFromPlayer();
            SyncSpellCooldowns();
            SyncDayFromGameManager();
            SyncCoinsFromManager();
            SyncScoreFromGameManager();
        }

        private void Update()
        {
            SyncSpellCooldowns();

            if (activeDraftChoices == null)
            {
                return;
            }

            if (GameInput.WasDraftChoicePressed(0))
            {
                ResolveDraftChoice(0);
            }
            else if (GameInput.WasDraftChoicePressed(1))
            {
                ResolveDraftChoice(1);
            }
            else if (GameInput.WasDraftChoicePressed(2))
            {
                ResolveDraftChoice(2);
            }
        }

        private void OnEnable()
        {
            GameEvents.PlayerHealthChanged += HandleHealthChanged;
            GameEvents.PlayerDamaged += HandlePlayerDamaged;
            GameEvents.ScoreChanged += HandleScoreChanged;
            GameEvents.CoinsChanged += HandleCoinsChanged;
            GameEvents.RoomProgressChanged += HandleRoomChanged;
            GameEvents.DayChanged += HandleDayChanged;
            GameEvents.GameStateChanged += HandleStateChanged;
            GameEvents.InventoryChanged += HandleInventoryChanged;
            GameEvents.SpellChargesChanged += HandleSpellChargesChanged;
            GameEvents.NotificationRaised += HandleNotification;
            GameEvents.InteractionPromptChanged += HandleInteractionPromptChanged;
        }

        private void OnDisable()
        {
            GameEvents.PlayerHealthChanged -= HandleHealthChanged;
            GameEvents.PlayerDamaged -= HandlePlayerDamaged;
            GameEvents.ScoreChanged -= HandleScoreChanged;
            GameEvents.CoinsChanged -= HandleCoinsChanged;
            GameEvents.RoomProgressChanged -= HandleRoomChanged;
            GameEvents.DayChanged -= HandleDayChanged;
            GameEvents.GameStateChanged -= HandleStateChanged;
            GameEvents.InventoryChanged -= HandleInventoryChanged;
            GameEvents.SpellChargesChanged -= HandleSpellChargesChanged;
            GameEvents.NotificationRaised -= HandleNotification;
            GameEvents.InteractionPromptChanged -= HandleInteractionPromptChanged;

            StopUiRoutine(ref notificationRoutine);
            StopUiRoutine(ref damageOverlayRoutine);
            StopUiRoutine(ref statePulseRoutine);
            StopUiRoutine(ref dayTransitionRoutine);
        }

        public bool ShowPerkDraft(
            string title,
            string subtitle,
            IReadOnlyList<PerkChoice> choices,
            Action<PerkChoice> onSelected)
        {
            if (choices == null || choices.Count == 0 || perkDraftOverlay == null || !HasAnyReadyPerkCard())
            {
                HidePerkDraftOverlay();
                return false;
            }

            activeDraftChoices = choices;
            activeDraftSelected = onSelected;

            if (perkDraftTitleText != null)
            {
                perkDraftTitleText.text = string.IsNullOrWhiteSpace(title) ? "Choose a Perk" : title;
            }

            if (perkDraftSubtitleText != null)
            {
                perkDraftSubtitleText.text = (subtitle ?? string.Empty) + "\nClick a card or press 1, 2, or 3.";
            }

            for (int i = 0; i < perkCards.Length; i++)
            {
                PerkCard card = perkCards[i];
                bool hasChoice = card != null && card.IsReady && i < activeDraftChoices.Count;
                if (card != null && card.Button != null)
                {
                    card.Button.gameObject.SetActive(hasChoice);
                }

                if (!hasChoice)
                {
                    continue;
                }

                PerkChoice choice = activeDraftChoices[i];
                card.Background.color = GetPerkCardColor(choice.Rarity);
                card.TitleText.text = choice.Title
                    + "\n<size=15><color=#"
                    + ColorUtility.ToHtmlStringRGB(GetRarityColor(choice.Rarity))
                    + ">"
                    + choice.Rarity
                    + "</color></size>";
                card.DescriptionText.text = choice.Description;
                card.HotkeyText.text = "[" + (i + 1) + "] Pick";
            }

            perkDraftOverlay.SetActive(true);
            if (perkDraftGroup != null)
            {
                perkDraftGroup.alpha = 1f;
                perkDraftGroup.interactable = true;
                perkDraftGroup.blocksRaycasts = true;
            }

            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem != null && perkCards.Length > 0 && perkCards[0] != null && perkCards[0].Button != null)
            {
                eventSystem.SetSelectedGameObject(perkCards[0].Button.gameObject);
            }

            return true;
        }

        private void WirePerkButtons()
        {
            for (int i = 0; i < perkCards.Length; i++)
            {
                if (perkCards[i] == null || perkCards[i].Button == null)
                {
                    continue;
                }

                int capturedIndex = i;
                perkCards[i].Button.onClick.AddListener(delegate { ResolveDraftChoice(capturedIndex); });
            }
        }

        private bool HasAnyReadyPerkCard()
        {
            for (int i = 0; i < perkCards.Length; i++)
            {
                if (perkCards[i] != null && perkCards[i].IsReady)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResolveDraftChoice(int index)
        {
            if (activeDraftChoices == null || index < 0 || index >= activeDraftChoices.Count)
            {
                return;
            }

            PerkChoice choice = activeDraftChoices[index];
            if (choice == null)
            {
                return;
            }

            Action<PerkChoice> onSelected = activeDraftSelected;
            onSelected?.Invoke(choice);
            HidePerkDraftOverlay();
        }

        private void HidePerkDraftOverlay()
        {
            activeDraftChoices = null;
            activeDraftSelected = null;

            if (perkDraftGroup != null)
            {
                perkDraftGroup.alpha = 0f;
                perkDraftGroup.interactable = false;
                perkDraftGroup.blocksRaycasts = false;
            }

            if (perkDraftOverlay != null)
            {
                perkDraftOverlay.SetActive(false);
            }

            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                eventSystem.SetSelectedGameObject(null);
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (healthBar != null)
            {
                healthBar.SetHealth(current, max);
            }
        }

        private void HandlePlayerDamaged(float damageAmount)
        {
            if (healthBar != null)
            {
                healthBar.PlayDamageFlash();
            }

            if (damageOverlay == null)
            {
                return;
            }

            StopUiRoutine(ref damageOverlayRoutine);
            damageOverlayRoutine = StartCoroutine(DamageOverlayRoutine(Mathf.Clamp01(0.12f + damageAmount * 0.01f)));
        }

        private void HandleScoreChanged(int score)
        {
            if (scoreValueText != null)
            {
                scoreValueText.text = "SCORE " + Mathf.Max(0, score);
            }
        }

        private void HandleCoinsChanged(int coins)
        {
            if (coinValueText != null)
            {
                coinValueText.text = "COIN " + Mathf.Max(0, coins);
            }
        }

        private void HandleRoomChanged(int current, int total)
        {
            if (roomProgress != null)
            {
                roomProgress.SetRoom(current, total);
            }
        }

        private void HandleDayChanged(int currentDay, int maxDay)
        {
            if (dayText != null)
            {
                dayText.text = "DAY " + currentDay + " / " + maxDay;
            }

            if (currentDay > 1)
            {
                ShowDayTransition(currentDay);
            }
        }

        private void HandleInventoryChanged(int potionCount, int speedBoostCount, int keyCount)
        {
            if (potionCountText != null)
            {
                potionCountText.text = potionCount.ToString();
            }

            if (boostCountText != null)
            {
                boostCountText.text = speedBoostCount.ToString();
            }

            if (keyCountText != null)
            {
                keyCountText.text = keyCount.ToString();
            }
        }

        private void HandleSpellChargesChanged(int stasisCharges, int poisonCharges)
        {
            SyncSpellCooldowns();
        }

        private void HandleStateChanged(GameState state)
        {
            if (stateText == null || stateGroup == null)
            {
                return;
            }

            bool visible = state != GameState.Playing
                && state != GameState.Booting
                && state != GameState.DayTransition;
            stateGroup.alpha = visible ? 1f : 0f;
            stateGroup.blocksRaycasts = false;
            stateText.text = visible ? state.ToString().ToUpperInvariant() : string.Empty;

            if (visible)
            {
                StopUiRoutine(ref statePulseRoutine);
                statePulseRoutine = StartCoroutine(PulseRectRoutine(stateGroup.transform as RectTransform));
            }
        }

        private void HandleNotification(string message)
        {
            StopUiRoutine(ref notificationRoutine);
            notificationRoutine = StartCoroutine(NotificationRoutine(message));
        }

        private void HandleInteractionPromptChanged(string message)
        {
            if (interactionPrompt == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                interactionPrompt.Hide();
            }
            else
            {
                interactionPrompt.Show(message);
            }
        }

        private IEnumerator NotificationRoutine(string message)
        {
            if (notificationGroup == null || notificationText == null)
            {
                yield break;
            }

            notificationText.text = message;
            yield return FadeCanvasGroup(notificationGroup, 1f, 0.14f);
            yield return new WaitForSecondsRealtime(2.1f);
            yield return FadeCanvasGroup(notificationGroup, 0f, 0.18f);
            notificationText.text = string.Empty;
            notificationRoutine = null;
        }

        private void ShowDayTransition(int day)
        {
            if (dayTransitionOverlay == null || dayTransitionGroup == null)
            {
                return;
            }

            StopUiRoutine(ref dayTransitionRoutine);
            dayTransitionRoutine = StartCoroutine(DayTransitionOverlayRoutine(day));
        }

        private IEnumerator DayTransitionOverlayRoutine(int day)
        {
            dayTransitionOverlay.SetActive(true);
            if (dayTransitionTitleText != null)
            {
                dayTransitionTitleText.text = "DAY " + day + " BEGINS";
            }

            if (dayTransitionSubtitleText != null)
            {
                dayTransitionSubtitleText.text = GetDayTransitionSubtitle(day);
            }

            yield return FadeCanvasGroup(dayTransitionGroup, 1f, 0.18f);
            yield return new WaitForSecondsRealtime(1.12f);
            yield return FadeCanvasGroup(dayTransitionGroup, 0f, 0.28f);

            dayTransitionOverlay.SetActive(false);
            dayTransitionRoutine = null;
        }

        private IEnumerator DamageOverlayRoutine(float targetAlpha)
        {
            Color color = new Color(hpAccentColor.r, hpAccentColor.g, hpAccentColor.b, targetAlpha);
            damageOverlay.color = color;

            float timer = 0f;
            const float duration = 0.22f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                color.a = Mathf.Lerp(targetAlpha, 0f, timer / duration);
                damageOverlay.color = color;
                yield return null;
            }

            damageOverlay.color = Color.clear;
            damageOverlayRoutine = null;
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup group, float target, float duration)
        {
            float start = group.alpha;
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, target, timer / Mathf.Max(0.01f, duration));
                yield return null;
            }

            group.alpha = target;
        }

        private IEnumerator PulseRectRoutine(RectTransform rect)
        {
            if (rect == null)
            {
                yield break;
            }

            float timer = 0f;
            const float duration = 0.22f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float scale = 1f + Mathf.Sin(timer / duration * Mathf.PI) * 0.08f;
                rect.localScale = Vector3.one * scale;
                yield return null;
            }

            rect.localScale = Vector3.one;
            statePulseRoutine = null;
        }

        private void StopUiRoutine(ref Coroutine routine)
        {
            if (routine == null)
            {
                return;
            }

            StopCoroutine(routine);
            routine = null;
        }

        private void SyncInventoryFromPlayer()
        {
            PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
            if (inventory == null)
            {
                return;
            }

            HandleInventoryChanged(
                inventory.GetAmount(PlayerInventory.HealthPotionId),
                inventory.GetAmount(PlayerInventory.SpeedBoostId),
                inventory.GetAmount(PlayerInventory.KeyId));
        }

        private void SyncPlayerVitals()
        {
            PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
            PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
            if (playerHealth != null && playerStats != null)
            {
                HandleHealthChanged(playerHealth.CurrentHealth, playerStats.MaxHealth);
            }
        }

        private void SyncSpellCooldowns()
        {
            if (stasisCooldownBar == null || poisonCooldownBar == null)
            {
                return;
            }

            if (playerSpellCaster == null)
            {
                playerSpellCaster = FindFirstObjectByType<PlayerSpellCaster>();
                if (playerSpellCaster == null)
                {
                    stasisCooldownBar.SetChargeCooldown(0f, 1f, 0, false);
                    poisonCooldownBar.SetChargeCooldown(0f, 1f, 0, false);
                    return;
                }
            }

            stasisCooldownBar.SetChargeCooldown(
                playerSpellCaster.StasisCooldownRemaining,
                playerSpellCaster.StasisCooldownDuration,
                playerSpellCaster.StasisCharges,
                playerSpellCaster.IsStasisUnlocked);
            poisonCooldownBar.SetChargeCooldown(
                playerSpellCaster.PoisonCooldownRemaining,
                playerSpellCaster.PoisonCooldownDuration,
                playerSpellCaster.PoisonCharges,
                playerSpellCaster.IsPoisonUnlocked);
        }

        private void SyncDayFromGameManager()
        {
            if (GameManager.Instance == null)
            {
                HandleDayChanged(1, DayProgression.DefaultMaxDay);
                return;
            }

            HandleDayChanged(GameManager.Instance.CurrentDay, GameManager.Instance.MaxDay);
        }

        private void SyncCoinsFromManager()
        {
            int coins = CoinManager.Instance != null ? CoinManager.Instance.Coins : 0;
            HandleCoinsChanged(coins);
        }

        private void SyncScoreFromGameManager()
        {
            int score = GameManager.Instance != null ? GameManager.Instance.Score : 0;
            HandleScoreChanged(score);
        }

        private string GetDayTransitionSubtitle(int day)
        {
            switch (day)
            {
                case 2:
                    return "The dungeon turns toward sunset.";
                case 3:
                    return "Night settles over the dungeon.";
                default:
                    return "The dungeon shifts again.";
            }
        }

        private Color GetPerkCardColor(PerkRarity rarity)
        {
            switch (rarity)
            {
                case PerkRarity.Legendary:
                    return new Color(0.30f, 0.22f, 0.07f, 0.98f);
                case PerkRarity.Rare:
                    return new Color(0.22f, 0.11f, 0.30f, 0.96f);
                case PerkRarity.Uncommon:
                    return new Color(0.08f, 0.23f, 0.14f, 0.96f);
                default:
                    return new Color(0.13f, 0.14f, 0.18f, 0.96f);
            }
        }

        private Color GetRarityColor(PerkRarity rarity)
        {
            switch (rarity)
            {
                case PerkRarity.Legendary:
                    return goldAccentColor;
                case PerkRarity.Rare:
                    return upgradeAccentColor;
                case PerkRarity.Uncommon:
                    return new Color(0.36f, 0.92f, 0.52f, 1f);
                default:
                    return mutedTextColor;
            }
        }
    }
}
