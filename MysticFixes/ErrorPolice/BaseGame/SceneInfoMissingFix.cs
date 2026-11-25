using EntityStates.AI;
using HarmonyLib;
using RoR2;
using RoR2.Items;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // Bodies and AI that start during a scene transition (e.g. minions carried over by mods) run
    // before the new scene's SceneInfo exists, and these read it unchecked.

    // Spare Drone Parts' leash only checks for Meridian here, and there's no Meridian event mid-transition.
    [HarmonyPatch(typeof(MinionLeashBodyBehavior), "OnEnable")]
    internal static class MinionLeashNoSceneInfoFix
    {
        private static bool Prefix()
        {
            var sceneInfo = SceneInfo.instance;
            return sceneInfo && sceneInfo.sceneDef;
        }
    }

    // No node graph to wander on yet; null is the method's own "no position" result.
    [HarmonyPatch(typeof(BaseAIState), "PickRandomNearbyReachablePosition")]
    internal static class AIWanderNoSceneInfoFix
    {
        private static bool Prefix(ref Vector3? __result)
        {
            if (SceneInfo.instance)
                return true;

            __result = null;
            return false;
        }
    }
}
