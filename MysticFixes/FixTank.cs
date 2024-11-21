using System;
using System.Collections.Generic;
using HarmonyLib;
using MonoMod.Cil;
using TanksMod.Modules.Components.BasicTank;
using TanksMod.Modules.Components.UI;
using TanksMod.Modules.Components;
using UnityEngine;
using RoR2.UI;
using Mono.Cecil.Cil;
using RoR2;
using TanksMod.Modules.Survivors;
using TanksMod.Modules;

namespace MiscFixes
{
    [HarmonyPatch]
    public class FixTank
    {
        [HarmonyPatch(typeof(BasicTank), nameof(BasicTank.InitializeSkills))]
        [HarmonyPostfix]
        public static void InitializeSkills()
        {
            // remove spaces, trim ends
            foreach (var skill in ContentPacks.skillDefs)
            {
                skill.skillName = skill.skillName.Trim().Replace(" ", "_");
                skill.skillNameToken = skill.skillNameToken.Trim().Replace(" ", "_");
                (skill as UnityEngine.Object).name = skill.skillName;
            }
        }

        [HarmonyPatch(typeof(StaticModels), nameof(StaticModels.CreateBodyModelFamily))]
        [HarmonyPostfix]
        public static void CreateBodyModelFamily(GenericSkill __result)
        {
            // add family to content pack, give it a name
            __result.skillName = "BodyModel";
            (__result._skillFamily as UnityEngine.Object).name = __result.gameObject.name + "BodyModelFamily";

            Content.AddSkillFamily(__result._skillFamily);
        }

        [HarmonyPatch(typeof(StaticModels), nameof(StaticModels.CreateTurretModelFamily))]
        [HarmonyPostfix]
        public static void CreateTurretModelFamily(GenericSkill __result)
        {
            // add family to content pack, give it a name
            __result.skillName = "TurretModel";
            (__result._skillFamily as UnityEngine.Object).name = __result.gameObject.name + "TurretModelFamily";

            Content.AddSkillFamily(__result._skillFamily);
        }

        [HarmonyPatch(typeof(StaticColors), nameof(StaticColors.CreateGlowColorFamily))]
        [HarmonyPostfix]
        public static void CreateGlowColorFamily(GenericSkill __result)
        {
            // add family to content pack, give it a name
            __result.skillName = "GlowColor";
            (__result._skillFamily as UnityEngine.Object).name = __result.gameObject.name + "GlowColorFamily";

            Content.AddSkillFamily(__result._skillFamily);
        }

        [HarmonyPatch(typeof(StaticColors), nameof(StaticColors.CreateBodyColorFamily))]
        [HarmonyPostfix]
        public static void CreateBodyColorFamily(GenericSkill __result)
        {
            // add family to content pack, give it a name
            __result.skillName = "BodyColor";
            (__result._skillFamily as UnityEngine.Object).name = __result.gameObject.name + "BodyColorFamily";

            Content.AddSkillFamily(__result._skillFamily);
        }

        [HarmonyPatch(typeof(TankCrosshair), "Start")]
        [HarmonyPrefix]
        public static void IHateUI(TankCrosshair __instance)
        {
            // destroy old crosshair
            var controller = __instance.GetComponentInParent<HUD>().targetBodyObject.GetComponent<TankController>();
            if (controller.crosshair != null && controller.crosshair != __instance)
                GameObject.Destroy(controller.crosshair.gameObject);
        }

        [HarmonyPatch(typeof(CheesePlayerHandler), "ExecuteInMenu")]
        [HarmonyILManipulator]
        public static void FuckUICode2(ILContext il)
        {
            //this is the most disgusting way to add index checking but it works
            /*
            int bodyRowId = StaticLoadouts.bodyRowId;
            int turretRowId = StaticLoadouts.turretRowId;
            int glowRowId = StaticLoadouts.glowRowId;
            int paintRowId = StaticLoadouts.paintRowId;

            if (panel.rows.Count > bodyRowId && panel.rows[bodyRowId].rowPanelTransform.GetComponentInChildren<HGTextMeshProUGUI>().text != "Body")
            {
                panel.rows[bodyRowId].rowPanelTransform.GetComponentInChildren<HGTextMeshProUGUI>().text = "Body";
            }

            if (panel.rows.Count > turretRowId && panel.rows[turretRowId].rowPanelTransform.GetComponentInChildren<HGTextMeshProUGUI>().text != "Turret")
            {
                panel.rows[turretRowId].rowPanelTransform.GetComponentInChildren<HGTextMeshProUGUI>().text = "Turret";
            }

            if (panel.rows.Count > glowRowId && panel.rows[glowRowId].rowPanelTransform.GetComponentInChildren<HGTextMeshProUGUI>().text != "Glow")
            {
                panel.rows[glowRowId].rowPanelTransform.GetComponentInChildren<HGTextMeshProUGUI>().text = "Glow";
            }

            if (panel.rows.Count > paintRowId && panel.rows[paintRowId].rowPanelTransform.GetComponentInChildren<HGTextMeshProUGUI>().text != "Paint")
            {
                panel.rows[paintRowId].rowPanelTransform.GetComponentInChildren<HGTextMeshProUGUI>().text = "Paint";
            }

            if (panel.rows.Count > bodyRowId)
            {
                bodyTips = panel.rows[bodyRowId].buttonContainerTransform.GetComponentsInChildren<TooltipProvider>();
            }

            if (panel.rows.Count > turretRowId)
            {
                turretTips = panel.rows[turretRowId].buttonContainerTransform.GetComponentsInChildren<TooltipProvider>();
            }
            
             */
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
        public static void FuckUICode(ILContext il)
        {
            //
            // if (mannequinSlots[i] and mannequinSlots[i].mannequinInstanceTransform)
            //    currentDisplayBody = mannequinSlots[i].mannequinInstanceTransform.gameObject : null;
            //

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
