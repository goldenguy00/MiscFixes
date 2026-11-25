using System;
using System.Collections.Generic;
using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // GenerateDirectorCardWeightedSelection calls IsAvailable on every card without checking it,
    // and mods that add or remove spawn cards sometimes leave null entries behind. The throw aborts
    // ClassicStageInfo.RebuildCards, so the stage ends up with no monster/interactable selection at
    // all (which then breaks the teleporter boss pick). Drop null cards before the loop.
    [HarmonyPatch(typeof(DirectorCardCategorySelection), nameof(DirectorCardCategorySelection.GenerateDirectorCardWeightedSelection))]
    internal static class DirectorCardNullFix
    {
        private static readonly HashSet<int> reportedSelections = new HashSet<int>();

        private static void Prefix(DirectorCardCategorySelection __instance)
        {
            var categories = __instance.categories;
            if (categories == null)
                return;

            for (int i = 0; i < categories.Length; i++)
            {
                var cards = categories[i].cards;
                if (cards == null)
                {
                    categories[i].cards = Array.Empty<DirectorCard>();
                    continue;
                }

                if (Array.IndexOf(cards, null) < 0)
                    continue;

                categories[i].cards = Array.FindAll(cards, c => c != null);
                if (reportedSelections.Add(__instance.GetInstanceID()))
                    Log.Warning($"Removed null director cards from '{__instance.name}' (category '{categories[i].name}').");
            }
        }
    }
}
