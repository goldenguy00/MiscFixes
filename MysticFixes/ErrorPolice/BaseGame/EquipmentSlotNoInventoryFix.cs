using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // EquipmentSlot.MyFixedUpdate handles an equipment request by first checking the inventory's
    // equipment set count, but that inventory is null for a body whose master is gone (e.g. after the
    // run ends). With auto-cast or a pending request it throws every frame. Count a missing inventory
    // as one set; the rest of the method already handles it (no equipment, no stock).
    [HarmonyPatch(typeof(EquipmentSlot), "MyFixedUpdate")]
    internal static class EquipmentSlotNoInventoryFix
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var getSetCount = AccessTools.PropertyGetter(typeof(Inventory), nameof(Inventory.accessibleEquipmentSetCount));
            var replacement = AccessTools.Method(typeof(EquipmentSlotNoInventoryFix), nameof(GetSetCount));

            var codes = new List<CodeInstruction>(instructions);
            foreach (var code in codes)
            {
                if (!code.Calls(getSetCount))
                    continue;

                code.opcode = OpCodes.Call;
                code.operand = replacement;
                return codes;
            }

            Log.Warning("Could not find accessibleEquipmentSetCount in EquipmentSlot.MyFixedUpdate");
            return codes;
        }

        private static byte GetSetCount(Inventory inventory)
        {
            return inventory ? inventory.accessibleEquipmentSetCount : (byte)1;
        }
    }
}
