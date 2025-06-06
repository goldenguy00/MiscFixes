using HarmonyLib;
using SS2.Modules;

namespace MiscFixes.Fixes.Compat
{
    [HarmonyPatch]
    public class StarstormFix
    {
        [HarmonyPatch(typeof(SkinSpecificOverrides), nameof(SkinSpecificOverrides.Initialize))]
        [HarmonyPrefix]
        public static bool SkinSpecificOverrides_Initialize() => false;
    }
}
