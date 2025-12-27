using HarmonyLib;
using SS2.Items;

namespace MiscFixes.ErrorPolice
{
    [HarmonyPatch]
    internal class FixStarstorm
    {
        [HarmonyPatch(typeof(SantaHat), nameof(SantaHat.IsAvailable))]
        [HarmonyPrefix]
        private static bool FuckYou() => false;
    }
}
