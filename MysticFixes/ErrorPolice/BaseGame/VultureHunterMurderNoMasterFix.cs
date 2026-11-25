using System.Collections.Generic;
using System.Reflection.Emit;
using EntityStates.VultureHunter.Body;
using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // The Alloy Hunter's spawn kills its chosen enemy with enemyToKill.master.TrueKill(), guarded only
    // by a C# null check on the body. Masterless bodies (and bodies whose master is already gone)
    // throw there. Kill those through their health component instead.
    [HarmonyPatch(typeof(DoMurder), nameof(DoMurder.FixedUpdate))]
    internal static class VultureHunterMurderNoMasterFix
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var getMaster = AccessTools.PropertyGetter(typeof(CharacterBody), nameof(CharacterBody.master));
            var trueKill = AccessTools.Method(typeof(CharacterMaster), nameof(CharacterMaster.TrueKill), System.Type.EmptyTypes);
            var replacement = AccessTools.Method(typeof(VultureHunterMurderNoMasterFix), nameof(Kill));

            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count - 1; i++)
            {
                if (!codes[i].Calls(getMaster) || !codes[i + 1].Calls(trueKill))
                    continue;

                codes[i] = new CodeInstruction(OpCodes.Call, replacement).MoveLabelsFrom(codes[i]);
                codes[i + 1].opcode = OpCodes.Nop;
                codes[i + 1].operand = null;
                return codes;
            }

            Log.Warning("Could not find enemyToKill.master.TrueKill() in DoMurder.FixedUpdate");
            return codes;
        }

        private static void Kill(CharacterBody body)
        {
            if (!body)
                return;

            var master = body.master;
            if (master)
                master.TrueKill();
            else if (body.healthComponent)
                body.healthComponent.Suicide();
        }
    }
}
