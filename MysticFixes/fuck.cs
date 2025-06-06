using HarmonyLib;

namespace MiscFixes
{
    [HarmonyPatch]
    public class fuck
    {
        [HarmonyPatch(typeof(MSU.VanillaSurvivorModule), nameof(MSU.VanillaSurvivorModule.AddProvider))]
        [HarmonyPrefix]
        public static bool fiuck() => false;
        [HarmonyPatch(typeof(SS2.Modules.SkinSpecificOverrides), nameof(SS2.Modules.SkinSpecificOverrides.Initialize))]
        [HarmonyPrefix]
        public static bool fwiuck() => false;
    }
}
