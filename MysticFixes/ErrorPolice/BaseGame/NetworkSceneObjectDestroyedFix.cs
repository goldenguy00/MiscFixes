using HarmonyLib;
using UnityEngine.Networking;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // SpawnObjects grabs every NetworkIdentity up front, activates the scene objects, then spawns
    // them in a second pass. Activating one can destroy another, and ValidateSceneObject then throws
    // on the dead identity. That aborts the whole pass, so every scene object after it never gets
    // spawned for clients. A destroyed identity isn't a valid scene object, so report it as such.
    [HarmonyPatch(typeof(NetworkServer), "ValidateSceneObject")]
    internal static class NetworkSceneObjectDestroyedFix
    {
        private static bool Prefix(NetworkIdentity netId, ref bool __result)
        {
            if (netId)
                return true;

            __result = false;
            return false;
        }
    }
}
