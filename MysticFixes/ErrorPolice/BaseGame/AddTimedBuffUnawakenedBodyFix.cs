using System.Collections.Generic;
using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // buffDurationControllers is only assigned in CharacterBody.Awake. TeamComponent registers itself
    // into the team list as soon as teamIndex is set, so a body whose Awake never ran (typically a
    // prefab a mod set teamIndex on) ends up as a permanent team member. Every BuffWard then hits it
    // each FixedUpdate and throws, which also aborts the ward loop so later members miss the buff.
    // A body that never woke up can't hold buffs anyway, so skip it and report it once.
    [HarmonyPatch(typeof(CharacterBody), "AddTimedBuffInternal")]
    internal static class AddTimedBuffUnawakenedBodyFix
    {
        private static readonly HashSet<int> reportedBodies = new HashSet<int>();

        private static bool Prefix(CharacterBody __instance, AlterBuffDuration[] ___buffDurationControllers)
        {
            if (___buffDurationControllers != null)
                return true;

            if (reportedBodies.Add(__instance.GetInstanceID()))
            {
                bool isPrefab = !__instance.gameObject.scene.IsValid();
                Log.Warning($"Skipped timed buff on '{__instance.name}', its CharacterBody never ran Awake (prefab: {isPrefab}). Some mod likely set teamIndex on it.");
            }
            return false;
        }
    }
}
