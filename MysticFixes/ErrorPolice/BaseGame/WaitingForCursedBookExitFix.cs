using System.Collections.Generic;
using System.Reflection.Emit;
using EntityStates.Wake;
using HarmonyLib;
using RoR2;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // OnExit hides the cursed book objective through WakeController.instance, but when the state
    // machine is destroyed along with the scene the controller can already be gone. Skip hiding the
    // objective in that case; the event unsubscribe before it and base.OnExit after still run.
    [HarmonyPatch(typeof(WaitingForCursedBook), nameof(WaitingForCursedBook.OnExit))]
    internal static class WaitingForCursedBookExitFix
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var objectiveField = AccessTools.Field(typeof(WakeController), nameof(WakeController.getCursedBookObjective));
            var setEnabled = AccessTools.PropertySetter(typeof(Behaviour), nameof(Behaviour.enabled));

            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (!codes[i].LoadsField(objectiveField))
                    continue;

                int setIndex = codes.FindIndex(i, c => c.Calls(setEnabled));
                if (setIndex < 0)
                    break;

                // Stack holds the controller; GetObjective(controller) and SetEnabled(objective, false) keep it balanced.
                codes[i] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WaitingForCursedBookExitFix), nameof(GetObjective))).MoveLabelsFrom(codes[i]);
                codes[setIndex] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WaitingForCursedBookExitFix), nameof(SetEnabled))).MoveLabelsFrom(codes[setIndex]);
                return codes;
            }

            Log.Warning("Could not find getCursedBookObjective access in WaitingForCursedBook.OnExit");
            return codes;
        }

        private static GenericObjectiveProvider GetObjective(WakeController wakeController)
        {
            return wakeController ? wakeController.getCursedBookObjective : null;
        }

        private static void SetEnabled(Behaviour behaviour, bool enabled)
        {
            if (behaviour)
                behaviour.enabled = enabled;
        }
    }
}
