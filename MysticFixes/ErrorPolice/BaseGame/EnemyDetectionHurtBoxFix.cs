using HarmonyLib;
using RoR2;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // IsEnemyTeam goes from the hurtbox it touched to that hurtbox's CharacterBody without checking
    // either step. Some objects have hurtboxes with no HealthComponent (or one without a body), and
    // the trigger then throws on contact. Those can't be an enemy, so report them as not one.
    [HarmonyPatch(typeof(EnemyDetection), "IsEnemyTeam")]
    internal static class EnemyDetectionHurtBoxFix
    {
        private static bool Prefix(GameObject otherGameObject, ref bool __result)
        {
            if (!otherGameObject || !otherGameObject.TryGetComponent(out HurtBox hurtBox))
                return true;

            HealthComponent healthComponent = hurtBox.healthComponent;
            if (healthComponent && healthComponent.TryGetComponent(out CharacterBody body) && body.teamComponent)
                return true;

            __result = false;
            return false;
        }
    }
}
