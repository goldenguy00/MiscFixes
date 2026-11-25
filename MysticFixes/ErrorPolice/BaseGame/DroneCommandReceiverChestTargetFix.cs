using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RoR2;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // When a chest is found in the sphere cast, DoTargetInfoSearch uses the hit returned by
    // FilterHits (hitInfo.collider.transform.position) instead of the chest hit it just matched.
    // FilterHits can come back with no hit at all, leaving hitInfo.collider null.
    // Fall back to the chest's own position in that case.
    [HarmonyPatch(typeof(DroneCommandReceiver), "DoTargetInfoSearch")]
    internal static class DroneCommandReceiverChestTargetFix
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var getCollider = AccessTools.PropertyGetter(typeof(RaycastHit), nameof(RaycastHit.collider));
            var getTransform = AccessTools.PropertyGetter(typeof(Component), nameof(Component.transform));
            var getPosition = AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.position));
            var getGameObject = AccessTools.PropertyGetter(typeof(Component), nameof(Component.gameObject));
            var replacement = AccessTools.Method(typeof(DroneCommandReceiverChestTargetFix), nameof(GetChestPosition));

            var codes = new List<CodeInstruction>(instructions);
            for (int i = 1; i < codes.Count - 2; i++)
            {
                if (!codes[i].Calls(getCollider) || !codes[i + 1].Calls(getTransform) || !codes[i + 2].Calls(getPosition))
                    continue;

                // The chest local is what was loaded for "gameObject = chestBehavior.gameObject" just before.
                CodeInstruction loadChest = null;
                for (int j = i - 1; j > 0; j--)
                {
                    if (codes[j].Calls(getGameObject))
                    {
                        loadChest = codes[j - 1];
                        break;
                    }
                }

                if (loadChest == null || !loadChest.IsLdloc())
                    break;

                // Stack already holds &hitInfo; replace collider.transform.position with GetChestPosition(ref hitInfo, chest).
                codes[i] = new CodeInstruction(loadChest.opcode, loadChest.operand).MoveLabelsFrom(codes[i]);
                codes[i + 1] = new CodeInstruction(OpCodes.Call, replacement);
                codes.RemoveAt(i + 2);
                return codes;
            }

            Log.Warning("Could not find chest target position in DroneCommandReceiver.DoTargetInfoSearch");
            return codes;
        }

        private static Vector3 GetChestPosition(ref RaycastHit hitInfo, Component chest)
        {
            var collider = hitInfo.collider;
            if (collider)
                return collider.transform.position;

            return chest ? chest.transform.position : Vector3.zero;
        }
    }
}
