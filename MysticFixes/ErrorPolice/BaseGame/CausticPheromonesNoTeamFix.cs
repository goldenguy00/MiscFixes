using HarmonyLib;
using RoR2;
using RoR2.Audio;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // The aura only looks up its owner's TeamComponent in FixedUpdate, so an aura destroyed before its
    // first FixedUpdate (or whose owner has no team) throws in OnDestroy. Without a team there are no
    // allied AIs to retarget, so just play the destroy sound.
    [HarmonyPatch(typeof(CausticPheromonesAuraController), "OnDestroy")]
    internal static class CausticPheromonesNoTeamFix
    {
        private static bool Prefix(CausticPheromonesAuraController __instance, TeamComponent ___teamComponent, string ___destroySoundString)
        {
            if (___teamComponent)
                return true;

            PointSoundManager.EmitSoundLocal(___destroySoundString, __instance.transform.position);
            return false;
        }
    }
}
