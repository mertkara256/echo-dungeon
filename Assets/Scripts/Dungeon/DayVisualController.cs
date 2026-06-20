using EchoDungeon.Core;
using UnityEngine;

namespace EchoDungeon.Dungeon
{
    public sealed class DayVisualController : MonoBehaviour
    {
        private struct DayPalette
        {
            public readonly Color FloorColor;
            public readonly Color WallColor;
            public readonly Color CorridorFloorColor;
            public readonly Color CorridorWallColor;
            public readonly Color BossFloorColor;
            public readonly Color BossWallColor;
            public readonly Color AmbientColor;
            public readonly Color LightColor;
            public readonly float LightIntensity;

            public DayPalette(
                Color floorColor,
                Color wallColor,
                Color corridorFloorColor,
                Color corridorWallColor,
                Color bossFloorColor,
                Color bossWallColor,
                Color ambientColor,
                Color lightColor,
                float lightIntensity)
            {
                FloorColor = floorColor;
                WallColor = wallColor;
                CorridorFloorColor = corridorFloorColor;
                CorridorWallColor = corridorWallColor;
                BossFloorColor = bossFloorColor;
                BossWallColor = bossWallColor;
                AmbientColor = ambientColor;
                LightColor = lightColor;
                LightIntensity = lightIntensity;
            }
        }

        private void OnEnable()
        {
            GameEvents.DayChanged += HandleDayChanged;
        }

        private void OnDisable()
        {
            GameEvents.DayChanged -= HandleDayChanged;
        }

        private void Start()
        {
            int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;
            ApplyPalette(day);
        }

        private void HandleDayChanged(int currentDay, int maxDay)
        {
            ApplyPalette(currentDay);
        }

        private void ApplyPalette(int day)
        {
            DayPalette palette = GetPalette(day);
            ApplyGeometryPalette(palette);
            ApplyLightingPalette(palette);
        }

        private void ApplyGeometryPalette(DayPalette palette)
        {
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer target = renderers[i];
                if (target == null)
                {
                    continue;
                }

                string objectName = target.gameObject.name;
                Room room = target.GetComponentInParent<Room>();
                bool bossRoom = room != null && room.RoomType == RoomType.Boss;

                if (objectName == "CorridorFloor")
                {
                    ApplyColor(target, palette.CorridorFloorColor);
                }
                else if (objectName == "CorridorLeftWall" || objectName == "CorridorRightWall")
                {
                    ApplyColor(target, palette.CorridorWallColor);
                }
                else if (objectName == "Floor")
                {
                    ApplyColor(target, bossRoom ? palette.BossFloorColor : palette.FloorColor);
                }
                else if (IsRoomWallName(objectName))
                {
                    ApplyColor(target, bossRoom ? palette.BossWallColor : palette.WallColor);
                }
            }
        }

        private void ApplyLightingPalette(DayPalette palette)
        {
            RenderSettings.ambientLight = palette.AmbientColor;

            Light sun = RenderSettings.sun;
            if (sun == null)
            {
                Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i] != null && lights[i].type == LightType.Directional)
                    {
                        sun = lights[i];
                        break;
                    }
                }
            }

            if (sun == null)
            {
                return;
            }

            sun.color = palette.LightColor;
            sun.intensity = palette.LightIntensity;
        }

        private static bool IsRoomWallName(string objectName)
        {
            return objectName == "LeftWall"
                || objectName == "RightWall"
                || objectName == "NorthWall"
                || objectName == "SouthWall"
                || objectName == "NorthWall_A"
                || objectName == "NorthWall_B"
                || objectName == "SouthWall_A"
                || objectName == "SouthWall_B";
        }

        private static void ApplyColor(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.material.color = color;
        }

        private static DayPalette GetPalette(int day)
        {
            switch (Mathf.Clamp(day, 1, DayProgression.DefaultMaxDay))
            {
                case 2:
                    return new DayPalette(
                        new Color(0.3f, 0.22f, 0.16f, 1f),
                        new Color(0.43f, 0.29f, 0.2f, 1f),
                        new Color(0.25f, 0.17f, 0.12f, 1f),
                        new Color(0.34f, 0.23f, 0.17f, 1f),
                        new Color(0.36f, 0.18f, 0.16f, 1f),
                        new Color(0.5f, 0.25f, 0.2f, 1f),
                        new Color(0.45f, 0.3f, 0.2f, 1f),
                        new Color(1f, 0.68f, 0.38f, 1f),
                        0.95f);
                case 3:
                    return new DayPalette(
                        new Color(0.14f, 0.16f, 0.22f, 1f),
                        new Color(0.21f, 0.24f, 0.34f, 1f),
                        new Color(0.1f, 0.12f, 0.18f, 1f),
                        new Color(0.16f, 0.18f, 0.28f, 1f),
                        new Color(0.18f, 0.1f, 0.16f, 1f),
                        new Color(0.3f, 0.15f, 0.25f, 1f),
                        new Color(0.17f, 0.19f, 0.29f, 1f),
                        new Color(0.52f, 0.64f, 1f, 1f),
                        0.68f);
                default:
                    return new DayPalette(
                        new Color(0.23f, 0.22f, 0.2f, 1f),
                        new Color(0.34f, 0.32f, 0.3f, 1f),
                        new Color(0.2f, 0.19f, 0.17f, 1f),
                        new Color(0.3f, 0.28f, 0.25f, 1f),
                        new Color(0.28f, 0.16f, 0.16f, 1f),
                        new Color(0.42f, 0.24f, 0.24f, 1f),
                        new Color(0.4f, 0.38f, 0.34f, 1f),
                        new Color(1f, 0.9f, 0.72f, 1f),
                        1.05f);
            }
        }
    }
}
