using System.Collections;
using EchoDungeon.Data;
using EchoDungeon.Dungeon;
using EchoDungeon.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoDungeon.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Day Progression")]
        [SerializeField] private int maxDay = DayProgression.DefaultMaxDay;
        [SerializeField] private float dayTransitionResetDelay = 0.45f;
        [SerializeField] private float dayTransitionResumeDelay = 1.15f;

        private PlayerStats playerStats;
        private PlayerHealth playerHealth;
        private PlayerInventory playerInventory;
        private RoomManager roomManager;

        private GameState state = GameState.Booting;
        private int score;
        private int currentRoom;
        private int totalRooms;
        private int currentDay = 1;
        private Coroutine hitPauseRoutine;
        private Coroutine dayTransitionRoutine;
        private bool isHitPauseActive;

        public bool IsPlaying
        {
            get { return state == GameState.Playing; }
        }

        public int Score
        {
            get { return score; }
        }

        public int CurrentDay
        {
            get { return currentDay; }
        }

        public int MaxDay
        {
            get { return maxDay; }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.PlayerDied += HandlePlayerDied;
            GameEvents.GameWon += HandleGameWon;
            GameEvents.BossCleared += HandleBossCleared;
            GameEvents.RoomProgressChanged += HandleRoomProgressChanged;
            GameEvents.HitPauseRequested += HandleHitPauseRequested;
        }

        private void OnDisable()
        {
            GameEvents.PlayerDied -= HandlePlayerDied;
            GameEvents.GameWon -= HandleGameWon;
            GameEvents.BossCleared -= HandleBossCleared;
            GameEvents.RoomProgressChanged -= HandleRoomProgressChanged;
            GameEvents.HitPauseRequested -= HandleHitPauseRequested;

            if (hitPauseRoutine != null)
            {
                StopCoroutine(hitPauseRoutine);
                hitPauseRoutine = null;
            }

            if (dayTransitionRoutine != null)
            {
                StopCoroutine(dayTransitionRoutine);
                dayTransitionRoutine = null;
            }

            isHitPauseActive = false;
            Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            currentDay = 1;
            maxDay = Mathf.Max(1, maxDay);
            UpgradeCatalog.ResetRunState();
            SetState(GameState.Playing);
            GameEvents.RaiseScoreChanged(score);
            GameEvents.RaiseDayChanged(currentDay, maxDay);
        }

        private void Update()
        {
            HandleGlobalInput();
        }

        public void RegisterSystems(
            PlayerStats stats,
            PlayerHealth health,
            PlayerInventory inventory,
            RoomManager rooms)
        {
            playerStats = stats;
            playerHealth = health;
            playerInventory = inventory;
            roomManager = rooms;
        }

        public void AddScore(int value)
        {
            if (value <= 0)
            {
                return;
            }

            score += value;
            GameEvents.RaiseScoreChanged(score);
        }

        public void SetState(GameState newState)
        {
            if (state == newState)
            {
                return;
            }

            state = newState;
            ApplyTimeScale();
            GameEvents.RaiseGameStateChanged(state);
        }

        private void HandleGlobalInput()
        {
            if (GameInput.WasPausePressed()
                && state != GameState.GameOver
                && state != GameState.Win
                && state != GameState.RewardSelection
                && state != GameState.DayTransition)
            {
                if (state == GameState.Paused)
                {
                    SetState(GameState.Playing);
                }
                else
                {
                    SetState(GameState.Paused);
                }
            }

            if ((state == GameState.GameOver || state == GameState.Win) && GameInput.WasRestartPressed())
            {
                Time.timeScale = 1f;
                if (CoinManager.Instance != null)
                {
                    CoinManager.Instance.ResetCoinsToStartingAmount();
                }

                Scene activeScene = SceneManager.GetActiveScene();
                SceneManager.LoadScene(activeScene.buildIndex);
            }

            if (GameInput.WasDebugAdvanceDayPressed())
            {
                TryStartNextDayTransition();
            }

            if (GameInput.WasDebugGrantEchoBarragePressed())
            {
                GrantDebugEchoBarrage();
            }
        }

        private void GrantDebugEchoBarrage()
        {
            if (playerStats == null)
            {
                return;
            }

            PlayerCombat combat = playerStats.GetComponent<PlayerCombat>();
            if (combat == null)
            {
                return;
            }

            combat.EnableEchoBarrage();
            GameEvents.RaiseNotification("Debug: Echo Barrage enabled");
        }

        private void HandlePlayerDied()
        {
            SetState(GameState.GameOver);
            GameEvents.RaiseNotification("Game Over - press R to restart");
        }

        private void HandleGameWon()
        {
            SetState(GameState.Win);
            GameEvents.RaiseNotification("Victory - press R to restart");
        }

        private void HandleBossCleared()
        {
            if (state == GameState.Win || state == GameState.GameOver || state == GameState.DayTransition)
            {
                return;
            }

            if (currentDay >= maxDay)
            {
                GameEvents.RaiseGameWon();
                return;
            }

            TryStartNextDayTransition();
        }

        private void TryStartNextDayTransition()
        {
            if (state != GameState.Playing || currentDay >= maxDay)
            {
                return;
            }

            if (dayTransitionRoutine != null)
            {
                StopCoroutine(dayTransitionRoutine);
            }

            dayTransitionRoutine = StartCoroutine(DayTransitionRoutine());
        }

        private void HandleRoomProgressChanged(int currentRoomIndex, int totalRoomCount)
        {
            currentRoom = currentRoomIndex;
            totalRooms = totalRoomCount;
        }

        private void HandleHitPauseRequested(float duration)
        {
            if (state != GameState.Playing || duration <= 0f)
            {
                return;
            }

            if (hitPauseRoutine != null)
            {
                StopCoroutine(hitPauseRoutine);
            }

            hitPauseRoutine = StartCoroutine(HitPauseRoutine(duration));
        }

        private IEnumerator HitPauseRoutine(float duration)
        {
            isHitPauseActive = true;
            ApplyTimeScale();
            yield return new WaitForSecondsRealtime(duration);
            isHitPauseActive = false;
            hitPauseRoutine = null;
            ApplyTimeScale();
        }

        private IEnumerator DayTransitionRoutine()
        {
            SetState(GameState.DayTransition);

            currentDay = Mathf.Min(maxDay, currentDay + 1);
            GameEvents.RaiseDayChanged(currentDay, maxDay);
            GameEvents.RaiseNotification("Day " + currentDay + " begins");

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, dayTransitionResetDelay));

            if (playerHealth != null)
            {
                playerHealth.ResetHealthToMax();
            }

            if (roomManager != null)
            {
                Transform playerTransform = playerHealth != null ? playerHealth.transform : null;
                roomManager.ResetForNewDay(playerTransform);
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, dayTransitionResumeDelay));

            dayTransitionRoutine = null;
            SetState(GameState.Playing);
        }

        private void ApplyTimeScale()
        {
            float timeScale = GetStateTimeScale();
            if (isHitPauseActive && state == GameState.Playing)
            {
                timeScale = 0f;
            }

            Time.timeScale = timeScale;
        }

        private float GetStateTimeScale()
        {
            return state == GameState.Paused
                || state == GameState.RewardSelection
                || state == GameState.DayTransition
                ? 0f
                : 1f;
        }
    }
}
