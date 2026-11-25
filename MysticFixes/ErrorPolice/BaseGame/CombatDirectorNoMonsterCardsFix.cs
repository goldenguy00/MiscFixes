using System;
using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // SetNextSpawnAsBoss reads the director's monster selection, falling back to the stage's, and
    // uses it without checking. When the stage never built one (usually because RebuildCards threw)
    // it's null and the teleporter event throws on activation. With no cards there's no boss to pick,
    // so do what the method already does when it finds no champion: just push the spawn timer back.
    [HarmonyPatch(typeof(CombatDirector), nameof(CombatDirector.SetNextSpawnAsBoss))]
    internal static class CombatDirectorNoMonsterCardsFix
    {
        private static readonly Func<CombatDirector, WeightedSelection<DirectorCard>> getFinalMonsterCardsSelection =
            AccessTools.MethodDelegate<Func<CombatDirector, WeightedSelection<DirectorCard>>>(
                AccessTools.PropertyGetter(typeof(CombatDirector), "finalMonsterCardsSelection"));

        private static bool Prefix(CombatDirector __instance, DirectorCard ____bossOverride)
        {
            if (____bossOverride != null || getFinalMonsterCardsSelection(__instance) != null)
                return true;

            Log.Warning($"'{__instance.name}' has no monster cards to pick a boss from, skipping boss spawn.");
            __instance.monsterSpawnTimer = -600f;
            return false;
        }
    }
}
