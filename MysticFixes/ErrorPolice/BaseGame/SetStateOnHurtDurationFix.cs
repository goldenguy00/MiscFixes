using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // buffDurationControllers is only assigned in Start, but SetFrozen/SetStun can be
    // called on the same frame the body spawns, before Start has run.
    // Fetch the controllers ourselves so the duration still gets scaled correctly.
    [HarmonyPatch(typeof(SetStateOnHurt), "GetModifiedDuration")]
    internal static class SetStateOnHurtDurationFix
    {
        private static void Prefix(SetStateOnHurt __instance, ref AlterBuffDuration[] ___buffDurationControllers)
        {
            if (___buffDurationControllers == null)
                ___buffDurationControllers = __instance.GetComponents<AlterBuffDuration>();
        }
    }
}
