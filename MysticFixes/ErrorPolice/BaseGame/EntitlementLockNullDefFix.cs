using HarmonyLib;
using RoR2;
using RoR2.EntitlementManagement;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // Mods registering viewables (survivors/skins/logbook entries) with a null required entitlement
    // make the "new" check throw. No entitlement means nothing is required.
    [HarmonyPatch(typeof(ViewablesCatalog.EntitlementLockNode), "UserPassesLock")]
    internal static class EntitlementLockNullDefFix
    {
        private static bool Prefix(EntitlementDef ___lockDef, ref bool __result)
        {
            if (___lockDef)
                return true;

            __result = true;
            return false;
        }
    }
}
