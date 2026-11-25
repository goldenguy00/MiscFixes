using System.Collections.Generic;
using HarmonyLib;
using RoR2;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // Start calls GetComponents on every collisionToDisable entry and transform on every
    // objectsToDetach entry, but some prefabs have missing/destroyed objects in those lists.
    // Drop dead entries before Start iterates them.
    [HarmonyPatch(typeof(SpecialObjectAttributes), "Start")]
    internal static class SpecialObjectAttributesStartFix
    {
        private static void Prefix(List<GameObject> ___collisionToDisable, List<GameObject> ___objectsToDetach)
        {
            ___collisionToDisable?.RemoveAll(o => !o);
            ___objectsToDetach?.RemoveAll(o => !o);
        }
    }
}
