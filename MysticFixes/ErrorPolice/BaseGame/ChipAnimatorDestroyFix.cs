using HarmonyLib;
using RoR2.SolusWeb;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // OnDestroy destroys every chip without checking it. The array is null if the animator is
    // destroyed before it set up its chips, and chips can already be gone during scene teardown.
    // Destroy only the chips that are still alive.
    [HarmonyPatch(typeof(ChipAnimator), "OnDestroy")]
    internal static class ChipAnimatorDestroyFix
    {
        private static bool Prefix(Transform[] ___chips)
        {
            if (___chips == null)
                return false;

            foreach (Transform chip in ___chips)
            {
                if (chip)
                    Object.Destroy(chip.gameObject);
            }
            return false;
        }
    }
}
