using System;
using System.Collections.Generic;
using System.Linq;
using EntityStates;
using EntityStates.LunarExploderMonster;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Rewired;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Items;
using RoR2.Navigation;
using RoR2.Orbs;
using RoR2.Projectile;
using RoR2.Stats;
using RoR2.UI;
using UnityEngine;

namespace MiscFixes
{
    [HarmonyPatch]
    public class FixVanilla
    {
        /// <summary>
        /// DotController.GetDotDef doesn't use ArrayUtils.GetSafe so it can throw
        /// </summary>
        [HarmonyPatch(typeof(DotController), nameof(DotController.GetDotDef))]
        [HarmonyILManipulator]
        public static void FixDotDefs(ILContext il)
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                    x => x.MatchLdsfld<DotController>(nameof(DotController.dotDefs)),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdelemRef()
                ))
            {
                c.Index--;
                c.Remove();
                // probably can do this better but idc tbh
                c.EmitDelegate(delegate (DotController.DotDef[] dotDefs, DotController.DotIndex index) 
                {
                    return HG.ArrayUtils.GetSafe(dotDefs, (int)index); 
                });
            }
            else Log.PatchFail(il);
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
            else Log.PatchFail(il);
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
            int bodyLoc = 0;
            if (c.TryGotoNext(MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<Orb>(nameof(Orb.target)),
                    x => x.MatchLdfld<HurtBox>(nameof(HurtBox.healthComponent)),
                    x => x.MatchLdfld<HealthComponent>(nameof(HealthComponent.body)),
                    x => x.MatchStloc(out bodyLoc)
                ))
            {
                var label = c.DefineLabel();
                c.Emit(OpCodes.Ldloc, bodyLoc);
                c.EmitOpImplicit();
                c.Emit(OpCodes.Brtrue_S, label);
                c.Emit(OpCodes.Ret);

                c.MarkLabel(label);
            }
            else Log.PatchFail(il);
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
            else Log.PatchFail(il);
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
            else Log.PatchFail(il);
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
            else Log.PatchFail(il);
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
            else Log.PatchFail(il);
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
            else Log.PatchFail(il.Method.Name + " 1");

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
            else Log.PatchFail(il.Method.Name + " 2");
        }

        /// <summary>
        /// Filter allies from Merc's Eviscerate target search.
        /// </summary>
        [HarmonyPatch(typeof(EntityStates.Merc.EvisDash), nameof(EntityStates.Merc.EvisDash.FixedUpdate))]
        [HarmonyILManipulator]
        public static void FixMercEvisAllyTargetting(ILContext il)
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
                Log.PatchFail(il);
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

        /// <summary>
        /// The printer uses EffectManager for VFX without an EffectComponent, use Object.Instantiate instead.
        /// </summary>
        [HarmonyPatch(typeof(EntityStates.Duplicator.Duplicating), nameof(EntityStates.Duplicator.Duplicating.DropDroplet))]
        [HarmonyILManipulator]
        public static void FixPrinterDropEffect(ILContext il)
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

        /// <summary>
        /// Prevent an error logging when trying to detatch a child from a disabled game object.
        /// </summary>
        [HarmonyPatch(typeof(DetachParticleOnDestroyAndEndEmission), nameof(DetachParticleOnDestroyAndEndEmission.OnDisable))]
        [HarmonyILManipulator]
        public static void FixParticleDetachOnDestroy(ILContext il)
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
        public static void FixCharacterModelNullHurtBoxes(ILContext il)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(
                MoveType.After,
                x => x.MatchLdfld<HurtBoxGroup>(nameof(HurtBoxGroup.hurtBoxes))))
            {
                Log.PatchFail(il);
                return;
            }
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<HurtBox[], CharacterModel, HurtBox[]>>((hurtBoxes, model) =>
            {
                var filteredHurtBoxes = new List<HurtBox>();
                foreach (var hurtBox in hurtBoxes)
                {
                    if (hurtBox && hurtBox.transform != null)
                    {
                        filteredHurtBoxes.Add(hurtBox);
                    }
                }
                return filteredHurtBoxes.Count == hurtBoxes.Length ? hurtBoxes : [.. filteredHurtBoxes];
            });
        }

        /// <summary>
        /// The OutsideInteractableLocker does not check if a Lemurian Egg lock already exists before creating the VFX.
        /// </summary>
        [HarmonyPatch(typeof(OutsideInteractableLocker), nameof(OutsideInteractableLocker.LockLemurianEgg))]
        [HarmonyILManipulator]
        public static void FixReapplingEggLockVFX(ILContext il)
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
        public static void FixPositionIndicatorWithHiddenHud(ILContext il)
        {
            var c = new ILCursor(il);
            int locVarIndex = 0;
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
        public static void FixIndicatorSetVisibleNRE(ILContext il)
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
        public static void FixCrosshairOverrideOnDestroy(ILContext il)
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
        //[HarmonyPatch(typeof(RuleChoiceController), nameof(RuleChoiceController.FindNetworkUser))]
        //[HarmonyILManipulator]
        public static void FixLobbyQuitEventSystem(ILContext il)
        {
            Log.Warning(il);
            var c = new ILCursor(il);
            if (!c.TryGotoNext(
                x => x.MatchPop(),
                x => x.MatchLdnull(),
                x => x.MatchRet()))
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
        public static bool FixNoRewiredInputOnQuit()
        {
            return ReInput.initialized && ReInput.controllers != null;
        }

        /// <summary>
        /// Disable a pointless ESM that spams stuff to the console.
        /// </summary>
        [HarmonyPatch(typeof(MeridianEventTriggerInteraction), nameof(MeridianEventTriggerInteraction.Awake))]
        [HarmonyPrefix]
        public static void FixMeridianTestStateSpam(MeridianEventTriggerInteraction __instance)
        {
            var esm = EntityStateMachine.FindByCustomName(__instance.gameObject, "");
            if (esm != null)
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
        /// Quiting/dying before killing False Son leaves a stale event subscribed. Allow gracious removal the next time it's called.
        /// </summary>
        [HarmonyPatch(typeof(EntityStates.MeridianEvent.FSBFPhaseBaseState), nameof(EntityStates.MeridianEvent.FSBFPhaseBaseState.OnBossGroupDefeated))]
        [HarmonyILManipulator]
        public static void FixFalseSonBossGroupDefeatedEvent(ILContext il)
        {
            var c = new ILCursor(il);
            Instruction skipInstr = null;
            if (!c.TryGotoNext(
                x => x.MatchCallOrCallvirt<MeridianEventTriggerInteraction>(nameof(MeridianEventTriggerInteraction.ResetPMHeadState)),
                x => x.MatchAny(out skipInstr)))
            {
                Log.PatchFail(il);
                return;
            }
            var continueInstr = c.Next;
            c.Emit(OpCodes.Dup);
            c.Emit(OpCodes.Brtrue_S, continueInstr);
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Br, skipInstr);
        }

        /// <summary>
        /// Prevent an IndexOutOfRange on an empty list if the Child does not find any suitable nodes to teleport to.
        /// </summary>
        [HarmonyPatch(typeof(EntityStates.ChildMonster.Frolic), nameof(EntityStates.ChildMonster.Frolic.TeleportAroundPlayer))]
        [HarmonyILManipulator]
        public static void FixFrolicTeleportWithoutAvailableNodes(ILContext il)
        {
            var c = new ILCursor(il);
            int varIndex = 0;
            if (!c.TryGotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt<NodeGraph>(nameof(NodeGraph.FindNodesInRange)),
                x => x.MatchStloc(out varIndex)))
            {
                Log.PatchFail(il);
                return;
            }
            var instr = c.Next;
            c.Emit(OpCodes.Ldloc, varIndex);
            c.Emit(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<NodeGraph.NodeIndex>), nameof(List<NodeGraph.NodeIndex>.Count)));
            c.Emit(OpCodes.Ldc_I4_0);
            c.Emit(OpCodes.Bgt_S, instr);
            c.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// When spawning next to a chest the body hasn't been linked to the master yet and body.inventory is null.
        /// Use pcmc.master.inventory which is more straightforward.
        /// </summary>
        [HarmonyPatch(typeof(TeamManager), nameof(TeamManager.LongstandingSolitudesInParty))]
        [HarmonyILManipulator]
        public static void FixSpawnNearInteractableWithLongstandingSolitude(ILContext il)
        {
            var c = new ILCursor(il);
            int pcmcVarIndex = 0;
            int bodyVarIndex = 0;
            if (!c.TryGotoNext(
                x => x.MatchLdloc(out pcmcVarIndex),
                x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(PlayerCharacterMasterController), nameof(PlayerCharacterMasterController.master))),
                x => x.MatchCallOrCallvirt<CharacterMaster>(nameof(CharacterMaster.GetBody)),
                x => x.MatchStloc(out bodyVarIndex)))
            {
                Log.PatchFail(il.Method.Name + " #1");
                return;
            }
            for (int i = 0; i < 2; i++)
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
        /// Fix Aurelionite not spawning for you on the next teleporter event after beating False Son.
        ///
        /// After the boss fight GoldTitanManager.isFalseSonBossLunarShardBrokenMaster remains true until the next teleporter event.
        /// The problem is this method is called before the boss spawns and sets it back to false, leading to NRE by executing unexpected code.
        /// A solution is to set the bool back to false the moment Aurelionite is summoned for False Son.
        /// </summary>
        [HarmonyPatch(typeof(GoldTitanManager), nameof(GoldTitanManager.TryStartChannelingTitansServer))]
        [HarmonyILManipulator]
        public static void FixAurelioniteNotSpawningAfterBeatingFalseSon(ILContext il)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(
                MoveType.After,
                x => x.MatchLdsfld(typeof(GoldTitanManager), nameof(GoldTitanManager.isFalseSonBossLunarShardBrokenMaster)),
                x => x.MatchBrfalse(out _)))
            {
                Log.PatchFail(il);
                return;
            }
            c.Emit(OpCodes.Ldc_I4_0);
            c.Emit(OpCodes.Stsfld, AccessTools.Field(typeof(GoldTitanManager), nameof(GoldTitanManager.isFalseSonBossLunarShardBrokenMaster)));
        }
    }
}
