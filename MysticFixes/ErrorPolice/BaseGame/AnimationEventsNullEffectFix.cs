using HarmonyLib;
using RoR2;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // CreateEffect spawns whatever object the animation event references, without checking that one
    // is set. Animations with an empty effect slot (seen in Starstorm 2's Stranger's Hideout) then make
    // SpawnEffect log an error every time the event fires. No effect means nothing to spawn, so skip it.
    [HarmonyPatch(typeof(AnimationEvents), nameof(AnimationEvents.CreateEffect))]
    internal static class AnimationEventsNullEffectFix
    {
        private static bool Prefix(AnimationEvent animationEvent)
        {
            return animationEvent != null && animationEvent.objectReferenceParameter is GameObject effectPrefab && effectPrefab;
        }
    }
}
