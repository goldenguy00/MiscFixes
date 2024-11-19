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
        [HarmonyPatch(typeof(SurvivorIconController), nameof(SurvivorIconController.GetLocalUser))]
        [HarmonyPatch(typeof(DamageIndicator), nameof(DamageIndicator.Awake))]
        [HarmonyPatch(typeof(DamageIndicator), nameof(DamageIndicator.OnRenderImage))]
        [HarmonyFinalizer]
        public static Exception IconEventSystem() => null;

        public static void FixDmgIndicator(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.After,
                    x => x.MatchLdfld<DamageIndicator>(nameof(DamageIndicator.mat))
                ))
            {
                var stlocLabel = c.DefineLabel();
                var instantiateLabel = c.DefineLabel();

                c.Emit(OpCodes.Dup);
                c.Emit<UnityEngine.Object>(OpCodes.Call, "op_Implicit");
                c.Emit(OpCodes.Brtrue, instantiateLabel);

                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Ldnull);
                c.Emit(OpCodes.Br, stlocLabel);

                c.MarkLabel(instantiateLabel);
                c.Index++;

                c.MarkLabel(stlocLabel);
            }
            else Debug.LogError($"IL hook failed for DamageIndicator.Awake");
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
