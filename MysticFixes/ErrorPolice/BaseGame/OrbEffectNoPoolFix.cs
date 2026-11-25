using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RoR2.Orbs;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // Same as TracerNoPoolFix: orb effects spawned outside the effect pool log "OrbEffect: No owning
    // pool set" when they arrive, then get destroyed anyway. The log sits in the middle of UpdateOrb,
    // so drop just that call and keep the destroy after it.
    [HarmonyPatch]
    internal static class OrbEffectNoPoolFix
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(OrbEffect), "OnAllEffectsOnArrivalCompleted");
            yield return AccessTools.Method(typeof(OrbEffect), nameof(OrbEffect.UpdateOrb));
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var logErrorFormat = AccessTools.Method(typeof(Debug), nameof(Debug.LogErrorFormat), new[] { typeof(string), typeof(object[]) });

            var codes = new List<CodeInstruction>(instructions);
            bool found = false;
            for (int i = 0; i < codes.Count; i++)
            {
                if (!codes[i].Calls(logErrorFormat))
                    continue;

                // Pop the format string and args instead of logging them.
                codes[i] = new CodeInstruction(OpCodes.Pop).MoveLabelsFrom(codes[i]);
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Pop));
                found = true;
            }

            if (!found)
                Log.Warning($"Could not find the owning pool error in OrbEffect.{original.Name}");
            return codes;
        }
    }
}
