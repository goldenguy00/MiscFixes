using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;

namespace MiscFixes
{
    [HarmonyPatch]
    public class fuck
    {
        [HarmonyPatch(typeof(MSU.VanillaSurvivorModule), nameof(MSU.VanillaSurvivorModule.AddProvider))]
        [HarmonyPrefix]
        public static bool fiuck() => false;
    }
}
