using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EntityStates.Missions.Arena.NullWard;
using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // The active Void Fields cell pauses/unpauses the run stopwatch through Run.instance without
    // checking it. When the run ends while a cell is charging (or the scene is torn down with it),
    // the holdout zone keeps calling HandlePenalties every frame and OnExit throws when the state
    // machine is destroyed, skipping the rest of the cleanup. With no run there's no stopwatch to
    // pause, so skip just that call.
    [HarmonyPatch]
    internal static class NullWardNoRunFix
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Active), "HandlePenalties");
            yield return AccessTools.Method(typeof(Active), nameof(Active.OnExit));
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            var original = AccessTools.Method(typeof(Run), nameof(Run.SetForcePauseRunStopwatch));
            var replacement = AccessTools.Method(typeof(NullWardNoRunFix), nameof(SetForcePauseRunStopwatch));

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
                Log.Warning($"Could not find SetForcePauseRunStopwatch call in NullWard Active.{__originalMethod.Name}");
        }

        private static void SetForcePauseRunStopwatch(Run run, bool forcePauseState)
        {
            if (run)
                run.SetForcePauseRunStopwatch(forcePauseState);
        }
    }
}
