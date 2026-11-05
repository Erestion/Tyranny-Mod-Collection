using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Game;
using HarmonyLib;
using UnityEngine;

namespace WeaponMasteryRework
{
    [BepInPlugin("tyranny.mod.weaponmasteryrework", "Weapon Mastery & Universal Riposte Rework", "5.6.1")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;
        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<bool> EnableMasteryRework;
        public static ConfigEntry<bool> EnableUniversalRiposte;

        private static readonly HashSet<string> Tier1Masteries = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "psv_pc_1h_mastery_01",
            "psv_pc_2h_mastery_01",
            "psv_pc_agility_dualwieldmastery_1of3",
            "psv_pc_range_bowmastery_1of3",
            "psv_pc_range_thrownmastery_1of3",
            "psv_pc_magic_magicstaffmastery_1of3",
            "psv_pc_power_unarmedmastery_1of5"
        };

        private static readonly HashSet<string> Tier2Masteries = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "psv_pc_1h_mastery_02",
            "psv_pc_2h_mastery_02",
            "psv_pc_agility_dualwieldmastery_2of3",
            "psv_pc_range_bowmastery_2of3",
            "psv_pc_range_thrownmastery_2of3",
            "psv_pc_magic_magicstaffmastery_2of3",
            "psv_pc_power_unarmedmastery_2of5"
        };

        private static readonly HashSet<string> Tier3Masteries = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "psv_pc_1h_mastery_03",
            "psv_pc_2h_mastery_03",
            "psv_pc_agility_dualwieldmastery_3of3",
            "psv_pc_range_bowmastery_3of3",
            "psv_pc_range_thrownmastery_3of3",
            "psv_pc_magic_magicstaffmastery_3of3",
            "psv_pc_power_unarmedmastery_3of5"
        };

        private void Awake()
        {
            Log = Logger;

            ModEnabled = Config.Bind("0.General", "Enabled", true, "Master toggle.");
            EnableMasteryRework = Config.Bind("1.Features", "EnableMasteryRework", true, "Enable multi-hit chances & mastery damage bonuses (10%/20%/30%).");
            EnableUniversalRiposte = Config.Bind("1.Features", "EnableUniversalRiposte", true, "Allow Duelist passives & Riposte to work with any weapon setup.");

            Game.GameState.LevelLoaded += OnGameLevelLoaded;

            Harmony harmony = new Harmony("tyranny.mod.weaponmasteryrework");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.LogInfo("==================================================");
            Log.LogInfo("Loaded: Weapon Mastery & Universal Riposte Rework v5.6.1");
            Log.LogInfo("==================================================");
        }

        private static void OnGameLevelLoaded(object sender, EventArgs e)
        {
            if (!ModEnabled.Value) return;

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            Game.GenericAbility[] abilities = Resources.FindObjectsOfTypeAll<Game.GenericAbility>();
            foreach (var ability in abilities)
            {
                if (ability == null) continue;
                ApplyMasteryAndRiposteToAbility(ability, flags);
            }

            Game.CharacterStats[] allStats = Resources.FindObjectsOfTypeAll<Game.CharacterStats>();
            foreach (var stats in allStats)
            {
                if (stats == null || stats.ActiveAbilities == null) continue;

                foreach (var actAbility in stats.ActiveAbilities)
                {
                    if (actAbility == null) continue;
                    ApplyMasteryAndRiposteToAbility(actAbility, flags);
                }
            }
        }

        private static void ApplyMasteryAndRiposteToAbility(SDK.GenericAbility baseAbility, BindingFlags flags)
        {
            if (baseAbility == null) return;

            Game.GenericAbility ability = baseAbility as Game.GenericAbility;
            string cleanName = baseAbility.gameObject.name.Replace("(Clone)", "").Trim();

            if (EnableMasteryRework.Value)
            {
                float targetMult = 0f;

                if (Tier1Masteries.Contains(cleanName)) targetMult = 1.10f;
                else if (Tier2Masteries.Contains(cleanName)) targetMult = 1.20f;
                else if (Tier3Masteries.Contains(cleanName)) targetMult = 1.30f;

                if (targetMult > 0f)
                {
                    if (baseAbility is WeaponSpecialization specAbility)
                    {
                        specAbility.BonusDamageMult = targetMult;
                    }
                    else
                    {
                        FieldInfo multField = baseAbility.GetType().GetField("BonusDamageMult", flags);
                        if (multField != null && multField.FieldType == typeof(float))
                        {
                            multField.SetValue(baseAbility, targetMult);
                        }
                    }
                }
            }

            if (EnableUniversalRiposte.Value && ability != null &&
               (cleanName.IndexOf("duelist", StringComparison.OrdinalIgnoreCase) >= 0 || cleanName.IndexOf("agility_2of3", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                FieldInfo weaponTypesField = typeof(Game.GenericAbility).GetField("PermittedWeaponTypes", flags);
                if (weaponTypesField != null) weaponTypesField.SetValue(ability, Weapon.WeaponType.None);

                FieldInfo equipVerifyField = typeof(Game.GenericAbility).GetField("EquipmentForVerification", flags);
                if (equipVerifyField != null) equipVerifyField.SetValue(ability, "");

                FieldInfo appPrereqField = typeof(Game.GenericAbility).GetField("ApplicationPrerequisites", flags);
                if (appPrereqField != null) appPrereqField.SetValue(ability, new PrerequisiteData[0]);

                FieldInfo actPrereqField = typeof(Game.GenericAbility).GetField("ActivationPrerequisites", flags);
                if (actPrereqField != null) actPrereqField.SetValue(ability, new PrerequisiteData[0]);
            }
        }
    }

    [HarmonyPatch(typeof(Health), "DoDamage", new Type[] { typeof(DamageInfo), typeof(GameObject), typeof(bool) })]
    internal static class Health_DoDamage_UniversalRiposte_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(Health __instance, DamageInfo damage, GameObject attacker)
        {
            if (!Plugin.ModEnabled.Value || !Plugin.EnableUniversalRiposte.Value || __instance == null || attacker == null || damage == null) return;

            if (damage.Attack != null && damage.Attack.IsMelee && (damage.IsMiss || damage.IsGraze) && damage.AttackIsHostile)
            {
                AIController aiComponent = __instance.GetComponent<AIController>();
                CharacterStats statsComponent = __instance.GetComponent<CharacterStats>();

                if (aiComponent != null && statsComponent != null && !__instance.Dead && !__instance.Unconscious)
                {
                    bool hasDuelist = false;
                    if (statsComponent.ActiveAbilities != null)
                    {
                        foreach (var actAbility in statsComponent.ActiveAbilities)
                        {
                            if (actAbility != null && actAbility.gameObject.name.IndexOf("duelist", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                hasDuelist = true;
                                break;
                            }
                        }
                    }

                    if (hasDuelist)
                    {
                        AttackBase.LaunchRiposteAttack(aiComponent, attacker);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Game.GenericAbility), "IsValidEquipment", new Type[] { typeof(Equippable), typeof(Equippable), typeof(Equipment) })]
    internal static class GenericAbility_IsValidEquipment_ThreeParams_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(Game.GenericAbility __instance, ref bool __result)
        {
            if (!Plugin.ModEnabled.Value || !Plugin.EnableUniversalRiposte.Value || __instance == null) return;
            string name = __instance.gameObject.name;
            if (name.IndexOf("duelist", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("agility_2of3", StringComparison.OrdinalIgnoreCase) >= 0) __result = true;
        }
    }

    [HarmonyPatch(typeof(Game.GenericAbility), "IsValidEquipment", new Type[] { typeof(WeaponSet), typeof(Equipment) })]
    internal static class GenericAbility_IsValidEquipment_WeaponSet_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(Game.GenericAbility __instance, ref bool __result)
        {
            if (!Plugin.ModEnabled.Value || !Plugin.EnableUniversalRiposte.Value || __instance == null) return;
            string name = __instance.gameObject.name;
            if (name.IndexOf("duelist", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("agility_2of3", StringComparison.OrdinalIgnoreCase) >= 0) __result = true;
        }
    }

    [HarmonyPatch(typeof(Game.GenericAbility), "get_UsingValidEquipment")]
    internal static class GenericAbility_get_UsingValidEquipment_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(Game.GenericAbility __instance, ref bool __result)
        {
            if (!Plugin.ModEnabled.Value || !Plugin.EnableUniversalRiposte.Value || __instance == null) return;
            string name = __instance.gameObject.name;
            if (name.IndexOf("duelist", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("agility_2of3", StringComparison.OrdinalIgnoreCase) >= 0) __result = true;
        }
    }

    [HarmonyPatch(typeof(Game.StatusEffectParams), "GetExtraValue", new Type[] { typeof(Game.CharacterStats), typeof(GameObject), typeof(Game.StatusEffectLevelScalingData) })]
    internal static class StatusEffectParams_GetExtraValue_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(Game.StatusEffectParams __instance, ref float __result)
        {
            if (!Plugin.ModEnabled.Value || !Plugin.EnableMasteryRework.Value || __instance == null) return;

            if ((int)__instance.AffectsStat == 2168)
            {
                if (Mathf.Approximately(__instance.Value, 2.0f))
                {
                    if (Mathf.Approximately(__instance.ExtraValue, 3.0f)) __result = 30.0f;
                    else if (Mathf.Approximately(__instance.ExtraValue, 6.0f)) __result = 40.0f;
                    else if (Mathf.Approximately(__instance.ExtraValue, 9.0f)) __result = 50.0f;
                    else __result = 30.0f;
                }
                else if (Mathf.Approximately(__instance.Value, 3.0f))
                {
                    if (Mathf.Approximately(__instance.ExtraValue, 2.0f)) __result = 10.0f;
                    else if (Mathf.Approximately(__instance.ExtraValue, 4.0f)) __result = 20.0f;
                    else if (Mathf.Approximately(__instance.ExtraValue, 6.0f)) __result = 30.0f;
                    else __result = 10.0f;
                }
            }
        }
    }
}