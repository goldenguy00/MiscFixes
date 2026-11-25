using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // GiveMoney checks master.inventory for the level-up unlock item without checking the inventory
    // exists, and mods (Life Savings, ...) call it on masters that are gone. Skip destroyed masters
    // and treat a missing inventory as not having the item, so the money is still given.
    [HarmonyPatch(typeof(CharacterMaster), nameof(CharacterMaster.GiveMoney), typeof(uint), typeof(bool))]
    internal static class GiveMoneyNoInventoryFix
    {
        private static bool Prefix(CharacterMaster __instance)
        {
            return __instance;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var original = AccessTools.Method(typeof(Inventory), nameof(Inventory.GetItemCountEffective), new[] { typeof(ItemDef) });

            bool patched = false;
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(original))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(GiveMoneyNoInventoryFix), nameof(SafeItemCount));
                    patched = true;
                }
                yield return instruction;
            }

            if (!patched)
                Log.Warning("Could not find Inventory.GetItemCountEffective call in CharacterMaster.GiveMoney");
        }

        private static int SafeItemCount(Inventory inventory, ItemDef itemDef)
        {
            return inventory ? inventory.GetItemCountEffective(itemDef) : 0;
        }
    }
}
