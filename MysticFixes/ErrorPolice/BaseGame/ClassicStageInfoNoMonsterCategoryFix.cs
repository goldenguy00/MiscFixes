using System;
using HarmonyLib;
using RoR2;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // RebuildCards evaluates the stage's monster pool categories without checking that any are
    // available. When none are (no category with available content, or only family categories with no
    // available family) it throws "Cannot call Evaluate without available choices", which aborts the
    // stage before its interactables are built, then breaks combat shrines and the teleporter boss.
    // In that case hand it an empty monster category instead, so the stage has no monster cards (as
    // it would anyway) but still builds its interactables.
    [HarmonyPatch(typeof(ClassicStageInfo), "RebuildCards")]
    internal static class ClassicStageInfoNoMonsterCategoryFix
    {
        private static readonly Func<ClassicStageInfo, DccsPool.Category, bool> isCategorySelectionAFamily =
            AccessTools.MethodDelegate<Func<ClassicStageInfo, DccsPool.Category, bool>>(
                AccessTools.Method(typeof(ClassicStageInfo), "IsCategorySelectionAFamily"));

        private static DirectorCardCategorySelection emptyMonsterCategory;

        private static void Prefix(ClassicStageInfo __instance, ref DirectorCardCategorySelection forcedMonsterCategory)
        {
            var pool = __instance.monsterDccsPool;
            if (forcedMonsterCategory != null || !pool || HasAvailableCategory(__instance, pool))
                return;

            Log.Warning($"'{__instance.name}' has no available monster pool category, building the stage without monster cards.");
            if (!emptyMonsterCategory)
            {
                emptyMonsterCategory = ScriptableObject.CreateInstance<DirectorCardCategorySelection>();
                emptyMonsterCategory.name = "MiscFixesEmptyMonsterCategory";
                emptyMonsterCategory.hideFlags = HideFlags.DontUnloadUnusedAsset;
            }
            forcedMonsterCategory = emptyMonsterCategory;
        }

        private static bool HasAvailableCategory(ClassicStageInfo stageInfo, DccsPool pool)
        {
            var selection = pool.GenerateWeightedCategorySelection();
            for (int i = 0; i < selection.Count; i++)
            {
                var category = selection.GetChoice(i).value;
                try
                {
                    // Non-family categories are blended, which handles an empty category on its own.
                    if (!isCategorySelectionAFamily(stageInfo, category) || DccsPoolEmptyCategoryFix.HasChoices(pool, category))
                        return true;
                }
                catch (Exception)
                {
                    // Mod-built categories can have null entry arrays; leave those to the game as before.
                    return true;
                }
            }
            return false;
        }
    }
}
