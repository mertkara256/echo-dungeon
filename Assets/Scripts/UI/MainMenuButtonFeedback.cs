using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EchoDungeon.UI
{
    public sealed class MainMenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float hoverScale = 1.035f;
        [SerializeField] private float pressedScale = 0.985f;
        [SerializeField] private float animationSpeed = 14f;

        private RectTransform rectTransform;
        private Coroutine scaleRoutine;
        private bool pointerInside;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            AnimateScale(hoverScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            AnimateScale(1f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            AnimateScale(pressedScale);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            AnimateScale(pointerInside ? hoverScale : 1f);
        }

        private void OnDisable()
        {
            if (scaleRoutine != null)
            {
                StopCoroutine(scaleRoutine);
                scaleRoutine = null;
            }

            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
            }
        }

        private void AnimateScale(float targetScale)
        {
            if (rectTransform == null)
            {
                return;
            }

            if (scaleRoutine != null)
            {
                StopCoroutine(scaleRoutine);
            }

            scaleRoutine = StartCoroutine(ScaleRoutine(targetScale));
        }

        private IEnumerator ScaleRoutine(float targetScale)
        {
            Vector3 target = Vector3.one * targetScale;
            while (Vector3.Distance(rectTransform.localScale, target) > 0.002f)
            {
                rectTransform.localScale = Vector3.Lerp(
                    rectTransform.localScale,
                    target,
                    Time.unscaledDeltaTime * animationSpeed);
                yield return null;
            }

            rectTransform.localScale = target;
            scaleRoutine = null;
        }
    }
}
