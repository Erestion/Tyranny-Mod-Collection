using BepInEx;
using BepInEx.Logging;
using Game;
using HarmonyLib;
using UnityEngine;

namespace GlobalSpeedRework
{
    [BepInPlugin("tyranny.mod.globalspeedrework", "Global Speed Rework", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            Harmony.CreateAndPatchAll(typeof(Patch_GlobalSpeed), null);
            Log.LogInfo("Loaded: Global Speed Rework with Debug Diagnostics");
        }
    }

    public static class Patch_GlobalSpeed
    {
        [HarmonyPatch(typeof(AttackBase), "CalculateAttackSpeed")]
        [HarmonyPostfix]
        public static void CalculateAttackSpeed_Postfix(AttackBase __instance, ref float __result)
        {
            if (__instance == null) return;

            GameObject ownerObj = __instance.Owner;
            if (ownerObj == null) return;

            CharacterStats ownerStats = ownerObj.GetComponent<CharacterStats>();
            if (ownerStats == null) return;

            Game.GenericAbility ab = __instance.AbilityOrigin as Game.GenericAbility;
            if (ab == null && __instance.TriggeringAbility != null)
            {
                ab = __instance.TriggeringAbility as Game.GenericAbility;
            }

            float speedBonus = 0f;

            if (ab != null && (ab.IsCraftedSpell || ab is Song || ab.GetComponent<Aria>() != null))
            {
                CharacterStats.SkillType magicSkill = GetAbilitySkill(ab);
                int magicSkillVal = (magicSkill != CharacterStats.SkillType.Undefined) ? ownerStats.CalculateSkill(magicSkill) : 0;
                int loreSkillVal = ownerStats.CalculateSkill(CharacterStats.SkillType.Lore);

                int effectiveSkill = Mathf.Max(magicSkillVal, Mathf.RoundToInt(loreSkillVal * 0.5f));
                speedBonus = (effectiveSkill / 10f) * 0.025f;
            }
            else
            {
                CharacterStats.SkillType attackSkill = GetEffectiveSkillForAttack(__instance, ownerStats);
                if (attackSkill != CharacterStats.SkillType.Undefined)
                {
                    int skillVal = ownerStats.CalculateSkill(attackSkill);
                    speedBonus = (skillVal / 10f) * 0.025f;
                }
            }

            float quicknessBonus = Mathf.Max(0, ownerStats.Quickness - 10) * 0.01f;
            float totalMultiplier = 1f + speedBonus + quicknessBonus;

            totalMultiplier = Mathf.Clamp(totalMultiplier, 1f, 2.2f);
            __result *= totalMultiplier;
        }

        [HarmonyPatch(typeof(AttackBase), "CalculateRecoveryTime")]
        [HarmonyPostfix]
        public static void CalculateRecoveryTime_Postfix(AttackBase __instance, CharacterStats stats, ref float __result)
        {
            if (__instance == null || stats == null) return;

            float minCooldown = StatsData.Instance != null ? StatsData.Instance.GlobalMinimumPersonalCooldown : 0.1f;
            if (__result < minCooldown)
            {
                __result = minCooldown;
            }
        }

        private static CharacterStats.SkillType GetAbilitySkill(Game.GenericAbility ab)
        {
            if (ab == null) return CharacterStats.SkillType.Undefined;

            try
            {
                if (ab.IsCraftedSpell && ab.SpellConfig != null)
                {
                    return ab.SpellConfig.Skill;
                }

                System.Reflection.FieldInfo skillField = ab.GetType().GetField("Skill", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                                       ?? ab.GetType().GetField("m_skill", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (skillField != null)
                {
                    object val = skillField.GetValue(ab);
                    if (val is CharacterStats.SkillType)
                    {
                        return (CharacterStats.SkillType)val;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[GetAbilitySkill] Failed to resolve skill for ability '{ab.name}': {ex}");
            }

            return CharacterStats.SkillType.Undefined;
        }

        private static CharacterStats.SkillType GetEffectiveSkillForAttack(AttackBase attack, CharacterStats ownerStats)
        {
            if (attack == null || ownerStats == null) return CharacterStats.SkillType.Undefined;

            Equipment equipment = ownerStats.GetComponent<Equipment>();
            if (equipment == null || equipment.CurrentItems == null) return CharacterStats.SkillType.Undefined;

            if (attack is AttackMelee && (attack as AttackMelee).Unarmed)
            {
                return CharacterStats.SkillType.Unarmed;
            }

            Weapon weapon = null;
            if (equipment.PrimaryAttack == attack)
            {
                weapon = equipment.CurrentItems.PrimaryWeapon as Weapon;
            }
            else if (equipment.SecondaryAttack == attack)
            {
                weapon = equipment.CurrentItems.SecondaryWeapon as Weapon;
            }
            else if (attack.IsWeaponAttack)
            {
                weapon = equipment.CurrentItems.PrimaryWeapon as Weapon;
            }

            if (weapon == null) return CharacterStats.SkillType.Undefined;

            CharacterStats.SkillType weaponSkill = CharacterStats.SkillType.Undefined;

            try
            {
                System.Reflection.MethodInfo getSkillMethod = weapon.GetType().GetMethod("GetSkillType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (getSkillMethod != null)
                {
                    weaponSkill = (CharacterStats.SkillType)getSkillMethod.Invoke(weapon, null);
                }
                else
                {
                    System.Reflection.FieldInfo wSkillField = weapon.GetType().GetField("WeaponSkill", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (wSkillField != null)
                    {
                        weaponSkill = (CharacterStats.SkillType)wSkillField.GetValue(weapon);
                    }
                    else
                    {
                        Plugin.Log.LogWarning($"[GetEffectiveSkillForAttack] Could not resolve weapon skill field/method on item '{weapon.name}'.");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[GetEffectiveSkillForAttack] Reflection error on weapon '{weapon.name}': {ex}");
            }

            if (weapon.weaponType == Weapon.WeaponType.Staff && attack.AssociatedWeapon != null)
            {
                try
                {
                    CharacterStats.SkillType elementalSkill = CharacterStats.SkillType.Undefined;
                    System.Reflection.MethodInfo assocGetSkill = attack.AssociatedWeapon.GetType().GetMethod("GetSkillType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (assocGetSkill != null)
                    {
                        elementalSkill = (CharacterStats.SkillType)assocGetSkill.Invoke(attack.AssociatedWeapon, null);
                    }

                    if (elementalSkill != CharacterStats.SkillType.Staff && elementalSkill != CharacterStats.SkillType.Undefined)
                    {
                        int elementalVal = ownerStats.CalculateSkill(elementalSkill);
                        int stavesVal = ownerStats.CalculateSkill(CharacterStats.SkillType.Staff);
                        return elementalVal > stavesVal ? elementalSkill : CharacterStats.SkillType.Staff;
                    }
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.LogError($"[GetEffectiveSkillForAttack] Staff elemental check failed: {ex}");
                }
            }

            return weaponSkill;
        }
    }
}