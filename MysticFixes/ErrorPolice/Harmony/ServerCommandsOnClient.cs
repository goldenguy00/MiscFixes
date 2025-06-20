using EntityStates;
using HarmonyLib;
using MiscFixes.Modules;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;

namespace MiscFixes.ErrorPolice.Harmony
{
    /// <summary>
    /// A collection of patches that skip Server method calls on a client preventing log spam.
    /// </summary>
    [HarmonyPatch]
    internal class ServerCommandsOnClient
    {
        [HarmonyPatch(typeof(EntityStates.BrotherMonster.EnterSkyLeap), nameof(EntityStates.BrotherMonster.EnterSkyLeap.OnEnter))]
        [HarmonyILManipulator]
        internal static void FixEnterSkyLeapOnEnter(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(EntityState), nameof(EntityState.characterBody))),
                x => x.MatchLdsfld(typeof(RoR2Content.Buffs), nameof(RoR2Content.Buffs.ArmorBoost)),
                x => x.MatchLdsfld<EntityStates.BrotherMonster.EnterSkyLeap>(nameof(EntityStates.BrotherMonster.EnterSkyLeap.baseDuration)),
                x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.AddTimedBuff)),
                x => x.MatchAny(out nextInstr)))
            {
                Log.PatchFail(il);
                return;
            }
            c.EmitNetworkServerActive();
            c.Emit(OpCodes.Brfalse_S, nextInstr);
        }

        [HarmonyPatch(typeof(EntityStates.BrotherMonster.ExitSkyLeap), nameof(EntityStates.BrotherMonster.ExitSkyLeap.OnEnter))]
        [HarmonyILManipulator]
        internal static void FixExitSkyLeapOnEnter(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(EntityState), nameof(EntityState.characterBody))),
                x => x.MatchLdsfld(typeof(RoR2Content.Buffs), nameof(RoR2Content.Buffs.ArmorBoost)),
                x => x.MatchLdsfld<EntityStates.BrotherMonster.ExitSkyLeap>(nameof(EntityStates.BrotherMonster.ExitSkyLeap.baseDuration)),
                x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.AddTimedBuff)),
                x => x.MatchAny(out nextInstr)))
            {
                Log.PatchFail(il);
                return;
            }
            c.EmitNetworkServerActive();
            c.Emit(OpCodes.Brfalse_S, nextInstr);
        }

        [HarmonyPatch(typeof(EntityStates.BrotherMonster.HoldSkyLeap), nameof(EntityStates.BrotherMonster.HoldSkyLeap.OnEnter))]
        [HarmonyILManipulator]
        internal static void FixHoldSkyLeapOnEnter(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(EntityState), nameof(EntityState.characterBody))),
                x => x.MatchLdsfld(typeof(RoR2Content.Buffs), nameof(RoR2Content.Buffs.HiddenInvincibility)),
                x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.AddBuff)),
                x => x.MatchAny(out nextInstr)))
            {
                Log.PatchFail(il.Method.Name + " #1");
                return;
            }
            c.EmitNetworkServerActive();
            var ifInstr = c.Prev;
            c.Emit(OpCodes.Brfalse_S, nextInstr);
            if (!c.TryGotoPrev(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<EntityStates.BrotherMonster.HoldSkyLeap>(nameof(EntityStates.BrotherMonster.HoldSkyLeap.hurtboxGroup)),
                x => x.MatchOpImplicit()))
            {
                Log.PatchFail(il.Method.Name + " #2");
                return;
            }
            c.Remove();
            c.Emit(OpCodes.Brfalse_S, ifInstr);
        }

        [HarmonyPatch(typeof(EntityStates.BrotherMonster.HoldSkyLeap), nameof(EntityStates.BrotherMonster.HoldSkyLeap.OnExit))]
        [HarmonyILManipulator]
        internal static void FixHoldSkyLeapOnExit(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(EntityState), nameof(EntityState.characterBody))),
                x => x.MatchLdsfld(typeof(RoR2Content.Buffs), nameof(RoR2Content.Buffs.HiddenInvincibility)),
                x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.RemoveBuff)),
                x => x.MatchAny(out nextInstr)))
            {
                Log.PatchFail(il.Method.Name + " #1");
                return;
            }
            c.EmitNetworkServerActive();
            var ifInstr = c.Prev;
            c.Emit(OpCodes.Brfalse_S, nextInstr);
            if (!c.TryGotoPrev(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<EntityStates.BrotherMonster.HoldSkyLeap>(nameof(EntityStates.BrotherMonster.HoldSkyLeap.hurtboxGroup)),
                x => x.MatchOpImplicit()))
            {
                Log.PatchFail(il.Method.Name + " #2");
                return;
            }
            c.Remove();
            c.Emit(OpCodes.Brfalse_S, ifInstr);
        }

        [HarmonyPatch(typeof(EntityStates.BrotherMonster.SpellChannelState), nameof(EntityStates.BrotherMonster.SpellChannelState.OnEnter))]
        [HarmonyILManipulator]
        internal static void FixSpellChannelStateOnEnter(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(EntityState), nameof(EntityState.characterBody))),
                x => x.MatchLdsfld(typeof(RoR2Content.Buffs), nameof(RoR2Content.Buffs.Immune)),
                x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.AddBuff)),
                x => x.MatchAny(out nextInstr)))
            {
                Log.PatchFail(il.Method.Name + " #1");
                return;
            }
            c.EmitNetworkServerActive();
            var ifInstr = c.Prev;
            c.Emit(OpCodes.Brfalse_S, nextInstr);
            if (!c.TryGotoPrev(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<EntityStates.BrotherMonster.SpellChannelState>(nameof(EntityStates.BrotherMonster.SpellChannelState.spellChannelChildTransform)),
                x => x.MatchOpImplicit()))
            {
                Log.PatchFail(il.Method.Name + " #2");
                return;
            }
            c.Remove();
            c.Emit(OpCodes.Brfalse_S, ifInstr);
        }

        [HarmonyPatch(typeof(EntityStates.BrotherMonster.SpellChannelState), nameof(EntityStates.BrotherMonster.SpellChannelState.OnExit))]
        [HarmonyILManipulator]
        internal static void SpellChannelState_OnExit(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(EntityState), nameof(EntityState.characterBody))),
                x => x.MatchLdsfld(typeof(RoR2Content.Buffs), nameof(RoR2Content.Buffs.Immune)),
                x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.RemoveBuff)),
                x => x.MatchAny(out nextInstr)))
            {
                Log.PatchFail(il);
                return;
            }
            c.EmitNetworkServerActive();
            c.Emit(OpCodes.Brfalse_S, nextInstr);
        }

        [HarmonyPatch(typeof(EntityStates.Duplicator.Duplicating), nameof(EntityStates.Duplicator.Duplicating.DropDroplet))]
        [HarmonyILManipulator]
        internal static void FixDuplicatingDropDroplet(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<EntityState>(nameof(EntityState.GetComponent)),
                x => x.MatchCallOrCallvirt<ShopTerminalBehavior>(nameof(ShopTerminalBehavior.DropPickup)),
                x => x.MatchAny(out nextInstr)))
            {
                Log.PatchFail(il);
                return;
            }
            c.EmitNetworkServerActive();
            c.Emit(OpCodes.Brfalse_S, nextInstr);
        }

        [HarmonyPatch(typeof(EntityStates.Huntress.Weapon.FireArrowSnipe), nameof(EntityStates.Huntress.Weapon.FireArrowSnipe.FireBullet))]
        [HarmonyILManipulator]
        internal static void FixFireArrowSnipeFireBullet(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(EntityState), nameof(EntityState.healthComponent))),
                x => x.MatchLdarga(1),
                x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(Ray), nameof(Ray.direction))),
                x => x.MatchLdcR4(out _),
                x => x.MatchCallOrCallvirt<Vector3>("op_Multiply"),
                x => x.MatchLdcI4(1),
                x => x.MatchLdcI4(0),
                x => x.MatchCallOrCallvirt<HealthComponent>(nameof(HealthComponent.TakeDamageForce)),
                x => x.MatchAny(out nextInstr)))
            {
                Log.PatchFail(il);
                return;
            }
            c.EmitNetworkServerActive();
            c.Emit(OpCodes.Brfalse_S, nextInstr);
        }

        [HarmonyPatch(typeof(EntityStates.MinorConstruct.Hidden), nameof(EntityStates.MinorConstruct.Hidden.OnEnter))]
        [HarmonyILManipulator]
        internal static void FixHiddenOnEnter(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel nextLabel = null;
            if (!c.TryGotoNext(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<EntityStates.MinorConstruct.Hidden>(nameof(EntityStates.MinorConstruct.Hidden.buffDef)),
                x => x.MatchOpImplicit(),
                x => x.MatchBrfalse(out nextLabel)))
            {
                Log.PatchFail(il);
                return;
            }
            c.EmitNetworkServerActive();
            c.Emit(OpCodes.Brfalse_S, nextLabel);
        }

        [HarmonyPatch(typeof(EntityStates.MinorConstruct.Hidden), nameof(EntityStates.MinorConstruct.Hidden.OnExit))]
        [HarmonyILManipulator]
        internal static void FixHiddenOnExit(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel nextLabel = null;
            if (!c.TryGotoNext(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<EntityStates.MinorConstruct.Hidden>(nameof(EntityStates.MinorConstruct.Hidden.buffDef)),
                x => x.MatchOpImplicit(),
                x => x.MatchBrfalse(out nextLabel)))
            {
                Log.PatchFail(il);
                return;
            }
            c.EmitNetworkServerActive();
            c.Emit(OpCodes.Brfalse_S, nextLabel);
        }

        [HarmonyPatch(typeof(EntityStates.Scrapper.ScrapperBaseState), nameof(EntityStates.Scrapper.ScrapperBaseState.OnEnter))]
        [HarmonyILManipulator]
        internal static void FixScrapperBaseStateOnEnter(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<EntityStates.Scrapper.ScrapperBaseState>(nameof(EntityStates.Scrapper.ScrapperBaseState.pickupPickerController)),
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(EntityStates.Scrapper.ScrapperBaseState), nameof(EntityStates.Scrapper.ScrapperBaseState.enableInteraction))),
                x => x.MatchCallOrCallvirt<PickupPickerController>(nameof(PickupPickerController.SetAvailable)),
                x => x.MatchAny(out nextInstr)))
            {
                Log.PatchFail(il);
                return;
            }
            c.EmitNetworkServerActive();
            c.Emit(OpCodes.Brfalse_S, nextInstr);
        }

        [HarmonyPatch(typeof(BuffPassengerWhileSeated), nameof(BuffPassengerWhileSeated.OnDisable))]
        [HarmonyILManipulator]
        internal static void FixBuffPassengerWhileSeatedOnDisable(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel nextLabel = null;
            if (!c.TryGotoNext(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<BuffPassengerWhileSeated>(nameof(BuffPassengerWhileSeated.vehicleSeat)),
                x => x.MatchOpImplicit(),
                x => x.MatchBrfalse(out nextLabel)))
            {
                Log.PatchFail(il);
                return;
            }
            c.EmitNetworkServerActive();
            c.Emit(OpCodes.Brfalse_S, nextLabel);
        }

        [HarmonyPatch(typeof(BuffPassengerWhileSeated), nameof(BuffPassengerWhileSeated.OnEnable))]
        [HarmonyILManipulator]
        internal static void FixBuffPassengerWhileSeatedOnEnable(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel nextLabel = null;
            if (!c.TryGotoNext(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<BuffPassengerWhileSeated>(nameof(BuffPassengerWhileSeated.vehicleSeat)),
                x => x.MatchOpImplicit(),
                x => x.MatchBrfalse(out nextLabel)))
            {
                Log.PatchFail(il);
                return;
            }
            c.EmitNetworkServerActive();
            c.Emit(OpCodes.Brfalse_S, nextLabel);
        }

        [HarmonyPatch(typeof(DelusionChestController), nameof(DelusionChestController.ResetChestForDelusion))]
        [HarmonyILManipulator]
        internal static void FixDelusionChestControllerResetChestForDelusion(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<DelusionChestController>(nameof(DelusionChestController._pickupPickerController)),
                x => x.MatchLdcI4(out _),
                x => x.MatchCallOrCallvirt<PickupPickerController>(nameof(PickupPickerController.SetAvailable)),
                x => x.MatchAny(out nextInstr)))
            {
                Log.PatchFail(il);
                return;
            }
            c.EmitNetworkServerActive();
            c.Emit(OpCodes.Brfalse_S, nextInstr);
        }

        [HarmonyPatch(typeof(DelusionChestController), nameof(DelusionChestController.Start))]
        [HarmonyILManipulator]
        internal static void FixDelusionChestControllerStart(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<DelusionChestController>(nameof(DelusionChestController._pickupPickerController)),
                x => x.MatchLdcI4(out _),
                x => x.MatchCallOrCallvirt<PickupPickerController>(nameof(PickupPickerController.SetAvailable)),
                x => x.MatchAny(out nextInstr)))
            {
                Log.PatchFail(il);
                return;
            }
            c.EmitNetworkServerActive();
            c.Emit(OpCodes.Brfalse_S, nextInstr);
        }

        [HarmonyPatch(typeof(DevotionInventoryController), nameof(DevotionInventoryController.Awake))]
        [HarmonyILManipulator]
        internal static void FixDevotionInventoryControllerAwake(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<DevotionInventoryController>(nameof(DevotionInventoryController._devotionMinionInventory)),
                x => x.MatchLdsfld(typeof(CU8Content.Items), nameof(CU8Content.Items.LemurianHarness)),
                x => x.MatchLdcI4(out _),
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GiveItem)),
                x => x.MatchAny(out nextInstr)))
            {
                Log.PatchFail(il);
                return;
            }
            c.EmitNetworkServerActive();
            c.Emit(OpCodes.Brfalse_S, nextInstr);
        }

        [HarmonyPatch(typeof(MasterDropDroplet), nameof(MasterDropDroplet.DropItems))]
        [HarmonyILManipulator]
        internal static void FixMasterDropDropletDropItems(ILContext il)
        {
            var c = new ILCursor(il);
            var nextInstr = c.Instrs[c.Instrs.Count - 1];
            c.EmitNetworkServerActive();
            c.Emit(OpCodes.Brfalse_S, nextInstr);
        }

        [HarmonyPatch(typeof(MinionOwnership.MinionGroup), nameof(MinionOwnership.MinionGroup.AddMinion))]
        [HarmonyILManipulator]
        internal static void FixMinionGroupAddMinion(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel nextLabel = null;
            if (!c.TryGotoNext(
                MoveType.After,
                x => x.MatchLdloc(1),
                x => x.MatchOpImplicit(),
                x => x.MatchBrfalse(out nextLabel)))
            {
                Log.PatchFail(il);
                return;
            }
            c.EmitNetworkServerActive();
            c.Emit(OpCodes.Brfalse_S, nextLabel);
        }

        [HarmonyPatch(typeof(MinionOwnership.MinionGroup), nameof(MinionOwnership.MinionGroup.RemoveMinion))]
        [HarmonyILManipulator]
        internal static void FixMinionGroupRemoveMinion(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel nextLabel = null;
            if (!c.TryGotoNext(
                MoveType.After,
                x => x.MatchLdloc(0),
                x => x.MatchOpImplicit(),
                x => x.MatchBrfalse(out nextLabel)))
            {
                Log.PatchFail(il);
                return;
            }
            c.EmitNetworkServerActive();
            c.Emit(OpCodes.Brfalse_S, nextLabel);
        }
    }
}
