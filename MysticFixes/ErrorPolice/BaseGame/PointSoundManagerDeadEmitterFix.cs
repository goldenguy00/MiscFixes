using System.Collections.Generic;
using HarmonyLib;
using RoR2.Audio;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // Pooled sound emitters live in the scene, so they're destroyed with it, but the pool is only
    // cleared once sceneUnloaded fires. Sounds played in between (PlaySoundOnEvent on destroy, e.g.
    // objects torn down with the scene) pop a destroyed emitter and throw. Skip the dead ones so a
    // fresh emitter gets made.
    [HarmonyPatch(typeof(PointSoundManager), "RequestEmitter")]
    internal static class PointSoundManagerDeadEmitterFix
    {
        private static readonly Stack<AkGameObj> emitterPool =
            AccessTools.StaticFieldRefAccess<Stack<AkGameObj>>(typeof(PointSoundManager), "emitterPool");

        private static void Prefix()
        {
            while (emitterPool.Count > 0 && !emitterPool.Peek())
                emitterPool.Pop();
        }
    }
}
