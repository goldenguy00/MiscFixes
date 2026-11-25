using EntityStates.HallowedCauldron;
using HarmonyLib;
using UnityEngine.Networking;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // DrainingItems.CheckIfDoneDraining asks the Hex cost type whether the activator can still pay,
    // but the activator is only set on the server. Clients run the state with no activator and throw
    // every frame while the cauldron drains. On the server the activator can also be destroyed
    // mid-drain (player left). Without an activator, clients just wait for the server, and the
    // server treats the drain as done so it still hands out the reward and resets.
    [HarmonyPatch(typeof(DrainingItems), "CheckIfDoneDraining")]
    internal static class HallowedCauldronClientActivatorFix
    {
        private static bool Prefix(DrainingItems __instance, ref bool __result)
        {
            if (__instance.activator)
                return true;

            __result = NetworkServer.active;
            return false;
        }
    }
}
