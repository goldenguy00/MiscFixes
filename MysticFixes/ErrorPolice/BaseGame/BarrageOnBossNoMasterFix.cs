using System;
using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // War Bonds unsubscribes from its body's master last thing in OnDisable. A body whose master is
    // already gone (died first, or masterless) throws there. Everything else was already unhooked by
    // then and a missing master has no event left to unhook from, so drop just that exception.
    [HarmonyPatch(typeof(BarrageOnBossBehaviour), "OnDisable")]
    internal static class BarrageOnBossNoMasterFix
    {
        private static Exception Finalizer(Exception __exception, BarrageOnBossBehaviour __instance)
        {
            if (__exception is NullReferenceException && !(__instance.body && __instance.body.master))
                return null;
            return __exception;
        }
    }
}
