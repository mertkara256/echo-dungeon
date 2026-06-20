using DG.Tweening;
using EchoDungeon.Core;
using UnityEngine;

namespace EchoDungeon.Player
{
    public sealed class TopDownCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 22f, -14f);
        [SerializeField] private float followSpeed = 10f;
        [SerializeField] private float lookAtHeight = 1f;
        [SerializeField] private float damageShakeDuration = 0.18f;
        [SerializeField] private float damageShakeStrength = 0.28f;
        [SerializeField] private float dashShakeDuration = 0.12f;
        [SerializeField] private float dashShakeStrength = 0.14f;
        [SerializeField] private float hitShakeDuration = 0.08f;
        [SerializeField] private float hitShakeStrength = 0.11f;

        private Vector3 shakeOffset;

        private void OnEnable()
        {
            GameEvents.PlayerDamaged += HandlePlayerDamaged;
            GameEvents.PlayerDashed += HandlePlayerDashed;
            GameEvents.PlayerHitLanded += HandlePlayerHitLanded;
        }

        private void OnDisable()
        {
            GameEvents.PlayerDamaged -= HandlePlayerDamaged;
            GameEvents.PlayerDashed -= HandlePlayerDashed;
            GameEvents.PlayerHitLanded -= HandlePlayerHitLanded;
            DOTween.Kill(this);
            shakeOffset = Vector3.zero;
        }

        public void SetTarget(Transform targetTransform)
        {
            target = targetTransform;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            float deltaTime = Time.timeScale > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
            Vector3 desiredPosition = target.position + offset + shakeOffset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * deltaTime);

            Vector3 lookPoint = target.position + Vector3.up * lookAtHeight;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookPoint - transform.position),
                followSpeed * deltaTime);
        }

        private void HandlePlayerDamaged(float damageAmount)
        {
            float strength = Mathf.Clamp(damageShakeStrength + damageAmount * 0.0125f, 0.22f, 0.52f);
            PlayShake(damageShakeDuration, strength, 0.08f);
        }

        private void HandlePlayerDashed()
        {
            PlayShake(dashShakeDuration, dashShakeStrength, 0.03f);
        }

        private void HandlePlayerHitLanded(float intensity)
        {
            float strength = Mathf.Lerp(hitShakeStrength, hitShakeStrength * 1.8f, Mathf.Clamp01(intensity));
            PlayShake(hitShakeDuration, strength, 0.025f);
        }

        private void PlayShake(float duration, float horizontalStrength, float verticalStrength)
        {
            DOTween.Kill(this);
            shakeOffset = Vector3.zero;
            DOTween
                .Shake(() => shakeOffset, x => shakeOffset = x, duration, new Vector3(horizontalStrength, verticalStrength, horizontalStrength), 18, 90f, true, ShakeRandomnessMode.Harmonic)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetTarget(this)
                .SetLink(gameObject)
                .OnKill(() => shakeOffset = Vector3.zero);
        }
    }
}
