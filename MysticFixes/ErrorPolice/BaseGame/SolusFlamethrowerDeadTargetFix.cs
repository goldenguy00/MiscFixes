using EntityStates.SolusAmalgamator;
using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // The Solus Amalgamator flamethrower turret aims at target.healthComponent.body every frame, but
    // the target hurtbox can outlive its body (target died mid-attack) and then it throws. Stop
    // turning until the attack ends.
    [HarmonyPatch(typeof(FlamethrowerTurret), "UpdateFlamethrowerRotation")]
    internal static class SolusFlamethrowerDeadTargetFix
    {
        private static bool Prefix(HurtBox ___target)
        {
            return !___target || (___target.healthComponent && ___target.healthComponent.body);
        }
    }
}
