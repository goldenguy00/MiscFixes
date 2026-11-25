using System;
using HarmonyLib;
using RoR2.Projectile;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // growthColorRenderers can contain renderers that were already destroyed
    // (e.g. the Dream Demon dying and destroying the projectile), which makes
    // Renderer.GetPropertyBlock throw. Drop dead entries before the loop runs.
    [HarmonyPatch(typeof(DreamDemonProjectileController), "SetGrowthLevel")]
    internal static class DreamDemonProjectileFix
    {
        private static void Prefix(ref Renderer[] ___growthColorRenderers)
        {
            if (___growthColorRenderers == null)
            {
                ___growthColorRenderers = Array.Empty<Renderer>();
                return;
            }

            foreach (var renderer in ___growthColorRenderers)
            {
                if (!renderer)
                {
                    ___growthColorRenderers = Array.FindAll(___growthColorRenderers, r => r);
                    return;
                }
            }
        }
    }
}
