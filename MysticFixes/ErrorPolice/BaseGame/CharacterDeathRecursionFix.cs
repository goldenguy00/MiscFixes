using System;
using System.Collections.Generic;
using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // On-kill effects that deal damage (Gasoline, mod equivalents, Augmentum's fake deaths...) kill
    // more enemies from inside OnCharacterDeath, whose on-kill effects run inside that, and so on.
    // Each level stacks the whole TakeDamage and OnCharacterDeath hook chains of every installed mod,
    // a few hundred frames, so a pack of enemies dying in a chain overflows the stack in a big
    // modpack and crashes the game. Run death procs at most MaxDepth levels deep; deeper ones are
    // queued and run once the outermost one returns, in the same frame. Every proc still happens.
    [HarmonyPatch(typeof(GlobalEventManager), nameof(GlobalEventManager.TriggerCharacterDeathProc))]
    internal static class CharacterDeathRecursionFix
    {
        private const int MaxDepth = 3;

        private static readonly Queue<(GlobalEventManager manager, DamageReport report)> deferred = new Queue<(GlobalEventManager, DamageReport)>();
        private static int depth;
        private static bool draining;

        private static bool Prefix(GlobalEventManager __instance, DamageReport damageReport, out bool __state)
        {
            __state = depth < MaxDepth;
            if (!__state)
            {
                deferred.Enqueue((__instance, damageReport));
                return false;
            }

            depth++;
            return true;
        }

        // Finalizers also run when the prefix skipped the original, hence __state.
        private static void Finalizer(bool __state)
        {
            if (!__state)
                return;

            depth--;
            if (depth > 0 || draining)
                return;

            draining = true;
            try
            {
                while (deferred.Count > 0)
                {
                    var (manager, report) = deferred.Dequeue();
                    if (!manager)
                        continue;

                    // These would have thrown into whichever TakeDamage caused them; log them the same way
                    // instead of letting one broken on-kill hook drop the rest of the queue.
                    try
                    {
                        manager.TriggerCharacterDeathProc(report);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                }
            }
            finally
            {
                draining = false;
            }
        }
    }
}
