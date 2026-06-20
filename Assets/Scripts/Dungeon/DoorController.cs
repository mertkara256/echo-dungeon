using UnityEngine;

namespace EchoDungeon.Dungeon
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Renderer))]
    public sealed class DoorController : MonoBehaviour
    {
        [SerializeField] private bool openOnStart = true;
        [SerializeField] private Color openColor = new Color(0.2f, 0.9f, 0.35f, 1f);
        [SerializeField] private Color closedColor = new Color(0.8f, 0.15f, 0.15f, 1f);

        private Collider blockingCollider;
        private Renderer doorRenderer;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            blockingCollider = GetComponent<Collider>();
            doorRenderer = GetComponent<Renderer>();
            SetOpen(openOnStart, true);
        }

        public void SetOpen(bool open)
        {
            SetOpen(open, false);
        }

        public void SetOpen(bool open, bool force)
        {
            if (!force && IsOpen == open)
            {
                return;
            }

            IsOpen = open;
            blockingCollider.enabled = !open;
            doorRenderer.material.color = open ? openColor : closedColor;
        }
    }
}
