using System;
using System.Collections.ObjectModel;
using EntityStates.LunarExploderMonster;
using HarmonyLib;
using HG;
using MiscFixes.Modules;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Items;
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
        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.HighlightEquipentDisplay))]
        [HarmonyILManipulator]
        public static void CharacterModel_HighlightEquipentDisplay(ILContext il)
        {
            var c = new ILCursor(il);

            /*
	// EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
	IL_000a: ldarg.1
	IL_000b: call class RoR2.EquipmentDef RoR2.EquipmentCatalog::GetEquipmentDef(valuetype RoR2.EquipmentIndex)
	IL_0010: stloc.0*/

            int defLoc = 0;
            Instruction retInstr = null;
            if (!c.TryGotoNext(
                    x => x.MatchBeq(out _),
                    x => x.MatchAny(out retInstr)
                ))
            {
                Log.PatchFail(il);
                return;
            }
            
            if (!c.TryGotoNext(
                    x => x.MatchLdarg(out _),
                    x => x.MatchCallOrCallvirt(typeof(EquipmentCatalog), nameof(EquipmentCatalog.GetEquipmentDef)),
                    x => x.MatchStloc(out defLoc)
                ))
            {
                Log.PatchFail(il);
                return;
            }

            c.Emit(OpCodes.Ldloc, defLoc);
            c.EmitOpImplicit();
            c.Emit(OpCodes.Brfalse, retInstr);
        }

        [HarmonyPatch(typeof(AttackSpeedPerNearbyCollider), nameof(AttackSpeedPerNearbyCollider.ReconcileBuffCount))]
        [HarmonyILManipulator]
        public static void AttackSpeedPerNearbyCollider_ReconcileBuffCount(ILContext il)
        {
            var c = new ILCursor(il);

            /*// int num = this.body.GetBuffCount(buffIndex);
	IL_0013: ldarg.0
	IL_0014: ldfld class RoR2.CharacterBody RoR2.AttackSpeedPerNearbyCollider::body
	IL_0019: ldloc.0
	IL_001a: callvirt instance int32 RoR2.CharacterBody::GetBuffCount(valuetype RoR2.BuffIndex)
	IL_001f: stloc.1*/
            Instruction retInstr = null;
            if (!c.TryGotoNext(
                    x => x.MatchCallOrCallvirt(out _),
                    x => x.MatchBrtrue(out _),
                    x => x.MatchAny(out retInstr)
                ))
            {
                Log.PatchFail(il);
                return;
            }

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, AccessTools.Field(typeof(AttackSpeedPerNearbyCollider), nameof(AttackSpeedPerNearbyCollider.body)));
            c.EmitOpImplicit();
            c.Emit(OpCodes.Brfalse, retInstr);
        }

        [HarmonyPatch(typeof(BuffWard), nameof(BuffWard.BuffTeam))]
        [HarmonyILManipulator]
        public static void BuffWard_BuffTeam(ILContext il)
        {
            var c = new ILCursor(il);

            /*		// foreach (TeamComponent recipient in recipients)
		    IL_001d: br IL_01ba
		    // loop start (head: IL_01ba)
			IL_0022: ldloc.0
			IL_0023: callvirt instance !0 class [netstandard]System.Collections.Generic.IEnumerator`1<class RoR2.TeamComponent>::get_Current()
			IL_0028: stloc.1*/

            ILLabel continueLabel = null;
            int targetLoc = 0;
            if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchBr(out continueLabel),
                    x => x.MatchLdloc(out _),
                    x => x.MatchCallvirt(out _),
                    x => x.MatchStloc(out targetLoc)
                ))
            {
                Log.PatchFail(il);
                return;
            }

            c.Emit(OpCodes.Ldloc, targetLoc);
            c.EmitOpImplicit();
            c.Emit(OpCodes.Brfalse, continueLabel);
        }

        [HarmonyPatch(typeof(PseudoCharacterMotor), nameof(PseudoCharacterMotor.velocityAuthority), MethodType.Setter)]
        [HarmonyFinalizer]
        public static Exception PseudoCharacterMotor_setVelocityAuthority(Exception __exception)
        {
            if (__exception is NotImplementedException nie)
            {
                Log.Error(nie);
                return null;
            }
            return __exception;
        }

        [HarmonyPatch(typeof(FlickerLight), nameof(FlickerLight.OnEnable))]
        [HarmonyPrefix]
        public static void Ugh(FlickerLight __instance)
        {
            if (!__instance.light)
            {
                Log.Error(Util.BuildPrefabTransformPath(__instance.transform.root, __instance.transform, false, true) + " does not have a light! Fix this in the prefab!");
                __instance.enabled = false;
            }
        }

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


        [HarmonyPatch(typeof(CharacterDeathBehavior), nameof(CharacterDeathBehavior.OnDeath))]
        [HarmonyILManipulator]
        public static void CharacterDeathBehavior_OnDeath(ILContext il)
        {
            /*  IL_003d: ldelem.ref
		        IL_003e: newobj instance void EntityStates.Idle::.ctor()
		        IL_0043: callvirt instance void RoR2.EntityStateMachine::SetNextState(class EntityStates.EntityState)*/

            var c = new ILCursor(il);
            Instruction nullBranch = null;

            if (!c.TryGotoNext(MoveType.Before,
                    x => x.MatchNewobj(out _),
                    x => x.MatchCallOrCallvirt<EntityStateMachine>(nameof(EntityStateMachine.SetNextState)),
                    x => x.MatchAny(out nullBranch)
                ))
            {
                Log.PatchFail(il);
                return;
            }

            c.EmitNullConditional(c.Next, nullBranch);
        }

        /// <summary>
        /// Skip null display instances
        /// </summary>
        [HarmonyPatch(typeof(ModelSkinController), nameof(ModelSkinController.ApplySkinAsync), MethodType.Enumerator)]
        [HarmonyILManipulator]
        public static void ModelSkinController_ApplySkinAsync(ILContext il)
        {
            /*
	IL_0052: ldloc.1
	IL_0053: call instance void RoR2.ModelSkinController::UnloadCurrentlyLoadedSkinAssets()*/

            var c = new ILCursor(il);

            int loc = 0;
            if (!c.TryGotoNext(MoveType.AfterLabel,
                    x => x.MatchLdloc(out loc),
                    x => x.MatchCallOrCallvirt<ModelSkinController>(nameof(ModelSkinController.UnloadCurrentlyLoadedSkinAssets))
                ))
            {
                Log.PatchFail(il);
                return;
            }

            var hasSkins = il.DefineLabel(c.Next);

            c.Emit(OpCodes.Ldloc, loc);
            c.EmitDelegate<Func<ModelSkinController, bool>>((mdlSkins) => mdlSkins.skins.Length > 0);
            c.Emit(OpCodes.Brtrue, hasSkins);
            c.Emit(OpCodes.Ldc_I4_0);
            c.Emit(OpCodes.Ret);

        }

        /// <summary>
        /// Skip null display instances
        /// </summary>
        [HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.OnSkillActivated))]
        [HarmonyILManipulator]
        public static void CharacterBody_OnSkillActivated(ILContext il)
        {
            /*
	IL_005b: ldarg.0
	IL_005c: call instance class RoR2.Inventory RoR2.CharacterBody::get_inventory()
	IL_0061: ldsfld class RoR2.ItemDef RoR2.DLC2Content/Items::IncreasePrimaryDamage
	IL_0066: callvirt instance int32 RoR2.Inventory::GetItemCountEffective(class RoR2.ItemDef)
	IL_006b: ldc.i4.0
	IL_006c: ble.s IL_00c1*/

            var c = new ILCursor(il);

            ILLabel nullBranch = null;
            if (c.TryGotoNext(MoveType.After,
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(CharacterBody), nameof(CharacterBody.inventory))
                )) && c.Clone().TryGotoNext(
                    x => x.MatchBle(out nullBranch)
                ))
            {
                c.EmitNullConditional(c.Next, nullBranch);
                return;
            }
            Log.PatchFail(il);
        }

        /// <summary>
        /// Skip null display instances
        /// </summary>
        [HarmonyPatch(typeof(JumpDamageStrikeBodyBehavior), nameof(JumpDamageStrikeBodyBehavior.UpdateDisplayInstances))]
        [HarmonyILManipulator]
        public static void JumpDamageStrikeBodyBehavior_UpdateDisplayInstances(ILContext il)
        {
            /*  IL_0015: ldarg.1
			    IL_0016: callvirt instance void [UnityEngine.CoreModule]UnityEngine.GameObject::SetActive(bool)*/

            var c = new ILCursor(il);
            Instruction nullBranch = null;

            if (!c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdarg(1),
                    x => x.MatchCallOrCallvirt<GameObject>(nameof(GameObject.SetActive)),
                    x => x.MatchAny(out nullBranch)
                ))
            {
                Log.PatchFail(il);
                return;
            }
            c.EmitNullConditional(c.Next, nullBranch);
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
            /*  IL_001c: ldelem.ref
		        IL_001d: ldc.i4.1
		        IL_001e: callvirt instance void [UnityEngine.CoreModule]UnityEngine.Behaviour::set_enabled(bool)*/

            var c = new ILCursor(il);
            Instruction nullBranch = null;

            while (c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdcI4(out _),
                    x => x.MatchCallOrCallvirt(AccessTools.PropertySetter(typeof(Behaviour), nameof(Behaviour.enabled))),
                    x => x.MatchAny(out nullBranch)
                ))
            {
                c.EmitNullConditional(c.Next, nullBranch);
                c.Goto(nullBranch, MoveType.After);
            }
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
    }
}
