using System.Collections.Generic;
using EntityStates.Missions.Arena.NullWard;
using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // Off is the initial state of the Void Fields cells, and it (via NullWardBaseState) uses the
    // SphereZone on its object without checking it exists. An object running these states without
    // one throws as soon as its state machine starts. Without a zone there is nothing for Off to
    // shrink or turn off, so skip it and report the object once.
    [HarmonyPatch(typeof(Off), nameof(Off.OnEnter))]
    internal static class NullWardMissingZoneFix
    {
        private static readonly HashSet<int> reportedObjects = new HashSet<int>();

        private static bool Prefix(Off __instance)
        {
            if (!__instance.outer || __instance.outer.GetComponent<SphereZone>())
                return true;

            if (reportedObjects.Add(__instance.outer.gameObject.GetInstanceID()))
                Log.Warning($"Skipped NullWard Off state on '{__instance.outer.name}', it has no SphereZone.");
            return false;
        }
    }
}
