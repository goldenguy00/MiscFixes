using System;
using EntityStates;
using HarmonyLib;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // BaseCharacterMain decides once on enter whether the body has an animator, then sets animator
    // parameters every frame. When the model (or its animator) is destroyed later the state keeps
    // calling into it and throws every frame. Skip the update while the animator is gone.
    [HarmonyPatch(typeof(BaseCharacterMain), "UpdateAnimationParameters")]
    internal static class CharacterMainMissingAnimatorFix
    {
        internal static readonly Func<BaseCharacterMain, Animator> GetModelAnimator =
            AccessTools.MethodDelegate<Func<BaseCharacterMain, Animator>>(
                AccessTools.PropertyGetter(typeof(BaseCharacterMain), "modelAnimator"));

        private static bool Prefix(BaseCharacterMain __instance, bool ___hasModelAnimator)
        {
            return !___hasModelAnimator || GetModelAnimator(__instance);
        }
    }

    // FixedUpdate plays the landing impact on the same cached flag, so clear it once the animator is gone.
    [HarmonyPatch(typeof(BaseCharacterMain), nameof(BaseCharacterMain.FixedUpdate))]
    internal static class CharacterMainMissingAnimatorFixedUpdateFix
    {
        private static void Prefix(BaseCharacterMain __instance, ref bool ___hasModelAnimator)
        {
            if (___hasModelAnimator && !CharacterMainMissingAnimatorFix.GetModelAnimator(__instance))
                ___hasModelAnimator = false;
        }
    }
}
