using System;
using EntityStates;
using EntityStates.DefectiveUnit;
using HarmonyLib;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // AttemptDeathBehavior reads cachedModelTransform.transform without a null check.
    // The model can already be gone (e.g. Void Fields wave end destroying the bodies),
    // in which case there's nothing to spawn the effect on or destroy anyway.
    [HarmonyPatch(typeof(DeathState), "AttemptDeathBehavior")]
    internal static class DefectiveUnitDeathStateFix
    {
        private static readonly Func<GenericCharacterDeath, Transform> getCachedModelTransform =
            AccessTools.MethodDelegate<Func<GenericCharacterDeath, Transform>>(
                AccessTools.PropertyGetter(typeof(GenericCharacterDeath), "cachedModelTransform"));

        private static bool Prefix(DeathState __instance)
        {
            return getCachedModelTransform(__instance);
        }
    }
}
