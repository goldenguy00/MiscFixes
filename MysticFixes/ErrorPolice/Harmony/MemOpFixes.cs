using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using HG;
using MiscFixes.Modules;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MiscFixes.ErrorPolice.Harmony
{
    [HarmonyPatch]
    public class MemOpFixes
    {
        [HarmonyPatch(typeof(ModelSkinController), nameof(ModelSkinController.ApplySkinAsync), MethodType.Enumerator)]
        [HarmonyILManipulator]
        public static void ModelSkinController_ApplySkinAsync(ILContext il)
        {
            var c = new ILCursor(il);

            if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchCallOrCallvirt<ModelSkinController>(nameof(ModelSkinController.UnloadCurrentlyLoadedSkinAssets))
                ))
            {
                Log.PatchFail(il);
                return;
            }

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(RevertSkin);
        }

        private static void RevertSkin(ModelSkinController modelSkinController)
        {
            var previousReverseSkin = modelSkinController.GetComponent<ReverseSkinAsync>();
            if (previousReverseSkin)
                previousReverseSkin.Dispose();
            else
                modelSkinController.gameObject.AddComponent<ReverseSkinAsync>();
        }

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

        /// <summary>
        /// KeyNotFoundException
        /// RoR2.ContentManagement.AssetAsyncReferenceManager`1[T].PreloadInMenuReferences doesnt get cleared after moving handles to AtWill release
        /// reported to gbx devs, probably fixed in next patch
        /// </summary>
        [HarmonyPatch(typeof(AssetAsyncReferenceManager<Object>), nameof(AssetAsyncReferenceManager<Object>.OnSceneChanged))]
        [HarmonyILManipulator]
        public static void Asset_OnSceneChanged(ILContext il)
        {
            var c = new ILCursor(il) { Index = il.Instrs.Count - 1 };

            ILLabel leave = null;
            if (!c.TryGotoPrev(MoveType.After, x => x.MatchEndfinally()) ||
                !c.TryFindPrev(out _, x => x.MatchLeaveS(out leave)))
            {
                Log.PatchFail(il);
                return;
            }

            c.Emit(OpCodes.Ldsfld, AccessTools.Field(typeof(AssetAsyncReferenceManager<Object>), nameof(AssetAsyncReferenceManager<Object>.PreloadedInMenuReferences)));
            c.Emit<List<string>>(OpCodes.Callvirt, nameof(List<string>.Clear));
            
            c.Index -= 2;
            c.MarkLabel(leave);
            il.Body.ExceptionHandlers.Last().HandlerEnd = leave.Target;
        }

        /// <summary>
        /// RoR2.CharacterModel.InstantiateDisplayRuleGroup(RoR2.DisplayRuleGroup displayRuleGroup, RoR2.ItemIndex itemIndex, RoR2.EquipmentIndex equipmentIndex)
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
        /// if (!this.<keyAssetRuleGroup>5__3.keyAssetAddress.RuntimeKeyIsValid())
        /// keyAssetAddress and directRef being null will throw
        /// </summary>
        [HarmonyPatch(typeof(ItemDisplayRuleSet), nameof(ItemDisplayRuleSet.GenerateRuntimeValuesAsync), MethodType.Enumerator)]
        [HarmonyILManipulator]
        public static void ItemDisplayRuleSet_GenerateRuntimeValuesAsync(ILContext il)
        {
            var c = new ILCursor(il);

            Instruction instr = null;

            if (c.TryGotoNext(MoveType.AfterLabel,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld(out _),
                    x => x.MatchCallOrCallvirt(out _),
                    x => x.MatchBrtrue(out _),
                    x => x.MatchAny(out instr)
                ))
            {
                c.Emit(OpCodes.Br, instr);
            }

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

    }
}
