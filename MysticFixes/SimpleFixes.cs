using System.Collections.ObjectModel;
using Facepunch.Steamworks;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.UI;
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
        /// <summary>
        /// Sometimes prevents loading, so suppress exceptions
        /// </summary>
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
            else Log.PatchFail(il);
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
            else Log.PatchFail(il);
        }

        /// <summary>
        /// ConVars are not registered as lower case but when submitting them from the console they are converted, leading to a match fail.
        /// </summary>
        [HarmonyPatch(typeof(Console), nameof(Console.RegisterConVarInternal))]
        [HarmonyILManipulator]
        public static void FixConVarCaseSensitive(ILContext il)
        {
            var c = new ILCursor(il);
            // Technically we aren't checking if ToLowerInvariant or ToLower(CultureInfo.InvariantCulture)
            // is called after these instructions just like Console.Awake does for ConCommands, mostly
            // because it's a hassle to check for the existence of either, but an extra call wouldn't hurt.
            if (!c.TryGotoNext(
                MoveType.After,
                x => x.MatchLdarg(1),
                x => x.MatchLdfld<RoR2.ConVar.BaseConVar>(nameof(RoR2.ConVar.BaseConVar.name))))
            {
                Log.PatchFail(il);
                return;
            }
            c.Emit<string>(OpCodes.Callvirt, nameof(string.ToLowerInvariant));
        }
    }
}
