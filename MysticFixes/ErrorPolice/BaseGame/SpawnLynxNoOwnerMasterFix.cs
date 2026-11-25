using System.Collections.Generic;
using System.Reflection.Emit;
using EntityStates.TotemSpawnOrb;
using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // OnMasterSpawned registers each summoned Lynx as a deployable of the totem's owner, checking
    // the owner body but not its master. A body with no master (masterless spawns, or a master that
    // died first) throws there and the summon is never hooked up. Skip just the registration then;
    // the summon itself is still tracked and cleaned up by the state.
    [HarmonyPatch(typeof(SpawnLynx), "OnMasterSpawned")]
    internal static class SpawnLynxNoOwnerMasterFix
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var original = AccessTools.Method(typeof(CharacterMaster), nameof(CharacterMaster.AddDeployable));
            var replacement = AccessTools.Method(typeof(SpawnLynxNoOwnerMasterFix), nameof(SafeAddDeployable));

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
                Log.Warning("Could not find CharacterMaster.AddDeployable call in SpawnLynx.OnMasterSpawned");
        }

        private static void SafeAddDeployable(CharacterMaster master, Deployable deployable, DeployableSlot slot)
        {
            if (master)
                master.AddDeployable(deployable, slot);
        }
    }
}
