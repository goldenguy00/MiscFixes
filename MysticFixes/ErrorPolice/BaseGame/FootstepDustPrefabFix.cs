using HarmonyLib;
using RoR2;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // Start instantiates footstepDustPrefab whenever enableFootstepDust is set, without checking
    // that a prefab was actually assigned (some modded bodies tick the box and leave it empty).
    // Turn the dust off for those; Footstep already copes with having no dust instance.
    [HarmonyPatch(typeof(FootstepHandler), "Start")]
    internal static class FootstepDustPrefabFix
    {
        private static void Prefix(ref bool ___enableFootstepDust, GameObject ___footstepDustPrefab)
        {
            if (___enableFootstepDust && !___footstepDustPrefab)
                ___enableFootstepDust = false;
        }
    }
}
