using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Game;
using HarmonyLib;

namespace NoPerRestAbilities
{
    [BepInPlugin("tyranny.mod.noperrestabilities", "No Per Rest Abilities (Per Encounter Mod)", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;
        public static ConfigEntry<bool> ModEnabled;

        private void Awake()
        {
            Log = Logger;

            ModEnabled = Config.Bind("General", "Enabled", true, "Enable converting all Per Rest abilities into Per Encounter.");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Log.LogInfo("Loaded: No Per Rest Abilities (Per Encounter Rework)");
        }
    }

    [HarmonyPatch(typeof(GenericAbility), "HandleGameUtilitiesOnCombatEnd")]
    internal static class GenericAbility_OnCombatEnd_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(GenericAbility __instance)
        {
            if (!Plugin.ModEnabled.Value || __instance == null) return;

            __instance.RestoreCooldownCounter(0);

            FieldInfo cooldownTimerField = typeof(GenericAbility).GetField("m_cooldownTimer", BindingFlags.Instance | BindingFlags.NonPublic);
            if (cooldownTimerField != null)
            {
                cooldownTimerField.SetValue(__instance, 0f);
            }
        }
    }
}