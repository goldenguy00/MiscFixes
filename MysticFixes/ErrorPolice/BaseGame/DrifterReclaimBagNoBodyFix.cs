using System.Reflection;
using HarmonyLib;
using RoR2.Achievements;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // The Drifter vault bag achievement runs for every player when a vault bag chest opens and reads
    // their current body's index without checking it. Any dead player throws there, and since it's
    // raised from the chest's OnEnter, the throw also cuts that state short. A dead player can't earn
    // it anyway.
    [HarmonyPatch]
    internal static class DrifterReclaimBagNoBodyFix
    {
        private static MethodBase TargetMethod()
        {
            var tracker = AccessTools.Inner(typeof(DrifterReclaimBagAchievement), "DrifterReclaimBagServerAchievement");
            return AccessTools.DeclaredMethod(tracker, "OnOpened");
        }

        private static bool Prefix(BaseServerAchievement __instance)
        {
            var networkUser = __instance.networkUser;
            return networkUser && networkUser.GetCurrentBody();
        }
    }
}
