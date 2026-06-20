using UnityEngine;
using EchoDungeon.Core.Interfaces;

namespace EchoDungeon.Player
{
    public abstract class GroundSpellField : MonoBehaviour
    {
        private const float FieldHeight = 0.04f;
        private const float PulseSpeed = 2.4f;
        private const float PulseStrength = 0.08f;

        private GameObject visualRoot;
        private Renderer cachedRenderer;
        private Material runtimeMaterial;
        private float baseAlpha;
        private float expiryTime;
        private bool initialized;

        protected GameObject Caster { get; private set; }
        protected float Radius { get; private set; }
        protected float Duration { get; private set; }
        protected float RemainingDuration
        {
            get { return Mathf.Max(0f, expiryTime - Time.time); }
        }

        public void Initialize(GameObject caster, float radius, float duration, Color fieldColor, string objectName)
        {
            Caster = caster;
            Radius = Mathf.Max(0.5f, radius);
            Duration = Mathf.Max(0.1f, duration);
            expiryTime = Time.time + Duration;
            gameObject.name = objectName;

            EnsureVisual();
            ApplyVisual(fieldColor);
            initialized = true;
            OnInitialized();
        }

        protected virtual void OnInitialized()
        {
        }

        protected virtual void Update()
        {
            if (!initialized)
            {
                return;
            }

            if (Time.time >= expiryTime)
            {
                Destroy(gameObject);
                return;
            }

            ApplyEffect();
            PulseVisual();
        }

        protected abstract void ApplyEffect();

        protected bool TryGetDamageable(Collider hit, out IDamageable damageable)
        {
            damageable = hit != null ? hit.GetComponentInParent<IDamageable>() : null;
            if (damageable == null)
            {
                return false;
            }

            Component component = damageable as Component;
            if (component == null)
            {
                return true;
            }

            if (Caster != null && component.transform.IsChildOf(Caster.transform))
            {
                return false;
            }

            return true;
        }

        private void EnsureVisual()
        {
            if (visualRoot != null)
            {
                return;
            }

            visualRoot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visualRoot.name = "FieldVisual";
            visualRoot.transform.SetParent(transform, false);
            visualRoot.transform.localPosition = Vector3.up * FieldHeight;
            visualRoot.transform.localScale = new Vector3(Radius * 2f, FieldHeight, Radius * 2f);

            Collider collider = visualRoot.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            cachedRenderer = visualRoot.GetComponent<Renderer>();
        }

        private void ApplyVisual(Color fieldColor)
        {
            if (cachedRenderer == null)
            {
                return;
            }

            runtimeMaterial = cachedRenderer.material;
            ConfigureTransparentMaterial(runtimeMaterial);
            runtimeMaterial.color = fieldColor;
            baseAlpha = fieldColor.a;
        }

        private void PulseVisual()
        {
            if (runtimeMaterial == null)
            {
                return;
            }

            Color color = runtimeMaterial.color;
            color.a = Mathf.Clamp01(baseAlpha * (1f + Mathf.Sin(Time.time * PulseSpeed) * PulseStrength));
            runtimeMaterial.color = color;
        }

        private static void ConfigureTransparentMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }
    }
}
