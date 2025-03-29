using System.Collections.ObjectModel;
using Facepunch.Steamworks;
using HarmonyLib;
using MonoMod.Cil;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MiscFixes
{
    /// <summary>
    /// For things that likely will never change or are otherwise out of place in the other class.
    /// these kinda just live here now and thats ok i guess
    /// </summary>
    [HarmonyPatch]
    public class SimpleFixes
    {
        [HarmonyPatch(typeof(BaseSteamworks), nameof(BaseSteamworks.RunUpdateCallbacks))]
        [HarmonyFinalizer]
        public static System.Exception FixFacepunch() => null;

        /// <summary>
        /// blame ss2
        /// </summary>
        [HarmonyPatch(typeof(FlickerLight), nameof(FlickerLight.OnEnable))]
        [HarmonyPrefix]
        public static void Ugh(FlickerLight __instance)
        {
            if (!__instance.light)
                __instance.enabled = false;
        }
        /// <summary>
        /// thanks to bubbet for the wonderful code
        /// 
        /// lobbby dies when the event system isn't the one that it's expecting.
        /// forces game restart to recover.
        /// </summary>
        /// <param name="il"></param>
        [HarmonyPatch(typeof(MPEventSystem), nameof(MPEventSystem.Update))]
        [HarmonyILManipulator]
        public static void FixThisFuckingBullshitGearbox(ILContext il)
        {
            ILCursor[] c = null;
            if (new ILCursor(il).TryFindNext(out c,
                    x => x.MatchCall(AccessTools.PropertyGetter(typeof(EventSystem), nameof(EventSystem.current))),
                    x => x.MatchCall(AccessTools.PropertySetter(typeof(EventSystem), nameof(EventSystem.current)))
                ))
            {
                c[0].Remove();
                c[1].Remove();
            }
            else MiscFixesPlugin.Logger.LogError($"IL hook failed for MPEventSystem.Update");
        }

        /// <summary>
        /// Sometimes an enemy will be dead but the call to destroy it never goes through, even when no errors have occurred.
        /// vanilla only checks if they exist, which is true until the scene changes
        /// </summary>
        [HarmonyPatch(typeof(EntityStates.VoidCamp.Idle), nameof(EntityStates.VoidCamp.Idle.FixedUpdate))]
        [HarmonyPatch(typeof(EntityStates.VoidCamp.Idle.VoidCampObjectiveTracker), nameof(EntityStates.VoidCamp.Idle.VoidCampObjectiveTracker.GenerateString))]
        [HarmonyILManipulator]
        public static void FixVoidSeed(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(ReadOnlyCollection<TeamComponent>), nameof(ReadOnlyCollection<TeamComponent>.Count)))
                ))
            {
                c.Remove();
                c.EmitDelegate(delegate (ReadOnlyCollection<TeamComponent> teamMembers)
                {
                    int count = 0;
                    foreach (var member in teamMembers)
                    {
                        var body = member ? member.body : null;
                        if (body && body.master && body.healthComponent && body.healthComponent.alive)
                            count++;
                    }
                    return count;
                });
            }
            else MiscFixesPlugin.Logger.LogError($"IL hook failed for EntityStates.VoidCamp.Idle");
        }
    }
}
