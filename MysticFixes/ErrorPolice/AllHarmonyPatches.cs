using System;
using System.Collections.ObjectModel;
using EntityStates.LunarExploderMonster;
using HarmonyLib;
using HG;
using MiscFixes.Modules;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Create a new class for each group of fixes. Helps prevent errors from ruining everything
/// </summary>

namespace MiscFixes.ErrorPolice
{
    [HarmonyPatch]
    internal class FixGameplay
    {
        [HarmonyPatch(typeof(FlickerLight), nameof(FlickerLight.OnEnable))]
        [HarmonyPrefix]
        public static void Ugh(FlickerLight __instance)
        {
            if (!__instance.light)
            {
                Log.Error(__instance.name + " does not have a light! Fix this in the prefab!");
                __instance.enabled = false;
            }
        }

        /// <summary>
        /// Halcyonite Shrine is able to drain 0 gold and will softlock itself.
        /// If the scaled gold cost is less than 1, it gets truncated to 0 (mostly for modded scalings)
        /// </summary>
        [HarmonyPatch(typeof(HalcyoniteShrineInteractable), nameof(HalcyoniteShrineInteractable.Awake))]
        [HarmonyPostfix]
        public static void HalcyoniteShrineInteractable_Awake(HalcyoniteShrineInteractable __instance)
        {
            __instance.goldDrainValue = Math.Max(1, __instance.goldDrainValue);
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
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(ReadOnlyCollection<TeamComponent>), nameof(ReadOnlyCollection<>.Count)))
                ))
            {
                c.Remove();
                c.EmitDelegate(GetValidTeamCount);
            }
            else Log.PatchFail(il);
        }

        private static int GetValidTeamCount(ReadOnlyCollection<TeamComponent> teamMembers)
        {
            int count = 0;
            foreach (var t in teamMembers)
            {
                if (t?.body && t.body.master && t.body.healthComponent)
                    count++;
            }
            return count;
        }
    }

    [HarmonyPatch]
    internal class FixEventSystem
    {
        /// <summary>
        /// unity explorer can eat my whole ass
        /// </summary>
        [HarmonyPatch(typeof(SurvivorIconController), nameof(SurvivorIconController.GetLocalUser))]
        [HarmonyPrefix]
        public static bool SurvivorIconController_GetLocalUser(ref LocalUser __result)
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
        public static bool RuleChoiceController_FindNetworkUser(ref NetworkUser __result)
        {
            if (EventSystem.current is MPEventSystem)
                return true;

            __result = LocalUserManager.GetFirstLocalUser()?.currentNetworkUser;
            return false;
        }
    }
    
    [HarmonyPatch]
    internal class FixNullRefs
    {
        /// <summary>
        /// nullchecking modelLocator and subsequent transforms with ?. is bad don't do it
        /// only ok for prefabs
        /// </summary>
        [HarmonyPatch(typeof(DeathState), nameof(DeathState.FixedUpdate))]
        [HarmonyILManipulator]
        public static void DeathState_FixedUpdate(ILContext il)
        {
            if (new ILCursor(il).TryFindNext(out var c,
                    x => x.MatchCallOrCallvirt<DeathState>(nameof(DeathState.FireExplosion)),
                    x => x.MatchCallOrCallvirt<GameObject>(nameof(GameObject.SetActive))
                ))
            {
                var c0 = c[0];
                var c1 = c[1];
                c0.Index++;
                c0.MoveAfterLabels();
                c1.Index++;

                c0.Emit(OpCodes.Br, c1.MarkLabel());
                c1.Emit(OpCodes.Ldarg_0);
                c1.EmitDelegate<Action<DeathState>>((self) =>
                {
                    if (self.modelLocator && self.modelLocator.modelTransform)
                        self.modelLocator.modelTransform.gameObject.SetActive(false);
                });
            }
            else Log.PatchFail(il);
        }

        /// <summary>
        /// Filter null HurtBox from HurtBoxGroup, e.g. Golden Dieback's hanging mushrooms.
        /// </summary>
        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.Awake))]
        [HarmonyILManipulator]
        public static void CharacterModel_Awake(ILContext il)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchLdfld<HurtBoxGroup>(nameof(HurtBoxGroup.hurtBoxes))
                ))
            {
                Log.PatchFail(il);
                return;
            }

            c.EmitDelegate<Func<HurtBox[], HurtBox[]>>((hurtBoxes) =>
            {
                for (var i = hurtBoxes.Length - 1; 0 <= i; i--)
                {
                    if (!hurtBoxes[i])
                        HG.ArrayUtils.ArrayRemoveAtAndResize(ref hurtBoxes, i);
                }

                return hurtBoxes;
            });
        }

        [HarmonyPatch(typeof(PseudoCharacterMotor), nameof(PseudoCharacterMotor.velocityAuthority), MethodType.Setter)]
        [HarmonyFinalizer]
        public static Exception PseudoCharacterMotor_setVelocityAuthority(Exception __exception)
        {
            Log.Error(__exception);
            return null;
        }
    }

    [HarmonyPatch]
    internal class FixParticleScale
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
        /// Prevent an error logging when trying to detatch a child from a disabled game object.
        /// </summary>
        [HarmonyPatch(typeof(DetachParticleOnDestroyAndEndEmission), nameof(DetachParticleOnDestroyAndEndEmission.OnDisable))]
        [HarmonyILManipulator]
        public static void DetachParticleOnDestroyAndEndEmission_OnDisable(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel returnLabel = null;
            if (!c.TryGotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<DetachParticleOnDestroyAndEndEmission>(nameof(DetachParticleOnDestroyAndEndEmission.particleSystem)),
                    x => x.MatchOpImplicit(),
                    x => x.MatchBrfalse(out returnLabel)) ||
                !c.TryGotoNext(
                    MoveType.After,
                    x => x.MatchCallOrCallvirt<ParticleSystem>(nameof(ParticleSystem.Stop))
                ))
            {
                Log.PatchFail(il);
                return;
            }
            c.Emit(OpCodes.Ldarg_0);
            c.Emit<DetachParticleOnDestroyAndEndEmission>(OpCodes.Ldfld, nameof(DetachParticleOnDestroyAndEndEmission.particleSystem));
            c.Emit(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Component), nameof(Component.gameObject)));
            c.Emit(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(GameObject), nameof(GameObject.activeInHierarchy)));
            c.Emit(OpCodes.Brfalse, returnLabel);
        }
    }

    [HarmonyPatch]
    internal class FixTempOverlay
    {
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
        /// null array elements ig, nullcheck all cuz its not super commonly called
        /// RoR2.TemporaryVisualEffect.RebuildVisualComponents() (at<c0d9c70405a04cceacc72f65157d1ebd>:IL_0057)
        /// RoR2.TemporaryVisualEffect.OnDestroy() (at<c0d9c70405a04cceacc72f65157d1ebd>:IL_0000)
        /// </summary>
        //[HarmonyPatch(typeof(TemporaryVisualEffect), nameof(TemporaryVisualEffect.RebuildVisualComponents))]
        //[HarmonyILManipulator]
        public static void TemporaryVisualEffect_RebuildVisualComponents(ILContext il)
        {
            var c = new ILCursor(il);

            Instruction instr = null;
            while (c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdelemRef(),
                    x => x.MatchLdcI4(out _),
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(Behaviour), nameof(Behaviour.enabled))),
                    x => x.MatchAny(out instr)
                ))
            {
                var setEnabledLabel = c.DefineLabel();
                c.Index++;
                c.Emit(OpCodes.Dup);
                c.EmitOpImplicit();
                c.Emit(OpCodes.Brfalse_S, instr);
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Br_S, setEnabledLabel);
                c.MarkLabel(setEnabledLabel);
            }
        }
    }
}
