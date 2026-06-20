using System.Collections.Generic;
using EchoDungeon.Bosses;
using EchoDungeon.Enemy;
using UnityEngine;

namespace EchoDungeon.Dungeon
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
        [SerializeField] private Transform spawnedEnemyRoot;

        [Header("Enemy Prefabs")]
        [SerializeField] private GameObject meleeEnemyPrefab;
        [SerializeField] private GameObject rangedEnemyPrefab;
        [SerializeField] private GameObject tankEnemyPrefab;
        [SerializeField] private GameObject eliteRangedEnemyPrefab;

        [Header("Coin Drops")]
        [SerializeField, Range(0f, 1f)] private float coinDropChance = 0.7f;
        [SerializeField] private int minCoinAmount = 1;
        [SerializeField] private int maxCoinAmount = 5;
        [SerializeField] private GameObject coinPrefab;

        [Header("Boss Encounter")]
        [SerializeField] private GameObject bossPrefab;
        [SerializeField] private List<EnemyArchetype> bossRoomWave = new List<EnemyArchetype>
        {
            EnemyArchetype.Tank,
            EnemyArchetype.EliteRanged,
            EnemyArchetype.Ranged
        };

        private Transform playerTransform;

        public void Configure(List<Transform> points, Transform player, Transform enemyRoot)
        {
            spawnPoints = points;
            playerTransform = player;
            spawnedEnemyRoot = enemyRoot;
        }

        public void ConfigureRuntime(Transform player)
        {
            playerTransform = player;
            if (spawnedEnemyRoot == null)
            {
                spawnedEnemyRoot = transform;
            }
        }

        public void ClearSpawnedRuntimeObjects()
        {
            if (spawnedEnemyRoot == null || spawnedEnemyRoot == transform)
            {
                return;
            }

            for (int i = spawnedEnemyRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = spawnedEnemyRoot.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        public List<EnemyBase> SpawnForRoom(RoomType roomType, int roomIndex)
        {
            List<EnemyBase> spawned = new List<EnemyBase>();

            if (playerTransform == null)
            {
                return spawned;
            }

            if (roomType == RoomType.Boss)
            {
                if (bossRoomWave == null || bossRoomWave.Count == 0)
                {
                    bossRoomWave = new List<EnemyArchetype> { EnemyArchetype.Tank, EnemyArchetype.EliteRanged, EnemyArchetype.Ranged };
                }

                for (int i = 0; i < bossRoomWave.Count; i++)
                {
                    EnemyBase add = SpawnEnemy(
                        bossRoomWave[i],
                        GetSpawnPosition(i + 1),
                        roomIndex + 1,
                        false);
                    ConfigureCoinDrop(add, 0f);
                    spawned.Add(add);
                }

                return spawned;
            }

            int minCount = Mathf.Clamp(2 + roomIndex / 2, 2, 6);
            int maxCount = Mathf.Clamp(3 + roomIndex / 2, 3, 8);
            int spawnCount = Random.Range(minCount, maxCount + 1);

            for (int i = 0; i < spawnCount; i++)
            {
                EnemyArchetype archetype = ChooseArchetype(roomIndex);
                EnemyBase enemy = SpawnEnemy(
                    archetype,
                    GetSpawnPosition(i),
                    roomIndex + 1,
                    false);
                ConfigureCoinDrop(enemy, coinDropChance);
                spawned.Add(enemy);
            }

            return spawned;
        }

        public BossController SpawnBoss(int roomIndex)
        {
            if (playerTransform == null)
            {
                return null;
            }

            if (bossPrefab == null)
            {
                Debug.LogWarning("EnemySpawner: Boss prefab is missing.");
                return null;
            }

            GameObject bossObject = Instantiate(bossPrefab, GetSpawnPosition(0), Quaternion.identity, spawnedEnemyRoot);
            BossController boss = bossObject.GetComponent<BossController>();
            if (boss == null)
            {
                Debug.LogWarning("EnemySpawner: Boss prefab does not have a BossController component.");
                return null;
            }

            boss.Setup(playerTransform, roomIndex + 1);
            return boss;
        }

        private EnemyBase SpawnEnemy(
            EnemyArchetype archetype,
            Vector3 position,
            int difficultyLevel,
            bool bossVariant)
        {
            GameObject prefab = GetEnemyPrefab(archetype);
            if (prefab == null)
            {
                Debug.LogWarning("EnemySpawner: Missing prefab for " + archetype + " enemy.");
                return null;
            }

            GameObject enemyObject = Instantiate(prefab, position, Quaternion.identity, spawnedEnemyRoot);
            enemyObject.name = archetype + "Enemy";
            EnemyBase enemy = enemyObject.GetComponent<EnemyBase>();
            if (enemy == null)
            {
                Debug.LogWarning("EnemySpawner: Enemy prefab does not have an EnemyBase component.");
                return null;
            }

            enemy.Initialize(playerTransform, difficultyLevel, bossVariant);
            return enemy;
        }

        private GameObject GetEnemyPrefab(EnemyArchetype archetype)
        {
            switch (archetype)
            {
                case EnemyArchetype.Ranged:
                    return rangedEnemyPrefab;
                case EnemyArchetype.EliteRanged:
                    return eliteRangedEnemyPrefab;
                case EnemyArchetype.Tank:
                    return tankEnemyPrefab;
                default:
                    return meleeEnemyPrefab;
            }
        }

        private EnemyArchetype ChooseArchetype(int roomIndex)
        {
            int roll = Random.Range(0, 100);
            int eliteRangedWeight = roomIndex >= 2 ? Mathf.Clamp(5 + roomIndex * 2, 5, 18) : 0;
            int rangedWeight = Mathf.Clamp(20 + roomIndex * 3, 20, 45);
            int tankWeight = Mathf.Clamp(8 + roomIndex * 2, 8, 28);

            if (roll < eliteRangedWeight)
            {
                return EnemyArchetype.EliteRanged;
            }

            if (roll < eliteRangedWeight + tankWeight)
            {
                return EnemyArchetype.Tank;
            }

            if (roll < eliteRangedWeight + tankWeight + rangedWeight)
            {
                return EnemyArchetype.Ranged;
            }

            return EnemyArchetype.Melee;
        }

        private Vector3 GetSpawnPosition(int index)
        {
            if (spawnPoints != null && spawnPoints.Count > 0)
            {
                return spawnPoints[index % spawnPoints.Count].position;
            }

            Vector2 randomCircle = Random.insideUnitCircle * 5.5f;
            return transform.position + new Vector3(randomCircle.x, 1f, randomCircle.y);
        }

        private void ConfigureCoinDrop(EnemyBase enemy, float dropChance)
        {
            if (enemy == null)
            {
                return;
            }

            EnemyCoinDrop coinDrop = enemy.GetComponent<EnemyCoinDrop>();
            if (coinDrop == null)
            {
                Debug.LogWarning("EnemySpawner: Enemy prefab is missing EnemyCoinDrop on " + enemy.name + ".");
                return;
            }

            coinDrop.Configure(dropChance, minCoinAmount, maxCoinAmount, coinPrefab);
        }
    }
}
