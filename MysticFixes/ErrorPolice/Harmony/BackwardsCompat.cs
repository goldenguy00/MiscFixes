using HarmonyLib;
using HG;
using MiscFixes.Modules;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;

namespace MiscFixes.ErrorPolice.Harmony
{
    /// <summary>
    /// For things that likely will never change or are otherwise out of place in the other class.
    /// these kinda just live here now and thats ok i guess
    /// </summary>
    [HarmonyPatch]
    public class BackwardsCompat
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
        [HarmonyPatch(typeof(TemporaryVisualEffect), nameof(TemporaryVisualEffect.RebuildVisualComponents))]
        [HarmonyILManipulator]
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
