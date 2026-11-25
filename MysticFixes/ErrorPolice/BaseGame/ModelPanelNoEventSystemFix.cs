using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RoR2.UI;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // ModelPanel.Update reads its event system's input source before the null check that follows it,
    // so a model panel without an event system throws every frame. Treat a missing event system as
    // not using a gamepad, which skips the gamepad controls like the null check meant to.
    [HarmonyPatch(typeof(ModelPanel), nameof(ModelPanel.Update))]
    internal static class ModelPanelNoEventSystemFix
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var getInputSource = AccessTools.PropertyGetter(typeof(MPEventSystem), nameof(MPEventSystem.currentInputSource));
            var replacement = AccessTools.Method(typeof(ModelPanelNoEventSystemFix), nameof(GetInputSource));

            var codes = new List<CodeInstruction>(instructions);
            int patched = 0;
            foreach (var code in codes)
            {
                if (code.Calls(getInputSource))
                {
                    code.opcode = OpCodes.Call;
                    code.operand = replacement;
                    patched++;
                }
            }

            if (patched == 0)
                Log.Warning("Could not find eventSystem.currentInputSource in ModelPanel.Update");
            return codes;
        }

        private static MPEventSystem.InputSource GetInputSource(MPEventSystem eventSystem)
        {
            return eventSystem ? eventSystem.currentInputSource : MPEventSystem.InputSource.MouseAndKeyboard;
        }
    }
}
