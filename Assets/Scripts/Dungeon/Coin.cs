using DG.Tweening;
using EchoDungeon.Core;
using EchoDungeon.Player;
using UnityEngine;

namespace EchoDungeon.Dungeon
{
    [RequireComponent(typeof(Collider))]
    public sealed class Coin : MonoBehaviour
    {
        [SerializeField] private int amount = 1;
        [SerializeField] private float bobHeight = 0.18f;
        [SerializeField] private float bobDuration = 0.75f;
        [SerializeField] private float rotateSpeed = 180f;
        [SerializeField] private Color coinColor = new Color(1f, 0.76f, 0.22f, 1f);

        private bool collected;
        private Vector3 startPosition;

        private void Awake()
        {
            Collider coinCollider = GetComponent<Collider>();
            coinCollider.isTrigger = true;

            Renderer coinRenderer = GetComponent<Renderer>();
            if (coinRenderer != null)
            {
                coinRenderer.material.color = coinColor;
            }
        }

        private void Start()
        {
            startPosition = transform.position;
            transform
                .DOMoveY(startPosition.y + bobHeight, bobDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetTarget(this)
                .SetLink(gameObject);
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        }

        private void OnDisable()
        {
            DOTween.Kill(this);
        }

        public void Configure(int coinAmount)
        {
            amount = Mathf.Max(1, coinAmount);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected || other.GetComponentInParent<PlayerStats>() == null)
            {
                return;
            }

            collected = true;
            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.AddCoins(amount);
            }

            Debug.Log("Coin collected: +" + amount);
            GameEvents.RaiseNotification("Coin +" + amount);
            Destroy(gameObject);
        }
    }
}
