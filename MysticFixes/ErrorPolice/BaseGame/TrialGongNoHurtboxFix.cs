using System;
using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // OnIncomingDamageServer uses damageInfo.inflictedHurtbox to work out the push direction, but
    // damage that didn't come through a hurtbox (DoTs, mostly) has none. Fall back to the gong's own
    // main hurtbox for the duration of the call so the hit still counts, then put the original back.
    [HarmonyPatch(typeof(TrialGongInteraction), nameof(TrialGongInteraction.OnIncomingDamageServer))]
    internal static class TrialGongNoHurtboxFix
    {
        private static void Prefix(TrialGongInteraction __instance, DamageInfo damageInfo, out bool __state)
        {
            __state = !damageInfo.inflictedHurtbox && __instance.mainHurtBox;
            if (__state)
                damageInfo.inflictedHurtbox = __instance.mainHurtBox;
        }

        private static Exception Finalizer(DamageInfo damageInfo, bool __state, Exception __exception)
        {
            if (__state)
                damageInfo.inflictedHurtbox = null;
            return __exception;
        }
    }
}
