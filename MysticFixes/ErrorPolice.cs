using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EntityStates.LightningStorm;
using EntityStates.LunarExploderMonster;
using Facepunch.Steamworks;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Items;
using RoR2.Navigation;
using RoR2.Orbs;
using RoR2.Projectile;
using RoR2.Stats;
using RoR2.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MiscFixes
{
    [HarmonyPatch]
    public class FixVanilla
    {
        [HarmonyPatch(typeof(FlickerLight), nameof(FlickerLight.Update))]
        [HarmonyPrefix]
        public static bool Ugh(FlickerLight __instance) => __instance.light;

        [HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.TriggerEnemyDebuffs))]
        [HarmonyILManipulator]
        public static void WhatTheFuck(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(
                    x => x.MatchLdloc(out _),
                    x => x.MatchCallOrCallvirt<DotController>(nameof(DotController.GetDotDef)),
                    x => x.MatchPop()
                ))
            {
                var label = c.DefineLabel();
                c.Emit(OpCodes.Br, label);
                c.GotoNext(MoveType.After, x => x.MatchPop());
                c.MarkLabel(label);
            }
            else Debug.LogError($"IL hook failed for CharacterBody.TriggerEnemyDebuffs");
        }

        [HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.TryGiveFreeUnlockWhenLevelUp))]
        [HarmonyILManipulator]
        public static void FreeFortniteCard(ILContext il)
        {
            var c = new ILCursor(il);

            ILLabel retLabel = null;
            if (c.TryGotoNext(MoveType.After,
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(CharacterBody), nameof(CharacterBody.inventory)))) &&
                c.TryFindNext(out _,
                    x => x.MatchBle(out retLabel)
                ))
            {
                var callLabel = c.DefineLabel();

                c.Emit(OpCodes.Dup);
                c.EmitOpImplicit();
                c.Emit(OpCodes.Brtrue, callLabel);

                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Br, retLabel);

                c.MarkLabel(callLabel);
            }
            else Debug.LogError($"IL hook failed for CharacterBody.TryGiveFreeUnlockWhenLevelUp");
        }

        [HarmonyPatch(typeof(VineOrb), nameof(VineOrb.OnArrival))]
        [HarmonyILManipulator]
        public static void VineOrbArrival(ILContext il)
        {
            var c = new ILCursor(il);

            int bodyLoc = 0;
            if (c.TryGotoNext(MoveType.After,
                    x => x.MatchLdfld<HealthComponent>(nameof(HealthComponent.body)),
                    x => x.MatchStloc(out bodyLoc)) &&
                c.TryFindNext(out _, 
                    x => x.MatchLdstr("Play_item_proc_triggerEnemyDebuffs")
                ))
            {
                var label = c.DefineLabel();
                c.Emit(OpCodes.Ldloc, bodyLoc);
                c.EmitOpImplicit();
                c.Emit(OpCodes.Brfalse, label);

                c.GotoNext(x => x.MatchLdstr("Play_item_proc_triggerEnemyDebuffs"));
                c.MarkLabel(label);
            }
            else Debug.LogError($"IL hook failed for VineOrb.OnArrival");
        }

        [HarmonyPatch(typeof(BossGroup), nameof(BossGroup.OnDefeatedServer))]
        [HarmonyILManipulator]
        public static void BossGroupEvent(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(Run), nameof(Run.instance))),
                    x => x.MatchLdarg(0),
                    x => x.MatchCallOrCallvirt<Run>(nameof(Run.OnServerBossDefeated))
                ))
            {
                var retLabel = c.DefineLabel();
                var callLabel = c.DefineLabel();
                c.Index++;

                c.Emit(OpCodes.Dup);
                c.EmitOpImplicit();
                c.Emit(OpCodes.Brfalse, callLabel);

                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Br, retLabel);

                c.MarkLabel(callLabel);
                c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt<Run>(nameof(Run.OnServerBossDefeated)));
                c.MarkLabel(retLabel);
            }
            else Debug.LogError($"IL hook failed for BossGroup.OnDefeatedServer");
        }

        [HarmonyPatch(typeof(ProjectileController), nameof(ProjectileController.Start))]
        [HarmonyILManipulator]
        public static void ProjectileStart(ILContext il)
        {
            var c = new ILCursor(il);

            ILLabel label = null;
            if (c.TryGotoNext(
                    x => x.MatchCallOrCallvirt(AccessTools.PropertySetter(typeof(ProjectileController), nameof(ProjectileController.shouldPlaySounds))),
                    x => x.MatchLdloc(out _),
                    x => x.MatchOpImplicit(),
                    x => x.MatchBrfalse(out label)) &&
                c.TryGotoNext(MoveType.AfterLabel,
                    x => x.MatchLdarg(0),
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(ProjectileController), nameof(ProjectileController.isPrediction))),
                    x => x.MatchBrfalse(out _)
                ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Call, AccessTools.PropertyGetter(typeof(ProjectileController), nameof(ProjectileController.ghost)));
                c.EmitOpImplicit();
                c.Emit(OpCodes.Brfalse, label);
            }
            else Debug.LogError($"IL hook failed for ProjectileController.Start");
        }

        [HarmonyPatch(typeof(TemporaryOverlayInstance), nameof(TemporaryOverlayInstance.SetupMaterial))]
        [HarmonyPrefix]
        public static void SetupMaterial(TemporaryOverlayInstance __instance)
        {
            if (!__instance.originalMaterial && __instance.ValidateOverlay())
            {
                __instance.componentReference.CopyDataFromPrefabToInstance();
            }
        }

        [HarmonyPatch(typeof(StatManager), nameof(StatManager.ProcessGoldEvents))]
        [HarmonyILManipulator]
        public static void ProcessGold(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(
                    x => x.MatchLdfld<StatManager.GoldEvent>(nameof(StatManager.GoldEvent.characterMaster)),
                    x => x.MatchDup(),
                    x => x.MatchBrtrue(out _)
                ))
            {
                c.GotoNext(MoveType.After, x => x.MatchDup());

                c.EmitOpImplicit();
            }
            else Debug.LogError($"IL hook failed for StatManager.ProcessGoldEvents 1");

            if (c.TryGotoNext(
                    x => x.MatchCallOrCallvirt<Component>(nameof(Component.GetComponent)),
                    x => x.MatchDup(),
                    x => x.MatchBrtrue(out _)
                ))
            {
                c.GotoNext(MoveType.After, x => x.MatchDup());

                c.EmitOpImplicit();
            }
            else Debug.LogError($"IL hook failed for StatManager.ProcessGoldEvents 2");
        }

        [HarmonyPatch(typeof(DevotedLemurianController), nameof(DevotedLemurianController.TryTeleport), MethodType.Enumerator)]
        [HarmonyILManipulator]
        private static void DevotionTele(ILContext il)
        {
            var c = new ILCursor(il);

            ILLabel nextLoopLabel = null;
            FieldReference bodyField = null;
            if (c.TryGotoNext(
                    x => x.MatchLdloc(out _),
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(List<NodeGraph.NodeIndex>), nameof(List<NodeGraph.NodeIndex>.Count))),
                    x => x.MatchBrfalse(out _),
                    x => x.MatchBr(out nextLoopLabel)) &&
                c.TryGotoPrev(
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld(out _),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld(out bodyField),
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(Component), nameof(Component.transform))),
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.position)))
                ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, bodyField);
                c.EmitOpImplicit();
                c.Emit(OpCodes.Brfalse, nextLoopLabel);
            }
            else Debug.LogError($"IL hook failed for DevotedLemurianController.TryTeleport");
        }

        [HarmonyPatch(typeof(MinionLeashBodyBehavior), nameof(MinionLeashBodyBehavior.OnDisable))]
        [HarmonyILManipulator]
        private static void MinionLeash(ILContext il)
        {
            var c = new ILCursor(il);

            if (new ILCursor(il).TryGotoNext(
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(SceneInfo), nameof(SceneInfo.instance))),
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(SceneInfo), nameof(SceneInfo.sceneDef))),
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(SceneDef), nameof(SceneDef.cachedName)))
                ))
            {
                c.Index++;

                var compareLabel = c.DefineLabel();
                var getSceneDefLabel = c.DefineLabel();
                var getCachedNameLabel = c.DefineLabel();

                // prev = SceneInfo.instance
                c.Emit(OpCodes.Dup);
                c.EmitOpImplicit();
                c.Emit(OpCodes.Brtrue, getSceneDefLabel);

                c.Emit(OpCodes.Pop);
                c.Emit<string>(OpCodes.Ldsfld, nameof(string.Empty));
                c.Emit(OpCodes.Br, compareLabel);

                c.MarkLabel(getSceneDefLabel);
                c.Index++;

                // prev = SceneInfo.instance?.sceneDef
                c.Emit(OpCodes.Dup);
                c.EmitOpImplicit();
                c.Emit(OpCodes.Brtrue, getCachedNameLabel);

                c.Emit(OpCodes.Pop);
                c.Emit<string>(OpCodes.Ldsfld, nameof(string.Empty));
                c.Emit(OpCodes.Br, compareLabel);

                c.MarkLabel(getCachedNameLabel);
                c.Index++;

                // prev = SceneInfo.instance?.sceneDef?.cachedName
                c.MarkLabel(compareLabel);
            }
            else Debug.LogError($"IL hook failed for MinionLeashBodyBehavior.OnDisable");
        }

        [HarmonyPatch(typeof(ElusiveAntlersPickup), nameof(ElusiveAntlersPickup.Start))]
        [HarmonyILManipulator]
        private static void FixAntlerStart(ILContext il)
        {
            var c = new ILCursor(il);

            ILLabel retLabel = null;
            if (new ILCursor(il).TryGotoNext(x => x.MatchBrfalse(out retLabel)) &&
                c.TryGotoNext(MoveType.After, x => x.MatchLdfld<ElusiveAntlersPickup>(nameof(ElusiveAntlersPickup.ownerBody))))
            {
                var callLabel = c.DefineLabel();

                c.Emit(OpCodes.Dup);
                c.EmitOpImplicit();
                c.Emit(OpCodes.Brtrue, callLabel);

                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Br, retLabel);

                c.MarkLabel(callLabel);
            }
            else Debug.LogError($"IL hook failed for ElusiveAntlersPickup.Start");
        }

        [HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.OnShardDestroyed))]
        [HarmonyILManipulator]
        private static void FixRpcShardDestroy(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchLdarg(1),
                    x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.CallRpcOnShardDestroyedClient))
                ))
            {
                var retLabel = c.DefineLabel();

                c.Emit(OpCodes.Ldarg_0);
                c.EmitOpImplicit();
                c.Emit(OpCodes.Brfalse, retLabel);

                c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.CallRpcOnShardDestroyedClient)));
                c.MarkLabel(retLabel);
            }
            else Debug.LogError($"IL hook failed for CharacterBody.OnShardDestroyed");
        }

        [HarmonyILManipulator, HarmonyPatch(typeof(ElusiveAntlersPickup), nameof(ElusiveAntlersPickup.FixedUpdate))]
        private static void ElusiveAntlersPickup_FixedUpdate(ILContext il)
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(
                    x => x.MatchLdfld<ElusiveAntlersPickup>(nameof(ElusiveAntlersPickup.ownerBody)),
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(Component), nameof(Component.gameObject))),
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(GameObject), nameof(GameObject.transform))),
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.position))),
                    x => x.MatchStloc(out _)
                ))
            {
                c.Index++;

                var stLocLabel = c.DefineLabel();
                var getGameObjectLabel = c.DefineLabel();

                c.Emit(OpCodes.Dup);
                c.EmitOpImplicit();
                c.Emit(OpCodes.Brtrue, getGameObjectLabel);

                c.Emit(OpCodes.Pop);
                c.Emit<Vector3>(OpCodes.Ldsfld, nameof(Vector3.zeroVector));
                c.Emit(OpCodes.Br, stLocLabel);

                c.MarkLabel(getGameObjectLabel);

                c.GotoNext(x => x.MatchStloc(out _));

                c.MarkLabel(stLocLabel);
            }
            else Debug.LogError($"IL hook failed for ElusiveAntlersPickup.FixedUpdate");
        }

        [HarmonyPatch(typeof(FogDamageController), nameof(FogDamageController.EvaluateTeam))]
        [HarmonyILManipulator]
        public static void FixFog(ILContext il)
        {
            var c = new ILCursor(il);

            int locTC = 0, locBody = 0;
            ILLabel label = null;
            if (c.TryGotoNext(
                    x => x.MatchLdloc(out locTC),
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(TeamComponent), nameof(TeamComponent.body))),
                    x => x.MatchStloc(out locBody)) &&
                c.TryFindPrev(out _,
                    x => x.MatchBr(out label)
                ))
            {
                c.Emit(OpCodes.Ldloc, locTC);
                c.EmitOpImplicit();
                c.Emit(OpCodes.Brfalse, label);

                c.GotoNext(MoveType.After, x => x.MatchStloc(locBody));

                c.Emit(OpCodes.Ldloc, locBody);
                c.EmitOpImplicit();
                c.Emit(OpCodes.Brfalse, label);
            }
            else Debug.LogError($"IL hook failed for FogDamageController.EvaluateTeam");
        }

        [HarmonyPatch(typeof(TetherVfxOrigin), nameof(TetherVfxOrigin.AddTether))]
        [HarmonyILManipulator]
        public static void FixTether(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<TetherVfxOrigin>(nameof(TetherVfxOrigin.onTetherAdded)),
                    x => x.MatchLdloc(0),
                    x => x.MatchLdarg(1),
                    x => x.MatchCallOrCallvirt<TetherVfxOrigin.TetherAddDelegate>(nameof(TetherVfxOrigin.TetherAddDelegate.Invoke))
                ))
            {
                var retLabel = c.MarkLabel();

                c.GotoPrev(MoveType.After, x => x.MatchLdfld<TetherVfxOrigin>(nameof(TetherVfxOrigin.onTetherAdded)));

                var callLabel = c.DefineLabel();

                c.Emit(OpCodes.Dup);
                c.Emit(OpCodes.Brtrue, callLabel);

                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Br, retLabel);

                c.MarkLabel(callLabel);
            }
            else Debug.LogError($"IL hook failed for TetherVfxOrigin.AddTether");
        }

        [HarmonyPatch(typeof(CharacterMaster), nameof(CharacterMaster.TrueKill), [typeof(GameObject), typeof(GameObject), typeof(DamageTypeCombo)])]
        [HarmonyILManipulator]
        public static void FixKill(ILContext il)
        {
            var c = new ILCursor(il);

            ILLabel label = null, label2 = null;
            if (c.TryGotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchCallOrCallvirt<CharacterMaster>(nameof(CharacterMaster.GetBody)),
                    x => x.MatchLdsfld(out _),
                    x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.HasBuff))) &&
                c.TryFindNext(out _,
                    x => x.MatchBrfalse(out label)
                ))
            {
                c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt<CharacterMaster>(nameof(CharacterMaster.GetBody)));

                var bodyLabel = c.DefineLabel();

                c.Emit(OpCodes.Dup);
                c.EmitOpImplicit();
                c.Emit(OpCodes.Brtrue, bodyLabel);

                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Br, label);

                c.MarkLabel(bodyLabel);
            }
            else Debug.LogError($"IL hook failed for CharacterMaster.TrueKill 1");

            if (c.TryGotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchCallOrCallvirt<CharacterMaster>(nameof(CharacterMaster.GetBody)),
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(CharacterBody), nameof(CharacterBody.equipmentSlot)))) &&
                c.TryFindNext(out _,
                    x => x.MatchBrfalse(out label2)
                ))
            {
                c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt<CharacterMaster>(nameof(CharacterMaster.GetBody)));

                var bodyLabel2 = c.DefineLabel();

                c.Emit(OpCodes.Dup);
                c.EmitOpImplicit();
                c.Emit(OpCodes.Brtrue, bodyLabel2);

                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Br, label2);

                c.MarkLabel(bodyLabel2);
            }
            else Debug.LogError($"IL hook failed for CharacterMaster.TrueKill 2");
        }

        [HarmonyPatch(typeof(DeathState), nameof(DeathState.FixedUpdate))]
        [HarmonyILManipulator]
        public static void FixExplode(ILContext il)
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
            else Debug.LogError($"IL hook failed for DeathState.FixedUpdate");
        }

        [HarmonyPatch(typeof(MPEventSystem), nameof(MPEventSystem.Update))]
        [HarmonyILManipulator]
        public static void FixThisFuckingBullshitGearbox(ILContext il)
        {
            ILCursor[] c = null;
            if (new ILCursor(il).TryFindNext(out c,
                    x => x.MatchCall(AccessTools.PropertyGetter(typeof(EventSystem), nameof(EventSystem.current))),
                    x => x.MatchCall(AccessTools.PropertySetter(typeof(EventSystem), nameof(EventSystem.current)))
                ))
            {
                c[0].Remove();
                c[1].Remove();
            }
            else Debug.LogError($"IL hook failed for MPEventSystem.Update");
        }

        [HarmonyPatch(typeof(RouletteChestController.Idle), nameof(RouletteChestController.Idle.OnEnter))]
        [HarmonyILManipulator]
        public static void Spinny(ILContext il)
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(RouletteChestController.RouletteChestControllerBaseState),
                                                                          nameof(RouletteChestController.RouletteChestControllerBaseState.rouletteChestController))),
                    x => x.MatchLdfld<RouletteChestController>(nameof(RouletteChestController.purchaseInteraction)),
                    x => x.MatchLdcI4(out _),
                    x => x.MatchCallOrCallvirt(AccessTools.PropertySetter(typeof(PurchaseInteraction), nameof(PurchaseInteraction.Networkavailable)))
                ))
            {
                var retLabel = c.DefineLabel();

                c.Emit(OpCodes.Br, retLabel);
                c.GotoNext(MoveType.After, x => x.MatchCallOrCallvirt(AccessTools.PropertySetter(typeof(PurchaseInteraction), nameof(PurchaseInteraction.Networkavailable))));
                c.MarkLabel(retLabel);
                
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<RouletteChestController.RouletteChestControllerBaseState>>((ctrl) =>
                {
                    if (ctrl.rouletteChestController && ctrl.rouletteChestController.purchaseInteraction)
                        ctrl.rouletteChestController.purchaseInteraction.Networkavailable = true;
                });
            }
            else Debug.LogError($"IL hook failed for MPEventSystem.Update");
        }

        [HarmonyPatch(typeof(BaseSteamworks), nameof(BaseSteamworks.RunUpdateCallbacks))]
        [HarmonyFinalizer]
        public static Exception FixFacepunch(Exception __exception) => null;

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
                c.Emit(OpCodes.Call, AccessTools.DeclaredMethod(typeof(FixVanilla), nameof(FixVanilla.GetRealCount)));
            }
            else Debug.LogError($"IL hook failed for EntityStates.VoidCamp.Idle");
        }

        [HarmonyPatch(typeof(Interactor), nameof(Interactor.FindBestInteractableObject))]
        [HarmonyILManipulator]
        public static void FixInteraction(ILContext il)
        {
            var c = new ILCursor(il);

            int loc = 0;
            ILLabel label = null;
            if (c.TryGotoNext(MoveType.After,
                    x => x.MatchLdloca(out loc),
                    x => x.MatchCall<EntityLocator>(nameof(EntityLocator.HasEntityLocator)),
                    x => x.MatchBrfalse(out label)
                ))
            {
                c.Emit(OpCodes.Ldloc, loc);
                c.Emit<EntityLocator>(OpCodes.Ldfld, nameof(EntityLocator.entity));
                c.EmitOpImplicit();
                c.Emit(OpCodes.Brfalse, label);
            }
            else Debug.LogError($"IL hook failed for Interactor.FindBestInteractableObject");

            int loc2 = 0;
            ILLabel label2 = null;
            if (c.TryGotoNext(MoveType.After,
                    x => x.MatchLdloca(out loc2),
                    x => x.MatchCall<EntityLocator>(nameof(EntityLocator.HasEntityLocator)),
                    x => x.MatchBrfalse(out label2)
                ))
            {
                c.Emit(OpCodes.Ldloc, loc2);
                c.Emit<EntityLocator>(OpCodes.Ldfld, nameof(EntityLocator.entity));
                c.EmitOpImplicit();
                c.Emit(OpCodes.Brfalse, label2);
            }
            else Debug.LogError($"IL hook failed for Interactor.FindBestInteractableObject2");
        }

        public static int GetRealCount(ReadOnlyCollection<TeamComponent> teamMembers)
        {
            int count = 0;
            foreach (var member in teamMembers)
            {
                var body = member ? member.body : null;
                if (body && body.master && body.healthComponent && body.healthComponent.alive)
                    count++;
            }
            return count;
        }
    }
}
