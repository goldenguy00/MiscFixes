using System.Collections.Generic;
using EntityStates.VoidBarnacle;
using HarmonyLib;
using RoR2;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // NoCastSpawn.OnEnter only checks that the EntityStateGameObject wrapper is non-null,
    // not that it actually resolves to a prefab, so SimpleMuzzleFlash gets a null effect.
    // Swap that call for one that skips null prefabs.
    [HarmonyPatch(typeof(NoCastSpawn), nameof(NoCastSpawn.OnEnter))]
    internal static class VoidBarnacleSpawnFXFix
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var original = AccessTools.Method(typeof(EffectManager), nameof(EffectManager.SimpleMuzzleFlash));
            var replacement = AccessTools.Method(typeof(VoidBarnacleSpawnFXFix), nameof(SafeSimpleMuzzleFlash));

            bool patched = false;
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(original))
                {
                    instruction.operand = replacement;
                    patched = true;
                }
                yield return instruction;
            }

            if (!patched)
                Log.Warning("Could not find EffectManager.SimpleMuzzleFlash call in NoCastSpawn.OnEnter");
        }

        private static void SafeSimpleMuzzleFlash(GameObject effectPrefab, GameObject obj, string muzzleName, bool transmit)
        {
            if (effectPrefab)
                EffectManager.SimpleMuzzleFlash(effectPrefab, obj, muzzleName, transmit);
        }
    }
}
