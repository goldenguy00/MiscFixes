using HarmonyLib;
using MSU;

namespace MiscFixes.Fixes.Compat
{
    [HarmonyPatch]
    public class MoonstormFix
    {
        [HarmonyPatch(typeof(VanillaSurvivorModule), nameof(VanillaSurvivorModule.AddProvider))]
        [HarmonyPrefix]
        public static bool VanillaSurvivorModule_AddProvider() => false;
    }
}
