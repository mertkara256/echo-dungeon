using UnityEngine.InputSystem;
using UnityEngine;

namespace EchoDungeon.Core
{
    public static class GameInput
    {
        public static Vector2 Move
        {
            get
            {
                Vector2 move = Vector2.zero;

                Keyboard keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                    {
                        move.x -= 1f;
                    }

                    if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                    {
                        move.x += 1f;
                    }

                    if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                    {
                        move.y -= 1f;
                    }

                    if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                    {
                        move.y += 1f;
                    }
                }

                return move.sqrMagnitude > 1f ? move.normalized : move;
            }
        }

        public static Vector2 PointerPosition
        {
            get
            {
                if (Mouse.current != null)
                {
                    return Mouse.current.position.ReadValue();
                }

                return Vector2.zero;
            }
        }

        public static bool IsPrimaryFireHeld()
        {
            bool pressed = false;

            pressed = Mouse.current != null && Mouse.current.leftButton.isPressed;

            return pressed;
        }

        public static bool WasSecondaryFirePressed()
        {
            bool pressed = false;

            pressed = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;

            return pressed;
        }

        public static bool WasPausePressed()
        {
            return WasKeyPressed(KeyBinding.Pause);
        }

        public static bool WasRestartPressed()
        {
            return WasKeyPressed(KeyBinding.Restart);
        }

        public static bool WasPotionPressed()
        {
            return WasKeyPressed(KeyBinding.Potion);
        }

        public static bool WasSpeedBoostPressed()
        {
            return WasKeyPressed(KeyBinding.SpeedBoost);
        }

        public static bool WasInteractPressed()
        {
            return WasKeyPressed(KeyBinding.Interact);
        }

        public static bool WasStasisSpellPressed()
        {
            return WasKeyPressed(KeyBinding.StasisSpell);
        }

        public static bool WasPoisonSpellPressed()
        {
            return WasKeyPressed(KeyBinding.PoisonSpell);
        }

        public static bool WasDashPressed()
        {
            return WasKeyPressed(KeyBinding.Dash);
        }

        public static bool WasDraftChoicePressed(int choiceIndex)
        {
            if (choiceIndex == 0)
            {
                return WasKeyPressed(KeyBinding.ChoiceOne);
            }

            if (choiceIndex == 1)
            {
                return WasKeyPressed(KeyBinding.ChoiceTwo);
            }

            if (choiceIndex == 2)
            {
                return WasKeyPressed(KeyBinding.ChoiceThree);
            }

            return false;
        }

        private static bool WasKeyPressed(KeyBinding binding)
        {
            bool pressed = false;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                pressed = WasNewInputKeyPressed(keyboard, binding);
            }

            return pressed;
        }

        private static bool WasNewInputKeyPressed(Keyboard keyboard, KeyBinding binding)
        {
            switch (binding)
            {
                case KeyBinding.Pause:
                    return keyboard.escapeKey.wasPressedThisFrame;
                case KeyBinding.Restart:
                    return keyboard.rKey.wasPressedThisFrame;
                case KeyBinding.Potion:
                    return keyboard.qKey.wasPressedThisFrame;
                case KeyBinding.SpeedBoost:
                    return keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame;
                case KeyBinding.Interact:
                    return keyboard.eKey.wasPressedThisFrame;
                case KeyBinding.StasisSpell:
                    return keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame;
                case KeyBinding.PoisonSpell:
                    return keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame;
                case KeyBinding.Dash:
                    return keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame;
                case KeyBinding.ChoiceOne:
                    return keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame;
                case KeyBinding.ChoiceTwo:
                    return keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame;
                case KeyBinding.ChoiceThree:
                    return keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame;
                default:
                    return false;
            }
        }

        private enum KeyBinding
        {
            Pause,
            Restart,
            Potion,
            SpeedBoost,
            Interact,
            StasisSpell,
            PoisonSpell,
            Dash,
            ChoiceOne,
            ChoiceTwo,
            ChoiceThree
        }
    }
}
