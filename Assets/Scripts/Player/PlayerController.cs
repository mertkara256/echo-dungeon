using DG.Tweening;
using EchoDungeon.Core;
using UnityEngine;

namespace EchoDungeon.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerStats))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float rotationSpeed = 15f;
        [SerializeField] private float rayDistance = 200f;
        [SerializeField] private float dashDistance = 4.5f;
        [SerializeField] private float dashDuration = 0.12f;
        [SerializeField] private float dashCooldown = 0.45f;
        [SerializeField] private float dashScalePunch = 0.16f;
        [SerializeField] private Color dashTrailColor = new Color(0.35f, 0.92f, 1f, 0.78f);

        private CharacterController characterController;
        private PlayerStats stats;
        private Camera mainCamera;
        private Vector3 velocity;
        private bool isDashing;
        private Vector3 dashDirection;
        private float dashEndsAt;
        private float nextDashTime;
        private TrailRenderer dashTrail;
        private Vector3 defaultScale;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            stats = GetComponent<PlayerStats>();
            mainCamera = Camera.main;
            defaultScale = transform.localScale;
            EnsureDashTrail();
        }

        private void OnDisable()
        {
            DOTween.Kill(this);
            transform.localScale = defaultScale;
            SetDashTrailActive(false, true);
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            HandleDashInput();
            HandleMovement();
            HandleRotation();
        }

        private void HandleMovement()
        {
            if (characterController.isGrounded && velocity.y < 0f)
            {
                velocity.y = -2f;
            }

            velocity.y += gravity * Time.deltaTime;

            if (isDashing)
            {
                if (Time.time >= dashEndsAt)
                {
                    isDashing = false;
                    SetDashTrailActive(false, false);
                }
                else
                {
                    float dashSpeed = dashDistance / Mathf.Max(0.01f, dashDuration);
                    Vector3 dashStep = dashDirection * dashSpeed * Time.deltaTime;
                    dashStep.y = velocity.y * Time.deltaTime;
                    characterController.Move(dashStep);
                    return;
                }
            }

            Vector2 move = GameInput.Move;
            Vector3 moveInput = new Vector3(move.x, 0f, move.y);
            Vector3 movement = moveInput * stats.MoveSpeed * Time.deltaTime;
            movement.y = velocity.y * Time.deltaTime;
            characterController.Move(movement);
        }

        private void HandleDashInput()
        {
            if (isDashing || Time.time < nextDashTime || !GameInput.WasDashPressed())
            {
                return;
            }

            Vector2 move = GameInput.Move;
            Vector3 moveDirection = new Vector3(move.x, 0f, move.y);
            if (moveDirection.sqrMagnitude < 0.0001f)
            {
                return;
            }

            dashDirection = moveDirection.normalized;
            dashEndsAt = Time.time + dashDuration;
            nextDashTime = Time.time + dashCooldown;
            isDashing = true;
            PlayDashFeedback();
        }

        private void HandleRotation()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    return;
                }
            }

            Ray ray = mainCamera.ScreenPointToRay(GameInput.PointerPosition);
            bool hitWorld = Physics.Raycast(ray, out RaycastHit hit, rayDistance);

            Vector3 lookPoint;
            if (hitWorld)
            {
                lookPoint = hit.point;
            }
            else
            {
                Plane fallbackPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
                if (!fallbackPlane.Raycast(ray, out float enter))
                {
                    return;
                }

                lookPoint = ray.GetPoint(enter);
            }

            lookPoint.y = transform.position.y;
            Vector3 direction = (lookPoint - transform.position).normalized;

            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        private void PlayDashFeedback()
        {
            DOTween.Kill(this);
            transform.localScale = defaultScale;
            transform
                .DOPunchScale(new Vector3(dashScalePunch, -dashScalePunch * 0.55f, dashScalePunch), dashDuration * 1.35f, 1, 0.05f)
                .SetUpdate(true)
                .SetTarget(this)
                .SetLink(gameObject);

            SetDashTrailActive(true, true);
            GameEvents.RaisePlayerDashed();
        }

        private void EnsureDashTrail()
        {
            dashTrail = GetComponent<TrailRenderer>();
            if (dashTrail == null)
            {
                dashTrail = gameObject.AddComponent<TrailRenderer>();
            }

            // Gelişmiş, estetik bir TrailRenderer ayarı
            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader != null)
            {
                dashTrail.material = new Material(spriteShader);
            }
            
            dashTrail.time = 0.35f; // Havada biraz daha asılı kalsın
            dashTrail.minVertexDistance = 0.02f; // Daha yumuşak dönüşler
            dashTrail.numCornerVertices = 5;
            dashTrail.numCapVertices = 5;

            // Eski şekle (düz ve azalan) geri dönüş
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(new Keyframe(0f, 1f));
            curve.AddKey(new Keyframe(1f, 0f));
            dashTrail.widthCurve = curve;
            dashTrail.startWidth = 0.8f;
            dashTrail.endWidth = 0f;

            // Renk gradyanı: Başlangıçta parlak beyaz/sarımtırak bir patlama, ardından seçilen renge yumuşak geçiş
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(Color.white, 0f), 
                    new GradientColorKey(new Color(1f, 0.9f, 0.4f), 0.1f), 
                    new GradientColorKey(dashTrailColor, 0.4f), 
                    new GradientColorKey(dashTrailColor, 1f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1f, 0f), 
                    new GradientAlphaKey(0.8f, 0.3f), 
                    new GradientAlphaKey(0f, 1f) 
                }
            );
            dashTrail.colorGradient = gradient;
            dashTrail.emitting = false;
        }

        private void SetDashTrailActive(bool active, bool clearTrail)
        {
            if (dashTrail == null)
            {
                return;
            }

            if (clearTrail)
            {
                dashTrail.Clear();
            }

            dashTrail.emitting = active;
        }
    }
}
