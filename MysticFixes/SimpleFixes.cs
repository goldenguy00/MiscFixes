using System.Collections.ObjectModel;
using Facepunch.Steamworks;
using HarmonyLib;
using HG;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.SurvivorMannequins;
using RoR2.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
        [HarmonyPatch(typeof(NormalizeParticleScale), nameof(NormalizeParticleScale.OnEnable))]
        [HarmonyPrefix]
        public static bool NormalizeParticleScale_OnEnable(NormalizeParticleScale __instance)
        {
            return !__instance.particleSystem;
        }

        [HarmonyPatch(typeof(BurnEffectController), nameof(BurnEffectController.AddFireParticles))]
        [HarmonyPostfix]
        public static void BurnEffectController_AddFireParticles(NormalizeParticleScale __instance, ref BurnEffectControllerHelper __result, Renderer modelRenderer, Transform targetParentTransform)
        {
            if (__result && modelRenderer)
            {
                var scale = modelRenderer.transform.localScale.ComponentMax();
                if (scale > 1f && __result.normalizeParticleScale && __result.burnParticleSystem)
                {
                    ParticleSystem.MainModule main = __result.burnParticleSystem.main;
                    ParticleSystem.MinMaxCurve startSize = main.startSize;

                    startSize.constantMin /= scale;
                    startSize.constantMax /= scale;
                    main.startSize = startSize;
                }
            }
        }
        ///RoR2.SurvivorMannequins.SurvivorMannequinSlotController.ApplyLoadoutToMannequinInstance() (at<c0d9c70405a04cceacc72f65157d1ebd>:IL_003C)
        ///RoR2.SurvivorMannequins.SurvivorMannequinSlotController.RebuildMannequinInstance() (at<c0d9c70405a04cceacc72f65157d1ebd>:IL_007C)
        ///RoR2.SurvivorMannequins.SurvivorMannequinSlotController.Update() (at<c0d9c70405a04cceacc72f65157d1ebd>:IL_0031)
        [HarmonyPatch(typeof(SurvivorMannequinSlotController), nameof(SurvivorMannequinSlotController.ApplyLoadoutToMannequinInstance))]
        [HarmonyPrefix]
        public static bool SurvivorMannequinSlotController_ApplyLoadoutToMannequinInstance(SurvivorMannequinSlotController __instance)
        {
            //dont care
            return __instance.mannequinInstanceTransform && __instance.mannequinInstanceTransform.GetComponentInChildren<ModelSkinController>();
        }

        /// <summary>
        /// RoR2.CharacterModel.InstantiateDisplayRuleGroup(RoR2.DisplayRuleGroup displayRuleGroup, RoR2.ItemIndex itemIndex, RoR2.EquipmentIndex equipmentIndex) (at<c0d9c70405a04cceacc72f65157d1ebd>:IL_008B)
        /// 
        /// IL_008b: ldloc.1
        /// IL_008c: ldfld class [Unity.Addressables] UnityEngine.AddressableAssets.AssetReferenceGameObject RoR2.ItemDisplayRule::followerPrefabAddress
        /// IL_0091: callvirt instance bool[Unity.Addressables] UnityEngine.AddressableAssets.AssetReference::RuntimeKeyIsValid()
        /// IL_0096: brfalse.s IL_00b7
        /// </summary>
        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.InstantiateDisplayRuleGroup))]
        [HarmonyILManipulator]
        public static void CharacterModel_InstantiateDisplayRuleGroup(ILContext il)
        {
            var c = new ILCursor(il);

            ILLabel label = null;
            if (!c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdfld<ItemDisplayRule>(nameof(ItemDisplayRule.followerPrefabAddress)),
                    x => x.MatchCallOrCallvirt<AssetReference>(nameof(AssetReference.RuntimeKeyIsValid)),
                    x => x.MatchBrfalse(out label)
                ))
            {
                Log.PatchFail(il);
                return;
            }

            c.Index++;
            var runtimeKeyValidCall = c.Next;
            c.Emit(OpCodes.Dup);
            c.Emit(OpCodes.Brtrue_S, runtimeKeyValidCall);
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Br, label);
        }
        /// <summary>
        /// 
        /*
	// if (!this.<keyAssetRuleGroup>5__3.keyAssetAddress.RuntimeKeyIsValid())
	IL_008f: ldarg.0
	IL_0090: ldflda valuetype RoR2.ItemDisplayRuleSet/KeyAssetRuleGroup RoR2.ItemDisplayRuleSet/'<GenerateRuntimeValuesAsync>d__16'::'<keyAssetRuleGroup>5__3'
	IL_0095: ldfld class RoR2.AddressableAssets.IDRSKeyAssetReference RoR2.ItemDisplayRuleSet/KeyAssetRuleGroup::keyAssetAddress
	IL_009a: callvirt instance bool [Unity.Addressables]UnityEngine.AddressableAssets.AssetReference::RuntimeKeyIsValid()
	IL_009f: brtrue.s IL_00c1*/
        /// </summary>
        [HarmonyPatch(typeof(ItemDisplayRuleSet), nameof(ItemDisplayRuleSet.GenerateRuntimeValuesAsync), MethodType.Enumerator)]
        [HarmonyILManipulator]
        public static void ItemDisplayRuleSet_GenerateRuntimeValuesAsync(ILContext il)
        {
            var c = new ILCursor(il);

            Instruction instr = null;
            if (!c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdflda(out _),
                    x => x.MatchLdfld<ItemDisplayRuleSet.KeyAssetRuleGroup>(nameof(ItemDisplayRuleSet.KeyAssetRuleGroup.keyAssetAddress)),
                    x => x.MatchCallOrCallvirt<AssetReference>(nameof(AssetReference.RuntimeKeyIsValid)),
                    x => x.MatchBrtrue(out _),
                    x => x.MatchAny(out instr)
                ))
            {
                Log.PatchFail(il);
                return;
            }

            c.Index++;
            c.Index++;
            var runtimeKeyValidCall = c.Next;
            c.Emit(OpCodes.Dup);
            c.Emit(OpCodes.Brtrue_S, runtimeKeyValidCall);
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Br, instr);
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
        /// ConVars are not registered as lower case but when submitting them from the console they are converted, leading to a match fail.
        /// </summary>
        //[HarmonyPatch(typeof(Console), nameof(Console.RegisterConVarInternal))]
        //[HarmonyILManipulator]
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
            {
                __instance.componentReference.CopyDataFromPrefabToInstance();
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
    }
}
