using HarmonyLib;
using RoR2;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // rb is only assigned in Start, but OnCollisionEnter can fire before Start runs.
    [HarmonyPatch(typeof(RigidbodySoundOnImpact), "OnCollisionEnter")]
    internal static class RigidbodySoundOnImpactFix
    {
        private static bool Prefix(RigidbodySoundOnImpact __instance, ref Rigidbody ___rb)
        {
            if (!___rb)
                ___rb = __instance.GetComponent<Rigidbody>();

            return ___rb;
        }
    }
}
