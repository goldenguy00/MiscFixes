using HarmonyLib;
using HunkMod.Modules.Survivors;
using HunkMod.SkillStates.Hunk.Counter;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using EntityStates.LunarExploderMonster;
using UnityEngine;
using RoR2.PostProcessing;
using System;
using UnityEngine.EventSystems;
using Tyranitar.Modules.Components;
using Facepunch.Steamworks;
using TanksMod.Modules.Components;
using TanksMod.Modules.Components.UI;
using TanksMod.Modules.Components.BasicTank;
using RoR2.UI;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace MiscFixes
{
    [HarmonyPatch]
    public class FixVanilla
    {
        [HarmonyPatch(typeof(DamageIndicator), nameof(DamageIndicator.Awake))]
        [HarmonyILManipulator]
        public static void FixDmgIndicator(ILContext il)
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<DamageIndicator>(nameof(DamageIndicator.mat))))
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

            if (c.TryGotoNext(x => x.MatchCallOrCallvirt(AccessTools.PropertySetter(typeof(Renderer), nameof(Renderer.enabled)))))
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
                x => x.MatchCall(AccessTools.PropertySetter(typeof(EventSystem), nameof(EventSystem.current)))))
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

            if (c.TryGotoNext(x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(ReadOnlyCollection<TeamComponent>), nameof(ReadOnlyCollection<TeamComponent>.Count)))))
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
            if (c.TryGotoNext(x => x.MatchLdfld<EntityLocator>(nameof(EntityLocator.entity))) &&
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

    [HarmonyPatch]
    public class FixHunk
    {
        [HarmonyPatch(typeof(Hunk), "TVirusDeathDefied")]
        [HarmonyILManipulator]
        public static void Tvirus(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdcI4(out _),
                    x => x.MatchCgt()
                ))
            {
                c.Remove();
                c.Emit(OpCodes.Ldc_I4_0);
            }
            else Debug.LogError($"IL hook failed for Hunk.TVirusDeathDefied");
        }

        [HarmonyPatch(typeof(UroLunge), "OnEnter")]
        [HarmonyILManipulator]
        public static void Uro(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(x => x.MatchLdstr("Aim")))
            {
                c.Next.Operand = "Slide";
            }
            else Debug.LogError($"IL hook failed for UroLunge.OnEnter");
        }
    }

    [HarmonyPatch]
    public class FixRocks
    {
        [HarmonyPatch(typeof(KingsRockBehavior), "KillAllRocks")]
        [HarmonyPrefix]
        public static bool Prefix(KingsRockBehavior __instance)
        {
            __instance.activeRocks = 0;
            if (__instance.rocks is not null)
            {
                for (int i = 0; i < __instance.rocks.Length; i++)
                {
                    var rock = __instance.rocks[i].rock;
                    if (rock && rock.activeSelf)
                    {
                        rock.SetActive(value: false);
                    }
                }
            }
            return false;
        }
    }

    [HarmonyPatch]
    public class FixCheese
    {
        [HarmonyPatch(typeof(TankCrosshair), "Start")]
        [HarmonyPrefix]
        public static void IHateUI(TankCrosshair __instance)
        {
            var controller = __instance.GetComponentInParent<HUD>().targetBodyObject.GetComponent<TankController>();
            if (controller.crosshair != null && controller.crosshair != __instance)
                GameObject.Destroy(controller.crosshair.gameObject);
        }

        [HarmonyPatch(typeof(CheesePlayerHandler), "ExecuteInMenu")]
        [HarmonyILManipulator]
        public static void FuckThisCode2(ILContext il)
        {
            ILCursor[] cs = null;
            if (new ILCursor(il).TryFindNext(out cs,
                    x => x.MatchLdsfld(AccessTools.DeclaredField(typeof(StaticLoadouts), nameof(StaticLoadouts.bodyRowId))),
                    x => x.MatchLdsfld(AccessTools.DeclaredField(typeof(StaticLoadouts), nameof(StaticLoadouts.turretRowId))),
                    x => x.MatchLdsfld(AccessTools.DeclaredField(typeof(StaticLoadouts), nameof(StaticLoadouts.glowRowId))),
                    x => x.MatchLdsfld(AccessTools.DeclaredField(typeof(StaticLoadouts), nameof(StaticLoadouts.paintRowId)))
                ))
            {
                for (int i = 0; i < cs.Length; i++)
                {
                    var c = cs[i];
                    int loc = 0;
                    ILLabel label = null;
                    c.GotoNext(x => x.MatchStloc(out loc));
                    c.GotoNext(MoveType.After, x => x.MatchLdloc(loc));
                    c.FindNext(out _, x => x.MatchBrfalse(out label));

                    c.EmitDelegate<Func<List<LoadoutPanelController.Row>, int, bool>>((rows, idx) => idx < rows.Count);
                    c.Emit(OpCodes.Brfalse, label);
                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit<CheesePlayerHandler>(OpCodes.Ldfld, nameof(CheesePlayerHandler.panel));
                    c.Emit<LoadoutPanelController>(OpCodes.Ldfld, nameof(LoadoutPanelController.rows));
                    c.Emit(OpCodes.Ldloc, loc);

                    if (i == 0)
                    {
                        c.GotoNext(x => x.MatchLdcI4(3));
                        c.Remove();
                        c.Emit(OpCodes.Ldloc, loc);
                    }
                    if (i == 1)
                    {
                        c.GotoNext(x => x.MatchStfld<CheesePlayerHandler>(nameof(CheesePlayerHandler.turretTips)));
                        c.GotoPrev(
                            x => x.MatchLdloc(out _),
                            x => x.MatchCgt());
                        c.Remove();
                        c.Emit(OpCodes.Ldloc, loc);
                    }
                }
            }
            else Debug.LogError($"IL hook failed for CheesePlayerHandler.ExecuteInMenu");
        }

        [HarmonyPatch(typeof(CheesePlayerHandler), "Update")]
        [HarmonyILManipulator]
        public static void FuckThisCode(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.After, x => x.MatchLdelemRef()))
            {
                var stlocLabel = c.DefineLabel();
                var transformLabel = c.DefineLabel();
                var gameObjectLabel = c.DefineLabel();

                c.Emit(OpCodes.Dup);
                c.Emit<UnityEngine.Object>(OpCodes.Call, "op_Implicit");
                c.Emit(OpCodes.Brtrue, transformLabel);

                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Ldnull);
                c.Emit(OpCodes.Br, stlocLabel);

                c.MarkLabel(transformLabel);
                c.Index++;

                c.Emit(OpCodes.Dup);
                c.Emit<UnityEngine.Object>(OpCodes.Call, "op_Implicit");
                c.Emit(OpCodes.Brtrue, gameObjectLabel);

                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Ldnull);
                c.Emit(OpCodes.Br, stlocLabel);

                c.MarkLabel(gameObjectLabel);
                c.Index++;

                c.MarkLabel(stlocLabel);
            }
            else Debug.LogError($"IL hook failed for CheesePlayerHandler.Update");
        }
    }
}
