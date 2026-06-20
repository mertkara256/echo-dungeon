using EchoDungeon.Dungeon;
using UnityEngine;

namespace EchoDungeon.Enemy
{
    [RequireComponent(typeof(EnemyBase))]
    public sealed class EnemyCoinDrop : MonoBehaviour
    {
        [Header("Drop Tuning")]
        [SerializeField, Range(0f, 1f)] private float dropChance = 0.7f;
        [SerializeField] private int minCoinAmount = 1;
        [SerializeField] private int maxCoinAmount = 5;
        [SerializeField] private GameObject coinPrefab;
        [SerializeField] private float dropHeight = 0.55f;

        private EnemyBase enemy;

        private void Awake()
        {
            enemy = GetComponent<EnemyBase>();
        }

        private void OnEnable()
        {
            if (enemy == null)
            {
                enemy = GetComponent<EnemyBase>();
            }

            if (enemy != null)
            {
                enemy.Died -= HandleEnemyDied;
                enemy.Died += HandleEnemyDied;
            }
        }

        private void OnDisable()
        {
            if (enemy != null)
            {
                enemy.Died -= HandleEnemyDied;
            }
        }

        private void HandleEnemyDied(EnemyBase killedEnemy)
        {
            if (dropChance <= 0f)
            {
                return;
            }

            if (Random.value > dropChance)
            {
                return;
            }

            int low = Mathf.Max(1, minCoinAmount);
            int high = Mathf.Max(low, maxCoinAmount);
            int amount = Random.Range(low, high + 1);
            SpawnCoin(amount);
        }

        public void Configure(float chance, int minAmount, int maxAmount, GameObject prefab)
        {
            dropChance = Mathf.Clamp01(chance);
            minCoinAmount = Mathf.Max(1, minAmount);
            maxCoinAmount = Mathf.Max(minCoinAmount, maxAmount);
            coinPrefab = prefab;
        }

        private void SpawnCoin(int amount)
        {
            if (coinPrefab == null)
            {
                Debug.LogWarning("EnemyCoinDrop: Coin prefab is missing.");
                return;
            }

            Vector3 position = transform.position;
            position.y += dropHeight;

            GameObject coinObject = Instantiate(coinPrefab, position, Quaternion.identity);

            Coin coin = coinObject.GetComponent<Coin>();
            if (coin == null)
            {
                Debug.LogWarning("EnemyCoinDrop: Coin prefab does not have a Coin component.");
                return;
            }

            coin.Configure(amount);
        }
    }
}
