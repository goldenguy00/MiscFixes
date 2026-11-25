using System.Collections.Generic;
using System.Reflection.Emit;
using EntityStates.VagrantMonster.Weapon;
using HarmonyLib;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // OnEnter only sets childLocator if the Vagrant still has a model, but FixedUpdate calls
    // childLocator.FindChild on every missile tick without checking it. Swap that call for one
    // that returns no muzzle when the locator is missing, which FixedUpdate already handles.
    [HarmonyPatch(typeof(JellyBarrage), nameof(JellyBarrage.FixedUpdate))]
    internal static class JellyBarrageChildLocatorFix
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var original = AccessTools.Method(typeof(ChildLocator), nameof(ChildLocator.FindChild), new[] { typeof(string) });
            var replacement = AccessTools.Method(typeof(JellyBarrageChildLocatorFix), nameof(SafeFindChild));

            bool patched = false;
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(original))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = replacement;
                    patched = true;
                }
                yield return instruction;
            }

            if (!patched)
                Log.Warning("Could not find ChildLocator.FindChild call in JellyBarrage.FixedUpdate");
        }

        private static Transform SafeFindChild(ChildLocator childLocator, string childName)
        {
            return childLocator ? childLocator.FindChild(childName) : null;
        }
    }
}
