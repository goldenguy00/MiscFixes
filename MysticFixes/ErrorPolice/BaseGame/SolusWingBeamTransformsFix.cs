using System;
using EntityStates.SolusWing;
using HarmonyLib;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // UpdateBeamTransforms runs every LateUpdate while the beam state is active and reads
    // beamVfxInstance.transform without checking it. The VFX can be destroyed out from under
    // the state (it's parented to the muzzle), which then throws every frame until OnExit.
    // Skip the update when the beam or muzzle is gone; there's nothing to move.
    [HarmonyPatch(typeof(SolusWingOverheatBeamBaseState), "UpdateBeamTransforms")]
    internal static class SolusWingBeamTransformsFix
    {
        private static readonly Func<SolusWingOverheatBeamBaseState, GameObject> getBeamVfxInstance =
            AccessTools.MethodDelegate<Func<SolusWingOverheatBeamBaseState, GameObject>>(
                AccessTools.PropertyGetter(typeof(SolusWingOverheatBeamBaseState), "beamVfxInstance"));

        private static readonly Func<SolusWingOverheatBeamBaseState, Transform> getMuzzleTransform =
            AccessTools.MethodDelegate<Func<SolusWingOverheatBeamBaseState, Transform>>(
                AccessTools.PropertyGetter(typeof(SolusWingOverheatBeamBaseState), "muzzleTransform"));

        private static bool Prefix(SolusWingOverheatBeamBaseState __instance)
        {
            return getBeamVfxInstance(__instance) && getMuzzleTransform(__instance);
        }
    }
}
