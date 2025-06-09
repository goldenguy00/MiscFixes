using System.Collections.Generic;
using HarmonyLib;
using MiscFixes.Modules;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine.AddressableAssets;

namespace MiscFixes.ErrorPolice.Harmony
{
    [HarmonyPatch]
    public class MemOpFixes
    {
        /// <summary>
        /// KeyNotFoundException
        /// RoR2.ContentManagement.AssetAsyncReferenceManager`1[T].PreloadInMenuReferences doesnt get cleared after moving handles to AtWill release
        /// reported to gbx devs, probably fixed in next patch
        /// </summary>
        [HarmonyPatch(typeof(AssetAsyncReferenceManager<UnityEngine.Object>), nameof(AssetAsyncReferenceManager<UnityEngine.Object>.OnSceneChanged))]
        [HarmonyILManipulator]
        public static void Asset_OnSceneChanged(ILContext il)
        {
            var c = new ILCursor(il) { Index = il.Instrs.Count - 1 };

            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldsfld, AccessTools.Field(typeof(AssetAsyncReferenceManager<UnityEngine.Object>), nameof(AssetAsyncReferenceManager<UnityEngine.Object>.PreloadedReferences)));
            c.Emit<List<string>>(OpCodes.Callvirt, nameof(List<string>.Clear));
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
