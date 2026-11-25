using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // Inventory.OnDestroy returns its temp item storage to the pool, but bodies keep their reference
    // to the master's inventory. When the master goes first, the Duplicator item's OnDisable on the
    // dying body sets the decay duration on the destroyed inventory and throws on the released
    // storage. There's nothing left to update, so skip it.
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.SetItemDecayDurationServer))]
    internal static class TempItemDecayDestroyedInventoryFix
    {
        private static bool Prefix(Inventory __instance)
        {
            return __instance;
        }
    }
}
