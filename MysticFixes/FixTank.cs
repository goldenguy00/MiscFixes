﻿using System;
using System.Collections.Generic;
using HarmonyLib;
using MonoMod.Cil;
using TanksMod.Modules.Components.BasicTank;
using TanksMod.Modules.Components.UI;
using TanksMod.Modules.Components;
using UnityEngine;
using RoR2.UI;
using Mono.Cecil.Cil;

namespace MiscFixes
{
    [HarmonyPatch]
    public class FixTank
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
