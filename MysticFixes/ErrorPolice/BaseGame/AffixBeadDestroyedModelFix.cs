using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RoR2;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // The Bead elite crystal is parented to the body's model. When that model is destroyed while the
    // attachment lives on, the crystal goes with it and Update and the hit counter throw every frame
    // and every hit. There is nothing left to show or fire from, so stop until the attachment goes.
    [HarmonyPatch]
    internal static class AffixBeadDestroyedModelFix
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(AffixBeadAttachment), "Update");
            yield return AccessTools.Method(typeof(AffixBeadAttachment), "HandleCountingIncomingDamage");
        }

        // A real null is before Initialize, which vanilla handles; only a destroyed one is a problem.
        private static bool Prefix(GameObject ___modelObject)
        {
            return ReferenceEquals(___modelObject, null) || ___modelObject;
        }
    }
}
