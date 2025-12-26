using HarmonyLib;
using SS2.Items;

namespace MiscFixes.ErrorPolice.Harmony
{
    [HarmonyPatch]
    internal class StarstormFix
    {
        [HarmonyPatch(typeof(SantaHat), nameof(SantaHat.IsAvailable))]
        [HarmonyPrefix]
        private static bool FuckYou() => false;
    }
}
