using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Game;
using HarmonyLib;
using SDK;
using UnityEngine;

namespace SmallPartyXp
{
    [BepInPlugin("tyranny.mod.smallpartyxp", "Small Party XP Bonus", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;
        public static ConfigEntry<int> PercentPerMissingMember;
        public static ConfigEntry<bool> ModEnabled;

        private void Awake()
        {
            Log = Logger;

            ModEnabled = Config.Bind("General", "Enabled", true, "Enable or disable Small Party XP bonus.");
            PercentPerMissingMember = Config.Bind("General", "PercentPerMissingMember", 10, "XP bonus percentage per missing party member (0 - 50%).");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Log.LogInfo("Loaded: Small Party XP Bonus (Clean BepInEx Build)");
        }
    }

    internal static class SmallPartyXpLogic
    {
        public const int MaxPartyMembers = 4;
        public static int s_questDepth = 0;
        public static int s_bestiaryDepth = 0;

        public static int MissingMembers => Mathf.Max(0, MaxPartyMembers - PartyHelper.NumPartyMembers);

        public static float Multiplier
        {
            get
            {
                int percent = Mathf.Clamp(Plugin.PercentPerMissingMember.Value, 0, 50);
                return 1f + (float)(MissingMembers * percent) / 100f;
            }
        }

        public static void Boost(ref int xp, string source)
        {
            if (!Plugin.ModEnabled.Value || xp <= 0) return;

            int missing = MissingMembers;
            if (missing == 0) return;

            int newXp = Mathf.RoundToInt((float)xp * Multiplier);
            int bonus = newXp - xp;

            if (bonus <= 0) return;

            xp = newXp;

            // Direct English output to in-game console & BepInEx log
            string msg = $"Small party bonus ({source}, -{missing} members): +{bonus} XP";
            global::Console.AddMessage("[" + NGUITools.EncodeColor(Color.green) + "]" + msg, global::Console.ConsoleState.BOTH);
            Plugin.Log.LogInfo($"[SmallPartyXP] {source}: +{bonus} XP (Total: {xp})");
        }
    }

    // ==========================================
    // Quest XP Patches
    // ==========================================

    [HarmonyPatch]
    internal static class QuestScope_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.DeclaredMethod(typeof(Game.ExperienceTable), "CompleteQuestObjective", null, null);
            yield return AccessTools.DeclaredMethod(typeof(Game.ExperienceTable), "CompleteQuest", null, null);
        }

        private static void Prefix() => SmallPartyXpLogic.s_questDepth++;

        private static Exception Finalizer(Exception __exception)
        {
            SmallPartyXpLogic.s_questDepth--;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(PartyHelper), "AssignProportionalSkillXPToParty")]
    internal static class Quests_Patch
    {
        private static void Prefix(ref int xp)
        {
            if (SmallPartyXpLogic.s_questDepth > 0)
            {
                SmallPartyXpLogic.Boost(ref xp, "quest");
            }
        }
    }

    // ==========================================
    // Bestiary XP Patches
    // ==========================================

    [HarmonyPatch(typeof(BestiaryManager), "RecordKill")]
    internal static class BestiaryScope_Patch
    {
        private static void Prefix() => SmallPartyXpLogic.s_bestiaryDepth++;

        private static Exception Finalizer(Exception __exception)
        {
            SmallPartyXpLogic.s_bestiaryDepth--;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(PartyHelper), "AssignXPToParty")]
    internal static class Bestiary_Patch
    {
        private static void Prefix(ref int xp)
        {
            if (SmallPartyXpLogic.s_bestiaryDepth > 0)
            {
                SmallPartyXpLogic.Boost(ref xp, "bestiary");
            }
        }
    }
}