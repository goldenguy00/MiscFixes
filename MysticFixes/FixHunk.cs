using HarmonyLib;
using HunkMod.Modules.Survivors;
using HunkMod.SkillStates.Hunk.Counter;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Tyranitar.Modules.Components;
using UnityEngine;

namespace MiscFixes
{
    [HarmonyPatch]
    public class FixHunk
    {
        [HarmonyPatch(typeof(Hunk), "TVirusDeathDefied")]
        [HarmonyILManipulator]
        public static void Tvirus(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(x => x.MatchLdcI4(-1)))
            {
                c.Remove();
                c.Emit(OpCodes.Ldc_I4_0);
            }
            else Debug.LogError($"IL hook failed for Hunk.TVirusDeathDefied");
        }

        [HarmonyPatch(typeof(UroLunge), "OnEnter")]
        [HarmonyILManipulator]
        public static void Uro(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(x => x.MatchLdstr("Aim")))
            {
                c.Next.Operand = "Slide";
            }
            else Debug.LogError($"IL hook failed for UroLunge.OnEnter");
        }
    }

    [HarmonyPatch]
    public class FixRocks
    {
        [HarmonyPatch(typeof(KingsRockBehavior), "KillAllRocks")]
        [HarmonyPrefix]
        public static bool Prefix(KingsRockBehavior __instance)
        {
            __instance.activeRocks = 0;
            if (__instance.rocks is not null)
            {
                for (int i = 0; i < __instance.rocks.Length; i++)
                {
                    var rock = __instance.rocks[i].rock;
                    if (rock && rock.activeSelf)
                    {
                        rock.SetActive(value: false);
                    }
                }
            }
            return false;
        }
    }
}
