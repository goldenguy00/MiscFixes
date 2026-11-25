using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // When a body with Aurelionite's Blessing takes damage, TakeDamageProcess drains gold from the
    // attacker's master. It never checks there is one, so attackerless damage (Shared Suffering,
    // DoTs whose owner died, environment) throws there. No attacker means no one to take gold from,
    // so treat the blessing as absent unless the attacker's master exists.
    [HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.TakeDamageProcess))]
    internal static class AurelioniteBlessingNoAttackerFix
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var blessingField = AccessTools.Field(typeof(DLC2Content.Buffs), nameof(DLC2Content.Buffs.AurelioniteBlessing));
            var hasBuff = AccessTools.Method(typeof(CharacterBody), nameof(CharacterBody.HasBuff), new[] { typeof(BuffDef) });
            var getMoney = AccessTools.PropertyGetter(typeof(CharacterMaster), nameof(CharacterMaster.money));

            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count - 1; i++)
            {
                if (!codes[i].LoadsField(blessingField) || !codes[i + 1].Calls(hasBuff))
                    continue;

                // The master being drained is the local loaded right before the first money read after the check.
                int moneyIndex = codes.FindIndex(i, c => c.Calls(getMoney));
                if (moneyIndex < 1 || !codes[moneyIndex - 1].IsLdloc())
                    break;

                var loadMaster = codes[moneyIndex - 1];
                codes.InsertRange(i + 2, new[]
                {
                    new CodeInstruction(loadMaster.opcode, loadMaster.operand),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AurelioniteBlessingNoAttackerFix), nameof(HasAttackerMaster))),
                });
                return codes;
            }

            Log.Warning("Could not find Aurelionite Blessing check in HealthComponent.TakeDamageProcess");
            return codes;
        }

        private static bool HasAttackerMaster(bool hasBlessing, CharacterMaster attackerMaster)
        {
            return hasBlessing && attackerMaster;
        }
    }
}
