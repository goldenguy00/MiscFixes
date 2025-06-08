using HarmonyLib;

namespace MiscFixes.Fixes.Compat
{
    [HarmonyPatch]
    public class UnfuckLobbySkins
    {
        [HarmonyPatch(typeof(LobbySkinsFix.LobbySkinsFixPlugin), "Awake")]
        [HarmonyPrefix]
        public static bool LobbySkinsFixPlugin_Awake() => false;

        [HarmonyPatch(typeof(LobbySkinsFix.LobbySkinsFixPlugin), "Destroy")]
        [HarmonyPrefix]
        public static bool LobbySkinsFixPlugin_Destroy() => false;
    }
}
