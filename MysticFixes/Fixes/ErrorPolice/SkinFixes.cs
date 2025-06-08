using HarmonyLib;
using HG;
using MiscFixes.Modules;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.ContentManagement;
using RoR2.SurvivorMannequins;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MiscFixes.Fixes.ErrorPolice
{
    [HarmonyPatch]
    internal class SkinFixes
    {
        [HarmonyPatch(typeof(SurvivorCatalog), nameof(SurvivorCatalog.ValidateEntry))]
        [HarmonyPostfix]
        public static void SurvivorCatalog_ValidateEntry(SurvivorDef survivorDef)
        {
            if (!survivorDef.bodyPrefab || !survivorDef.displayPrefab)
                return;

            var bodySkins = survivorDef.bodyPrefab.GetComponentInChildren<ModelSkinController>();
            if (!bodySkins)
                return;

            var displayModel = survivorDef.displayPrefab.GetComponentInChildren<CharacterModel>();
            if (!displayModel)
                return;

            var displaySkins = displayModel.GetComponent<ModelSkinController>();
            if (displaySkins)
                return;

            Log.Warning($"Adding ModelSkinController on DisplayPrefab for {survivorDef}");
            displaySkins = displayModel.gameObject.AddComponent<ModelSkinController>();
            displaySkins.skins = ArrayUtils.Clone(bodySkins.skins);
        }
        /// <summary>
        ///IL_000e: pop
        ///IL_000f: ldarg.0
	    ///IL_0010: call instance valuetype[Unity.ResourceManager] UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle`1<!0> class RoR2.ContentManagement.AssetOrDirectReference`1<!T>::get_loadHandle()
        ///IL_0015: stloc.0
	    ///IL_0016: ldloca.s 0
	    ///IL_0018: call instance !0 valuetype[Unity.ResourceManager] UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle`1<!T>::get_Result()
        ///IL_001d: ret
        /// </summary>
        [HarmonyPatch(typeof(AssetOrDirectReference<Object>), "Result", MethodType.Getter)]
        [HarmonyILManipulator]
        public static void SkinDef_ApplyAsync(ILContext il)
        {
            var c = new ILCursor(il);

            Instruction ret = null;
            if (!c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdloca(out _),
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(AsyncOperationHandle<Object>), "Result")),
                    x => x.MatchAny(out ret)
                ))
            {
                Log.PatchFail(il);
                return;
            }

            c.Index++;
            var getResultLabel = c.DefineLabel();

            c.Emit(OpCodes.Dup);
            c.Emit(OpCodes.Call, AccessTools.Method(typeof(AsyncOperationHandle<Object>), "IsValid"));
            c.Emit(OpCodes.Brtrue, getResultLabel);

            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldnull);
            c.Emit(OpCodes.Br, ret);

            c.MarkLabel(getResultLabel);
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
    }
}
