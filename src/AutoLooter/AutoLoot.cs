using BepInEx;
using BepInEx.Logging;
using Game;
using HarmonyLib;
using SDK;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AutoLootBepInEx
{
    [BepInPlugin("org.bepinex.plugin.autoloot", "Auto Loot BepInEx", "1.1.1")]
    public class AutoLootPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static float Radius = 10f;
        internal static bool CollectOnManualOpen = false;

        internal static float nextScan;
        internal static Game.Container scanning;
        internal static readonly HashSet<Game.Container> cleaned = new HashSet<Game.Container>();
        internal static readonly HashSet<AutoLootContainer> pendingPickups = new HashSet<AutoLootContainer>();

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo("AutoLoot BepInEx initializing...");

            LoadSettingsSimple();


            Harmony.CreateAndPatchAll(System.Reflection.Assembly.GetExecutingAssembly(), null);

            Log.LogInfo("AutoLoot patches applied successfully via Harmony attributes!");
        }

        private void LoadSettingsSimple()
        {
            try
            {
                string path = Path.Combine(Path.Combine(Paths.PluginPath, "AutoLoot"), "Settings.xml");
                if (!File.Exists(path))
                {
                    path = Path.Combine(Paths.PluginPath, "Settings.xml");
                }

                if (File.Exists(path))
                {
                    string content = File.ReadAllText(path);
                    int rStart = content.IndexOf("<Radius>");
                    int rEnd = content.IndexOf("</Radius>");
                    if (rStart != -1 && rEnd != -1)
                    {
                        if (float.TryParse(content.Substring(rStart + 8, rEnd - (rStart + 8)).Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float r))
                        {
                            Radius = r;
                        }
                    }
                    int mStart = content.IndexOf("<CollectOnManualOpen>");
                    int mEnd = content.IndexOf("</CollectOnManualOpen>");
                    if (mStart != -1 && mEnd != -1)
                    {
                        string val = content.Substring(mStart + 21, mEnd - (mStart + 21)).Trim().ToLower();
                        CollectOnManualOpen = (val == "true" || val == "1" || val == "yes");
                    }
                    Log.LogInfo($"Settings loaded: Radius = {Radius}, CollectOnManualOpen = {CollectOnManualOpen}");
                }
            }
            catch (Exception ex)
            {
                Log.LogError("Failed to load Settings.xml: " + ex.Message);
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < nextScan || Game.GameState.InCombat || Game.GameState.IsLoading || Game.GameResources.PreLoadingGame || Game.Cutscene.CutsceneActive)
            {
                return;
            }
            if (UILootManager.Instance && UILootManager.Instance.IsVisible && !CollectOnManualOpen)
            {
                return;
            }

            nextScan = Time.unscaledTime + 0.3f;
            Game.Player s_playerCharacter = Game.GameState.s_playerCharacter;
            Game.PlayerInventory playerInventory = s_playerCharacter ? s_playerCharacter.GetComponent<Game.PlayerInventory>() : null;
            if (!playerInventory)
            {
                return;
            }

            float num = Radius * Radius;
            cleaned.RemoveWhere((Game.Container c) => !c);
            Game.Container[] array = UnityEngine.Object.FindObjectsOfType<Game.Container>();

            foreach (var container in array)
            {
                if (!container || !container.isActiveAndEnabled) continue;

                float sqrDist = (container.transform.position - s_playerCharacter.transform.position).sqrMagnitude;
                if (sqrDist > num) continue;

                DeadBody deadBody = container.GetComponent<DeadBody>();
                if (deadBody)
                {
                    if (container.HasInteracted && container.IsEmpty && cleaned.Add(container))
                    {
                        try { container.CloseInventoryCB(); } catch { }
                    }
                    else if (!container.IsEmpty && Accessible(container))
                    {
                        try
                        {
                            if (LootEligibility.HasCollectibleItems(container, playerInventory))
                            {
                                scanning = container;
                                container.Open(s_playerCharacter.gameObject, (OCL.OCLOptions)4);
                                if (container.IsEmpty) container.CloseInventoryCB();
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.LogError(ex.ToString());
                        }
                        finally
                        {
                            scanning = null;
                        }
                    }
                    continue;
                }

                if (!Accessible(container)) continue;

                try
                {
                    if (!LootEligibility.HasCollectibleItems(container, playerInventory)) continue;

                    scanning = container;
                    if (container.Open(s_playerCharacter.gameObject, (OCL.OCLOptions)4) && container.IsEmpty)
                    {
                        container.CloseInventoryCB();
                    }
                }
                catch (Exception ex2)
                {
                    Log.LogError(ex2.ToString());
                }
                finally
                {
                    scanning = null;
                }

                if (Game.GameState.InCombat || (UILootManager.Instance && UILootManager.Instance.IsVisible && !CollectOnManualOpen))
                {
                    break;
                }
            }
        }

        internal static bool Accessible(OCL target)
        {
            if (!target || !target.isActiveAndEnabled || !target.IsUsable)
            {
                return false;
            }
            Game.Trap component = target.GetComponent<Game.Trap>();
            return !component || component.Disarmed;
        }
    }


    [HarmonyPatch(typeof(Game.Container))]
    public class ContainerPatches
    {
        [HarmonyPatch("ShowSingleContainerLootUI")]
        [HarmonyPrefix]
        public static bool BeforeLootUI(Game.Container __instance)
        {
            return ProcessAutoLoot(__instance);
        }

        [HarmonyPatch("LootAllContainersInArea")]
        [HarmonyPrefix]
        public static bool BeforeLootArea(Game.Container __instance)
        {
            return ProcessAutoLoot(__instance);
        }

        [HarmonyPatch("SetHighlightTarget")]
        [HarmonyPrefix]
        public static void BeforeHighlight(Game.Container __instance, ref bool enabled)
        {
            if (__instance.IsEmptyDeadBody())
            {
                enabled = false;
            }
        }

        private static bool ProcessAutoLoot(Game.Container __instance)
        {
            bool flag = AutoLootPlugin.scanning == __instance;
            if ((!flag && !AutoLootPlugin.CollectOnManualOpen) || Game.GameState.InCombat)
            {
                return true;
            }
            Game.Trap component = __instance.GetComponent<Game.Trap>();
            if (component && !component.Disarmed)
            {
                return true;
            }
            try
            {
                Collect(__instance);
                return !flag && !__instance.IsEmpty;
            }
            catch (Exception ex)
            {
                AutoLootPlugin.Log.LogError(ex.ToString());
                return !flag;
            }
        }

        private static void Collect(Game.Container container)
        {
            Game.Player s_playerCharacter = Game.GameState.s_playerCharacter;
            Game.PlayerInventory playerInventory = s_playerCharacter ? s_playerCharacter.GetComponent<Game.PlayerInventory>() : null;


            StashInventory targetStash = playerInventory ? playerInventory.StashInventory : null;

            Inventory component = container.GetComponent<Inventory>();
            if (!targetStash || !component)
            {
                return;
            }

            foreach (InventoryItem inventoryItem in new List<InventoryItem>(component.ItemList))
            {
                if (inventoryItem != null && inventoryItem.BaseItem && targetStash.CanPutItem(inventoryItem, false))
                {
                    InventoryItem inventoryItem2 = component.TakeItem(inventoryItem);
                    if (inventoryItem2 != null && !targetStash.PutItem(inventoryItem2, -1))
                    {
                        component.PutItem(inventoryItem2);
                    }
                }
            }
            container.NotifyMouseOver(false);
        }
    }
}