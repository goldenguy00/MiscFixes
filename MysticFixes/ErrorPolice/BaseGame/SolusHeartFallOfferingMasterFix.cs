using System.Collections.Generic;
using EntityStates.SolusHeart.Death;
using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // The Solus Heart death sequence's Fall state (and its swarm variant) flies toward
    // manager.offeringMaster on the last tier, only checking that the mission controller exists.
    // If the offering master is gone it throws in OnEnter, so the fall never gets a target. Treat a
    // missing offering master like a missing controller, which falls to the nearest grid position.
    [HarmonyPatch(typeof(SolusHeartDeathSequence.Fall), nameof(SolusHeartDeathSequence.Fall.OnEnter))]
    internal static class SolusHeartFallOfferingMasterFix
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var managerField = AccessTools.Field(typeof(SolusHeartDeathSequence), "manager");
            var implicitBool = AccessTools.Method(typeof(UnityEngine.Object), "op_Implicit");

            // SolusWebMissionController solusWebMissionController = manager; ... && (bool)solusWebMissionController
            var codes = new List<CodeInstruction>(instructions);
            int managerIndex = codes.FindIndex(c => c.LoadsField(managerField));
            int implicitIndex = managerIndex < 0 ? -1 : codes.FindIndex(managerIndex, c => c.Calls(implicitBool));
            if (implicitIndex < 0)
            {
                Log.Warning("Could not find the mission controller check in SolusHeartDeathSequence.Fall.OnEnter");
                return codes;
            }

            codes[implicitIndex].operand = AccessTools.Method(typeof(SolusHeartFallOfferingMasterFix), nameof(HasOfferingMaster));
            return codes;
        }

        private static bool HasOfferingMaster(SolusWebMissionController manager)
        {
            return manager && manager.offeringMaster;
        }
    }
}
