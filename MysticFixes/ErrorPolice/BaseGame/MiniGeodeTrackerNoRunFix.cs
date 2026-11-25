using EntityStates.Missions.MiniGeodeTracker;
using HarmonyLib;
using RoR2;
using UnityEngine.Networking;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // The geode tracker keeps ticking after the run ends (game over screen) and sets a run event
    // flag once enough geodes are gone, which throws every frame with no Run. Nothing to track then.
    [HarmonyPatch(typeof(Listening), nameof(Listening.FixedUpdate))]
    internal static class MiniGeodeTrackerNoRunFix
    {
        private static bool Prefix()
        {
            return !NetworkServer.active || Run.instance;
        }
    }
}
