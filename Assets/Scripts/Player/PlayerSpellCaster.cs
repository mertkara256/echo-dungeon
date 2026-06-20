using EchoDungeon.Core;
using UnityEngine;

namespace EchoDungeon.Player
{
    public sealed class PlayerSpellCaster : MonoBehaviour
    {
        [Header("Shared")]
        [SerializeField] private float targetingRayDistance = 200f;

        [Header("Stasis Spell")]
        [SerializeField] private float stasisRadius = 2.8f;
        [SerializeField] private float stasisDuration = 5f;
        [SerializeField] private float stasisCooldown = 13f;
        [SerializeField] private Color stasisColor = new Color(0.22f, 0.82f, 1f, 0.3f);

        [Header("Poison Spell")]
        [SerializeField] private float poisonRadius = 2.5f;
        [SerializeField] private float poisonDuration = 5f;
        [SerializeField] private float poisonTickInterval = 0.5f;
        [SerializeField] private float poisonDamagePerTick = 5f;
        [SerializeField] private float poisonCooldown = 11f;
        [SerializeField] private Color poisonColor = new Color(0.35f, 0.92f, 0.36f, 0.3f);

        private Camera mainCamera;
        private float nextStasisTime;
        private float nextPoisonTime;
        private int stasisCharges;
        private int poisonCharges;
        private bool stasisUnlocked;
        private bool poisonUnlocked;

        public float StasisCooldownDuration
        {
            get { return stasisCooldown; }
        }

        public float PoisonCooldownDuration
        {
            get { return poisonCooldown; }
        }

        public float StasisCooldownRemaining
        {
            get { return Mathf.Max(0f, nextStasisTime - Time.time); }
        }

        public float PoisonCooldownRemaining
        {
            get { return Mathf.Max(0f, nextPoisonTime - Time.time); }
        }

        public int StasisCharges
        {
            get { return stasisCharges; }
        }

        public int PoisonCharges
        {
            get { return poisonCharges; }
        }

        public bool IsStasisUnlocked
        {
            get { return stasisUnlocked; }
        }

        public bool IsPoisonUnlocked
        {
            get { return poisonUnlocked; }
        }

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        private void Start()
        {
            RaiseSpellChargesChanged();
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            if (GameInput.WasStasisSpellPressed())
            {
                TryCastStasisSpell();
            }

            if (GameInput.WasPoisonSpellPressed())
            {
                TryCastPoisonSpell();
            }
        }

        private void TryCastStasisSpell()
        {
            if (stasisCharges <= 0)
            {
                GameEvents.RaiseNotification(stasisUnlocked ? "No Stop charges" : "Stop spell locked");
                return;
            }

            if (Time.time < nextStasisTime || !TryGetTargetPoint(out Vector3 targetPoint))
            {
                return;
            }

            nextStasisTime = Time.time + Mathf.Max(0f, stasisCooldown);
            stasisCharges--;
            RaiseSpellChargesChanged();
            GameObject spellObject = new GameObject("StasisFieldSpell");
            spellObject.transform.position = targetPoint;

            StasisFieldSpell field = spellObject.AddComponent<StasisFieldSpell>();
            field.Initialize(gameObject, stasisRadius, stasisDuration, stasisColor, "Stasis Field");
        }

        private void TryCastPoisonSpell()
        {
            if (poisonCharges <= 0)
            {
                GameEvents.RaiseNotification(poisonUnlocked ? "No Poison charges" : "Poison spell locked");
                return;
            }

            if (Time.time < nextPoisonTime || !TryGetTargetPoint(out Vector3 targetPoint))
            {
                return;
            }

            nextPoisonTime = Time.time + Mathf.Max(0f, poisonCooldown);
            poisonCharges--;
            RaiseSpellChargesChanged();
            GameObject spellObject = new GameObject("PoisonFieldSpell");
            spellObject.transform.position = targetPoint;

            PoisonFieldSpell field = spellObject.AddComponent<PoisonFieldSpell>();
            field.ConfigureDamage(poisonTickInterval, poisonDamagePerTick);
            field.Initialize(gameObject, poisonRadius, poisonDuration, poisonColor, "Poison Field");
        }

        private bool TryGetTargetPoint(out Vector3 targetPoint)
        {
            targetPoint = transform.position;

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    return false;
                }
            }

            Ray ray = mainCamera.ScreenPointToRay(GameInput.PointerPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, targetingRayDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                targetPoint = hit.point;
                targetPoint.y = 0f;
                return true;
            }

            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (!groundPlane.Raycast(ray, out float enter))
            {
                return false;
            }

            targetPoint = ray.GetPoint(enter);
            targetPoint.y = 0f;
            return true;
        }

        public void GrantStasisCharges(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            stasisUnlocked = true;
            stasisCharges = Mathf.Max(0, stasisCharges + amount);
            RaiseSpellChargesChanged();
        }

        public void GrantPoisonCharges(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            poisonUnlocked = true;
            poisonCharges = Mathf.Max(0, poisonCharges + amount);
            RaiseSpellChargesChanged();
        }

        private void RaiseSpellChargesChanged()
        {
            GameEvents.RaiseSpellChargesChanged(stasisCharges, poisonCharges);
        }
    }
}
