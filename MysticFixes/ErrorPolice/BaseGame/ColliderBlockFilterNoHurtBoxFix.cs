using HarmonyLib;
using RoR2;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // Shields with better blocking (e.g. a turret's shield sphere) compare their own raycast against
    // one to the hit hurtbox, but bullet hits on entities can come without a hurtbox (or with one whose
    // collider is gone), and that throws for the whole bullet. With nothing to compare against, the
    // shield blocks the hit if it's in the way of it.
    [HarmonyPatch(typeof(HurtBox), nameof(HurtBox.SimpleColliderBlockFilter))]
    internal static class ColliderBlockFilterNoHurtBoxFix
    {
        private static bool Prefix(HurtBox.HurtBoxAttackHitProcessorArgs damageProcessorArgs, HurtBox processorHurtBox, ref bool __result)
        {
            var hitHurtBox = damageProcessorArgs.hitHurtBox;
            if (hitHurtBox && hitHurtBox.collider)
                return true;

            Vector3 origin = damageProcessorArgs.attackOrigin;
            Vector3 direction = damageProcessorArgs.hitPosition - origin;
            __result = processorHurtBox.collider && processorHurtBox.collider.Raycast(new Ray(origin, direction), out _, direction.magnitude + 0.001f);
            return false;
        }
    }
}
