using HarmonyLib;
using HG;
using MiscFixes.Modules;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MiscFixes.ErrorPolice.Harmony
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
        /// loadHandle should be checked for validity
        /// directRef ?? loadHandle.Result;
        /// directRef ?? loadHandle.IsValid() ? loadHandle.Result : null
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
    }
}
