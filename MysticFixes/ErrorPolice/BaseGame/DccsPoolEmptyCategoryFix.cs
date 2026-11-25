using System;
using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // RebuildCards picks a monster pool category, then for a family category evaluates the family
    // DCCSs in it. GenerateWeightedCategorySelection only checks that a category has entries, not that
    // any of them are available, so when every family is unavailable (stage range, disabled content,
    // mods) the family pick throws "Cannot call Evaluate without available choices". That aborts
    // RebuildCards before the interactables are built, so the stage then spawns nothing (or throws in
    // SceneDirector). Leave out categories with nothing to pick, unless that would leave none at all.
    [HarmonyPatch(typeof(DccsPool), nameof(DccsPool.GenerateWeightedCategorySelection))]
    internal static class DccsPoolEmptyCategoryFix
    {
        private static void Postfix(DccsPool __instance, ref WeightedSelection<DccsPool.Category> __result)
        {
            if (__result == null || __result.Count == 0)
                return;

            WeightedSelection<DccsPool.Category> filtered = null;
            for (int i = 0; i < __result.Count; i++)
            {
                var choice = __result.GetChoice(i);
                if (HasChoices(__instance, choice.value))
                {
                    filtered ??= new WeightedSelection<DccsPool.Category>();
                    filtered.AddChoice(choice);
                }
            }

            if (filtered != null && filtered.Count < __result.Count)
                __result = filtered;
        }

        internal static bool HasChoices(DccsPool pool, DccsPool.Category category)
        {
            try
            {
                return pool.GenerateWeightSelectionFromSingleCategory(category).Count > 0;
            }
            catch (Exception)
            {
                // Mod-built categories can have null entry arrays; leave those to the game as before.
                return true;
            }
        }
    }
}
