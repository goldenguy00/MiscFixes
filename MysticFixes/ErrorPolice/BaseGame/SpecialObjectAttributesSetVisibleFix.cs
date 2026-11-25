using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // InternalSetVisible toggles every renderer, light and pickup display in its lists without
    // checking them, and some get destroyed while the object lives. Vehicle seats call it when
    // hiding/showing their passenger, so a throw there aborts the seat's enter/exit halfway: an
    // ejected passenger (e.g. from Rising Tides' Aquamarine bubble) is left with its collisions and
    // hurtboxes disabled. Drop dead entries first.
    [HarmonyPatch(typeof(SpecialObjectAttributes), nameof(SpecialObjectAttributes.InternalSetVisible))]
    internal static class SpecialObjectAttributesSetVisibleFix
    {
        private static void Prefix(SpecialObjectAttributes __instance)
        {
            __instance.renderersToDisable?.RemoveAll(o => !o);
            __instance.lightsToDisable?.RemoveAll(o => !o);
            __instance.pickupDisplaysToDisable?.RemoveAll(o => !o);
        }
    }
}
