using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Game;
using HarmonyLib;
using UnityEngine;

namespace FloatingXPNumbers
{
    [BepInPlugin("tyranny.mod.floatingxpnumbers", "Floating XP Numbers", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;
        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<string> TextFormat;

        private void Awake()
        {
            Log = Logger;

            ModEnabled = Config.Bind("General", "Enabled", true, "Enable or disable floating XP numbers above characters.");
            TextFormat = Config.Bind("General", "TextFormat", "+{0} XP", "Format string for floating text. {0} is replaced by XP amount.");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Log.LogInfo("Loaded: Floating XP Numbers (BepInEx Build)");
        }
    }

    [HarmonyPatch(typeof(CharacterStats), "AddExperience", new System.Type[] { typeof(int) })]
    internal static class CharacterStats_AddExperience_Patch
    {
        private static void Postfix(CharacterStats __instance, int xp)
        {
            if (!Plugin.ModEnabled.Value || xp <= 0 || __instance == null || __instance.gameObject == null)
            {
                return;
            }

            if (UIHealthstringManager.Instance != null)
            {
                string text = string.Format(Plugin.TextFormat.Value, xp);
                UIHealthstringManager.Instance.ShowString(text, Color.yellow, __instance.gameObject);
            }
        }
    }
}