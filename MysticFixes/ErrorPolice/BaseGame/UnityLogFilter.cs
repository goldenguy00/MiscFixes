using System;
using HarmonyLib;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // Some messages are harmless but logged constantly, burying real errors. Every Unity log (managed
    // and native) reaches listeners like BepInEx's console through Application.CallLogCallback, so
    // drop the known-noisy ones there. Unity's own Player.log is unaffected.
    [HarmonyPatch(typeof(Application), "CallLogCallback")]
    internal static class UnityLogFilter
    {
        // Messages that start with the first string and end with the second ("" matches any ending).
        private static readonly (string start, string end)[] filtered =
        {
            // Effect pools are released on scene change while effects are still alive; the game cleans them up anyway.
            ("GenericPool: ReleasePools called on non-empty pool", ""),
            // Native particle warning for effects on meshes that are scaled to zero or have no faces.
            ("Particle System is trying to spawn on a mesh with zero surface area", ""),
            // Items and tiers (usually from disabled mod content) the game fixes up on first access, as the message says.
            ("ItemDef '", "has an item index of 'None'.  Attempting to fix..."),
            ("ItemTierDef '", "has a tier of 'AssignedAtRuntime'.  Attempting to fix..."),
            // Item behaviors of disabled items (and a vanilla one) at startup; they just never get added.
            ("", ".GetItemDef returned null."),
            // Content fields for vanilla cut content, logged once at startup.
            ("PopulateTypeFields issues found within RoR2.", ""),
            // Native particle warning for effects whose sub-emitters have a stop action set; Unity just ignores it.
            ("Sub-emitters may not use stop actions.", ""),
            // Landing sound RPC for a body the client already destroyed; the sound just doesn't play.
            ("Could not find target object with netId:", "RpcOnHitGroundSFX"),
            // Effect prefabs mods marked as networked; they're spawned locally and never need registering.
            ("Could not register '", "since it contains no NetworkIdentity component"),
            // Native warning for prefabs (vanilla ones included) with networked children; only the root is used.
            ("The prefab '", "There can only be one NetworkIdentity on a prefab, and it must be on the root object."),
            // Native animator warnings for mods playing states their animator doesn't have; nothing plays.
            ("Animator.GotoState: State could not be found", ""),
            ("Invalid Layer Index '-1'", ""),
            // Leftover debug log from some mod's skill code.
            ("DeductCooldown", "DeductCooldown"),
        };

        private static bool Prefix(string logString)
        {
            if (logString == null)
                return true;

            foreach (var (start, end) in filtered)
            {
                if (logString.StartsWith(start, StringComparison.Ordinal) && logString.EndsWith(end, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }
    }
}
