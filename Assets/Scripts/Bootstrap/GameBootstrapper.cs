using EchoDungeon.Core;
using EchoDungeon.Dungeon;
using EchoDungeon.Player;
using EchoDungeon.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoDungeon.Bootstrap
{
    public static class GameBootstrapper
    {
        private const string MainMenuSceneName = "MainMenu";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SetupScene();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SetupScene();
        }

        private static void SetupScene()
        {
            if (ShouldSkipGameplayBootstrap(SceneManager.GetActiveScene()))
            {
                Time.timeScale = 1f;
                return;
            }

            CoinManager coinManager = Object.FindFirstObjectByType<CoinManager>();
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            RoomManager roomManager = Object.FindFirstObjectByType<RoomManager>();
            DayVisualController dayVisualController = Object.FindFirstObjectByType<DayVisualController>();
            HUDController hudController = Object.FindFirstObjectByType<HUDController>();

            PlayerStats playerStats = Object.FindFirstObjectByType<PlayerStats>();
            PlayerHealth playerHealth = playerStats != null ? playerStats.GetComponent<PlayerHealth>() : null;
            PlayerInventory playerInventory = playerStats != null ? playerStats.GetComponent<PlayerInventory>() : null;
            Transform playerTransform = playerStats != null ? playerStats.transform : null;

            WarnIfMissing(coinManager, "CoinManager");
            WarnIfMissing(gameManager, "GameManager");
            WarnIfMissing(roomManager, "RoomManager");
            WarnIfMissing(dayVisualController, "DayVisualController");
            WarnIfMissing(hudController, "HUDController");
            WarnIfMissing(playerStats, "Player with PlayerStats");
            WarnIfMissing(playerHealth, "PlayerHealth");
            WarnIfMissing(playerInventory, "PlayerInventory");

            ConfigureCamera(playerTransform);

            if (roomManager != null)
            {
                roomManager.Initialize(playerTransform);
            }

            if (gameManager != null)
            {
                gameManager.RegisterSystems(playerStats, playerHealth, playerInventory, roomManager);
            }

            GameEvents.RaiseNotification("Echo Dungeon MVP ready");
        }

        private static bool ShouldSkipGameplayBootstrap(Scene scene)
        {
            // Main menu scenes should stay UI-only; gameplay systems are created after Start Game loads SampleScene.
            return scene.name == MainMenuSceneName
                || Object.FindFirstObjectByType<MainMenuUI>() != null;
        }

        private static void WarnIfMissing(Object target, string objectName)
        {
            if (target == null)
            {
                Debug.LogWarning("GameBootstrapper: " + objectName + " is missing from the scene.");
            }
        }

        private static void ConfigureCamera(Transform playerTarget)
        {
            if (playerTarget == null)
            {
                return;
            }

            Camera camera = Camera.main;
            WarnIfMissing(camera, "Main Camera");
            if (camera == null)
            {
                return;
            }

            TopDownCameraFollow follow = camera.GetComponent<TopDownCameraFollow>();
            WarnIfMissing(follow, "TopDownCameraFollow");

            if (follow != null)
            {
                follow.SetTarget(playerTarget);
            }
        }
    }
}
