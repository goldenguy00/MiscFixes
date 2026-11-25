using HarmonyLib;
using RoR2;
using RoR2.Navigation;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // Void Fields spawns each round's monsters on nodes around the active cell's "MonsterSpawn"
    // marker. On at least one cell that marker is right next to the cell, so monsters can spawn
    // inside the bubble you're defending. While a cell is active, treat nodes inside its holdout
    // radius as taken, so the director picks another node from the same range instead.
    [HarmonyPatch(typeof(DirectorCore), "CheckPositionFree")]
    internal static class VoidFieldsCellSpawnFix
    {
        private static void Postfix(NodeGraph nodeGraph, NodeGraph.NodeIndex nodeIndex, ref bool __result)
        {
            if (!__result || !ArenaMissionController.instance || !nodeGraph.GetNodePosition(nodeIndex, out Vector3 position))
                return;

            foreach (var holdoutZone in InstanceTracker.GetInstancesList<HoldoutZoneController>())
            {
                // currentRadius grows from 0 when the cell activates, so use the full radius from the start.
                float radius = Mathf.Max(holdoutZone.currentRadius, holdoutZone.baseRadius);
                if ((position - holdoutZone.transform.position).sqrMagnitude < radius * radius)
                {
                    __result = false;
                    return;
                }
            }
        }
    }
}
