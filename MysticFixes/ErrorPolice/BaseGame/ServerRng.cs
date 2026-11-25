using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // Run's RNGs (runRNG, stageRng, treasureRng...) are only created on the server, so on clients
    // they're null. Code that seeds its own RNG from them in Start/onRunStart then throws on clients,
    // skipping the rest of its setup. Transpilers use this to hand those reads a throwaway RNG when
    // the real one is missing; whatever is rolled with it client-side is discarded by the server anyway.
    internal static class ServerRng
    {
        internal static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var orDummy = AccessTools.Method(typeof(ServerRng), nameof(OrDummy));

            int patched = 0;
            foreach (var code in instructions)
            {
                yield return code;

                if (code.opcode == OpCodes.Ldfld && code.operand is FieldInfo field && field.DeclaringType == typeof(Run) && field.FieldType == typeof(Xoroshiro128Plus))
                {
                    yield return new CodeInstruction(OpCodes.Call, orDummy);
                    patched++;
                }
            }

            if (patched == 0)
                Log.Warning($"Could not find a Run RNG read in {original.DeclaringType?.FullName}.{original.Name}");
        }

        private static Xoroshiro128Plus OrDummy(Xoroshiro128Plus rng)
        {
            return rng ?? new Xoroshiro128Plus(0UL);
        }
    }
}
