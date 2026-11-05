using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace TimeSpeedControl
{
    [BepInPlugin("tyranny.mod.timespeedcontrol", "Time Speed Control QoL", "2.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;

        // Config Entries: General
        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<float> DefaultSpeed;
        public static ConfigEntry<float> StepSize;
        public static ConfigEntry<float> MinSpeed;
        public static ConfigEntry<float> MaxSpeed;

        // Config Entries: Hotkeys
        public static ConfigEntry<KeyboardShortcut> KeyIncrease;
        public static ConfigEntry<KeyboardShortcut> KeyDecrease;
        public static ConfigEntry<KeyboardShortcut> KeyToggleMax;
        public static ConfigEntry<KeyboardShortcut> KeyToggleUI;

        // Config Entries: UI
        public static ConfigEntry<bool> ShowUI;
        public static ConfigEntry<float> WindowX;
        public static ConfigEntry<float> WindowY;

        // Runtime State
        public static float CurrentSpeed = 1.0f;
        private static Rect s_windowRect = new Rect(20f, 20f, 240f, 150f);

        private void Awake()
        {
            Log = Logger;

            // Bind Settings
            ModEnabled = Config.Bind("General", "Enabled", true, "Enable or disable time speed modifier.");
            DefaultSpeed = Config.Bind("General", "DefaultSpeed", 1.0f, "Initial target speed multiplier.");
            StepSize = Config.Bind("General", "StepSize", 0.25f, "Speed adjustment step per keypress.");
            MinSpeed = Config.Bind("General", "MinSpeed", 0.5f, "Minimum allowed speed.");
            MaxSpeed = Config.Bind("General", "MaxSpeed", 4.0f, "Maximum allowed speed.");

            // Bind Hotkeys
            KeyIncrease = Config.Bind("Hotkeys", "IncreaseSpeed", new KeyboardShortcut(KeyCode.KeypadPlus), "Hotkey to increase speed.");
            KeyDecrease = Config.Bind("Hotkeys", "DecreaseSpeed", new KeyboardShortcut(KeyCode.KeypadMinus), "Hotkey to decrease speed.");
            KeyToggleMax = Config.Bind("Hotkeys", "ToggleMaxOrReset", new KeyboardShortcut(KeyCode.KeypadMultiply), "Hotkey: toggles between 1.0x and MaxSpeed.");
            KeyToggleUI = Config.Bind("Hotkeys", "ToggleUI", new KeyboardShortcut(KeyCode.KeypadDivide), "Hotkey to hide/show the UI window.");

            // Bind UI Settings
            ShowUI = Config.Bind("UI", "ShowUI", true, "Show or hide the interactive UI window.");
            WindowX = Config.Bind("UI", "WindowX", 20f, "Saved Window X position.");
            WindowY = Config.Bind("UI", "WindowY", 20f, "Saved Window Y position.");

            CurrentSpeed = DefaultSpeed.Value;
            s_windowRect.x = WindowX.Value;
            s_windowRect.y = WindowY.Value;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Log.LogInfo("Loaded: Time Speed Control QoL v2.0.0");
        }

        private void Update()
        {
            if (!ModEnabled.Value) return;

            HandleHotkeys();
        }

        private void HandleHotkeys()
        {
            if (KeyToggleUI.Value.IsDown())
            {
                ShowUI.Value = !ShowUI.Value;
            }

            if (KeyIncrease.Value.IsDown())
            {
                SetSpeed(CurrentSpeed + StepSize.Value);
            }
            else if (KeyDecrease.Value.IsDown())
            {
                SetSpeed(CurrentSpeed - StepSize.Value);
            }
            else if (KeyToggleMax.Value.IsDown())
            {
                if (Mathf.Approximately(CurrentSpeed, 1.0f))
                {
                    SetSpeed(MaxSpeed.Value);
                }
                else
                {
                    SetSpeed(1.0f);
                }
            }
        }

        public static void SetSpeed(float newSpeed)
        {
            CurrentSpeed = Mathf.Clamp(newSpeed, MinSpeed.Value, MaxSpeed.Value);
        }

        private void OnGUI()
        {
            if (!ModEnabled.Value || !ShowUI.Value) return;

            float oldX = s_windowRect.x;
            float oldY = s_windowRect.y;

            s_windowRect = GUILayout.Window(9901, s_windowRect, DrawWindowContents, "Time Speed 2.0");

            if (!Mathf.Approximately(oldX, s_windowRect.x) || !Mathf.Approximately(oldY, s_windowRect.y))
            {
                WindowX.Value = s_windowRect.x;
                WindowY.Value = s_windowRect.y;
            }
        }

        private static void DrawWindowContents(int windowID)
        {
            GUILayout.BeginVertical();

            // Status Indicator
            GUILayout.Label($"Speed: {CurrentSpeed:F2}x", GUILayout.ExpandWidth(true));

            // Interactive Slider
            float newSliderVal = GUILayout.HorizontalSlider(CurrentSpeed, MinSpeed.Value, MaxSpeed.Value);
            if (!Mathf.Approximately(newSliderVal, CurrentSpeed))
            {
                SetSpeed(newSliderVal);
            }

            GUILayout.Space(8f);

            // Quick Preset Buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("1.0x")) SetSpeed(1.0f);
            if (GUILayout.Button("1.5x")) SetSpeed(1.5f);
            if (GUILayout.Button("2.0x")) SetSpeed(2.0f);
            if (GUILayout.Button($"Max ({MaxSpeed.Value:F1}x)")) SetSpeed(MaxSpeed.Value);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }
    }

    [HarmonyPatch(typeof(TimeController))]
    internal static class TimeController_UpdateTimeScale_Patch
    {
        [HarmonyPatch("UpdateTimeScale", new Type[0])]
        [HarmonyPostfix]
        private static void Postfix()
        {
            if (!Plugin.ModEnabled.Value) return;

            if (TimeController.Instance != null && TimeController.Instance.Paused)
            {
                return;
            }

            if (!Mathf.Approximately(Time.timeScale, Plugin.CurrentSpeed))
            {
                Time.timeScale = Plugin.CurrentSpeed;
            }
        }
    }
}