using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // ShouldHitProceedInternal reads victim.body.teamComponent without checks, but attacks can reach
    // health components whose body never woke up or that have no TeamComponent (some modded bodies).
    // Such a victim has no team to be friendly with, so let the hit proceed like the vanilla fallthrough.
    [HarmonyPatch(typeof(FriendlyFireManager), "ShouldHitProceedInternal")]
    internal static class FriendlyFireNoBodyFix
    {
        private static bool Prefix(HealthComponent victim, ref bool __result)
        {
            if (victim && victim.body && victim.body.teamComponent)
                return true;

            __result = victim;
            return false;
        }
    }
}
