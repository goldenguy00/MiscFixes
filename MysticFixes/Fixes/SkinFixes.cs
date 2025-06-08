using HarmonyLib;
using HG;
using MiscFixes.Modules;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.SurvivorMannequins;

namespace MiscFixes.Fixes
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

        [HarmonyPatch(typeof(SkinCatalog), nameof(SkinCatalog.FindSkinsForBody))]
        [HarmonyILManipulator]
        public static void SkinCatalog_FindSkinsForBody(ILContext il)
        {
            var c = new ILCursor(il);

            int modelSkinControllerLoc = 0;
            if (!c.TryGotoNext(MoveType.AfterLabel,
                    x => x.MatchLdloc(out modelSkinControllerLoc),
                    x => x.MatchLdfld<ModelSkinController>(nameof(ModelSkinController.skins))
                ))
            {
                Log.PatchFail(il);
            }

            c.Emit(OpCodes.Ldloc, modelSkinControllerLoc);
            c.EmitDelegate(CullSkins);
        }

        private static void CullSkins(ModelSkinController skinController)
        {
            if (skinController.skins is null)
                return;

            for (int i = skinController.skins.Length - 1; i >= 0; i--)
            {
                var skinDef = skinController.skins[i];
                if (skinDef.skinDefParams || skinDef.skinDefParamsAddress.RuntimeKeyIsValid())
                    continue;

                if (ShouldRemove(skinDef))
                {
                    ArrayUtils.ArrayRemoveAtAndResize(ref skinController.skins, i);
                }
            }
        }

#pragma warning disable CS0618 // Type or member is obsolete
        private static bool ShouldRemove(SkinDef skinDef)
        {
            for (int i = 0; i < skinDef.gameObjectActivations.Length; i++)
            {
                if (!skinDef.gameObjectActivations[i].gameObject)
                {
                    Log.Error($"Removing SkinDef {skinDef.name} for having a null gameObjectActivations.gameObject at index {i}!");
                    return true;
                }
            }

            for (int j = 0; j < skinDef.meshReplacements.Length; j++)
            {
                if (!skinDef.meshReplacements[j].mesh)
                {
                    Log.Error($"Removing SkinDef {skinDef.name} for having a null meshReplacements.mesh at index {j}!");
                    return true;
                }

                if (!skinDef.meshReplacements[j].renderer)
                {
                    Log.Error($"Removing SkinDef {skinDef.name} for having a null meshReplacements.renderer at index {j}!");
                    return true;
                }
            }

            for (int k = 0; k < skinDef.projectileGhostReplacements.Length; k++)
            {
                if (!skinDef.projectileGhostReplacements[k].projectilePrefab)
                {
                    Log.Error($"Removing SkinDef {skinDef.name} for having a null rojectileGhostReplacements.projectilePrefab at index {k}!");
                    return true;
                }
                if (!skinDef.projectileGhostReplacements[k].projectileGhostReplacementPrefab)
                {
                    Log.Error($"Removing SkinDef {skinDef.name} for having a null projectileGhostReplacements.projectileGhostReplacementPrefab at index {k}!");
                    return true;
                }
            }

            for (int l = 0; l < skinDef.minionSkinReplacements.Length; l++)
            {
                if (!skinDef.minionSkinReplacements[l].minionSkin)
                {
                    Log.Error($"Removing SkinDef {skinDef.name} for having a null minionSkinReplacements.minionSkin at index {l}!");
                    return true;
                }
                if (!skinDef.minionSkinReplacements[l].minionBodyPrefab)
                {
                    Log.Error($"Removing SkinDef {skinDef.name} for having a null minionSkinReplacements.minionBodyPrefab at index {l}!");
                    return true;
                }
            }

            return false;
        }
#pragma warning restore CS0618 // Type or member is obsolete



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
