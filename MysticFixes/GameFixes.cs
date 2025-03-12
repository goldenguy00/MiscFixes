using EntityStates;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Rewired;
using RoR2;
using RoR2.Navigation;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace MiscFixes
{
    internal static class GameFixes
    {
        internal static void EmitOpImplicit(this ILCursor c) => c.Emit<UnityEngine.Object>(OpCodes.Call, "op_Implicit");
        internal static bool MatchOpImplicit(this Instruction instr) => instr.MatchCallOrCallvirt<UnityEngine.Object>("op_Implicit");
        internal static bool MatchOpInequality(this Instruction instr) => instr.MatchCallOrCallvirt<UnityEngine.Object>("op_Inequality");

        internal static bool MatchAny(this Instruction instr, out Instruction instruction)
        {
            instruction = instr;
            return true;
        }

        private static void ApplyManualILHook(Type type, string methodName, ILContext.Manipulator manipulator)
        {
            var allFlags = (BindingFlags)(-1);
            var method = type.
                GetMethods(allFlags).
                FirstOrDefault(t => t.Name.Contains(methodName));
            new ILHook(method, manipulator);
        }

        private static void LogError(string method)
        {
            MiscFixesPlugin.Logger.LogError("Failed to patch " + method);
        }

        private static void LogError(ILContext il)
        {
            LogError(il.Method.Name);
        }

        internal static void Init()
        {
            IL.EntityStates.Merc.EvisDash.FixedUpdate += FixMercEvisAllyTargetting;
            IL.EntityStates.Duplicator.Duplicating.DropDroplet += FixPrinterDropEffect;
            IL.RoR2.DetachParticleOnDestroyAndEndEmission.OnDisable += FixParticleDetachOnDestroy;
            IL.RoR2.PositionIndicator.UpdatePositions += FixPositionIndicatorWithHiddenHud;
            IL.RoR2.Indicator.SetVisibleInternal += FixIndicatorSetVisibleNRE;
            IL.RoR2.UI.CrosshairUtils.CrosshairOverrideBehavior.OnDestroy += FixCrosshairOverrideOnDestroy;
            IL.RoR2.UI.RuleChoiceController.FindNetworkUser += FixLobbyQuitEventSystem;
            On.RoR2.RewiredIntegrationManager.RefreshJoystickAssignment += FixNoRewiredInputOnQuit;

            FixServerMethodsCalledOnClient();
            //FixKinPanelPersisting();
            SotsFixes();
        }

        private static void FixMercEvisAllyTargetting(ILContext il)
        {
            var c = new ILCursor(il);
            var varIndex = 0;
            ILLabel label = null;
            if (!c.TryGotoNext(
                    x => x.MatchCallOrCallvirt<Component>(nameof(Component.GetComponent)),
                    x => x.MatchStloc(out varIndex)) ||
                !c.TryGotoNext(
                    MoveType.After,
                    x => x.MatchLdloc(varIndex),
                    x => x.MatchLdfld<HurtBox>(nameof(HurtBox.healthComponent)),
                    x => x.MatchLdarg(0),
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(EntityState), nameof(EntityState.healthComponent))),
                    x => x.MatchOpInequality(),
                    x => x.MatchBrfalse(out label)))
            {
                LogError(il);
                return;
            }
            // victim
            c.Emit(OpCodes.Ldloc, varIndex);
            c.Emit<HurtBox>(OpCodes.Ldfld, nameof(HurtBox.healthComponent));

            // teamindex
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Call, AccessTools.PropertyGetter(typeof(EntityState), nameof(EntityState.characterBody)));
            c.Emit(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(CharacterBody), nameof(CharacterBody.teamComponent)));
            c.Emit(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(TeamComponent), nameof(TeamComponent.teamIndex)));

            // ShouldDirectHitProceed(HealthComponent victim, TeamIndex attackerTeamIndex)
            c.EmitDelegate(FriendlyFireManager.ShouldDirectHitProceed);
            c.Emit(OpCodes.Brfalse_S, label);
        }

        private static void FixPrinterDropEffect(ILContext il)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(x => x.MatchCallOrCallvirt(typeof(EffectManager), "SimpleMuzzleFlash")))
            {
                LogError(il);
                return;
            }
            c.Remove();
            c.EmitDelegate<Action<GameObject, GameObject, string, bool>>((effectPrefab, obj, muzzleName, _) =>
            {
                if (obj && obj.TryGetComponent<ModelLocator>(out var modelLocator) && modelLocator.modelTransform)
                {
                    var childLocator = modelLocator.modelTransform.GetComponent<ChildLocator>();
                    if (childLocator)
                    {
                        int childIndex = childLocator.FindChildIndex(muzzleName);
                        Transform transform = childLocator.FindChild(childIndex);
                        if (transform)
                        {
                            UnityEngine.Object.Instantiate(effectPrefab, transform.position, Quaternion.identity, transform);
                        }
                    }
                }
            });
        }

        private static void FixParticleDetachOnDestroy(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel returnLabel = null;
            if (!c.TryGotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<DetachParticleOnDestroyAndEndEmission>(nameof(DetachParticleOnDestroyAndEndEmission.particleSystem)),
                    x => x.MatchOpImplicit(),
                    x => x.MatchBrfalse(out returnLabel)) ||
                !c.TryGotoNext(
                    MoveType.After,
                    x => x.MatchCallOrCallvirt<ParticleSystem>(nameof(ParticleSystem.Stop))
                ))
            {
                LogError(il);
                return;
            }
            c.Emit(OpCodes.Ldarg_0);
            c.Emit<DetachParticleOnDestroyAndEndEmission>(OpCodes.Ldfld, nameof(DetachParticleOnDestroyAndEndEmission.particleSystem));
            c.Emit(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Component), nameof(Component.gameObject)));
            c.Emit(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(GameObject), nameof(GameObject.activeInHierarchy)));
            c.Emit(OpCodes.Brfalse, returnLabel);
        }

        private static void FixPositionIndicatorWithHiddenHud(ILContext il)
        {
            var c = new ILCursor(il);
            int locVarIndex = 0;
            ILLabel nextLabel = null;
            if (!c.TryGotoNext(
                x => x.MatchLdloc(out locVarIndex),
                x => x.MatchLdfld<PositionIndicator>("alwaysVisibleObject"),
                x => x.MatchLdcI4(0),
                x => x.MatchCallOrCallvirt<GameObject>("SetActive"),
                x => x.MatchBr(out nextLabel)))
            {
                LogError(il);
                return;
            }
            c.Index += 2;
            c.Emit<UnityEngine.Object>(OpCodes.Call, "op_Implicit");
            c.Emit(OpCodes.Brfalse, nextLabel.Target);
            c.Emit(OpCodes.Ldloc, locVarIndex);
            c.Emit<PositionIndicator>(OpCodes.Ldfld, "alwaysVisibleObject");
        }

        private static void FixIndicatorSetVisibleNRE(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction ifInstr = null;
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(1),
                x => x.MatchCallOrCallvirt<Renderer>("set_enabled"),
                x => x.MatchAny(out nextInstr)))
            {
                LogError(il);
                return;
            }
            ifInstr = c.Next;
            c.Emit(OpCodes.Dup);
            c.Emit<UnityEngine.Object>(OpCodes.Call, "op_Implicit");
            c.Emit(OpCodes.Brtrue_S, ifInstr);
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Br_S, nextInstr);
        }

        private static void FixCrosshairOverrideOnDestroy(ILContext il)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<CrosshairUtils.CrosshairOverrideBehavior>("requestList")))
            {
                LogError(il);
                return;
            }
            c.EmitDelegate<Func<List<CrosshairUtils.OverrideRequest>, List<CrosshairUtils.OverrideRequest>>>(requestList => [.. requestList]);
        }

        private static void FixNoRewiredInputOnQuit(On.RoR2.RewiredIntegrationManager.orig_RefreshJoystickAssignment orig)
        {
            if (ReInput.initialized && ReInput.controllers != null)
            {
                orig();
            }
        }

        private static void FixLobbyQuitEventSystem(ILContext il)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(
                x => x.MatchPop(),
                x => x.MatchLdnull(),
                x => x.MatchRet()))
            {
                LogError(il.Method.Name + " #1");
                return;
            }
            var instr = c.Next;
            if (!c.TryGotoPrev(
                MoveType.After,
                x => x.MatchCallOrCallvirt<UnityEngine.EventSystems.EventSystem>("get_current")))
            {
                LogError(il.Method.Name + " #2");
                return;
            }
            c.Remove();
            c.Emit(OpCodes.Isinst, typeof(MPEventSystem));
            c.Emit(OpCodes.Dup);
            c.Emit<UnityEngine.Object>(OpCodes.Call, "op_Implicit");
            c.Emit(OpCodes.Brfalse, instr);
        }

        #region Server Methods On Client
        internal static void FixServerMethodsCalledOnClient()
        {
            IL.EntityStates.BrotherMonster.EnterSkyLeap.OnEnter += EnterSkyLeap_OnEnter;
            IL.EntityStates.BrotherMonster.ExitSkyLeap.OnEnter += ExitSkyLeap_OnEnter;
            IL.EntityStates.BrotherMonster.HoldSkyLeap.OnEnter += HoldSkyLeap_OnEnter;
            IL.EntityStates.BrotherMonster.HoldSkyLeap.OnExit += HoldSkyLeap_OnExit;
            IL.EntityStates.BrotherMonster.SpellChannelState.OnEnter += SpellChannelState_OnEnter;
            IL.EntityStates.BrotherMonster.SpellChannelState.OnExit += SpellChannelState_OnExit;
            IL.EntityStates.Duplicator.Duplicating.DropDroplet += Duplicating_DropDroplet;
            IL.EntityStates.Huntress.Weapon.FireArrowSnipe.FireBullet += FireArrowSnipe_FireBullet;
            IL.EntityStates.MinorConstruct.Hidden.OnEnter += Hidden_OnEnter;
            IL.EntityStates.MinorConstruct.Hidden.OnExit += Hidden_OnExit;
            IL.EntityStates.Scrapper.ScrapperBaseState.OnEnter += ScrapperBaseState_OnEnter;
            IL.RoR2.BuffPassengerWhileSeated.OnDisable += BuffPassengerWhileSeated_OnDisable;
            IL.RoR2.BuffPassengerWhileSeated.OnEnable += BuffPassengerWhileSeated_OnEnable;
            IL.RoR2.DelusionChestController.ResetChestForDelusion += DelusionChestController_ResetChestForDelusion;
            IL.RoR2.DelusionChestController.Start += DelusionChestController_Start;
            IL.RoR2.DevotionInventoryController.Awake += DevotionInventoryController_Awake;
            IL.RoR2.MasterDropDroplet.DropItems += MasterDropDroplet_DropItems;
            IL.RoR2.MinionOwnership.MinionGroup.AddMinion += MinionGroup_AddMinion;
            IL.RoR2.MinionOwnership.MinionGroup.RemoveMinion += MinionGroup_RemoveMinion;
        }

        private static void EnterSkyLeap_OnEnter(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<EntityState>("get_characterBody"),
                x => x.MatchLdsfld(typeof(RoR2Content.Buffs), "ArmorBoost"),
                x => x.MatchLdsfld<EntityStates.BrotherMonster.EnterSkyLeap>("baseDuration"),
                x => x.MatchCallOrCallvirt<CharacterBody>("AddTimedBuff"),
                x => x.MatchAny(out nextInstr)))
            {
                LogError(il);
                return;
            }
            c.Emit<NetworkServer>(OpCodes.Call, "get_active");
            c.Emit(OpCodes.Brfalse_S, nextInstr);
        }

        private static void ExitSkyLeap_OnEnter(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<EntityState>("get_characterBody"),
                x => x.MatchLdsfld(typeof(RoR2Content.Buffs), "ArmorBoost"),
                x => x.MatchLdsfld<EntityStates.BrotherMonster.ExitSkyLeap>("baseDuration"),
                x => x.MatchCallOrCallvirt<CharacterBody>("AddTimedBuff"),
                x => x.MatchAny(out nextInstr)))
            {
                LogError(il);
                return;
            }
            c.Emit<NetworkServer>(OpCodes.Call, "get_active");
            c.Emit(OpCodes.Brfalse_S, nextInstr);
        }

        private static void HoldSkyLeap_OnEnter(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<EntityState>("get_characterBody"),
                x => x.MatchLdsfld(typeof(RoR2Content.Buffs), "HiddenInvincibility"),
                x => x.MatchCallOrCallvirt<CharacterBody>("AddBuff"),
                x => x.MatchAny(out nextInstr)))
            {
                LogError(il.Method.Name + " #1");
                return;
            }
            c.Emit<NetworkServer>(OpCodes.Call, "get_active");
            var ifInstr = c.Prev;
            c.Emit(OpCodes.Brfalse_S, nextInstr);
            if (!c.TryGotoPrev(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<EntityStates.BrotherMonster.HoldSkyLeap>("hurtboxGroup"),
                x => x.MatchCallOrCallvirt<UnityEngine.Object>("op_Implicit")))
            {
                LogError(il.Method.Name + " #2");
                return;
            }
            c.Remove();
            c.Emit(OpCodes.Brfalse_S, ifInstr);
        }

        private static void HoldSkyLeap_OnExit(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<EntityState>("get_characterBody"),
                x => x.MatchLdsfld(typeof(RoR2Content.Buffs), "HiddenInvincibility"),
                x => x.MatchCallOrCallvirt<CharacterBody>("RemoveBuff"),
                x => x.MatchAny(out nextInstr)))
            {
                LogError(il.Method.Name + " #1");
                return;
            }
            c.Emit<NetworkServer>(OpCodes.Call, "get_active");
            var ifInstr = c.Prev;
            c.Emit(OpCodes.Brfalse_S, nextInstr);
            if (!c.TryGotoPrev(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<EntityStates.BrotherMonster.HoldSkyLeap>("hurtboxGroup"),
                x => x.MatchCallOrCallvirt<UnityEngine.Object>("op_Implicit")))
            {
                LogError(il.Method.Name + " #2");
                return;
            }
            c.Remove();
            c.Emit(OpCodes.Brfalse_S, ifInstr);
        }

        private static void SpellChannelState_OnEnter(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<EntityState>("get_characterBody"),
                x => x.MatchLdsfld(typeof(RoR2Content.Buffs), "Immune"),
                x => x.MatchCallOrCallvirt<CharacterBody>("AddBuff"),
                x => x.MatchAny(out nextInstr)))
            {
                LogError(il.Method.Name + " #1");
                return;
            }
            c.Emit<NetworkServer>(OpCodes.Call, "get_active");
            var ifInstr = c.Prev;
            c.Emit(OpCodes.Brfalse_S, nextInstr);
            if (!c.TryGotoPrev(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<EntityStates.BrotherMonster.SpellChannelState>("spellChannelChildTransform"),
                x => x.MatchCallOrCallvirt<UnityEngine.Object>("op_Implicit")))
            {
                LogError(il.Method.Name + " #2");
                return;
            }
            c.Remove();
            c.Emit(OpCodes.Brfalse_S, ifInstr);
        }

        private static void SpellChannelState_OnExit(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<EntityState>("get_characterBody"),
                x => x.MatchLdsfld(typeof(RoR2Content.Buffs), "Immune"),
                x => x.MatchCallOrCallvirt<CharacterBody>("RemoveBuff"),
                x => x.MatchAny(out nextInstr)))
            {
                LogError(il);
                return;
            }
            c.Emit<NetworkServer>(OpCodes.Call, "get_active");
            c.Emit(OpCodes.Brfalse_S, nextInstr);
        }

        private static void Duplicating_DropDroplet(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<EntityState>("GetComponent"),
                x => x.MatchCallOrCallvirt<ShopTerminalBehavior>("DropPickup"),
                x => x.MatchAny(out nextInstr)))
            {
                LogError(il);
                return;
            }
            c.Emit<NetworkServer>(OpCodes.Call, "get_active");
            c.Emit(OpCodes.Brfalse_S, nextInstr);
        }

        private static void FireArrowSnipe_FireBullet(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<EntityState>("get_healthComponent"),
                x => x.MatchLdarga(1),
                x => x.MatchCallOrCallvirt<Ray>("get_direction"),
                x => x.MatchLdcR4(out _),
                x => x.MatchCallOrCallvirt<Vector3>("op_Multiply"),
                x => x.MatchLdcI4(1),
                x => x.MatchLdcI4(0),
                x => x.MatchCallOrCallvirt<HealthComponent>("TakeDamageForce"),
                x => x.MatchAny(out nextInstr)))
            {
                LogError(il);
                return;
            }
            c.Emit<NetworkServer>(OpCodes.Call, "get_active");
            c.Emit(OpCodes.Brfalse_S, nextInstr);
        }

        private static void Hidden_OnEnter(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel nextLabel = null;
            if (!c.TryGotoNext(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<EntityStates.MinorConstruct.Hidden>("buffDef"),
                x => x.MatchCallOrCallvirt<UnityEngine.Object>("op_Implicit"),
                x => x.MatchBrfalse(out nextLabel)))
            {
                LogError(il);
                return;
            }
            c.Emit<NetworkServer>(OpCodes.Call, "get_active");
            c.Emit(OpCodes.Brfalse_S, nextLabel);
        }

        private static void Hidden_OnExit(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel nextLabel = null;
            if (!c.TryGotoNext(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<EntityStates.MinorConstruct.Hidden>("buffDef"),
                x => x.MatchCallOrCallvirt<UnityEngine.Object>("op_Implicit"),
                x => x.MatchBrfalse(out nextLabel)))
            {
                LogError(il);
                return;
            }
            c.Emit<NetworkServer>(OpCodes.Call, "get_active");
            c.Emit(OpCodes.Brfalse_S, nextLabel);
        }

        private static void ScrapperBaseState_OnEnter(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<EntityStates.Scrapper.ScrapperBaseState>("pickupPickerController"),
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<EntityStates.Scrapper.ScrapperBaseState>("get_enableInteraction"),
                x => x.MatchCallOrCallvirt<PickupPickerController>("SetAvailable"),
                x => x.MatchAny(out nextInstr)))
            {
                LogError(il);
                return;
            }
            c.Emit<NetworkServer>(OpCodes.Call, "get_active");
            c.Emit(OpCodes.Brfalse_S, nextInstr);
        }

        private static void BuffPassengerWhileSeated_OnDisable(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel nextLabel = null;
            if (!c.TryGotoNext(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<BuffPassengerWhileSeated>("vehicleSeat"),
                x => x.MatchCallOrCallvirt<UnityEngine.Object>("op_Implicit"),
                x => x.MatchBrfalse(out nextLabel)))
            {
                LogError(il);
                return;
            }
            c.Emit<NetworkServer>(OpCodes.Call, "get_active");
            c.Emit(OpCodes.Brfalse_S, nextLabel);
        }

        private static void BuffPassengerWhileSeated_OnEnable(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel nextLabel = null;
            if (!c.TryGotoNext(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<BuffPassengerWhileSeated>("vehicleSeat"),
                x => x.MatchCallOrCallvirt<UnityEngine.Object>("op_Implicit"),
                x => x.MatchBrfalse(out nextLabel)))
            {
                LogError(il);
                return;
            }
            c.Emit<NetworkServer>(OpCodes.Call, "get_active");
            c.Emit(OpCodes.Brfalse_S, nextLabel);
        }

        private static void DelusionChestController_ResetChestForDelusion(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<DelusionChestController>("_pickupPickerController"),
                x => x.MatchLdcI4(out _),
                x => x.MatchCallOrCallvirt<PickupPickerController>("SetAvailable"),
                x => x.MatchAny(out nextInstr)))
            {
                LogError(il);
                return;
            }
            c.Emit<NetworkServer>(OpCodes.Call, "get_active");
            c.Emit(OpCodes.Brfalse_S, nextInstr);
        }

        private static void DelusionChestController_Start(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<DelusionChestController>("_pickupPickerController"),
                x => x.MatchLdcI4(out _),
                x => x.MatchCallOrCallvirt<PickupPickerController>("SetAvailable"),
                x => x.MatchAny(out nextInstr)))
            {
                LogError(il);
                return;
            }
            c.Emit<NetworkServer>(OpCodes.Call, "get_active");
            c.Emit(OpCodes.Brfalse_S, nextInstr);
        }

        private static void DevotionInventoryController_Awake(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<DevotionInventoryController>("_devotionMinionInventory"),
                x => x.MatchLdsfld(typeof(CU8Content.Items), "LemurianHarness"),
                x => x.MatchLdcI4(out _),
                x => x.MatchCallOrCallvirt<Inventory>("GiveItem"),
                x => x.MatchAny(out nextInstr)))
            {
                LogError(il);
                return;
            }
            c.Emit<NetworkServer>(OpCodes.Call, "get_active");
            c.Emit(OpCodes.Brfalse_S, nextInstr);
        }

        private static void MasterDropDroplet_DropItems(ILContext il)
        {
            var c = new ILCursor(il);
            var nextInstr = c.Instrs[c.Instrs.Count - 1];
            c.Emit<NetworkServer>(OpCodes.Call, "get_active");
            c.Emit(OpCodes.Brfalse_S, nextInstr);
        }

        private static void MinionGroup_AddMinion(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel nextLabel = null;
            if (!c.TryGotoNext(
                MoveType.After,
                x => x.MatchLdloc(1),
                x => x.MatchCallOrCallvirt<UnityEngine.Object>("op_Implicit"),
                x => x.MatchBrfalse(out nextLabel)))
            {
                LogError(il);
                return;
            }
            c.Emit<NetworkServer>(OpCodes.Call, "get_active");
            c.Emit(OpCodes.Brfalse_S, nextLabel);
        }

        private static void MinionGroup_RemoveMinion(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel nextLabel = null;
            if (!c.TryGotoNext(
                MoveType.After,
                x => x.MatchLdloc(0),
                x => x.MatchCallOrCallvirt<UnityEngine.Object>("op_Implicit"),
                x => x.MatchBrfalse(out nextLabel)))
            {
                LogError(il);
                return;
            }
            c.Emit<NetworkServer>(OpCodes.Call, "get_active");
            c.Emit(OpCodes.Brfalse_S, nextLabel);
        }
        #endregion

        #region Kin Panel
        private static void FixKinPanelPersisting()
        {
            IL.RoR2.ClassicStageInfo.RebuildCards += FixKinBodyIndexNotReseting;
            ApplyManualILHook(typeof(Stage), "set_singleMonsterTypeBodyIndex", FixKinNotUpdatingPanel);
        }

        private static void FixKinBodyIndexNotReseting(ILContext il)
        {
            var c = new ILCursor(il);
            int varIndex = 0;
            ILLabel nextLabel = null;
            if (!c.TryGotoNext(
                    x => x.MatchCallOrCallvirt(typeof(RoR2Content.Artifacts), "get_singleMonsterTypeArtifactDef"),
                    x => x.MatchCallOrCallvirt<RunArtifactManager>("IsArtifactEnabled"),
                    x => x.MatchStloc(out varIndex)) ||
                !c.TryGotoNext(
                    x => x.MatchLdloc(varIndex),
                    x => x.MatchBrfalse(out nextLabel)))
            {
                LogError(il);
                return;
            }
            c.GotoLabel(nextLabel, MoveType.Before);
            c.Emit(OpCodes.Br, nextLabel.Target);
            c.Emit<Stage>(OpCodes.Call, "get_instance");
            var elseInstruction = c.Prev;
            c.Emit<UnityEngine.Object>(OpCodes.Call, "op_Implicit");
            c.Emit(OpCodes.Brfalse, nextLabel.Target);
            c.Emit<Stage>(OpCodes.Call, "get_instance");
            c.Emit(OpCodes.Ldc_I4_M1);
            c.Emit<Stage>(OpCodes.Callvirt, "set_singleMonsterTypeBodyIndex");
            c.GotoPrev(MoveType.After, x => x.MatchLdloc(varIndex));
            c.Remove();
            c.Emit(OpCodes.Brfalse, elseInstruction);
        }

        private static void FixKinNotUpdatingPanel(ILContext il)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt<Stage>("set_Network_singleMonsterTypeBodyIndex")))
            {
                LogError(il);
                return;
            }
            c.Emit<EnemyInfoPanel>(OpCodes.Call, "RefreshAll");

        }
        #endregion

        #region SotS Fixes
        internal static void SotsFixes()
        {
            IL.EntityStates.ChildMonster.Frolic.TeleportAroundPlayer += FixFrolicTeleportWithoutAvailableNodes;
            On.RoR2.MeridianEventTriggerInteraction.Awake += FixMeridianTestStateSpam;
            FixSaleStarCollider();
        }

        private static void FixFrolicTeleportWithoutAvailableNodes(ILContext il)
        {
            var c = new ILCursor(il);
            int varIndex = 0;
            if (!c.TryGotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt<NodeGraph>("FindNodesInRange"),
                x => x.MatchStloc(out varIndex)))
            {
                LogError(il);
                return;
            }
            var instr = c.Next;
            c.Emit(OpCodes.Ldloc, varIndex);
            c.Emit<List<NodeGraph.NodeIndex>>(OpCodes.Callvirt, "get_Count");
            c.Emit(OpCodes.Ldc_I4_0);
            c.Emit(OpCodes.Bgt_S, instr);
            c.Emit(OpCodes.Ret);
        }

        private static void FixMeridianTestStateSpam(On.RoR2.MeridianEventTriggerInteraction.orig_Awake orig, MeridianEventTriggerInteraction self)
        {
            var esm = EntityStateMachine.FindByCustomName(self.gameObject, "");
            if (esm != null)
            {
                esm.initialStateType = new SerializableEntityStateType(typeof(Uninitialized));
                esm.enabled = false;
            }
            else
            {
                LogError("Failed to modify meridian test state");
            }
            orig(self);
        }

        private static void FixSaleStarCollider()
        {
            var asset = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Items/LowerPricedChests/PickupSaleStar.prefab").WaitForCompletion();
            var collider = asset.transform.Find("SaleStar")?.GetComponent<MeshCollider>();
            if (collider == null || collider.convex)
            {
                LogError("Failed to modify collider of SaleStar");
                return;
            }
            collider.convex = true;
        }
        #endregion
    }
}