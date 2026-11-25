using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // VultureFightOverride.Start seeds its RNG from Run.stageRng, which only exists on the server, so
    // it throws on clients. The RNG is only used by server-side boss spawning.
    [HarmonyPatch(typeof(VultureFightOverride), nameof(VultureFightOverride.Start))]
    internal static class VultureFightOverrideClientRngFix
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            return ServerRng.Transpile(instructions, __originalMethod);
        }
    }
}
