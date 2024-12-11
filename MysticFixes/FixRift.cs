using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityStates;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RifterMod.Characters.Survivors.Rifter.Components;
using RifterMod.Survivors.Rifter;
using RifterMod.Survivors.Rifter.SkillStates;
using RoR2;
using UnityEngine;
using UnityEngine.UIElements;

namespace MiscFixes
{
    [HarmonyPatch]
    public class FixRift
    {
        [HarmonyPatch(typeof(RifterSurvivor), "AddPrimarySkills")]
        [HarmonyPatch(typeof(RifterSurvivor), "AddSecondarySkills")]
        [HarmonyPatch(typeof(RifterSurvivor), "AddUtiitySkills")]
        [HarmonyPatch(typeof(RifterSurvivor), "InitializeScepter")]
        [HarmonyILManipulator]
        public static void FixNames(ILContext il)
        {
            var c = new ILCursor(il);

            string str = null;
            while (c.TryGotoNext(x => x.MatchLdstr(out str)))
            {
                var news = str.Trim().Replace(" ", "_");
                if (news != str)
                {
                    Debug.LogError(news);
                    c.Next.Operand = news;
                }
            }
        }

        [HarmonyPatch(typeof(ModifiedTeleport), "CalculateSnapDestination")]
        [HarmonyILManipulator]
        public static void FixSnap(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(
                x => x.MatchCallOrCallvirt(AccessTools.PropertySetter(typeof(CharacterDirection), nameof(CharacterDirection.forward)))
            ))
            {
                c.Remove();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<CharacterDirection, Vector3, ModifiedTeleport>>((cd, targetFootPosition, self) =>
                {
                    if (cd)
                        cd.forward = targetFootPosition;
                    else
                        self.transform.rotation = Util.QuaternionSafeLookRotation(targetFootPosition);
                });
            }
            else Debug.LogError($"IL hook failed for ModifiedTeleport.CalculateSnapDestination");
        }

        [HarmonyPatch(typeof(RiftBase), nameof(RiftBase.Blast))]
        [HarmonyILManipulator]
        public static void FixProc(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(x => x.MatchLdcR4(0.8f)))
            {
                c.Next.Operand = 1f;
            }
            else Debug.LogError($"IL hook failed for RiftBase.Blast");
        }
        
        [HarmonyPatch(typeof(RifterTracker), "<FixedUpdate>g__SearchForTarget|19_0")]
        [HarmonyPrefix]
        public static bool FixList(RifterTracker __instance, ref Ray aimRay, ref Vector3 position, ref TeamComponent ___teamComponent, ref GameObject ___trackingTarget, ref BullseyeSearch ___search)
        {
            var allButNeutral = TeamMask.allButNeutral;
            allButNeutral.RemoveTeam(___teamComponent.teamIndex);

            ___search.Reset();
            ___search.teamMaskFilter = allButNeutral;
            ___search.filterByLoS = true;
            ___search.searchOrigin = aimRay.origin;
            ___search.searchDirection = aimRay.direction;
            ___search.sortMode = BullseyeSearch.SortMode.Distance;
            ___search.maxDistanceFilter = 56.75f;
            ___search.maxAngleFilter = 5f;

            ___search.RefreshCandidates();
            ___search.FilterCandidatesByHealthFraction(Mathf.Epsilon);
            ___search.FilterOutGameObject(__instance.gameObject);

            HurtBox result = null;
            float distance = 0f;

            foreach (var hitbox in ___search.GetResults())
            {
                var other = (hitbox.transform.position - position).sqrMagnitude;
                if (distance < other)
                {
                    result = hitbox;
                    distance = other;
                }
            }
            ___trackingTarget = result ? result.gameObject : null;

            return false;
        }
    }
}
