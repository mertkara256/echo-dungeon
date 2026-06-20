using System;

namespace EchoDungeon.Core
{
    public static class GameEvents
    {
        public static event Action<float, float> PlayerHealthChanged;
        public static event Action<float> PlayerDamaged;
        public static event Action<int> ScoreChanged;
        public static event Action<int> CoinsChanged;
        public static event Action<int, int> RoomProgressChanged;
        public static event Action<int, int> DayChanged;
        public static event Action<GameState> GameStateChanged;
        public static event Action<string> NotificationRaised;
        public static event Action<string> InteractionPromptChanged;
        public static event Action<int, int, int> InventoryChanged;
        public static event Action<int, int> SpellChargesChanged;
        public static event Action PlayerDashed;
        public static event Action<float> PlayerHitLanded;
        public static event Action<float> HitPauseRequested;
        public static event Action BossCleared;
        public static event Action PlayerDied;
        public static event Action GameWon;

        public static void RaisePlayerHealthChanged(float current, float max)
        {
            PlayerHealthChanged?.Invoke(current, max);
        }

        public static void RaisePlayerDamaged(float amount)
        {
            PlayerDamaged?.Invoke(amount);
        }

        public static void RaiseScoreChanged(int score)
        {
            ScoreChanged?.Invoke(score);
        }

        public static void RaiseCoinsChanged(int coins)
        {
            CoinsChanged?.Invoke(coins);
        }

        public static void RaiseRoomProgressChanged(int currentRoomIndex, int totalRooms)
        {
            RoomProgressChanged?.Invoke(currentRoomIndex, totalRooms);
        }

        public static void RaiseDayChanged(int currentDay, int maxDay)
        {
            DayChanged?.Invoke(currentDay, maxDay);
        }

        public static void RaiseGameStateChanged(GameState state)
        {
            GameStateChanged?.Invoke(state);
        }

        public static void RaiseNotification(string message)
        {
            NotificationRaised?.Invoke(message);
        }

        public static void RaiseInteractionPrompt(string message)
        {
            InteractionPromptChanged?.Invoke(message);
        }

        public static void ClearInteractionPrompt()
        {
            InteractionPromptChanged?.Invoke(string.Empty);
        }

        public static void RaiseInventoryChanged(int potionCount, int speedBoostCount, int keyCount)
        {
            InventoryChanged?.Invoke(potionCount, speedBoostCount, keyCount);
        }

        public static void RaiseSpellChargesChanged(int stasisCharges, int poisonCharges)
        {
            SpellChargesChanged?.Invoke(stasisCharges, poisonCharges);
        }

        public static void RaisePlayerDashed()
        {
            PlayerDashed?.Invoke();
        }

        public static void RaisePlayerHitLanded(float intensity)
        {
            PlayerHitLanded?.Invoke(intensity);
        }

        public static void RaiseHitPauseRequested(float duration)
        {
            HitPauseRequested?.Invoke(duration);
        }

        public static void RaiseBossCleared()
        {
            BossCleared?.Invoke();
        }

        public static void RaisePlayerDied()
        {
            PlayerDied?.Invoke();
        }

        public static void RaiseGameWon()
        {
            GameWon?.Invoke();
        }

    }
}
