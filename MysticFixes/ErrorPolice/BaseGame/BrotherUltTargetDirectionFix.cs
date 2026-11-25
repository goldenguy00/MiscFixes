using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // EvaluatePattern null checks the owner's characterDirection but not the target's. Mithrix's
    // phase 2 death pizzas target whoever killed the add, and flying allies like drones have no
    // CharacterDirection, so it throws and the pizza never fires. Fall back to the body's transform.
    [HarmonyPatch(typeof(BrotherUltUtils), nameof(BrotherUltUtils.EvaluatePattern))]
    internal static class BrotherUltTargetDirectionFix
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var getDirection = AccessTools.PropertyGetter(typeof(CharacterBody), nameof(CharacterBody.characterDirection));
            var getForward = AccessTools.PropertyGetter(typeof(CharacterDirection), nameof(CharacterDirection.forward));
            var replacement = AccessTools.Method(typeof(BrotherUltTargetDirectionFix), nameof(GetForward));

            var codes = new List<CodeInstruction>(instructions);
            bool patched = false;
            for (int i = 0; i < codes.Count - 1; i++)
            {
                if (!codes[i].Calls(getDirection) || !codes[i + 1].Calls(getForward))
                    continue;

                codes[i].opcode = OpCodes.Call;
                codes[i].operand = replacement;
                codes[i + 1].opcode = OpCodes.Nop;
                codes[i + 1].operand = null;
                patched = true;
            }

            if (!patched)
                Log.Warning("Could not find characterDirection.forward in BrotherUltUtils.EvaluatePattern");
            return codes;
        }

        private static UnityEngine.Vector3 GetForward(CharacterBody body)
        {
            return body.characterDirection ? body.characterDirection.forward : body.transform.forward;
        }
    }
}
