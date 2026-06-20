using TMPro;
using UnityEngine;

namespace EchoDungeon.UI
{
    public sealed class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI keyText;
        [SerializeField] private TextMeshProUGUI actionText;
        [SerializeField] private float fadeSpeed = 10f;

        private float targetAlpha;

        private void Awake()
        {
            HideImmediate();
        }

        public void Show(string actionLabel)
        {
            if (string.IsNullOrWhiteSpace(actionLabel))
            {
                Hide();
                return;
            }

            if (keyText != null)
            {
                keyText.text = "E";
            }

            if (actionText != null)
            {
                actionText.text = actionLabel;
            }

            targetAlpha = 1f;
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void Hide()
        {
            targetAlpha = 0f;
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void HideImmediate()
        {
            targetAlpha = 0f;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private void Update()
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = Mathf.MoveTowards(
                canvasGroup.alpha,
                targetAlpha,
                Time.unscaledDeltaTime * fadeSpeed);
        }
    }
}
