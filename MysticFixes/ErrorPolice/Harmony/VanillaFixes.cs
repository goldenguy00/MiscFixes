extern alias Rewired_Core_NS;

using System;
using System.Collections.Generic;
using EntityStates;
using EntityStates.LunarExploderMonster;
using HarmonyLib;
using MiscFixes.Modules;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Rewired_Core_NS::Rewired;
using RoR2;
using RoR2.Achievements;
using RoR2.CharacterAI;
using RoR2.Items;
using RoR2.Navigation;
using RoR2.Orbs;
using RoR2.Projectile;
using RoR2.Stats;
using RoR2.UI;
using UnityEngine;
using RPServer = RoR2.Achievements.Chef.RolyPolyHitFiveAirEnemies.RolyPolyHitFiveAirEnemiesServerAchievement;

namespace MiscFixes.ErrorPolice.Harmony
{
    [HarmonyPatch]
    public class VanillaFixes
    {
        /// <summary>
        /// null array elements ig, nullcheck all cuz its not super commonly called
        /// RoR2.TemporaryVisualEffect.RebuildVisualComponents() (at<c0d9c70405a04cceacc72f65157d1ebd>:IL_0057)
        /// RoR2.TemporaryVisualEffect.OnDestroy() (at<c0d9c70405a04cceacc72f65157d1ebd>:IL_0000)
        /// </summary>
        [HarmonyPatch(typeof(TemporaryVisualEffect), nameof(TemporaryVisualEffect.RebuildVisualComponents))]
        [HarmonyILManipulator]
        public static void TemporaryVisualEffect_RebuildVisualComponents(ILContext il)
        {
            var c = new ILCursor(il);

            Instruction instr = null;
            while (c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdelemRef(),
                    x => x.MatchLdcI4(out _),
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(Behaviour), nameof(Behaviour.enabled))),
                    x => x.MatchAny(out instr)
                ))
            {
                var setEnabledLabel = c.DefineLabel();
                c.Index++;
                c.Emit(OpCodes.Dup);
                c.EmitOpImplicit();
                c.Emit(OpCodes.Brfalse_S, instr);
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Br_S, setEnabledLabel);
                c.MarkLabel(setEnabledLabel);
            }

        }
        /// <summary>
        ///  cachedCharacterMotor = GetCurrentBody().GetComponent<CharacterMotor>();
        /// </summary>

        [HarmonyPatch(typeof(RPServer), nameof(RPServer.GetCharacterMotor))]
        [HarmonyILManipulator]
        public static void RolyPolyHitFiveAirEnemiesServerAchievement_GetCharacterMotor(ILContext il)
        {
            var c = new ILCursor(il);

            if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchCallOrCallvirt<BaseServerAchievement>(nameof(BaseServerAchievement.GetCurrentBody)),
                    x => x.MatchCallOrCallvirt<Component>(nameof(Component.GetComponent))
                ))
            {
                Log.PatchFail(il);
                return;
            }
            
            c.Index--;

            var getComponentLabel = c.DefineLabel();
            var skipGetComponentLabel = c.DefineLabel();

            c.Emit(OpCodes.Dup);
            c.EmitOpImplicit();
            c.Emit(OpCodes.Brtrue_S, getComponentLabel);

            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldnull);
            c.Emit(OpCodes.Br_S, skipGetComponentLabel);

            c.MarkLabel(getComponentLabel);
            c.Index++;
            c.MarkLabel(skipGetComponentLabel);
            
        }
        /// <summary>
        /// Affix Aurelionite calling transform.position from update
        /// Prevent running update when body is null
        /// 
        /// [Error  : Unity Log] NullReferenceException
        /// Stack trace:
        /// UnityEngine.Transform.get_position() (at:IL_0000)
        /// RoR2.AffixAurelioniteBehavior.StepPredictAndFireWarningProjectile() (at:IL_0046)
        /// RoR2.AffixAurelioniteBehavior.Update() (at:IL_0059)
        /// </summary>
        [HarmonyPatch(typeof(AffixAurelioniteBehavior), nameof(AffixAurelioniteBehavior.Update))]
        [HarmonyPrefix]
        public static bool AffixAurelioniteBehavior_Update(AffixAurelioniteBehavior __instance) => __instance.body?.coreTransform;

        /// <summary>
        /// RoR2.CharacterBody.HandleDisableAllSkillsDebuffg__HandleSkillDisableState
        /// if (NetworkServer.active)
        ///     this.inventory.SetEquipmentDisabled(_disable);
        /// 
        /// inventory shouldnt be null, but jsut in case
        /// </summary>
        [HarmonyPatch(typeof(CharacterBody), "<HandleDisableAllSkillsDebuff>g__HandleSkillDisableState|389_0")]
        [HarmonyILManipulator]
        public static void CharacterBody_HandleDisableAllSkillsDebuff(ILContext il)
        {
            var c = new ILCursor(il) { Index = il.Instrs.Count - 1 };

            ILLabel retLabel = null;
            if (c.TryGotoPrev(MoveType.Before,
                    x => x.MatchNetworkServerActive(),
                    x => x.MatchBrfalse(out retLabel),
                    x => x.MatchLdarg(0),
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(CharacterBody), nameof(CharacterBody.inventory))),
                    x => x.MatchLdarg(out _),
                    x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.SetEquipmentDisabled))
                ))
            {
                // c.next == ldarg_0
                c.Index += 2;

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Call, AccessTools.PropertyGetter(typeof(CharacterBody), nameof(CharacterBody.inventory)));
                c.EmitOpImplicit();
                c.Emit(OpCodes.Brfalse, retLabel);
            }
            else Log.PatchFail(il);
        }

        /// <summary>
        /// NullReferenceException:
        /// RoR2.CharacterBody.TryGiveFreeUnlockWhenLevelUp() (at:IL_0006)
        /// RoR2.CharacterBody.OnLevelUp() (at:IL_0006)
        /// RoR2.CharacterBody.OnCalculatedLevelChanged(System.Single oldLevel, System.Single newLevel) (at:IL_0017)
        /// </summary>
        [HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.TryGiveFreeUnlockWhenLevelUp))]
        [HarmonyILManipulator]
        public static void CharacterBody_FreeFortniteCard(ILContext il)
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
            else Log.PatchFail(il);
        }

        /// <summary>
        /// The method never null checks target, which can lead to multiple body.gameObject NREs.
        /// The dotDef can also be null, in which case we should continue to the next iteration.
        /// </summary>
        [HarmonyPatch(typeof(VineOrb), nameof(VineOrb.OnArrival))]
        [HarmonyILManipulator]
        public static void VineOrb_OnArrival(ILContext il, ILLabel retLabel)
        {
            var c = new ILCursor(il);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit<Orb>(OpCodes.Ldfld, nameof(Orb.target));
            c.EmitOpImplicit();
            c.Emit(OpCodes.Brfalse_S, retLabel);

            // The check for null dotDef isn't necessary anymore in vanilla, but should stay in because of mods and good principles
            if (c.TryGotoNext(MoveType.After,
                    x => x.MatchCallOrCallvirt<GlobalEventManager>(nameof(GlobalEventManager.ProcDeathMark))
                ))
            {
                var continueLabel = c.DefineLabel();
                c.MarkLabel(continueLabel);
                var dotLoc = 0;
                if (c.TryGotoPrev(MoveType.After,
                        x => x.MatchCallOrCallvirt<DotController>(nameof(DotController.GetDotDef)),
                        x => x.MatchStloc(out dotLoc)
                    ))
                {
                    c.Emit(OpCodes.Ldloc, dotLoc);
                    c.EmitOpImplicit();
                    c.Emit(OpCodes.Brfalse_S, continueLabel);
                }
                else Log.PatchFail(il.Method.Name + " #2");
            }
            else Log.PatchFail(il.Method.Name + " #1");
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
        /// (wrapper dynamic-method) RoR2.Projectile.ProjectileController.DMDRoR2.Projectile.ProjectileController::Start(RoR2.Projectile.ProjectileController)
        /// </summary>
        [HarmonyPatch(typeof(ProjectileController), nameof(ProjectileController.Start))]
        [HarmonyILManipulator]
        public static void ProjectileController_Start(ILContext il)
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
            else Log.PatchFail(il);
        }

        /// <summary>
        /// NullReferenceException:
        /// 
        /// UnityEngine.Component.GetComponent[T] () (at:IL_0021)
        /// RoR2.Stats.StatManager.ProcessGoldEvents() (at:IL_0017)
        /// RoR2.Stats.StatManager.ProcessEvents() (at:IL_000F)
        /// RoR2.RoR2Application.FixedUpdate() (at:IL_0024)
        /// </summary>
        [HarmonyPatch(typeof(StatManager), nameof(StatManager.ProcessGoldEvents))]
        [HarmonyILManipulator]
        public static void StatManager_ProcessGoldEvents(ILContext il)
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
            else Log.PatchFail(il.Method.Name + " 1");

            if (c.TryGotoNext(
                    x => x.MatchCallOrCallvirt<Component>(nameof(Component.GetComponent)),
                    x => x.MatchDup(),
                    x => x.MatchBrtrue(out _)
                ))
            {
                c.GotoNext(MoveType.After, x => x.MatchDup());

                c.EmitOpImplicit();
            }
            else Log.PatchFail(il.Method.Name + " 2");
        }

        /// <summary>
        /// SceneInfo.instance.sceneDef.cachedName getting called from OnDisable is just never gonna be error proof
        /// </summary>
        [HarmonyPatch(typeof(MinionLeashBodyBehavior), nameof(MinionLeashBodyBehavior.OnDisable))]
        [HarmonyILManipulator]
        private static void MinionLeashBodyBehavior_OnDisable(ILContext il)
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
            else Log.PatchFail(il);
        }

        /// <summary>
        /// ownerbody is null, also gameObject.transform sucks
        /// 
        /// Vector3 position = ownerBody.gameObject.transform.position;
        /// </summary>
        [HarmonyPatch(typeof(ElusiveAntlersPickup), nameof(ElusiveAntlersPickup.FixedUpdate))]
        [HarmonyILManipulator]
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
            else Log.PatchFail(il);
        }

        /// <summary>
        /// Fixes an NRE when picking up an Elusive Antlers orb from a respawned/dead character.
        /// </summary>
        [HarmonyPatch(typeof(ElusiveAntlersPickup), nameof(ElusiveAntlersPickup.OnShardDestroyed))]
        [HarmonyILManipulator]
        private static void ElusiveAntlersPickup_OnShardDestroyed(ILContext il)
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(x => x.MatchCallOrCallvirt<Component>(nameof(Component.GetComponent))) &&
                c.TryGotoPrev(
                    x => x.MatchLdfld<ElusiveAntlersPickup>(nameof(ElusiveAntlersPickup.ownerBody)),
                    x => x.MatchDup(),
                    x => x.MatchBrtrue(out _)
                ))
            {
                c.Index += 2;
                // ownerBody?.GetComponent is not good enough, an actual null check is needed.
                c.EmitOpImplicit();
            }
            else Log.PatchFail(il);
        }

        /// <summary>
        /// seen most often when the fog is attacking enemies
        /// </summary>
        [HarmonyPatch(typeof(FogDamageController), nameof(FogDamageController.EvaluateTeam))]
        [HarmonyILManipulator]
        public static void FogDamageController_EvaluateTeam(ILContext il)
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
            else Log.PatchFail(il);
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
        /// something on sundered grove throws an error here periodically
        /// </summary>
        [HarmonyPatch(typeof(RouletteChestController.Idle), nameof(RouletteChestController.Idle.OnEnter))]
        [HarmonyILManipulator]
        public static void RouletteChestController_Idle_OnEnter(ILContext il)
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
            else Log.PatchFail(il);
        }

        /// <summary>
        /// gold coast plus revived chest is fucked, and that's like the only time ive ever seen it
        /// </summary>
        [HarmonyPatch(typeof(Interactor), nameof(Interactor.FindBestInteractableObject))]
        [HarmonyILManipulator]
        public static void Interactor_FindBestInteractableObject(ILContext il)
        {
            var c = new ILCursor(il);

            var loc = 0;
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
            else Log.PatchFail(il.Method.Name + " 1");

            var loc2 = 0;
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
            else Log.PatchFail(il.Method.Name + " 2");
        }

        /// <summary>
        /// The printer uses EffectManager for VFX without an EffectComponent, use Object.Instantiate instead.
        /// </summary>
        [HarmonyPatch(typeof(EntityStates.Duplicator.Duplicating), nameof(EntityStates.Duplicator.Duplicating.DropDroplet))]
        [HarmonyILManipulator]
        public static void Duplicating_DropDroplet(ILContext il)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(x => x.MatchCallOrCallvirt(typeof(EffectManager), nameof(EffectManager.SimpleMuzzleFlash))))
            {
                Log.PatchFail(il);
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
                        var childIndex = childLocator.FindChildIndex(muzzleName);
                        var transform = childLocator.FindChild(childIndex);
                        if (transform)
                            UnityEngine.Object.Instantiate(effectPrefab, transform.position, Quaternion.identity, transform);
                    }
                }
            });
        }

        /// <summary>
        /// Prevent an error logging when trying to detatch a child from a disabled game object.
        /// </summary>
        [HarmonyPatch(typeof(DetachParticleOnDestroyAndEndEmission), nameof(DetachParticleOnDestroyAndEndEmission.OnDisable))]
        [HarmonyILManipulator]
        public static void DetachParticleOnDestroyAndEndEmission_OnDisable(ILContext il)
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
                Log.PatchFail(il);
                return;
            }
            c.Emit(OpCodes.Ldarg_0);
            c.Emit<DetachParticleOnDestroyAndEndEmission>(OpCodes.Ldfld, nameof(DetachParticleOnDestroyAndEndEmission.particleSystem));
            c.Emit(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Component), nameof(Component.gameObject)));
            c.Emit(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(GameObject), nameof(GameObject.activeInHierarchy)));
            c.Emit(OpCodes.Brfalse, returnLabel);
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

        /// <summary>
        /// The OutsideInteractableLocker does not check if a Lemurian Egg lock already exists before creating the VFX.
        /// </summary>
        [HarmonyPatch(typeof(OutsideInteractableLocker), nameof(OutsideInteractableLocker.LockLemurianEgg))]
        [HarmonyILManipulator]
        public static void OutsideInteractableLocker_LockLemurianEgg(ILContext il)
        {
            var c = new ILCursor(il);
            var nextInstr = c.Next;
            c.Emit(OpCodes.Ldarg_0);
            c.Emit<OutsideInteractableLocker>(OpCodes.Ldfld, nameof(OutsideInteractableLocker.eggLockInfoMap));
            c.Emit(OpCodes.Ldarg_1);
            c.Emit<Dictionary<LemurianEggController, OutsideInteractableLocker.LockInfo>>(OpCodes.Callvirt, "get_Item");
            c.Emit<OutsideInteractableLocker.LockInfo>(OpCodes.Callvirt, nameof(OutsideInteractableLocker.LockInfo.IsLocked));
            c.Emit(OpCodes.Brfalse_S, nextInstr);
            c.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// PositionIndicator.alwaysVisibleObject is not null checked before every access (is null with hidden UI).
        /// </summary>
        [HarmonyPatch(typeof(PositionIndicator), nameof(PositionIndicator.UpdatePositions))]
        [HarmonyILManipulator]
        public static void PositionIndicator_UpdatePositions(ILContext il)
        {
            var c = new ILCursor(il);
            var locVarIndex = 0;
            ILLabel nextLabel = null;
            if (!c.TryGotoNext(
                x => x.MatchLdloc(out locVarIndex),
                x => x.MatchLdfld<PositionIndicator>(nameof(PositionIndicator.alwaysVisibleObject)),
                x => x.MatchLdcI4(0),
                x => x.MatchCallOrCallvirt<GameObject>(nameof(GameObject.SetActive)),
                x => x.MatchBr(out nextLabel)))
            {
                Log.PatchFail(il);
                return;
            }
            c.Index += 2;
            c.EmitOpImplicit();
            c.Emit(OpCodes.Brfalse, nextLabel.Target);
            c.Emit(OpCodes.Ldloc, locVarIndex);
            c.Emit<PositionIndicator>(OpCodes.Ldfld, nameof(PositionIndicator.alwaysVisibleObject));
        }

        /// <summary>
        /// Some Renderers in the collection can be null.
        /// </summary>
        [HarmonyPatch(typeof(Indicator), nameof(Indicator.SetVisibleInternal))]
        [HarmonyILManipulator]
        public static void Indicator_SetVisibleInternal(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction ifInstr = null;
            Instruction nextInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchLdarg(1),
                x => x.MatchCallOrCallvirt<Renderer>("set_enabled"),
                x => x.MatchAny(out nextInstr)))
            {
                Log.PatchFail(il);
                return;
            }
            ifInstr = c.Next;
            c.Emit(OpCodes.Dup);
            c.EmitOpImplicit();
            c.Emit(OpCodes.Brtrue_S, ifInstr);
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Br_S, nextInstr);
        }

        /// <summary>
        /// Iterating a list while modifying it raises an error, iterate on a copy of it instead. Occurs when Seeker respawns.
        /// </summary>
        [HarmonyPatch(typeof(CrosshairUtils.CrosshairOverrideBehavior), nameof(CrosshairUtils.CrosshairOverrideBehavior.OnDestroy))]
        [HarmonyILManipulator]
        public static void CrosshairUtils_CrosshairOverrideBehavior_OnDestroy(ILContext il)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<CrosshairUtils.CrosshairOverrideBehavior>(nameof(CrosshairUtils.CrosshairOverrideBehavior.requestList))))
            {
                Log.PatchFail(il);
                return;
            }
            c.EmitDelegate<Func<List<CrosshairUtils.OverrideRequest>, List<CrosshairUtils.OverrideRequest>>>(requestList => [.. requestList]);
        }

        /// <summary>
        /// Null check EventSystem.current, occurs when exiting a lobby.
        /// </summary>
        [HarmonyPatch(typeof(RuleChoiceController), nameof(RuleChoiceController.FindNetworkUser))]
        [HarmonyILManipulator]
        public static void RuleChoiceController_FindNetworkUser(ILContext il)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(
                x => x.MatchPop(),
                x => x.MatchLdnull(),
                // Harmony replaces ret with custom br operations for internal reasons
                x => x.Match(OpCodes.Br)))
            {
                Log.PatchFail(il.Method.Name + " #1");
                return;
            }
            var instr = c.Next;
            if (!c.TryGotoPrev(
                MoveType.After,
                x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(UnityEngine.EventSystems.EventSystem), nameof(UnityEngine.EventSystems.EventSystem.current)))))
            {
                Log.PatchFail(il.Method.Name + " #2");
                return;
            }
            c.Remove();
            c.Emit(OpCodes.Isinst, typeof(MPEventSystem));
            c.Emit(OpCodes.Dup);
            c.EmitOpImplicit();
            c.Emit(OpCodes.Brfalse, instr);
        }

        /// <summary>
        /// Prevent RewiredIntegration from running if not initialised, occurs when exiting the game.
        /// </summary>
        [HarmonyPatch(typeof(RewiredIntegrationManager), nameof(RewiredIntegrationManager.RefreshJoystickAssignment))]
        [HarmonyPrefix]
        public static bool RewiredIntegrationManager_RefreshJoystickAssignment()
        {
            return ReInput.initialized && ReInput.controllers != null;
        }

        /// <summary>
        /// Disable a pointless ESM that spams stuff to the console.
        /// </summary>
        [HarmonyPatch(typeof(MeridianEventTriggerInteraction), nameof(MeridianEventTriggerInteraction.Awake))]
        [HarmonyPrefix]
        public static void MeridianEventTriggerInteraction_Awake(MeridianEventTriggerInteraction __instance)
        {
            var esm = EntityStateMachine.FindByCustomName(__instance.gameObject, "");
            if (esm != null && esm.initialStateType.stateType == typeof(TestState1))
            {
                esm.initialStateType = new SerializableEntityStateType(typeof(Uninitialized));
                esm.enabled = false;
            }
            else
            {
                Log.PatchFail("Meridian Test ESM");
            }
        }

        /// <summary>
        /// Prevent an IndexOutOfRange if the Child finds 0 or only 1 suitable node to teleport to.
        /// </summary>
        [HarmonyPatch(typeof(EntityStates.ChildMonster.Frolic), nameof(EntityStates.ChildMonster.Frolic.TeleportAroundPlayer))]
        [HarmonyILManipulator]
        public static void Frolic_TeleportAroundPlayer(ILContext il)
        {
            var c = new ILCursor(il);
            int listVar = 0, vectorVar = 0, boolVar = 0;
            if (!c.TryGotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt<NodeGraph>(nameof(NodeGraph.FindNodesInRange)),
                x => x.MatchStloc(out listVar),
                x => x.MatchLdloca(out vectorVar),
                x => x.MatchInitobj<Vector3>(),
                x => x.MatchLdcI4(out _),
                x => x.MatchStloc(out boolVar)))
            {
                Log.PatchFail(il);
                return;
            }
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Call, AccessTools.PropertyGetter(typeof(EntityState), nameof(EntityState.characterBody)));
            c.Emit(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(CharacterBody), nameof(CharacterBody.coreTransform)));
            c.Emit(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.position)));
            c.Emit(OpCodes.Stloc, vectorVar);
            c.Emit(OpCodes.Ldloc, listVar);
            c.Emit(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<NodeGraph.NodeIndex>), nameof(List<NodeGraph.NodeIndex>.Count)));
            c.Emit(OpCodes.Ldc_I4_1);
            // x <= 1 becomes !(x > 1) in CIL
            c.Emit(OpCodes.Cgt);
            c.Emit(OpCodes.Ldc_I4_0);
            c.Emit(OpCodes.Ceq);
            c.Emit(OpCodes.Stloc, boolVar);
        }

        /// <summary>
        /// When spawning next to a chest the body hasn't been linked to the master yet and body.inventory is null.
        /// Use pcmc.master.inventory which is more straightforward.
        /// </summary>
        [HarmonyPatch(typeof(TeamManager), nameof(TeamManager.LongstandingSolitudesInParty))]
        [HarmonyILManipulator]
        public static void TeamManager_LongstandingSolitudesInParty(ILContext il)
        {
            var c = new ILCursor(il);
            var pcmcVarIndex = 0;
            var bodyVarIndex = 0;
            if (!c.TryGotoNext(
                x => x.MatchLdloc(out pcmcVarIndex),
                x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(PlayerCharacterMasterController), nameof(PlayerCharacterMasterController.master))),
                x => x.MatchCallOrCallvirt<CharacterMaster>(nameof(CharacterMaster.GetBody)),
                x => x.MatchStloc(out bodyVarIndex)))
            {
                Log.PatchFail(il.Method.Name + " #1");
                return;
            }
            for (var i = 0; i < 2; i++)
            {
                if (!c.TryGotoNext(
                    x => x.MatchLdloc(bodyVarIndex),
                    x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(CharacterBody), nameof(CharacterBody.inventory)))))
                {
                    Log.PatchFail(il.Method.Name + $" #{i + 2}");
                    return;
                }
                c.RemoveRange(2);
                c.Emit(OpCodes.Ldloc, pcmcVarIndex);
                c.Emit(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlayerCharacterMasterController), nameof(PlayerCharacterMasterController.master)));
                c.Emit(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(CharacterMaster), nameof(CharacterMaster.inventory)));
            }
        }

        /// <summary>
        /// Fix NREs related to TetherVfxOrigin.AddTether and TetherVfxOrigin.RemoveTetherAt for the twisted elite.
        ///
        /// The root issue is that the patched method originally uses body.coreTransform instead of body.mainHurtBox.transform,
        /// which for some characters, e.g.,Commando, cannot reverse engineer the HealthComponent game object. Related errors:
        ///
        /// [Error  : Unity Log] ArgumentNullException: Value cannot be null.
        /// Parameter name: key
        /// Stack trace:
        /// System.Collections.Generic.Dictionary`2[TKey, TValue].FindEntry(TKey key) (at:IL_0008)
        /// System.Collections.Generic.Dictionary`2[TKey, TValue].ContainsKey(TKey key) (at:IL_0000)
        /// RoR2.AffixBeadAttachment.OnTetherAdded(RoR2.TetherVfx vfx, UnityEngine.Transform transform) (at:IL_0008)
        /// RoR2.TetherVfxOrigin.AddTether(UnityEngine.Transform target) (at:IL_0058)
        /// RoR2.TetherVfxOrigin.UpdateTargets(System.Collections.ObjectModel.ReadOnlyCollection`1[T] listOfHealthComponents, System.Collections.ObjectModel.ReadOnlyCollection`1[T] discoveredHealthComponents, System.Collections.ObjectModel.ReadOnlyCollection`1[T] lostHealthComponents) (at:IL_008A)
        /// RoR2.TargetNearbyHealthComponents.Tick() (at:IL_018B)
        /// RoR2.TargetNearbyHealthComponents.FixedUpdate() (at:IL_004B)
        ///
        /// [Error  : Unity Log] ArgumentOutOfRangeException: Index was out of range. Must be non-negative and less than the size of the collection.
        /// Parameter name: index
        /// Stack trace:
        /// System.Collections.Generic.List`1[T].get_Item(System.Int32 index) (at:IL_0009)
        /// RoR2.TetherVfxOrigin.RemoveTetherAt(System.Int32 i) (at:IL_0000)
        /// RoR2.TetherVfxOrigin.UpdateTargets(System.Collections.ObjectModel.ReadOnlyCollection`1[T] listOfHealthComponents, System.Collections.ObjectModel.ReadOnlyCollection`1[T] discoveredHealthComponents, System.Collections.ObjectModel.ReadOnlyCollection`1[T] lostHealthComponents) (at:IL_0062)
        /// RoR2.TargetNearbyHealthComponents.Tick() (at:IL_018B)
        /// RoR2.TargetNearbyHealthComponents.FixedUpdate() (at:IL_004B)
        /// </summary>
        [HarmonyPatch(typeof(Util), nameof(Util.HealthComponentToTransform))]
        [HarmonyILManipulator]
        public static void Util_HealthComponentToTransform(ILContext il)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(CharacterBody), nameof(CharacterBody.coreTransform)))))
            {
                Log.PatchFail(il);
                return;
            }
            c.Remove();
            c.Emit(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(CharacterBody), nameof(CharacterBody.mainHurtBox)));
            c.Emit(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Component), nameof(Component.transform)));
        }

        /// <summary>
        /// Fix Halcyonite's Whirlwind NRE spam when its target is killed during the skill.
        /// </summary>
        [HarmonyPatch(typeof(EntityStates.Halcyonite.WhirlWindPersuitCycle), nameof(EntityStates.Halcyonite.WhirlWindPersuitCycle.UpdateDecelerate))]
        [HarmonyILManipulator]
        public static void WhirlWindPersuitCycle_UpdateDecelerate(ILContext il, ILLabel retLabel)
        {
            var c = new ILCursor(il);
            // In theory all we want is to convert `if (age > duration) { A(); return; } B();` into
            // `if (age > duration || !this.targetBody) ...` However, in IL the original check branches if the
            // condition isn't satisfied, while with the OR as we want it we need to branch into `A`,
            // effectively changing the comparison instruction and branch targets. I am very worried this will
            // break if the method ever changes, so it's safer to add our own check separately at the top, i.e.,
            // `if (!self.targetBody) { A(); return; } <the rest>`. Can `A` change in the original method? I guess.
            // Is it likely? Not at all, so we're replicating it here.
            var firstOriginalInstr = c.Next;
            c.Emit(OpCodes.Ldarg_0);
            c.Emit<EntityStates.Halcyonite.WhirlWindPersuitCycle>(OpCodes.Ldfld, nameof(EntityStates.Halcyonite.WhirlWindPersuitCycle.targetBody));
            c.EmitOpImplicit();
            c.Emit(OpCodes.Brtrue_S, firstOriginalInstr);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldc_I4, (int)EntityStates.Halcyonite.WhirlWindPersuitCycle.PersuitState.Land);
            c.Emit<EntityStates.Halcyonite.WhirlWindPersuitCycle>(OpCodes.Stfld, nameof(EntityStates.Halcyonite.WhirlWindPersuitCycle.state));
            c.Emit(OpCodes.Br, retLabel);
        }

        /// <summary>
        /// Fix Meridian's Will NRE for targets without a rigid body, e.g. Grandparent
        /// </summary>
        [HarmonyPatch(typeof(EntityStates.FalseSon.MeridiansWillFire), nameof(EntityStates.FalseSon.MeridiansWillFire.InitializePullInfo))]
        [HarmonyILManipulator]
        public static void MeridiansWillFire_InitializePullInfo(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel label = null;
            if (c.TryGotoNext(
                x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(CharacterBody), nameof(CharacterBody.rigidbody))),
                x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(Rigidbody), nameof(Rigidbody.mass))),
                x => x.MatchBr(out label)))
            {
                c.Index++;
                var getMassInstr = c.Next;
                c.Emit(OpCodes.Dup);
                c.EmitOpImplicit();
                c.Emit(OpCodes.Brtrue_S, getMassInstr);
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Ldc_R4, 0f);
                c.Emit(OpCodes.Br, label);
            }
            else Log.PatchFail(il);
        }

        /// <summary>
        /// Fix Meridian's Will NRE for targets without a rigid body, e.g. Grandparent
        /// </summary>
        [HarmonyPatch(typeof(EntityStates.FalseSon.MeridiansWillFire), nameof(EntityStates.FalseSon.MeridiansWillFire.ApplyForce))]
        [HarmonyILManipulator]
        public static void MeridiansWillFire_ApplyForce(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction nextInstr = null;
            if (c.TryGotoNext(
                x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(CharacterBody), nameof(CharacterBody.rigidbody))),
                x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(Vector3), nameof(Vector3.zero))),
                x => x.MatchCallOrCallvirt(AccessTools.PropertySetter(typeof(Rigidbody), nameof(Rigidbody.velocity))),
                x => x.MatchAny(out nextInstr)
                ))
            {
                c.Index++;
                var continueWithSetVelocity = c.Next;
                c.Emit(OpCodes.Dup);
                c.EmitOpImplicit();
                c.Emit(OpCodes.Brtrue_S, continueWithSetVelocity);
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Br, nextInstr);
            }
            else Log.PatchFail(il);
        }
    }
}
