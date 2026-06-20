using System;
using System.Collections.Generic;
using System.Collections;
using EchoDungeon.Core;
using EchoDungeon.Bosses;
using EchoDungeon.Enemy;
using EchoDungeon.Player;
using UnityEngine;

namespace EchoDungeon.Dungeon
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class Room : MonoBehaviour
    {
        [SerializeField] private RoomType roomType;
        [SerializeField] private int roomIndex;
        [SerializeField] private DoorController entranceDoor;
        [SerializeField] private DoorController exitDoor;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private Transform contentRoot;
        [SerializeField] private Vector3 playerSpawnOffset = new Vector3(0f, 1.1f, -9f);
        [SerializeField] private float entranceCloseDepth = 1.35f;
        [SerializeField] private float entranceCloseTimeout = 4f;
        [SerializeField] private float keyDropHeight = 0.75f;

        [Header("Reward Prefabs")]
        [SerializeField] private GameObject healthPotionPickupPrefab;
        [SerializeField] private GameObject speedBoostPickupPrefab;
        [SerializeField] private GameObject keyPickupPrefab;
        [SerializeField] private GameObject upgradeStationPrefab;
        [SerializeField] private GameObject lockedRewardChestPrefab;

        [Header("Shop")]
        [SerializeField] private GameObject shopPrefab;
        [SerializeField] private GameObject shopItemPrefab;
        [SerializeField] private int shopHealthPotionPrice = 10;
        [SerializeField] private int shopSpeedPotionPrice = 12;
        [SerializeField] private int shopStasisChargePackPrice = 18;
        [SerializeField] private int shopPoisonChargePackPrice = 16;
        [SerializeField] private int shopRandomUpgradePrice = 15;

        private static readonly Vector3 ShopLocalOffset = new Vector3(0f, 0f, -1.75f);
        private const float ShopItemSpacing = 1.85f;

        private readonly List<EnemyBase> aliveEnemies = new List<EnemyBase>();
        private bool activated;
        private bool cleared;
        private bool bossSpawned;
        private bool hasLastEnemyDeathPosition;
        private Transform activePlayerTransform;
        private BossController activeBoss;
        private Coroutine delayedEntranceCloseRoutine;
        private Vector3 lastEnemyDeathPosition;
        private int bossPrepDropCount;

        public event Action<Room> Activated;
        public event Action<Room> Cleared;

        public int RoomIndex
        {
            get { return roomIndex; }
        }

        public RoomType RoomType
        {
            get { return roomType; }
        }

        public bool IsCleared
        {
            get { return cleared; }
        }

        public void Initialize(
            int index,
            RoomType type,
            DoorController inDoor,
            DoorController outDoor,
            EnemySpawner spawner,
            Transform roomContentRoot)
        {
            roomIndex = index;
            roomType = type;
            entranceDoor = inDoor;
            exitDoor = outDoor;
            enemySpawner = spawner;
            contentRoot = roomContentRoot;
            activated = false;
            cleared = false;
            bossSpawned = false;
            bossPrepDropCount = 0;
            hasLastEnemyDeathPosition = false;
            aliveEnemies.Clear();
            activeBoss = null;

            BoxCollider trigger = GetComponent<BoxCollider>();
            trigger.isTrigger = true;
        }

        public void PrepareForRun(Transform player)
        {
            activated = false;
            cleared = false;
            bossSpawned = false;
            bossPrepDropCount = 0;
            hasLastEnemyDeathPosition = false;
            aliveEnemies.Clear();
            activeBoss = null;

            BoxCollider trigger = GetComponent<BoxCollider>();
            if (trigger != null)
            {
                trigger.isTrigger = true;
            }

            if (enemySpawner != null)
            {
                enemySpawner.ConfigureRuntime(player);
            }

            activePlayerTransform = player;

            if (entranceDoor != null)
            {
                entranceDoor.gameObject.SetActive(true);
                entranceDoor.SetOpen(true, true);
            }

            if (exitDoor != null)
            {
                exitDoor.gameObject.SetActive(true);
                exitDoor.SetOpen(true, true);
            }
        }

        public void ResetForNewDay(Transform player)
        {
            CleanupRuntimeObjects();
            PrepareForRun(player);
        }

        public void CleanupRuntimeObjects()
        {
            if (delayedEntranceCloseRoutine != null)
            {
                StopCoroutine(delayedEntranceCloseRoutine);
                delayedEntranceCloseRoutine = null;
            }

            if (activeBoss != null)
            {
                activeBoss.BossDefeated -= HandleBossDefeated;
                DestroyRuntimeObject(activeBoss.gameObject);
            }

            for (int i = aliveEnemies.Count - 1; i >= 0; i--)
            {
                EnemyBase enemy = aliveEnemies[i];
                if (enemy == null)
                {
                    continue;
                }

                enemy.Died -= OnEnemyDied;
                DestroyRuntimeObject(enemy.gameObject);
            }

            aliveEnemies.Clear();
            activeBoss = null;

            if (enemySpawner != null)
            {
                enemySpawner.ClearSpawnedRuntimeObjects();
            }

            if (contentRoot != null && contentRoot != transform)
            {
                DestroyChildren(contentRoot);
            }
        }

        public Vector3 GetPlayerSpawnPoint()
        {
            return transform.position + playerSpawnOffset;
        }

        public void ForceActivate()
        {
            if (!activated)
            {
                Activate();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (activated)
            {
                return;
            }

            PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
            if (player == null)
            {
                return;
            }

            activePlayerTransform = player.transform;
            Activate();
        }

        private void Activate()
        {
            activated = true;
            if (Activated != null)
            {
                Activated.Invoke(this);
            }

            if (roomType == RoomType.Combat || roomType == RoomType.Boss)
            {
                CloseExitDoor();
                StartDelayedEntranceClose();
                SpawnEnemies();
                return;
            }

            CloseDoors();
            SpawnNonCombatReward();
            ClearRoom();
        }

        private void SpawnEnemies()
        {
            if (enemySpawner == null)
            {
                ClearRoom();
                return;
            }

            List<EnemyBase> spawned = enemySpawner.SpawnForRoom(roomType, roomIndex);
            aliveEnemies.Clear();
            hasLastEnemyDeathPosition = false;
            for (int i = 0; i < spawned.Count; i++)
            {
                EnemyBase enemy = spawned[i];
                if (enemy == null)
                {
                    continue;
                }

                enemy.ForceAggroPlayer();
                enemy.ForceChase();
                enemy.Died += OnEnemyDied;
                aliveEnemies.Add(enemy);
            }

            if (aliveEnemies.Count == 0)
            {
                ClearRoom();
                return;
            }

            GameEvents.RaiseNotification("Room " + (roomIndex + 1) + ": clear all enemies");
        }

        private void SpawnNonCombatReward()
        {
            if (roomType == RoomType.Treasure)
            {
                SpawnTreasureDrops();
                SpawnShop();
                SpawnLockedRewardChest(
                    new Vector3(3.4f, 1f, 2.8f),
                    "Treasure Vault",
                    "Spend 1 key to claim a premium treasure perk");
                GameEvents.RaiseNotification("Treasure room - loot, market, and a locked cache");
                return;
            }

            if (roomType == RoomType.Upgrade)
            {
                SpawnUpgradeStation();
                SpawnLockedRewardChest(
                    new Vector3(4f, 1f, 0f),
                    "Reserve Cache",
                    "Spend 1 key to add a premium perk");
                GameEvents.RaiseNotification("Upgrade room - draft a perk or unlock the reserve cache");
            }
        }

        private void SpawnTreasureDrops()
        {
            Transform rewardRoot = GetRewardRoot();

            ItemPickup potion = SpawnPickupPrefab(
                healthPotionPickupPrefab,
                "TreasurePickup",
                transform.position + new Vector3(-2.2f, 0.75f, 1.2f),
                rewardRoot);
            if (potion != null)
            {
                potion.SetupDefaultPotion(2);
            }

            ItemPickup speedBoost = SpawnPickupPrefab(
                speedBoostPickupPrefab,
                "SpeedPickup",
                transform.position + new Vector3(0.2f, 0.75f, 1.2f),
                rewardRoot);
            if (speedBoost != null)
            {
                speedBoost.SetupDefaultSpeedBoost(1);
            }
        }

        private void SpawnUpgradeStation()
        {
            Transform rewardRoot = GetRewardRoot();
            if (upgradeStationPrefab == null)
            {
                Debug.LogWarning("Room: Upgrade station prefab is missing.");
                return;
            }

            GameObject stationObject = Instantiate(
                upgradeStationPrefab,
                transform.position + new Vector3(0f, 1f, 0f),
                Quaternion.identity,
                rewardRoot);
            stationObject.name = "UpgradeStation";
            UpgradeStation station = stationObject.GetComponent<UpgradeStation>();
            if (station == null)
            {
                Debug.LogWarning("Room: Upgrade station prefab does not have an UpgradeStation component.");
                return;
            }

            station.Configure(roomIndex);
        }

        private void SpawnLockedRewardChest(Vector3 localOffset, string title, string subtitle)
        {
            Transform rewardRoot = GetRewardRoot();
            if (lockedRewardChestPrefab == null)
            {
                Debug.LogWarning("Room: Locked reward chest prefab is missing.");
                return;
            }

            GameObject chestObject = Instantiate(
                lockedRewardChestPrefab,
                transform.position + localOffset,
                Quaternion.identity,
                rewardRoot);
            chestObject.name = "LockedRewardChest";
            LockedRewardChest chest = chestObject.GetComponent<LockedRewardChest>();
            if (chest == null)
            {
                Debug.LogWarning("Room: Locked chest prefab does not have a LockedRewardChest component.");
                return;
            }

            chest.Configure(roomIndex, title, subtitle);
        }

        private void SpawnShop()
        {
            Transform rewardRoot = GetRewardRoot();
            if (shopPrefab == null)
            {
                Debug.LogWarning("Room: Shop prefab is missing.");
                return;
            }

            GameObject shopObject = Instantiate(
                shopPrefab,
                transform.position + ShopLocalOffset,
                Quaternion.identity,
                rewardRoot);
            shopObject.name = "Shop";

            SpawnShopItem(ShopItemType.HealthPotion, shopHealthPotionPrice, shopObject.transform.TransformPoint(new Vector3(-ShopItemSpacing * 2f, 1.15f, 0.1f)), shopObject.transform);
            SpawnShopItem(ShopItemType.SpeedPotion, shopSpeedPotionPrice, shopObject.transform.TransformPoint(new Vector3(-ShopItemSpacing, 1.15f, 0.1f)), shopObject.transform);
            SpawnShopItem(ShopItemType.StasisChargePack, shopStasisChargePackPrice, shopObject.transform.TransformPoint(new Vector3(0f, 1.15f, 0.1f)), shopObject.transform);
            SpawnShopItem(ShopItemType.PoisonChargePack, shopPoisonChargePackPrice, shopObject.transform.TransformPoint(new Vector3(ShopItemSpacing, 1.15f, 0.1f)), shopObject.transform);
            SpawnShopItem(ShopItemType.RandomUpgrade, shopRandomUpgradePrice, shopObject.transform.TransformPoint(new Vector3(ShopItemSpacing * 2f, 1.18f, 0.1f)), shopObject.transform);
        }

        private void SpawnShopItem(ShopItemType type, int price, Vector3 position, Transform parent)
        {
            if (shopItemPrefab == null)
            {
                Debug.LogWarning("Room: Shop item prefab is missing.");
                return;
            }

            GameObject itemObject = Instantiate(shopItemPrefab, position, Quaternion.identity, parent);
            itemObject.name = "ShopItem_" + type;
            ShopItem shopItem = itemObject.GetComponent<ShopItem>();
            if (shopItem == null)
            {
                Debug.LogWarning("Room: Shop item prefab does not have a ShopItem component.");
                return;
            }

            shopItem.Configure(type, price);
        }

        private void SpawnKeyReward(Vector3 worldPosition)
        {
            Transform rewardRoot = GetRewardRoot();
            worldPosition.y = transform.position.y + keyDropHeight;
            ItemPickup key = SpawnPickupPrefab(keyPickupPrefab, "KeyPickup", worldPosition, rewardRoot);
            if (key == null)
            {
                return;
            }

            key.SetupDefaultKey(1);
            GameEvents.RaiseNotification("A dungeon key dropped");
        }

        private bool ShouldSpawnKeyReward()
        {
            return roomType == RoomType.Combat && roomIndex > 0 && roomIndex % 3 == 2;
        }

        private void OnEnemyDied(EnemyBase enemy)
        {
            enemy.Died -= OnEnemyDied;
            aliveEnemies.Remove(enemy);
            if (enemy != null)
            {
                lastEnemyDeathPosition = enemy.transform.position;
                hasLastEnemyDeathPosition = true;
                TrySpawnBossPrepDrop(lastEnemyDeathPosition);
            }

            if (aliveEnemies.Count == 0)
            {
                if (roomType == RoomType.Boss && !bossSpawned)
                {
                    SpawnBossEncounter();
                    return;
                }

                ClearRoom();
            }
        }

        private void SpawnBossEncounter()
        {
            bossSpawned = true;
            if (enemySpawner == null)
            {
                ClearRoom();
                return;
            }

            activeBoss = enemySpawner.SpawnBoss(roomIndex);
            if (activeBoss == null)
            {
                ClearRoom();
                return;
            }

            activeBoss.BossDefeated -= HandleBossDefeated;
            activeBoss.BossDefeated += HandleBossDefeated;
            GameEvents.RaiseNotification("Corrupted Guardian emerges");
        }

        private void HandleBossDefeated(BossController boss)
        {
            if (boss != null)
            {
                boss.BossDefeated -= HandleBossDefeated;
            }

            activeBoss = null;
            ClearRoom();
        }

        public void HideExitDoor()
        {
            if (exitDoor == null)
            {
                return;
            }

            exitDoor.gameObject.SetActive(false);
        }

        private void CloseDoors()
        {
            CloseEntranceDoor();
            CloseExitDoor();
        }

        private void CloseEntranceDoor()
        {
            if (entranceDoor != null)
            {
                entranceDoor.SetOpen(false);
            }
        }

        private void CloseExitDoor()
        {
            if (exitDoor != null)
            {
                exitDoor.SetOpen(false);
            }
        }

        private void OpenDoors()
        {
            if (entranceDoor != null)
            {
                entranceDoor.SetOpen(true);
            }

            if (exitDoor != null)
            {
                exitDoor.SetOpen(true);
            }
        }

        private void ClearRoom()
        {
            if (cleared)
            {
                return;
            }

            cleared = true;
            OpenDoors();

            if (ShouldSpawnKeyReward())
            {
                Vector3 dropPosition = hasLastEnemyDeathPosition
                    ? lastEnemyDeathPosition
                    : transform.position + new Vector3(0f, keyDropHeight, 1.6f);
                SpawnKeyReward(dropPosition);
            }

            if (roomType == RoomType.Boss)
            {
                GameEvents.RaiseBossCleared();
            }

            if (Cleared != null)
            {
                Cleared.Invoke(this);
            }
        }

        private void StartDelayedEntranceClose()
        {
            if (entranceDoor == null)
            {
                return;
            }

            if (delayedEntranceCloseRoutine != null)
            {
                StopCoroutine(delayedEntranceCloseRoutine);
            }

            delayedEntranceCloseRoutine = StartCoroutine(CloseEntranceWhenPlayerIsInsideRoutine());
        }

        private IEnumerator CloseEntranceWhenPlayerIsInsideRoutine()
        {
            float remaining = entranceCloseTimeout;

            while (remaining > 0f)
            {
                remaining -= Time.deltaTime;

                if (activePlayerTransform != null && IsPlayerPastEntrance(activePlayerTransform))
                {
                    CloseEntranceDoor();
                    delayedEntranceCloseRoutine = null;
                    yield break;
                }

                yield return null;
            }

            CloseEntranceDoor();
            delayedEntranceCloseRoutine = null;
        }

        private bool IsPlayerPastEntrance(Transform player)
        {
            if (entranceDoor == null || player == null)
            {
                return false;
            }

            Vector3 doorToCenter = transform.position - entranceDoor.transform.position;
            doorToCenter.y = 0f;

            if (doorToCenter.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            Vector3 doorToPlayer = player.position - entranceDoor.transform.position;
            doorToPlayer.y = 0f;

            float depth = Vector3.Dot(doorToPlayer, doorToCenter.normalized);
            return depth >= entranceCloseDepth;
        }

        private Transform GetRewardRoot()
        {
            return contentRoot != null ? contentRoot : transform;
        }

        private static void DestroyChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child != null)
                {
                    DestroyRuntimeObject(child.gameObject);
                }
            }
        }

        private static void DestroyRuntimeObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private void TrySpawnBossPrepDrop(Vector3 worldPosition)
        {
            if (roomType != RoomType.Boss || bossSpawned || bossPrepDropCount >= 2)
            {
                return;
            }

            Transform rewardRoot = GetRewardRoot();
            worldPosition.y = transform.position.y + keyDropHeight;

            ItemPickup itemPickup = SpawnPickupPrefab(
                bossPrepDropCount == 0 ? healthPotionPickupPrefab : speedBoostPickupPrefab,
                bossPrepDropCount == 0 ? "BossPrepHealthPotion" : "BossPrepSpeedPotion",
                worldPosition,
                rewardRoot);
            if (itemPickup == null)
            {
                return;
            }

            if (bossPrepDropCount == 0)
            {
                itemPickup.SetupDefaultPotion(1);
                GameEvents.RaiseNotification("Health potion dropped for the boss fight");
            }
            else
            {
                itemPickup.SetupDefaultSpeedBoost(1);
                GameEvents.RaiseNotification("Speed boost dropped for the boss fight");
            }

            bossPrepDropCount++;
        }

        private ItemPickup SpawnPickupPrefab(GameObject prefab, string objectName, Vector3 position, Transform parent)
        {
            if (prefab == null)
            {
                Debug.LogWarning("Room: Pickup prefab is missing for " + objectName + ".");
                return null;
            }

            GameObject pickupObject = Instantiate(prefab, position, Quaternion.identity, parent);
            pickupObject.name = objectName;
            ItemPickup pickup = pickupObject.GetComponent<ItemPickup>();
            if (pickup == null)
            {
                Debug.LogWarning("Room: Pickup prefab does not have an ItemPickup component.");
                return null;
            }

            return pickup;
        }
    }
}
