using HarmonyLib;
using RoR2;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // States like DroneCopycat.FireLaserDisc null-check their EntityStateGameObject wrapper, not the
    // GameObject inside it, so an unassigned muzzle flash still reaches SimpleMuzzleFlash as null.
    // SpawnEffect then logs an error on every shot. No prefab means no flash, so just skip it.
    [HarmonyPatch(typeof(EffectManager), nameof(EffectManager.SimpleMuzzleFlash))]
    internal static class SimpleMuzzleFlashNullPrefabFix
    {
        private static bool Prefix(GameObject effectPrefab)
        {
            return effectPrefab;
        }
    }
}
