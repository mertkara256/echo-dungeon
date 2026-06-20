using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EchoDungeon.UI
{
    public sealed class HealthBarUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Image flashImage;
        [SerializeField] private TextMeshProUGUI valueText;

        [Header("Tuning")]
        [SerializeField] private Color healthyColor = new Color(0.86f, 0.16f, 0.18f, 1f);
        [SerializeField] private Color lowHealthColor = new Color(1f, 0.34f, 0.22f, 1f);
        [SerializeField] private Color damageFlashColor = new Color(1f, 0.78f, 0.72f, 0.75f);
        [SerializeField] private Color healPulseColor = new Color(0.5f, 1f, 0.72f, 0.7f);
        [SerializeField] private float fillSmoothSpeed = 14f;

        private Coroutine flashRoutine;
        private float targetFill = 1f;
        private float currentFill = 1f;
        private float lastHealth;
        private bool initialized;
        private RectTransform fillRect;

        private void Awake()
        {
            CacheFillRect();
            ApplyFill(currentFill);
        }

        public void SetColors(Color healthy, Color low, Color damageFlash, Color healPulse)
        {
            healthyColor = healthy;
            lowHealthColor = low;
            damageFlashColor = damageFlash;
            healPulseColor = healPulse;
        }

        public void SetHealth(float current, float max)
        {
            float safeMax = Mathf.Max(1f, max);
            float clampedCurrent = Mathf.Clamp(current, 0f, safeMax);
            targetFill = clampedCurrent / safeMax;

            if (valueText != null)
            {
                valueText.text = Mathf.CeilToInt(clampedCurrent) + " / " + Mathf.CeilToInt(safeMax);
            }

            if (initialized)
            {
                if (clampedCurrent < lastHealth)
                {
                    PlayFlash(damageFlashColor);
                }
                else if (clampedCurrent > lastHealth)
                {
                    PlayFlash(healPulseColor);
                }
            }
            else
            {
                ApplyFill(targetFill);
                initialized = true;
            }

            lastHealth = clampedCurrent;
        }

        public void PlayDamageFlash()
        {
            PlayFlash(damageFlashColor);
        }

        private void Update()
        {
            if (fillImage == null)
            {
                return;
            }

            float nextFill = Mathf.Lerp(currentFill, targetFill, Time.unscaledDeltaTime * fillSmoothSpeed);
            if (Mathf.Abs(nextFill - targetFill) < 0.001f)
            {
                nextFill = targetFill;
            }

            ApplyFill(nextFill);
        }

        private void ApplyFill(float amount)
        {
            if (fillImage == null)
            {
                return;
            }

            amount = Mathf.Clamp01(amount);
            currentFill = amount;
            fillImage.type = Image.Type.Simple;
            fillImage.fillAmount = 1f;
            CacheFillRect();
            if (fillRect != null)
            {
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = new Vector2(amount, 1f);
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
                fillRect.pivot = new Vector2(0f, 0.5f);
            }

            fillImage.color = Color.Lerp(lowHealthColor, healthyColor, Mathf.Clamp01(amount * 1.35f));
        }

        private void CacheFillRect()
        {
            if (fillRect == null && fillImage != null)
            {
                fillRect = fillImage.rectTransform;
            }
        }

        private void PlayFlash(Color color)
        {
            if (flashImage == null)
            {
                return;
            }

            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }

            flashRoutine = StartCoroutine(FlashRoutine(color));
        }

        private IEnumerator FlashRoutine(Color color)
        {
            float timer = 0f;
            const float duration = 0.26f;
            flashImage.color = color;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                Color next = color;
                next.a = Mathf.Lerp(color.a, 0f, timer / duration);
                flashImage.color = next;
                yield return null;
            }

            flashImage.color = Color.clear;
            flashRoutine = null;
        }
    }
}
