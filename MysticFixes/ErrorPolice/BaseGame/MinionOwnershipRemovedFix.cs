using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // When an owner master is destroyed, MinionGroupDestroyer disposes the group and calls
    // HandleMinionRemoved for every member. A member can already be destroyed by then, and
    // TryGetComponent on a dead component throws. That aborts Dispose midway, leaving the
    // remaining members attached and the group stuck in instancesList. A dead minion has no
    // CharacterMaster to report as lost anyway, so just skip it.
    [HarmonyPatch(typeof(MinionOwnership), "HandleMinionRemoved")]
    internal static class MinionOwnershipRemovedFix
    {
        private static bool Prefix(MinionOwnership minion)
        {
            return minion;
        }
    }
}
