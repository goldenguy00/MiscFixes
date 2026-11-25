using HarmonyLib;
using RoR2;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // Tracers spawned outside the effect pool (e.g. instantiated directly by mods) still carry an
    // EffectManagerHelper, just with no owning pool. Arrive then logs "Tracer: No owning pool set"
    // and destroys the tracer, which is the right thing to do anyway. Destroy it without the error.
    [HarmonyPatch(typeof(Tracer), "Arrive")]
    internal static class TracerNoPoolFix
    {
        private static bool Prefix(Tracer __instance, EffectManagerHelper ____effectManagerHelper)
        {
            if (!EffectManager.UsePools || ____effectManagerHelper == null || ____effectManagerHelper.OwningPool != null)
                return true;

            Object.Destroy(__instance.gameObject);
            return false;
        }
    }
}
