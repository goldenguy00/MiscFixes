using System;
using System.Collections.ObjectModel;
using EntityStates.LunarExploderMonster;
using Facepunch.Steamworks;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.PostProcessing;
using RoR2.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MiscFixes
{
    [HarmonyPatch]
    public class FixVanilla
    {
        [HarmonyPatch(typeof(FogDamageController), nameof(FogDamageController.EvaluateTeam))]
        [HarmonyILManipulator]
        public static void FixFog(ILContext il)
        {
            var c = new ILCursor(il);

            int locTC = 0, locBody = 0;
            ILLabel label = null;
            if (c.TryGotoNext(
                    x => x.MatchLdloc(out locTC),
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(TeamComponent), nameof(TeamComponent.body))),
                    x => x.MatchStloc(out locBody)) &&
                c.TryFindPrev(out _,
                    x => x.MatchBr(out label)
                ))
            {
                c.Emit(OpCodes.Ldloc, locTC);
                c.Emit<UnityEngine.Object>(OpCodes.Call, "op_Implicit");
                c.Emit(OpCodes.Brfalse, label);

                c.GotoNext(MoveType.After, x => x.MatchStloc(locBody));

                c.Emit(OpCodes.Ldloc, locBody);
                c.Emit<UnityEngine.Object>(OpCodes.Call, "op_Implicit");
                c.Emit(OpCodes.Brfalse, label);
            }
            else Debug.LogError($"IL hook failed for FogDamageController.EvaluateTeam");
        }

        [HarmonyPatch(typeof(TetherVfxOrigin), nameof(TetherVfxOrigin.AddTether))]
        [HarmonyILManipulator]
        public static void FixTether(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<TetherVfxOrigin>(nameof(TetherVfxOrigin.onTetherAdded)),
                    x => x.MatchLdloc(0),
                    x => x.MatchLdarg(1),
                    x => x.MatchCallOrCallvirt<TetherVfxOrigin.TetherAddDelegate>(nameof(TetherVfxOrigin.TetherAddDelegate.Invoke))
                ))
            {
                var retLabel = c.MarkLabel();

                c.GotoPrev(MoveType.After, x => x.MatchLdfld<TetherVfxOrigin>(nameof(TetherVfxOrigin.onTetherAdded)));

                var callLabel = c.DefineLabel();

                c.Emit(OpCodes.Dup);
                c.Emit(OpCodes.Brtrue, callLabel);

                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Br, retLabel);

                c.MarkLabel(callLabel);
            }
            else Debug.LogError($"IL hook failed for TetherVfxOrigin.AddTether");
        }

        [HarmonyPatch(typeof(CharacterMaster), nameof(CharacterMaster.TrueKill), [typeof(GameObject), typeof(GameObject), typeof(DamageTypeCombo)])]
        [HarmonyILManipulator]
        public static void FixKill(ILContext il)
        {
            var c = new ILCursor(il);

            ILLabel label = null, label2 = null;
            if (c.TryGotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchCallOrCallvirt<CharacterMaster>(nameof(CharacterMaster.GetBody)),
                    x => x.MatchLdsfld(out _),
                    x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.HasBuff))) &&
                c.TryFindNext(out _,
                    x => x.MatchBrfalse(out label)
                ))
            {
                c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt<CharacterMaster>(nameof(CharacterMaster.GetBody)));

                var bodyLabel = c.DefineLabel();

                c.Emit(OpCodes.Dup);
                c.Emit<UnityEngine.Object>(OpCodes.Call, "op_Implicit");
                c.Emit(OpCodes.Brtrue, bodyLabel);

                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Br, label);

                c.MarkLabel(bodyLabel);
            }
            else Debug.LogError($"IL hook failed for CharacterMaster.TrueKill 1");

            if (c.TryGotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchCallOrCallvirt<CharacterMaster>(nameof(CharacterMaster.GetBody)),
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(CharacterBody), nameof(CharacterBody.equipmentSlot)))) &&
                c.TryFindNext(out _,
                    x => x.MatchBrfalse(out label2)
                ))
            {
                c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt<CharacterMaster>(nameof(CharacterMaster.GetBody)));

                var bodyLabel2 = c.DefineLabel();

                c.Emit(OpCodes.Dup);
                c.Emit<UnityEngine.Object>(OpCodes.Call, "op_Implicit");
                c.Emit(OpCodes.Brtrue, bodyLabel2);

                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Br, label2);

                c.MarkLabel(bodyLabel2);
            }
            else Debug.LogError($"IL hook failed for CharacterMaster.TrueKill 2");
        }

        [HarmonyPatch(typeof(FlickerLight), nameof(FlickerLight.Update))]
        [HarmonyPrefix]
        public static bool FixFlicker(FlickerLight __instance) => __instance.light;

        [HarmonyPatch(typeof(Indicator), nameof(Indicator.SetVisibleInternal))]
        [HarmonyILManipulator]
        public static void FixIndicator(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(
                    x => x.MatchCallOrCallvirt(AccessTools.PropertySetter(typeof(Renderer), nameof(Renderer.enabled)))
                ))
            {
                c.Remove();
                c.EmitDelegate<Action<Renderer, bool>>((renderer, newVisible) =>
                {
                    if (renderer)
                        renderer.enabled = newVisible;
                });
            }
            else Debug.LogError($"IL hook failed for Indicator.SetVisibleInternal");
        }

        [HarmonyPatch(typeof(DeathState), nameof(DeathState.FixedUpdate))]
        [HarmonyILManipulator]
        public static void FixExplode(ILContext il)
        {
            var c = new ILCursor(il);

            ILCursor[] cList = [];
            if (c.TryFindNext(out cList, 
                    x => x.MatchCallOrCallvirt<DeathState>(nameof(DeathState.FireExplosion)),
                    x => x.MatchCallOrCallvirt<GameObject>(nameof(GameObject.SetActive))
                ))
            {
                var c0 = cList[0];
                var c1 = cList[1];
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
            else Debug.LogError($"IL hook failed for DeathState.FixedUpdate");
        }

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
            else Debug.LogError($"IL hook failed for MPEventSystem.Update");
        }

        [HarmonyPatch(typeof(BaseSteamworks), nameof(BaseSteamworks.RunUpdateCallbacks))]
        [HarmonyFinalizer]
        public static Exception FixFacepunch(Exception __exception) => null;

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
                c.Emit(OpCodes.Call, AccessTools.DeclaredMethod(typeof(FixVanilla), nameof(FixVanilla.GetRealCount)));
            }
            else Debug.LogError($"IL hook failed for EntityStates.VoidCamp.Idle");
        }

        [HarmonyPatch(typeof(Interactor), nameof(Interactor.FindBestInteractableObject))]
        [HarmonyILManipulator]
        public static void FixInteraction(ILContext il)
        {
            var c = new ILCursor(il);

            int loc = 0;
            ILLabel label = null;
            if (c.TryGotoNext(
                    x => x.MatchLdfld<EntityLocator>(nameof(EntityLocator.entity))) &&
                c.TryGotoNext(MoveType.After,
                    x => x.MatchBrfalse(out label),
                    x => x.MatchLdloc(out _),
                    x => x.MatchLdfld<EntityLocator>(nameof(EntityLocator.entity)),
                    x => x.MatchStloc(out loc)
                ))
            {
                c.Emit(OpCodes.Ldloc, loc);
                c.Emit<UnityEngine.Object>(OpCodes.Call, "op_Implicit");
                c.Emit(OpCodes.Brfalse, label);
            }
            else Debug.LogError($"IL hook failed for Interactor.FindBestInteractableObject");
        }

        public static int GetRealCount(ReadOnlyCollection<TeamComponent> teamMembers)
        {
            int count = 0;
            foreach (var member in teamMembers)
            {
                var body = member ? member.body : null;
                if (body && body.master && body.healthComponent && body.healthComponent.alive)
                    count++;
            }
            return count;
        }
    }
}
