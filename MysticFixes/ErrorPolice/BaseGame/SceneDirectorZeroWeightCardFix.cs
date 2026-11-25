using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // SelectCard copies every affordable card into a selection and evaluates it. When the affordable
    // cards all have a weight of 0 (mods use calcCardWeight or a 0 selection weight to disable cards),
    // WeightedSelection.EvaluateToChoiceIndex falls through and returns -1, so Evaluate throws an
    // IndexOutOfRangeException and the rest of the scene's interactables never spawn. A weight of 0
    // means never pick it, so leave those cards out; with nothing left SelectCard returns null, which
    // the scene population already treats as "nothing affordable". Evaluate can also return -1 from
    // float rounding on the last card, so fall back to the last choice there.
    [HarmonyPatch(typeof(SceneDirector), "SelectCard")]
    internal static class SceneDirectorZeroWeightCardFix
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var addChoice = AccessTools.Method(typeof(WeightedSelection<DirectorCard>), nameof(WeightedSelection<DirectorCard>.AddChoice), new[] { typeof(WeightedSelection<DirectorCard>.ChoiceInfo) });
            var evaluate = AccessTools.Method(typeof(WeightedSelection<DirectorCard>), nameof(WeightedSelection<DirectorCard>.Evaluate));

            bool patchedAdd = false, patchedEvaluate = false;
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(addChoice))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(SceneDirectorZeroWeightCardFix), nameof(AddWeightedChoice));
                    patchedAdd = true;
                }
                else if (instruction.Calls(evaluate))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(SceneDirectorZeroWeightCardFix), nameof(Evaluate));
                    patchedEvaluate = true;
                }
                yield return instruction;
            }

            if (!patchedAdd || !patchedEvaluate)
                Log.Warning("Could not find AddChoice/Evaluate calls in SceneDirector.SelectCard");
        }

        private static void AddWeightedChoice(WeightedSelection<DirectorCard> selection, WeightedSelection<DirectorCard>.ChoiceInfo choice)
        {
            if (choice.weight > 0f)
                selection.AddChoice(choice);
        }

        private static DirectorCard Evaluate(WeightedSelection<DirectorCard> selection, float normalizedIndex)
        {
            int index = selection.EvaluateToChoiceIndex(normalizedIndex);
            if (index < 0)
                index = selection.Count - 1;
            return selection.GetChoice(index).value;
        }
    }
}
