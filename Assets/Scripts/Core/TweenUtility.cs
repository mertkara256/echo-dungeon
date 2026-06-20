using DG.Tweening;
using UnityEngine;

namespace EchoDungeon.Core
{
    public static class TweenUtility
    {
        public static void StartPulse(Material material, Color baseColor, float blend, float duration, Object target, GameObject link)
        {
            if (material == null)
            {
                return;
            }

            DOTween.Kill(target);
            material.color = baseColor;
            material.DOColor(Color.Lerp(baseColor, Color.white, blend), duration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetTarget(target)
                .SetLink(link);
        }
    }
}
