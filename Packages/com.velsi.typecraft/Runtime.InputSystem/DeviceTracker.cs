using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using InputSystemApi = UnityEngine.InputSystem.InputSystem;

namespace Velsi.Typecraft.InputSystem
{
    public static class DeviceTracker
    {
        private static IDisposable _buttonListener;

        public static void Start()
        {
            if (_buttonListener != null || !Application.isPlaying)
                return;

            _buttonListener = InputSystemApi.onAnyButtonPress.Call(control => Typecraft.SetDevice(FamilyOf(control.device)));
            InputSystemApi.onDeviceChange += OnDeviceChange;
            Typecraft.SetDevice(InitialFamily());
        }

        public static DeviceFamily FamilyOf(InputDevice device)
        {
            switch (device)
            {
                case Keyboard:
                case Mouse:
                    return DeviceFamily.KeyboardMouse;
                case Touchscreen:
                    return DeviceFamily.Touch;
                case Gamepad:
                    return GamepadFamily(device);
                default:
                    return Typecraft.Device;
            }
        }

        private static DeviceFamily GamepadFamily(InputDevice device)
        {
            string product = device.description.product ?? string.Empty;
            if (product.IndexOf("Steam Deck", StringComparison.OrdinalIgnoreCase) >= 0)
                return DeviceFamily.SteamDeck;

            string layout = device.layout;
            if (IsBasedOn(layout, "DualShockGamepad") || IsBasedOn(layout, "DualSenseGamepadHID"))
                return DeviceFamily.PlayStation;
            if (IsBasedOn(layout, "SwitchProControllerHID"))
                return DeviceFamily.Switch;
            if (IsBasedOn(layout, "XInputController") || IsBasedOn(layout, "XboxOneGampad"))
                return DeviceFamily.Xbox;

            return DeviceFamily.GenericGamepad;
        }

        private static bool IsBasedOn(string layout, string baseLayout)
        {
            try
            {
                return InputSystemApi.IsFirstLayoutBasedOnSecond(layout, baseLayout);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static DeviceFamily InitialFamily()
        {
            if (Application.isMobilePlatform && Touchscreen.current != null)
                return DeviceFamily.Touch;
            if (Application.isConsolePlatform && Gamepad.current != null)
                return GamepadFamily(Gamepad.current);
            return Typecraft.Settings != null ? Typecraft.Settings.DefaultDevice : DeviceFamily.KeyboardMouse;
        }

        private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Removed && FamilyOf(device) == Typecraft.Device && Gamepad.current == null)
                Typecraft.SetDevice(InitialFamily());
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _buttonListener?.Dispose();
            _buttonListener = null;
            InputSystemApi.onDeviceChange -= OnDeviceChange;
        }
    }
}
