using System.Collections.Generic;
using EchoDungeon.Bosses;
using EchoDungeon.Core;
using EchoDungeon.Enemy;
using EchoDungeon.Player;
using UnityEngine;

namespace EchoDungeon.Dungeon
{
    public sealed class RoomManager : MonoBehaviour
    {
        [Header("Scene Setup")]
        [SerializeField] private Transform roomsRoot;
        [SerializeField] private bool movePlayerToFirstRoomOnStart = true;

        private readonly List<Room> rooms = new List<Room>();
        private Transform playerTransform;
        private bool initialized;
        private int currentRoomIndex;
        private Room lastActivatedRoom;

        public int CurrentRoomIndex
        {
            get { return currentRoomIndex; }
        }

        public int TotalRoomCount
        {
            get { return rooms.Count; }
        }

        public void Initialize(Transform player)
        {
            if (initialized)
            {
                return;
            }

            playerTransform = player;
            if (playerTransform == null)
            {
                PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
                if (playerStats != null)
                {
                    playerTransform = playerStats.transform;
                }
            }

            CollectRoomsFromScene();
            BindRoomEvents();

            for (int i = 0; i < rooms.Count; i++)
            {
                rooms[i].PrepareForRun(playerTransform);
            }

            initialized = true;
            currentRoomIndex = 0;
            lastActivatedRoom = null;

            if (rooms.Count == 0)
            {
                GameEvents.RaiseNotification("No room found in scene");
                GameEvents.RaiseRoomProgressChanged(0, 0);
                return;
            }

            if (movePlayerToFirstRoomOnStart)
            {
                MovePlayerToRoom(0);
                return;
            }

            GameEvents.RaiseRoomProgressChanged(1, rooms.Count);
        }

        public void SetRoomsRoot(Transform root)
        {
            roomsRoot = root;
        }

        public void MovePlayerToRoom(int index)
        {
            if (playerTransform == null || rooms.Count == 0)
            {
                return;
            }

            int clamped = Mathf.Clamp(index, 0, rooms.Count - 1);
            Vector3 spawnPoint = rooms[clamped].GetPlayerSpawnPoint();

            CharacterController controller = playerTransform.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            playerTransform.position = spawnPoint;

            if (controller != null)
            {
                controller.enabled = true;
            }

            currentRoomIndex = clamped;
            GameEvents.RaiseRoomProgressChanged(clamped + 1, rooms.Count);
            rooms[clamped].ForceActivate();
        }

        public void ResetForNewDay(Transform player)
        {
            if (player != null)
            {
                playerTransform = player;
            }

            CleanupLooseRuntimeObjects();

            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i] != null)
                {
                    rooms[i].ResetForNewDay(playerTransform);
                }
            }

            currentRoomIndex = 0;
            lastActivatedRoom = null;

            if (rooms.Count == 0)
            {
                GameEvents.RaiseRoomProgressChanged(0, 0);
                return;
            }

            MovePlayerToRoom(0);
        }

        private void CollectRoomsFromScene()
        {
            rooms.Clear();

            if (roomsRoot != null)
            {
                rooms.AddRange(roomsRoot.GetComponentsInChildren<Room>(true));
            }
            else
            {
                rooms.AddRange(FindObjectsByType<Room>(FindObjectsSortMode.None));
            }

            rooms.Sort(CompareRoomOrder);
        }

        private void BindRoomEvents()
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                Room room = rooms[i];
                room.Activated -= HandleRoomActivated;
                room.Cleared -= HandleRoomCleared;
                room.Activated += HandleRoomActivated;
                room.Cleared += HandleRoomCleared;
            }
        }

        private int CompareRoomOrder(Room a, Room b)
        {
            if (a == null && b == null)
            {
                return 0;
            }

            if (a == null)
            {
                return 1;
            }

            if (b == null)
            {
                return -1;
            }

            return a.RoomIndex.CompareTo(b.RoomIndex);
        }

        private void HandleRoomActivated(Room room)
        {
            if (lastActivatedRoom != null && lastActivatedRoom != room)
            {
                lastActivatedRoom.HideExitDoor();
            }

            lastActivatedRoom = room;
            currentRoomIndex = room.RoomIndex;
            GameEvents.RaiseRoomProgressChanged(currentRoomIndex + 1, rooms.Count);
        }

        private void HandleRoomCleared(Room room)
        {
            if (room.RoomType == RoomType.Boss)
            {
                GameEvents.RaiseNotification("Boss defeated");
            }
        }

        private void CleanupLooseRuntimeObjects()
        {
            HashSet<GameObject> objectsToDestroy = new HashSet<GameObject>();
            AddObjectsToDestroy(FindObjectsByType<EnemyBase>(FindObjectsSortMode.None), objectsToDestroy);
            AddObjectsToDestroy(FindObjectsByType<BossController>(FindObjectsSortMode.None), objectsToDestroy);
            AddObjectsToDestroy(FindObjectsByType<Coin>(FindObjectsSortMode.None), objectsToDestroy);
            AddObjectsToDestroy(FindObjectsByType<ItemPickup>(FindObjectsSortMode.None), objectsToDestroy);
            AddObjectsToDestroy(FindObjectsByType<ShopItem>(FindObjectsSortMode.None), objectsToDestroy);
            AddObjectsToDestroy(FindObjectsByType<UpgradeStation>(FindObjectsSortMode.None), objectsToDestroy);
            AddObjectsToDestroy(FindObjectsByType<LockedRewardChest>(FindObjectsSortMode.None), objectsToDestroy);
            AddObjectsToDestroy(FindObjectsByType<BossHazardZone>(FindObjectsSortMode.None), objectsToDestroy);

            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform target = transforms[i];
                if (target == null)
                {
                    continue;
                }

                if (target.name == "BossHazardWarning"
                    || target.name == "BossHazardArea"
                    || target.name == "BossSlamTelegraph")
                {
                    objectsToDestroy.Add(target.gameObject);
                }
            }

            foreach (GameObject target in objectsToDestroy)
            {
                if (target != null)
                {
                    Destroy(target);
                }
            }
        }

        private static void AddObjectsToDestroy<T>(T[] components, HashSet<GameObject> objectsToDestroy) where T : Component
        {
            if (components == null)
            {
                return;
            }

            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                {
                    objectsToDestroy.Add(components[i].gameObject);
                }
            }
        }

    }
}
