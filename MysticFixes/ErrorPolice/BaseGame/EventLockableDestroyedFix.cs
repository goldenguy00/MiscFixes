using HarmonyLib;
using RoR2;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // OutsideInteractableLocker remembers every interactable it locked and unlocks them all when it's
    // disabled. If one was destroyed in the meantime, GetGameObject throws on it and the rest of the
    // list never gets unlocked. A destroyed interactable has no object, which UnlockAll handles.
    [HarmonyPatch(typeof(EventLockableInteractable), nameof(EventLockableInteractable.GetGameObject))]
    internal static class EventLockableDestroyedFix
    {
        private static bool Prefix(EventLockableInteractable __instance, ref GameObject __result)
        {
            if (__instance)
                return true;

            __result = null;
            return false;
        }
    }
}
