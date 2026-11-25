using System.Collections.Generic;
using System.Reflection.Emit;
using EntityStates.Bell.BellWeapon;
using HarmonyLib;
using RoR2;
using RoR2.CharacterAI;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // Brass Contraption's trio bomb checks whether its AI's current enemy is a player for the
    // off-screen miss, reading master.GetComponent and currentEnemy.characterBody.teamComponent
    // without checks. A masterless bell, or an enemy without a body, throws and the bomb never fires.
    // Treat those as "no AI" and "not a player".
    [HarmonyPatch(typeof(ChargeTrioBomb), nameof(ChargeTrioBomb.FixedUpdate))]
    internal static class BellTrioBombNoEnemyBodyFix
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var getMaster = AccessTools.PropertyGetter(typeof(CharacterBody), nameof(CharacterBody.master));
            var getAI = AccessTools.Method(typeof(UnityEngine.Component), nameof(UnityEngine.Component.GetComponent), System.Type.EmptyTypes, new[] { typeof(BaseAI) });
            var getTeamComponent = AccessTools.PropertyGetter(typeof(CharacterBody), nameof(CharacterBody.teamComponent));
            var getTeamIndex = AccessTools.PropertyGetter(typeof(TeamComponent), nameof(TeamComponent.teamIndex));

            var codes = new List<CodeInstruction>(instructions);
            bool aiPatched = false, teamPatched = false;
            for (int i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].Calls(getMaster) && codes[i + 1].Calls(getAI))
                {
                    Replace(codes, i, nameof(AIOf));
                    aiPatched = true;
                }
                else if (codes[i].Calls(getTeamComponent) && codes[i + 1].Calls(getTeamIndex))
                {
                    Replace(codes, i, nameof(TeamIndexOf));
                    teamPatched = true;
                }
            }

            if (!aiPatched || !teamPatched)
                Log.Warning("Could not find the off-screen miss check in ChargeTrioBomb.FixedUpdate");
            return codes;
        }

        private static void Replace(List<CodeInstruction> codes, int i, string helper)
        {
            codes[i].opcode = OpCodes.Call;
            codes[i].operand = AccessTools.Method(typeof(BellTrioBombNoEnemyBodyFix), helper);
            codes[i + 1].opcode = OpCodes.Nop;
            codes[i + 1].operand = null;
        }

        private static BaseAI AIOf(CharacterBody body)
        {
            return body && body.master ? body.master.GetComponent<BaseAI>() : null;
        }

        private static TeamIndex TeamIndexOf(CharacterBody body)
        {
            return body && body.teamComponent ? body.teamComponent.teamIndex : TeamIndex.None;
        }
    }
}
