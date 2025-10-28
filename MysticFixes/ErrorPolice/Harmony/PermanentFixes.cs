using System.Collections.ObjectModel;
using System.Linq;
using Facepunch.Steamworks;
using HarmonyLib;
using HG;
using MiscFixes.Modules;
using MonoMod.Cil;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MiscFixes.ErrorPolice.Harmony
{
    /// <summary>
    /// For things that likely will never change or are otherwise out of place in the other class.
    /// these kinda just live here now and thats ok i guess
    /// </summary>
    [HarmonyPatch]
    public class PermanentFixes
    {
        /// <summary>
        /// normalize particles once
        /// From DOTParticleFix
        /// </summary>
        [HarmonyPatch(typeof(NormalizeParticleScale), nameof(NormalizeParticleScale.OnEnable))]
        [HarmonyPrefix]
        public static bool NormalizeParticleScale_OnEnable(NormalizeParticleScale __instance) => !__instance.particleSystem;

        /// <summary>
        /// idk what it does but it works probably
        /// From DOTParticleFix
        /// </summary>
        [HarmonyPatch(typeof(BurnEffectController), nameof(BurnEffectController.AddFireParticles))]
        [HarmonyPostfix]
        public static void BurnEffectController_AddFireParticles(NormalizeParticleScale __instance, ref BurnEffectControllerHelper __result, Renderer modelRenderer)
        {
            if (__result && modelRenderer)
            {
                var scale = modelRenderer.transform.localScale.ComponentMax();
                if (scale > 1f && __result.normalizeParticleScale && __result.burnParticleSystem)
                {
                    var main = __result.burnParticleSystem.main;
                    var startSize = main.startSize;

                    startSize.constantMin /= scale;
                    startSize.constantMax /= scale;
                    main.startSize = startSize;
                }
            }
        }

        /// <summary>
        /// unity explorer can eat my whole ass
        /// </summary>
        [HarmonyPatch(typeof(SurvivorIconController), nameof(SurvivorIconController.GetLocalUser))]
        [HarmonyPrefix]
        public static bool SurvivorIconController_GetLocalUser(SurvivorIconController __instance, ref LocalUser __result)
        {
            if (EventSystem.current is MPEventSystem)
                return true;

            __result = LocalUserManager.GetFirstLocalUser();
            return false;
        }

        /// <summary>
        /// Null check EventSystem.current, occurs when exiting a lobby.
        /// </summary>
        [HarmonyPatch(typeof(RuleChoiceController), nameof(RuleChoiceController.FindNetworkUser))]
        [HarmonyPrefix]
        public static bool RuleChoiceController_FindNetworkUser(RuleChoiceController __instance, ref NetworkUser __result)
        {
            if (EventSystem.current is MPEventSystem)
                return true;

            __result = LocalUserManager.GetFirstLocalUser()?.currentNetworkUser;
            return false;
        }

        /// <summary>
        /// Checks if the component exists, which should be the case for pre-sots overlay code
        /// If true, it creates a temporary overlay instance from the component for backwards compatibility 
        /// 
        /// ArgumentNullException: Parameter name: source
        /// 
        /// UnityEngine.Material..ctor(UnityEngine.Material source) (at:IL_0008)
        /// RoR2.TemporaryOverlayInstance.SetupMaterial() (at:IL_000D)
        /// RoR2.TemporaryOverlayInstance.AddToCharacterModel(RoR2.CharacterModel characterModel) (at:IL_0000)
        /// RoR2.TemporaryOverlay.AddToCharacerModel(RoR2.CharacterModel characterModel) (at:IL_0006)
        /// 
        /// UnityEngine.Material..ctor(UnityEngine.Material source) (at:IL_0008)
        /// RoR2.TemporaryOverlayInstance.SetupMaterial() (at:IL_000D)
        /// RoR2.TemporaryOverlayInstance.Start() (at:IL_0009)
        /// RoR2.TemporaryOverlay.Start() (at:IL_0006)
        /// </summary>
        [HarmonyPatch(typeof(TemporaryOverlayInstance), nameof(TemporaryOverlayInstance.SetupMaterial))]
        [HarmonyPrefix]
        public static void TemporaryOverlayInstance_SetupMaterial(TemporaryOverlayInstance __instance)
        {
            if (!__instance.originalMaterial && __instance.componentReference && __instance.ValidateOverlay())
                __instance.componentReference.CopyDataFromPrefabToInstance();
        }

        /// <summary>
        /// Halcyonite Shrine is able to drain 0 gold and will softlock itself.
        /// If the scaled gold cost is less than 1, it gets truncated to 0 (mostly for modded scalings)
        /// </summary>
        [HarmonyPatch(typeof(HalcyoniteShrineInteractable), nameof(HalcyoniteShrineInteractable.Awake))]
        [HarmonyPostfix]
        public static void HalcyoniteShrineInteractable_Awake(HalcyoniteShrineInteractable __instance)
        {
            __instance.goldDrainValue = System.Math.Max(1, __instance.goldDrainValue);
        }

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
        //[HarmonyPatch(typeof(MPEventSystem), nameof(MPEventSystem.Update))]
        //[HarmonyILManipulator]
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
                c.EmitDelegate(GetValidTeamCount);
            }
            else Log.PatchFail(il);
        }

        private static int GetValidTeamCount(ReadOnlyCollection<TeamComponent> teamMembers) => teamMembers.Count(t => t?.body && t.body.master && t.body.healthComponent);
    }
}
