using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // TakeDamageProcess reads body.teamComponent.teamIndex for the victim and attacker without
    // checking either. A (usually modded) body without a TeamComponent throws there and the hit is
    // lost. Treat a missing team as TeamIndex.None, which the friendly-fire checks already handle.
    [HarmonyPatch(typeof(HealthComponent), "TakeDamageProcess")]
    internal static class TakeDamageNoTeamComponentFix
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var getTeamComponent = AccessTools.PropertyGetter(typeof(CharacterBody), nameof(CharacterBody.teamComponent));
            var getTeamIndex = AccessTools.PropertyGetter(typeof(TeamComponent), nameof(TeamComponent.teamIndex));

            var codes = new List<CodeInstruction>(instructions);
            int patched = 0;
            for (int i = 0; i < codes.Count - 1; i++)
            {
                if (!codes[i].Calls(getTeamComponent) || !codes[i + 1].Calls(getTeamIndex))
                    continue;

                codes[i].opcode = OpCodes.Call;
                codes[i].operand = AccessTools.Method(typeof(TakeDamageNoTeamComponentFix), nameof(TeamIndexOf));
                codes[i + 1].opcode = OpCodes.Nop;
                codes[i + 1].operand = null;
                patched++;
            }

            if (patched == 0)
                Log.Warning("Could not find body.teamComponent.teamIndex in HealthComponent.TakeDamageProcess");
            return codes;
        }

        private static TeamIndex TeamIndexOf(CharacterBody body)
        {
            return body && body.teamComponent ? body.teamComponent.teamIndex : TeamIndex.None;
        }
    }
}
