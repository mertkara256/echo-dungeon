using System.Collections;
using System.Collections.Generic;
using EchoDungeon.Core;
using EchoDungeon.Core.Interfaces;
using UnityEngine;

namespace EchoDungeon.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerStats))]
    public sealed class PlayerMeleeAttack : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private float attackCooldown = 0.45f;
        [SerializeField] private bool bufferInputDuringCooldown = true;
        [SerializeField] private float inputBufferTime = 0.12f;

        [Header("Animator")]
        [SerializeField] private Animator animator;
        [SerializeField] private string attackParameterName = "Attack";
        [SerializeField] private string swingDirectionParameterName = "SwingDirection";

        [Header("Melee Hit")]
        [SerializeField] private LayerMask damageLayers = ~0;
        [SerializeField] private float meleeDamageMultiplier = 1.3f;
        [SerializeField] private float meleeRange = 2.55f;
        [SerializeField] private float meleeRadius = 0.9f;
        [SerializeField] private int maxHitColliders = 24;

        [Header("Weapon Visual")]
        [SerializeField] private bool autoCreatePlaceholderWeapon = true;
        [SerializeField] private Transform weaponPivot;
        [SerializeField] private float placeholderBladeLength = 2.55f;
        [SerializeField] private float placeholderBladeRadius = 0.12f;
        [SerializeField] private Vector3 placeholderPivotPosition = new Vector3(0.48f, 0.52f, 0.32f);
        [SerializeField] private Vector3 placeholderIdleRotation = new Vector3(-42f, 0f, 0f);
        [SerializeField] private Vector3 swingPivotOffset = new Vector3(0f, -0.2f, 0.18f);
        [SerializeField] private Vector3 swingPivotRotation = Vector3.zero;
        [SerializeField] private float swingDuration = 0.3f;
        [SerializeField] private float swingAngle = 92f;
        [SerializeField] private float hitStartDelay = 0f;
        [SerializeField] private float hitDuration = 0.3f;

        private readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

        private PlayerStats stats;
        private float nextAttackTime;
        private float bufferedInputExpiresAt;
        private int attackParameterHash;
        private int swingDirectionParameterHash;
        private Coroutine attackRoutine;
        private bool warnedMissingAttackParameter;
        private bool nextSwingStartsFromRight = true;
        private Vector3 weaponIdleLocalPosition;
        private Quaternion weaponIdleLocalRotation;
        private bool hasWeaponIdlePose;
        private Collider[] hitResults;
        private TrailRenderer weaponTrail;

        public bool IsAttackInProgress { get; private set; }

        private void Awake()
        {
            attackParameterHash = Animator.StringToHash(attackParameterName);
            swingDirectionParameterHash = Animator.StringToHash(swingDirectionParameterName);
            stats = GetComponent<PlayerStats>();
            hitResults = new Collider[Mathf.Max(4, maxHitColliders)];

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (autoCreatePlaceholderWeapon)
            {
                CreateOrUpdatePlaceholderWeapon();
            }

            CaptureWeaponIdlePose();
            SetTrailActive(false, true);
        }

        private void OnDisable()
        {
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }

            SetTrailActive(false, true);
            RestoreWeaponIdlePose();
            IsAttackInProgress = false;
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            if (GameInput.WasSecondaryFirePressed())
            {
                RequestAttack();
            }

            if (bufferedInputExpiresAt > 0f && Time.time > bufferedInputExpiresAt)
            {
                bufferedInputExpiresAt = 0f;
            }

            if (bufferedInputExpiresAt > 0f && Time.time >= nextAttackTime)
            {
                bufferedInputExpiresAt = 0f;
                StartAttack();
            }
        }

        public void RequestAttack()
        {
            if (Time.time < nextAttackTime)
            {
                if (bufferInputDuringCooldown)
                {
                    bufferedInputExpiresAt = Time.time + inputBufferTime;
                }

                return;
            }

            StartAttack();
        }

        public void AnimEvent_EnableWeaponHitbox()
        {
            ApplyMeleeDamage();
        }

        public void AnimEvent_EnableWeaponTrail()
        {
            SetTrailActive(true, true);
        }

        public void AnimEvent_DisableWeaponHitbox()
        {
        }

        public void AnimEvent_DisableWeaponTrail()
        {
        }

        public void AnimEvent_AttackFinished()
        {
        }

        private void StartAttack()
        {
            nextAttackTime = Time.time + Mathf.Max(0.01f, attackCooldown);
            bufferedInputExpiresAt = 0f;
            damagedTargets.Clear();
            IsAttackInProgress = true;
            MoveWeaponToSwingHeight();
            SetTrailActive(true, true);

            int swingDirection = ConsumeSwingDirection();
            TryTriggerAnimatorAttack(swingDirection);

            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
            }

            attackRoutine = StartCoroutine(TimedAttackRoutine(swingDirection));
        }

        private IEnumerator TimedAttackRoutine(int swingDirection)
        {
            float duration = Mathf.Max(0.01f, swingDuration);
            float hitStart = Mathf.Clamp(hitStartDelay, 0f, duration);
            float hitEnd = Mathf.Clamp(hitStart + Mathf.Max(0.01f, hitDuration), hitStart, duration);
            float timer = 0f;
            bool hitApplied = false;
            Quaternion baseRotation = weaponPivot != null ? weaponPivot.localRotation : Quaternion.identity;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(timer / duration);
                float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);

                if (weaponPivot != null)
                {
                    float angle = Mathf.Lerp(swingAngle * swingDirection, -swingAngle * swingDirection, easedTime);
                    weaponPivot.localRotation = baseRotation * Quaternion.Euler(0f, angle, 0f);
                }

                if (timer >= hitStart && timer <= hitEnd)
                {
                    hitApplied = true;
                    ApplyMeleeDamage();
                }

                yield return null;
            }

            if (!hitApplied)
            {
                ApplyMeleeDamage();
            }

            FinishAttack();
        }

        private void FinishAttack()
        {
            if (weaponPivot != null && hasWeaponIdlePose)
            {
                weaponPivot.localRotation = weaponIdleLocalRotation;
            }

            SetTrailActive(false, false);
            RestoreWeaponIdlePose();
            IsAttackInProgress = false;
            attackRoutine = null;
        }

        private void ApplyMeleeDamage()
        {
            if (hitResults == null || hitResults.Length != Mathf.Max(4, maxHitColliders))
            {
                hitResults = new Collider[Mathf.Max(4, maxHitColliders)];
            }

            Vector3 capsuleStart = transform.position + Vector3.up * 0.85f + transform.forward * 0.55f;
            Vector3 capsuleEnd = transform.position + Vector3.up * 0.85f + transform.forward * Mathf.Max(0.65f, meleeRange);
            int hitCount = Physics.OverlapCapsuleNonAlloc(
                capsuleStart,
                capsuleEnd,
                Mathf.Max(0.1f, meleeRadius),
                hitResults,
                damageLayers,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < hitCount; i++)
            {
                Collider candidate = hitResults[i];
                hitResults[i] = null;
                if (candidate != null)
                {
                    TryDamage(candidate);
                }
            }
        }

        private void TryDamage(Collider other)
        {
            if (other == null)
            {
                return;
            }

            if ((damageLayers.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }

            if (other.transform.IsChildOf(transform))
            {
                return;
            }

            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable.IsDead || damagedTargets.Contains(damageable))
            {
                return;
            }

            Component damageableComponent = damageable as Component;
            if (damageableComponent != null && damageableComponent.transform.IsChildOf(transform))
            {
                return;
            }

            damagedTargets.Add(damageable);
            damageable.TakeDamage(GetCurrentMeleeDamage(), gameObject);
        }

        private int ConsumeSwingDirection()
        {
            int direction = nextSwingStartsFromRight ? 1 : -1;
            nextSwingStartsFromRight = !nextSwingStartsFromRight;
            return direction;
        }

        private bool TryTriggerAnimatorAttack(int swingDirection)
        {
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
            {
                return false;
            }

            if (!HasAnimatorTrigger(attackParameterName))
            {
                if (!warnedMissingAttackParameter)
                {
                    warnedMissingAttackParameter = true;
                    Debug.LogWarning("Player Animator is missing a Trigger parameter named '" + attackParameterName + "'.", this);
                }

                return false;
            }

            TrySetAnimatorSwingDirection(swingDirection);
            animator.ResetTrigger(attackParameterHash);
            animator.SetTrigger(attackParameterHash);
            return true;
        }

        private void TrySetAnimatorSwingDirection(int swingDirection)
        {
            if (string.IsNullOrEmpty(swingDirectionParameterName))
            {
                return;
            }

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                if (parameter.name != swingDirectionParameterName)
                {
                    continue;
                }

                if (parameter.type == AnimatorControllerParameterType.Float)
                {
                    animator.SetFloat(swingDirectionParameterHash, swingDirection);
                    return;
                }

                if (parameter.type == AnimatorControllerParameterType.Int)
                {
                    animator.SetInteger(swingDirectionParameterHash, swingDirection);
                    return;
                }
            }
        }

        private bool HasAnimatorTrigger(string parameterName)
        {
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == parameterName)
                {
                    return true;
                }
            }

            return false;
        }

        private void CaptureWeaponIdlePose()
        {
            if (weaponPivot == null)
            {
                return;
            }

            weaponIdleLocalPosition = weaponPivot.localPosition;
            weaponIdleLocalRotation = weaponPivot.localRotation;
            hasWeaponIdlePose = true;
        }

        private void MoveWeaponToSwingHeight()
        {
            if (weaponPivot == null)
            {
                return;
            }

            if (!hasWeaponIdlePose)
            {
                CaptureWeaponIdlePose();
            }

            weaponPivot.localPosition = weaponIdleLocalPosition + swingPivotOffset;
            weaponPivot.localRotation = Quaternion.Euler(swingPivotRotation);
        }

        private void RestoreWeaponIdlePose()
        {
            if (weaponPivot == null || !hasWeaponIdlePose)
            {
                return;
            }

            weaponPivot.localPosition = weaponIdleLocalPosition;
            weaponPivot.localRotation = weaponIdleLocalRotation;
        }

        private float GetCurrentMeleeDamage()
        {
            if (stats == null)
            {
                stats = GetComponent<PlayerStats>();
            }

            return stats != null ? stats.Damage * meleeDamageMultiplier : 0f;
        }

        private void CreateOrUpdatePlaceholderWeapon()
        {
            float bladeLength = Mathf.Max(0.5f, placeholderBladeLength);
            float bladeRadius = Mathf.Max(0.03f, placeholderBladeRadius);
            float bladeDiameter = bladeRadius * 2f;

            if (weaponPivot == null)
            {
                weaponPivot = FindOrCreateChild(transform, "MeleeWeaponPivot").transform;
            }

            weaponPivot.localPosition = placeholderPivotPosition;
            weaponPivot.localRotation = Quaternion.Euler(placeholderIdleRotation);
            weaponPivot.localScale = Vector3.one;

            GameObject bladeTipObject = FindOrCreateChild(weaponPivot, "BladeTip");
            bladeTipObject.transform.localPosition = new Vector3(0f, 0f, bladeLength);
            bladeTipObject.transform.localRotation = Quaternion.identity;
            bladeTipObject.transform.localScale = Vector3.one;

            GameObject stickObject = FindOrCreatePrimitiveChild(weaponPivot, "PlaceholderSwordStick", PrimitiveType.Cylinder);
            stickObject.transform.localPosition = new Vector3(0f, 0f, bladeLength * 0.5f);
            stickObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            stickObject.transform.localScale = new Vector3(bladeDiameter, bladeLength * 0.5f, bladeDiameter);

            Collider visualCollider = stickObject.GetComponent<Collider>();
            if (visualCollider != null)
            {
                Destroy(visualCollider);
            }

            Renderer renderer = stickObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                SetRendererColor(renderer, new Color(0.8f, 0.82f, 0.86f, 1f));
            }

            weaponTrail = bladeTipObject.GetComponent<TrailRenderer>();
            if (weaponTrail == null)
            {
                weaponTrail = bladeTipObject.AddComponent<TrailRenderer>();
            }

            ConfigureTrailRenderer();
        }

        private void ConfigureTrailRenderer()
        {
            if (weaponTrail == null)
            {
                return;
            }

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null && weaponTrail.sharedMaterial == null)
            {
                weaponTrail.sharedMaterial = new Material(shader);
            }

            weaponTrail.time = 0.18f;
            weaponTrail.startWidth = 0.5f;
            weaponTrail.endWidth = 0.05f;
            weaponTrail.minVertexDistance = 0.015f;
            weaponTrail.numCornerVertices = 8;
            weaponTrail.numCapVertices = 8;
            weaponTrail.emitting = false;
        }

        private void SetTrailActive(bool active, bool clearTrail)
        {
            if (weaponTrail == null)
            {
                return;
            }

            if (clearTrail)
            {
                weaponTrail.Clear();
            }

            weaponTrail.emitting = active;
        }

        private static GameObject FindOrCreateChild(Transform parent, string childName)
        {
            Transform existingChild = parent.Find(childName);
            if (existingChild != null)
            {
                return existingChild.gameObject;
            }

            GameObject child = new GameObject(childName);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static GameObject FindOrCreatePrimitiveChild(Transform parent, string childName, PrimitiveType primitiveType)
        {
            Transform existingChild = parent.Find(childName);
            if (existingChild != null)
            {
                return existingChild.gameObject;
            }

            GameObject child = GameObject.CreatePrimitive(primitiveType);
            child.name = childName;
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void SetRendererColor(Renderer targetRenderer, Color color)
        {
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", color);
            propertyBlock.SetColor("_Color", color);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.35f, 0.25f, 0.85f);
            Vector3 capsuleStart = transform.position + Vector3.up * 0.85f + transform.forward * 0.55f;
            Vector3 capsuleEnd = transform.position + Vector3.up * 0.85f + transform.forward * Mathf.Max(0.65f, meleeRange);
            Gizmos.DrawWireSphere(capsuleStart, Mathf.Max(0.1f, meleeRadius));
            Gizmos.DrawWireSphere(capsuleEnd, Mathf.Max(0.1f, meleeRadius));
            Gizmos.DrawLine(capsuleStart, capsuleEnd);
        }
    }
}
