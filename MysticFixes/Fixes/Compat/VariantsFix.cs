using HarmonyLib;
using MiscFixes.Modules;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;
using VAPI.Components;

namespace MiscFixes.Fixes.Compat
{
    [HarmonyPatch]
    internal class VariantsFix
    {
        [HarmonyPatch(typeof(BodyVariantManager), nameof(BodyVariantManager.ApplyVisuals), MethodType.Enumerator)]
        [HarmonyILManipulator]
        public static void BodyVariantManager_ApplyVisuals(ILContext il)
        {
            var c = new ILCursor(il);

            if (!c.TryGotoNext(MoveType.Before,
                    x => x.MatchNewobj(out _)
                ))
            {
                Log.PatchFail(il);
            }

            c.Emit(OpCodes.Ldc_R4, 0.1f);
            c.Next.Operand = AccessTools.Constructor(typeof(WaitForSeconds), [typeof(float)]);
        }
    }
}
