using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EchoDungeon.UI
{
    public sealed class SpellCooldownBarUI : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI keyText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI statusText;

        [SerializeField] private Color chargingColor = new Color(0.42f, 0.82f, 1f, 1f);
        [SerializeField] private Color readyColor = new Color(1f, 0.73f, 0.24f, 1f);
        [SerializeField] private Color inactiveColor = new Color(0.46f, 0.5f, 0.56f, 1f);
        [SerializeField] private float fillSmoothSpeed = 14f;

        private float targetFill = 1f;
        private bool isReady = true;
        private bool isInactive;

        public void SetCooldown(float remaining, float duration)
        {
            float safeDuration = Mathf.Max(0.01f, duration);
            float clampedRemaining = Mathf.Clamp(remaining, 0f, safeDuration);
            targetFill = 1f - clampedRemaining / safeDuration;
            isReady = clampedRemaining <= 0.01f;
            isInactive = false;

            if (statusText != null)
            {
                statusText.text = isReady ? "READY" : clampedRemaining.ToString("0.0") + "s";
                statusText.color = isReady ? readyColor : new Color(0.92f, 0.9f, 0.84f, 1f);
            }
        }

        public void SetChargeCooldown(float remaining, float duration, int charges, bool unlocked)
        {
            if (!unlocked)
            {
                SetInactiveStatus("LOCKED");
                return;
            }

            if (charges <= 0)
            {
                SetInactiveStatus("EMPTY");
                return;
            }

            float safeDuration = Mathf.Max(0.01f, duration);
            float clampedRemaining = Mathf.Clamp(remaining, 0f, safeDuration);
            targetFill = 1f - clampedRemaining / safeDuration;
            isReady = clampedRemaining <= 0.01f;
            isInactive = false;

            if (statusText != null)
            {
                string chargeSuffix = " x" + charges;
                statusText.text = isReady
                    ? "READY" + chargeSuffix
                    : clampedRemaining.ToString("0.0") + "s" + chargeSuffix;
                statusText.color = isReady ? readyColor : new Color(0.92f, 0.9f, 0.84f, 1f);
            }
        }

        private void Update()
        {
            if (fillImage == null)
            {
                return;
            }

            float nextFill = Mathf.Lerp(fillImage.fillAmount, targetFill, Time.unscaledDeltaTime * fillSmoothSpeed);
            ApplyFill(nextFill);
        }

        private void ApplyFill(float amount)
        {
            if (fillImage == null)
            {
                return;
            }

            fillImage.fillAmount = Mathf.Clamp01(amount);
            fillImage.color = isInactive ? inactiveColor : isReady ? readyColor : chargingColor;
        }

        private void SetInactiveStatus(string status)
        {
            targetFill = 0f;
            isReady = false;
            isInactive = true;

            if (statusText != null)
            {
                statusText.text = status;
                statusText.color = inactiveColor;
            }
        }
    }
}
