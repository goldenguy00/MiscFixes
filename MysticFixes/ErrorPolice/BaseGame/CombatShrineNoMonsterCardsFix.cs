using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // Shrines of Combat (through CreateReasonableDirectorCardSpawnList) read the stage's monster
    // selection without checking it. When the stage never built one (usually because RebuildCards
    // threw) using the shrine throws and leaves its encounter object behind. Return an empty list
    // instead, which the shrine already handles by not spawning anything.
    [HarmonyPatch(typeof(CombatShrineUtils), nameof(CombatShrineUtils.CreateReasonableDirectorCardSpawnList))]
    internal static class CombatShrineNoMonsterCardsFix
    {
        private static bool Prefix(ref WeightedSelection<DirectorCard> __result)
        {
            if (ClassicStageInfo.instance && ClassicStageInfo.instance.monsterSelection != null)
                return true;

            __result = new WeightedSelection<DirectorCard>();
            return false;
        }
    }
}
