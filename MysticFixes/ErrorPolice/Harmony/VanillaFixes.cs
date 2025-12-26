using System;
using System.Collections.ObjectModel;
using EntityStates;
using EntityStates.LunarExploderMonster;
using Facepunch.Steamworks;
using HarmonyLib;
using MiscFixes.Modules;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MiscFixes.ErrorPolice.Harmony
{
    [HarmonyPatch]
    public class VanillaFixes
    {
        [HarmonyPatch(typeof(PseudoCharacterMotor), nameof(PseudoCharacterMotor.velocity), MethodType.Setter)]
        [HarmonyPrefix]
        public static bool PseudoCharacterMotor_setVelocity() => false;

        /// <summary>
        /// blame ss2
        /// </summary>
        [HarmonyPatch(typeof(FlickerLight), nameof(FlickerLight.OnEnable))]
        [HarmonyPrefix]
        public static void Ugh(FlickerLight __instance)
        {
            if (!__instance.light)
            {
                Log.Error(__instance.name + " does not have a light! Fix this in the prefab!");
                __instance.enabled = false;
            }
        }

        /// <summary>
        /// Sometimes prevents loading, so suppress exceptions
        /// </summary>
        [HarmonyPatch(typeof(BaseSteamworks), nameof(BaseSteamworks.RunUpdateCallbacks))]
        [HarmonyFinalizer]
        public static Exception FixFacepunch() => null;

        /// <summary>
        /// Sometimes an enemy will be dead but the call to destroy it never goes through, even when no errors have occurred.
        /// vanilla only checks if they exist, which is true until the scene changes
        /// </summary>
        [HarmonyPatch(typeof(EntityStates.VoidCamp.Idle), nameof(EntityStates.VoidCamp.Idle.FixedUpdate))]
        [HarmonyPatch(typeof(EntityStates.VoidCamp.Idle.VoidCampObjectiveTracker), nameof(EntityStates.VoidCamp.Idle.VoidCampObjectiveTracker.GenerateString))]
        [HarmonyILManipulator]
        public static void FixVoidSeed(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(ReadOnlyCollection<TeamComponent>), nameof(ReadOnlyCollection<TeamComponent>.Count)))
                ))
            {
                c.Remove();
                c.EmitDelegate(GetValidTeamCount);
            }
            else Log.PatchFail(il);
        }

        private static int GetValidTeamCount(ReadOnlyCollection<TeamComponent> teamMembers)
        {
            int count = 0;
            foreach (var t in teamMembers)
            {
                if (t?.body && t.body.master && t.body.healthComponent)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// unity explorer can eat my whole ass
        /// </summary>
        [HarmonyPatch(typeof(SurvivorIconController), nameof(SurvivorIconController.GetLocalUser))]
        [HarmonyPrefix]
        public static bool SurvivorIconController_GetLocalUser(SurvivorIconController __instance, ref LocalUser __result)
        {
            if (EventSystem.current is MPEventSystem)
                return true;

            __result = LocalUserManager.GetFirstLocalUser();
            return false;
        }


        /// <summary>
        /// Null check EventSystem.current, occurs when exiting a lobby.
        /// </summary>
        [HarmonyPatch(typeof(RuleChoiceController), nameof(RuleChoiceController.FindNetworkUser))]
        [HarmonyPrefix]
        public static bool RuleChoiceController_FindNetworkUser(RuleChoiceController __instance, ref NetworkUser __result)
        {
            if (EventSystem.current is MPEventSystem)
                return true;

            __result = LocalUserManager.GetFirstLocalUser()?.currentNetworkUser;
            return false;
        }

        /// <summary>
        /// Halcyonite Shrine is able to drain 0 gold and will softlock itself.
        /// If the scaled gold cost is less than 1, it gets truncated to 0 (mostly for modded scalings)
        /// </summary>
        [HarmonyPatch(typeof(HalcyoniteShrineInteractable), nameof(HalcyoniteShrineInteractable.Awake))]
        [HarmonyPostfix]
        public static void HalcyoniteShrineInteractable_Awake(HalcyoniteShrineInteractable __instance)
        {
            __instance.goldDrainValue = Math.Max(1, __instance.goldDrainValue);
        }

        /// <summary>
        /// nullchecking modelLocator and subsequent transforms with ?. is bad don't do it
        /// only ok for prefabs
        /// </summary>
        [HarmonyPatch(typeof(DeathState), nameof(DeathState.FixedUpdate))]
        [HarmonyILManipulator]
        public static void DeathState_FixedUpdate(ILContext il)
        {
            if (new ILCursor(il).TryFindNext(out var c,
                    x => x.MatchCallOrCallvirt<DeathState>(nameof(DeathState.FireExplosion)),
                    x => x.MatchCallOrCallvirt<GameObject>(nameof(GameObject.SetActive))
                ))
            {
                var c0 = c[0];
                var c1 = c[1];
                c0.Index++;
                c0.MoveAfterLabels();
                c1.Index++;

                c0.Emit(OpCodes.Br, c1.MarkLabel());
                c1.Emit(OpCodes.Ldarg_0);
                c1.EmitDelegate<Action<DeathState>>((self) =>
                {
                    if (self.modelLocator && self.modelLocator.modelTransform)
                        self.modelLocator.modelTransform.gameObject.SetActive(false);
                });
            }
            else Log.PatchFail(il);
        }

        /// <summary>
        /// Filter null HurtBox from HurtBoxGroup, e.g. Golden Dieback's hanging mushrooms.
        /// </summary>
        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.Awake))]
        [HarmonyILManipulator]
        public static void CharacterModel_Awake(ILContext il)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchLdfld<HurtBoxGroup>(nameof(HurtBoxGroup.hurtBoxes))
                ))
            {
                Log.PatchFail(il);
                return;
            }

            c.EmitDelegate<Func<HurtBox[], HurtBox[]>>((hurtBoxes) =>
            {
                for (var i = hurtBoxes.Length - 1; 0 <= i; i--)
                {
                    if (!hurtBoxes[i])
                        HG.ArrayUtils.ArrayRemoveAtAndResize(ref hurtBoxes, i);
                }

                return hurtBoxes;
            });
        }
    }
}
