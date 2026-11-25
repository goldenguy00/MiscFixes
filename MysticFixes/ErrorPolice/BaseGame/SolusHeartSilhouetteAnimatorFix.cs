using System.Collections.Generic;
using System.Reflection;
using EntityStates.SolusHeart;
using HarmonyLib;
using RoR2.SolusWeb;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // Solus Heart attack states cache chipAnimator.targetSilhouette as currentSilhouette on enter and
    // then read currentSilhouette.GetComponent<Animator>() without checking it. When the chip animator
    // has no target silhouette that throws, every tick for the teleport state and on every attack for
    // the others. The code after it already handles a missing Animator, so return null instead.
    [HarmonyPatch]
    internal static class SolusHeartSilhouetteAnimatorFix
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(TeleportBaseState), nameof(TeleportBaseState.FixedUpdate));
            yield return AccessTools.Method(typeof(DDOS), nameof(DDOS.OnEnter));
            yield return AccessTools.Method(typeof(FireOrbitalStrike), nameof(FireOrbitalStrike.OnEnter));
            yield return AccessTools.Method(typeof(GroundSlamState), nameof(GroundSlamState.OnEnter));
            yield return AccessTools.Method(typeof(Underclock), nameof(Underclock.OnEnter));
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var getCurrentSilhouette = AccessTools.PropertyGetter(typeof(SolusHeartBossBaseState), "currentSilhouette");
            var getAnimator = AccessTools.Method(typeof(Component), nameof(Component.GetComponent), System.Type.EmptyTypes, new[] { typeof(Animator) });
            var replacement = AccessTools.Method(typeof(SolusHeartSilhouetteAnimatorFix), nameof(GetAnimator));

            var codes = new List<CodeInstruction>(instructions);
            int patched = 0;
            for (int i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].Calls(getCurrentSilhouette) && codes[i + 1].Calls(getAnimator))
                {
                    codes[i + 1].opcode = System.Reflection.Emit.OpCodes.Call;
                    codes[i + 1].operand = replacement;
                    patched++;
                }
            }

            if (patched == 0)
                Log.Warning($"Could not find currentSilhouette.GetComponent<Animator>() in {original.DeclaringType.Name}.{original.Name}");
            return codes;
        }

        private static Animator GetAnimator(ChipSilhouette silhouette)
        {
            return silhouette ? silhouette.GetComponent<Animator>() : null;
        }
    }
}
