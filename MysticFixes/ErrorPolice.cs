using System;
using EntityStates.LunarExploderMonster;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Items;
using RoR2.Orbs;
using RoR2.Projectile;
using RoR2.Stats;
using UnityEngine;

namespace MiscFixes
{
    [HarmonyPatch]
    public class FixVanilla
    {
        /// <summary>
        /// call to DotController.GetDotDef(dotDefIndex); and pops the result. useless call, probably leftover code. functions fine without it.
        /// DotController.GetDotDef doesn't use ArrayUtils.GetSafe so DotDefIndex.None throws
        /// </summary>
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
            else MiscFixesPlugin.Logger.LogError($"IL hook failed for CharacterBody.TriggerEnemyDebuffs");
        }

        /// <summary>
        /// NullReferenceException: 
        /// 
        /// RoR2.CharacterBody.TryGiveFreeUnlockWhenLevelUp() (at<a43009bc6a5f4aee99e5521ef176a18d>:IL_0006)
        /// RoR2.CharacterBody.OnLevelUp() (at<a43009bc6a5f4aee99e5521ef176a18d>:IL_0006)
        /// RoR2.CharacterBody.OnCalculatedLevelChanged(System.Single oldLevel, System.Single newLevel) (at<a43009bc6a5f4aee99e5521ef176a18d>:IL_0017)
        /// </summary>
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
            else MiscFixesPlugin.Logger.LogError($"IL hook failed for CharacterBody.TryGiveFreeUnlockWhenLevelUp");
        }

        /// <summary>
        /// orb calling target.healthComponent.body OnArrival and never null checking it.
        /// probably shouldn't match the sound string, it should just jump right over the foreach loop
        /// 
        /// NullReferenceException:
        /// 
        /// RoR2.Orbs.VineOrb.OnArrival() (at<a43009bc6a5f4aee99e5521ef176a18d>:IL_0067)
        /// RoR2.Orbs.OrbManager.FixedUpdate() (at<a43009bc6a5f4aee99e5521ef176a18d>:IL_00A3)
        /// </summary>
        [HarmonyPatch(typeof(VineOrb), nameof(VineOrb.OnArrival))]
        [HarmonyILManipulator]
        public static void VineOrbArrival(ILContext il)
        {
            var c = new ILCursor(il);

            var nextInstr = c.Next;
            c.Emit(OpCodes.Ldarg_0);
            c.Emit<Orb>(OpCodes.Ldfld, nameof(Orb.target));
            c.EmitOpImplicit();
            c.Emit(OpCodes.Brtrue_S, nextInstr);
            c.Emit(OpCodes.Ret);

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
            else MiscFixesPlugin.Logger.LogError($"IL hook failed for VineOrb.OnArrival");
        }

        /// <summary>
        /// Unknown cause, possibly vanilla. highly likely the ghost is null, but this never gets caught by the catalog.
        /// Skipping the following section removes the error
        /// 
        /// if (isPrediction)
        ///      ghost.predictionTransform = transform;
        ///  else
        ///     ghost.authorityTransform = transform;
        /// ghost.enabled = true;
        /// 
        /// NullReferenceException:
        /// (wrapper dynamic-method) RoR2.Projectile.ProjectileController.DMD<RoR2.Projectile.ProjectileController::Start>(RoR2.Projectile.ProjectileController)
        /// </summary>
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
            else MiscFixesPlugin.Logger.LogError($"IL hook failed for ProjectileController.Start");
        }

        /// <summary>
        /// Checks if the component exists, which should be the case for pre-sots overlay code
        /// If true, it creates a temporary overlay instance from the component for backwards compatibility 
        /// 
        /// ArgumentNullException: Parameter name: source
        /// 
        /// UnityEngine.Material..ctor(UnityEngine.Material source) (at<a20b3695b7ce4017b7981f9d06962bd1>:IL_0008)
        /// RoR2.TemporaryOverlayInstance.SetupMaterial() (at<a43009bc6a5f4aee99e5521ef176a18d>:IL_000D)
        /// RoR2.TemporaryOverlayInstance.AddToCharacterModel(RoR2.CharacterModel characterModel) (at<a43009bc6a5f4aee99e5521ef176a18d>:IL_0000)
        /// RoR2.TemporaryOverlay.AddToCharacerModel(RoR2.CharacterModel characterModel) (at<a43009bc6a5f4aee99e5521ef176a18d>:IL_0006)
        /// 
        /// UnityEngine.Material..ctor(UnityEngine.Material source) (at<a20b3695b7ce4017b7981f9d06962bd1>:IL_0008)
        /// RoR2.TemporaryOverlayInstance.SetupMaterial() (at<a43009bc6a5f4aee99e5521ef176a18d>:IL_000D)
        /// RoR2.TemporaryOverlayInstance.Start() (at<a43009bc6a5f4aee99e5521ef176a18d>:IL_0009)
        /// RoR2.TemporaryOverlay.Start() (at<a43009bc6a5f4aee99e5521ef176a18d>:IL_0006)
        /// </summary>
        [HarmonyPatch(typeof(TemporaryOverlayInstance), nameof(TemporaryOverlayInstance.SetupMaterial))]
        [HarmonyPrefix]
        public static void SetupMaterial(TemporaryOverlayInstance __instance)
        {
            if (!__instance.originalMaterial && __instance.ValidateOverlay())
            {
                __instance.componentReference.CopyDataFromPrefabToInstance();
            }
        }

        /// <summary>
        /// NullReferenceException:
        /// 
        /// UnityEngine.Component.GetComponent[T] () (at<a20b3695b7ce4017b7981f9d06962bd1>:IL_0021)
        /// RoR2.Stats.StatManager.ProcessGoldEvents() (at<a43009bc6a5f4aee99e5521ef176a18d>:IL_0017)
        /// RoR2.Stats.StatManager.ProcessEvents() (at<a43009bc6a5f4aee99e5521ef176a18d>:IL_000F)
        /// RoR2.RoR2Application.FixedUpdate() (at<a43009bc6a5f4aee99e5521ef176a18d>:IL_0024)
        /// </summary>
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
            else MiscFixesPlugin.Logger.LogError($"IL hook failed for StatManager.ProcessGoldEvents 1");

            if (c.TryGotoNext(
                    x => x.MatchCallOrCallvirt<Component>(nameof(Component.GetComponent)),
                    x => x.MatchDup(),
                    x => x.MatchBrtrue(out _)
                ))
            {
                c.GotoNext(MoveType.After, x => x.MatchDup());

                c.EmitOpImplicit();
            }
            else MiscFixesPlugin.Logger.LogError($"IL hook failed for StatManager.ProcessGoldEvents 2");
        }

        /// <summary>
        /// SceneInfo.instance.sceneDef.cachedName getting called from OnDisable is just never gonna be error proof
        /// </summary>
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
            else MiscFixesPlugin.Logger.LogError($"IL hook failed for MinionLeashBodyBehavior.OnDisable");
        }

        /// <summary>
        /// ownerbody is null, also gameObject.transform sucks
        /// 
        /// Vector3 position = ownerBody.gameObject.transform.position;
        /// </summary>
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
            else MiscFixesPlugin.Logger.LogError($"IL hook failed for ElusiveAntlersPickup.FixedUpdate");
        }

        /// <summary>
        /// seen most often when the fog is attacking enemies
        /// </summary>
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
            else MiscFixesPlugin.Logger.LogError($"IL hook failed for FogDamageController.EvaluateTeam");
        }

        /// <summary>
        /// nullchecking modelLocator and subsequent transforms with ?. is bad don't do it
        /// only ok for prefabs
        /// </summary>
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
            else MiscFixesPlugin.Logger.LogError($"IL hook failed for DeathState.FixedUpdate");
        }

        /// <summary>
        /// something on sundered grove throws an error here periodically
        /// </summary>
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
            else MiscFixesPlugin.Logger.LogError($"IL hook failed for MPEventSystem.Update");
        }

        /// <summary>
        /// gold coast plus revived chest is fucked, and that's like the only time ive ever seen it
        /// </summary>
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
            else MiscFixesPlugin.Logger.LogError($"IL hook failed for Interactor.FindBestInteractableObject");

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
            else MiscFixesPlugin.Logger.LogError($"IL hook failed for Interactor.FindBestInteractableObject2");
        }
    }
}
