using UnityEngine;

namespace EchoDungeon.Core
{
    public struct DayScaling
    {
        public readonly float HealthMultiplier;
        public readonly float DamageMultiplier;
        public readonly float MoveSpeedMultiplier;

        public DayScaling(float healthMultiplier, float damageMultiplier, float moveSpeedMultiplier)
        {
            HealthMultiplier = Mathf.Max(0.01f, healthMultiplier);
            DamageMultiplier = Mathf.Max(0.01f, damageMultiplier);
            MoveSpeedMultiplier = Mathf.Max(0.01f, moveSpeedMultiplier);
        }
    }

    public static class DayProgression
    {
        public const int DefaultMaxDay = 3;

        public static DayScaling CurrentScaling
        {
            get
            {
                int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;
                return GetScalingForDay(day);
            }
        }

        public static DayScaling GetScalingForDay(int day)
        {
            switch (Mathf.Clamp(day, 1, DefaultMaxDay))
            {
                case 2:
                    return new DayScaling(1.25f, 1.15f, 1.05f);
                case 3:
                    return new DayScaling(1.55f, 1.3f, 1.1f);
                default:
                    return new DayScaling(1f, 1f, 1f);
            }
        }
    }
}
