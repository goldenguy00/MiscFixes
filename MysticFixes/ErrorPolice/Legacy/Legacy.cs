
/*


        /// <summary>
        /// Disable a pointless ESM that spams stuff to the console.
        /// </summary>
        //[HarmonyPatch(typeof(MeridianEventTriggerInteraction), nameof(MeridianEventTriggerInteraction.Awake))]
        //[HarmonyPrefix]
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
        /// something on sundered grove throws an error here periodically
        /// </summary>
        //[HarmonyPatch(typeof(RouletteChestController.Idle), nameof(RouletteChestController.Idle.OnEnter))]
        //[HarmonyILManipulator]
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
        /// Fix two Elder Lemurian footstep events to play sound and not spam Layer Index -1.
        /// </summary>
        public static void FixElderLemurianFootstepEvents()
        {
            var animRef = new AssetReferenceT<RuntimeAnimatorController>(RoR2_Base_Lemurian.animLemurianBruiser_controller);
            Utils.PreloadAsset(animRef).Completed += delegate (AsyncOperationHandle<RuntimeAnimatorController> animHandle)
            {
                var anim = animHandle.Result;
                PatchClip(4, "LemurianBruiserArmature|RunRight", 1, "", "FootR");
                PatchClip(12, "LemurianBruiserArmature|Death", 2, "MouthMuzzle", "MuzzleMouth");

                Log.Debug("Elder lemurian footsteps done");

                void PatchClip(int clipIndex, string clipName, int eventIndex, string oldEventString, string newEventString)
                {
                    if (anim.animationClips.Length > clipIndex && anim.animationClips[clipIndex].name == clipName)
                    {
                        var clip = anim.animationClips[clipIndex];
                        if (clip.events.Length > eventIndex && clip.events[eventIndex].stringParameter == oldEventString)
                        {
                            var events = clip.events;
                            events[eventIndex].stringParameter = newEventString;
                            clip.events = events;
                            return;
                        }
                    }
                    Log.PatchFail(anim.name + " - " + clipName);
                }

                Utils.UnloadAsset(animRef);
            };
        }

        /// <summary>
        /// Prevent Sale Star's pickup from complaining about its collider's settings.
        /// </summary>
        public static void FixSaleStarCollider()
        {
            var objRef = new AssetReferenceGameObject(RoR2_DLC2_Items_LowerPricedChests.PickupSaleStar_prefab);
            Utils.PreloadAsset(objRef).Completed += delegate (AsyncOperationHandle<GameObject> objHandle)
            {
                var collider = objHandle.Result.transform.Find("SaleStar")?.GetComponent<MeshCollider>();
                if (collider == null || collider.convex)
                {
                    Log.PatchFail("collider of SaleStar");
                }
                else
                {
                    collider.convex = true;
                    Log.Debug("SaleStar Collider done");
                }
                Utils.UnloadAsset(objRef);
            };
        }


        private static void FixVermin()
        {
            var spawnPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2_DLC1_Vermin.VerminSpawn_prefab).WaitForCompletion();
            if (spawnPrefab && spawnPrefab.TryGetComponent<EffectComponent>(out var ec) && ec.positionAtReferencedTransform == false)
            {
                ec.positionAtReferencedTransform = true;
                return;
            }

            Log.PatchFail("Vermin spawn effect");
        }

        [HarmonyPatch(typeof(MinionOwnership.MinionGroup), nameof(MinionOwnership.MinionGroup.AddMinion))]
        [HarmonyILManipulator]
        public static void FixMinionGroupAddMinion(ILContext il)
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
        public static void FixMinionGroupRemoveMinion(ILContext il)
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
        [HarmonyPatch(typeof(EntityStates.Duplicator.Duplicating), nameof(EntityStates.Duplicator.Duplicating.DropDroplet))]
        [HarmonyILManipulator]
        public static void FixDuplicatingDropDroplet(ILContext il)
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
        /// gold coast plus revived chest is fucked, and that's like the only time ive ever seen it
        /// </summary>
        //[HarmonyPatch(typeof(Interactor), nameof(Interactor.FindBestInteractableObject))]
        //[HarmonyILManipulator]
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
        /// RoR2.CharacterBody.HandleDisableAllSkillsDebuffg__HandleSkillDisableState
        /// if (NetworkServer.active)
        ///     this.inventory.SetEquipmentDisabled(_disable);
        /// 
        /// inventory shouldnt be null, but jsut in case
        /// </summary>
        //[HarmonyPatch(typeof(CharacterBody), "<HandleDisableAllSkillsDebuff>g__HandleSkillDisableState|389_0")]
        //[HarmonyILManipulator]
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
        /// call can be suppressed if stuff is null
        /// </summary>
        //[HarmonyPatch(typeof(InspectPanelController), nameof(InspectPanelController.Show))]
        //[HarmonyPrefix]
        public static bool InspectPanelController_Show(InspectPanelController __instance) => __instance.eventSystem && __instance.eventSystem.localUser?.userProfile is not null;

        /// <summary>
        /// KeyNotFoundException
        /// RoR2.ContentManagement.AssetAsyncReferenceManager`1[T].PreloadInMenuReferences doesnt get cleared after moving handles to AtWill release
        /// reported to gbx devs, probably fixed in next patch
        /// </summary>
        //[HarmonyPatch(typeof(AssetAsyncReferenceManager<Object>), nameof(AssetAsyncReferenceManager<Object>.OnSceneChanged))]
        //[HarmonyILManipulator]
        public static void AssetAsyncReferenceManager_OnSceneChanged(ILContext il)
        {
            var c = new ILCursor(il) { Index = il.Instrs.Count - 1 };

            ILLabel leave = null;
            if (!c.TryGotoPrev(MoveType.After, x => x.MatchEndfinally()) ||
                !c.TryFindPrev(out _, x => x.MatchLeaveS(out leave)))
            {
                Log.PatchFail(il);
                return;
            }

            c.Emit(OpCodes.Ldsfld, AccessTools.Field(typeof(AssetAsyncReferenceManager<Object>), nameof(AssetAsyncReferenceManager<Object>.PreloadedInMenuReferences)));
            c.Emit<List<string>>(OpCodes.Callvirt, nameof(List<string>.Clear));
            
            c.Index -= 2;
            c.MarkLabel(leave);
            il.Body.ExceptionHandlers.Last().HandlerEnd = leave.Target;
        }


        /// <summary>
        /// RoR2.CharacterModel.InstantiateDisplayRuleGroup(RoR2.DisplayRuleGroup displayRuleGroup, RoR2.ItemIndex itemIndex, RoR2.EquipmentIndex equipmentIndex)
        /// 
        /// IL_008b: ldloc.1
        /// IL_008c: ldfld class [Unity.Addressables] UnityEngine.AddressableAssets.AssetReferenceGameObject RoR2.ItemDisplayRule::followerPrefabAddress
        /// IL_0091: callvirt instance bool[Unity.Addressables] UnityEngine.AddressableAssets.AssetReference::RuntimeKeyIsValid()
        /// IL_0096: brfalse.s IL_00b7
        /// </summary>
        //[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.InstantiateDisplayRuleGroup))]
        //[HarmonyILManipulator]
        public static void CharacterModel_InstantiateDisplayRuleGroup(ILContext il)
        {
            var c = new ILCursor(il);

            ILLabel label = null;
            if (!c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdfld<ItemDisplayRule>(nameof(ItemDisplayRule.followerPrefabAddress)),
                    x => x.MatchCallOrCallvirt<AssetReference>(nameof(AssetReference.RuntimeKeyIsValid)),
                    x => x.MatchBrfalse(out label)
                ))
            {
                Log.PatchFail(il);
                return;
            }

            c.Index++;
            var runtimeKeyValidCall = c.Next;
            c.Emit(OpCodes.Dup);
            c.Emit(OpCodes.Brtrue_S, runtimeKeyValidCall);
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Br, label);
        }

        [HarmonyPatch(typeof(SurvivorMannequinSlotController), nameof(SurvivorMannequinSlotController.ApplyLoadoutToMannequinInstance))]
        [HarmonyILManipulator]
        public static void ModelSkinController_ApplySkinAsync(ILContext il)
        {
            var c = new ILCursor(il);

            if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchCallOrCallvirt<Component>(nameof(Component.GetComponentInChildren)),
                    x => x.MatchDup()
                ))
            {
                Log.PatchFail(il);
                return;
            }

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(RevertSkin);
        }

        private static ModelSkinController RevertSkin(ModelSkinController modelSkinController, SurvivorMannequinSlotController slotController)
        {
            BodyIndex bodyIndexFromSurvivorIndex = SurvivorCatalog.GetBodyIndexFromSurvivorIndex(slotController.currentSurvivorDef.survivorIndex);
            int newSkinIndex = (int)slotController.currentLoadout.bodyLoadoutManager.GetSkinIndex(bodyIndexFromSurvivorIndex);

            modelSkinController.GetOrAddComponent<ReverseSkinAsync>().ApplyDelta(newSkinIndex);

            // make IL gods happy
            return modelSkinController;
        }


        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private void AddCompatPatches()
        {
            try { PatchLobbySkins("1.2.1"); } catch { }
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private void PatchLobbySkins(string version)
        {
            var targetVersion = new Version(version);
            var bepinAttribute = typeof(LobbySkinsFix.LobbySkinsFixPlugin).GetCustomAttribute<BepInPlugin>();

            if (bepinAttribute.Version.Equals(targetVersion))
                harmonyPatcher.CreateClassProcessor(typeof(UnfuckLobbySkins)).Patch();
        }

    /// <summary>
    /// ConVars are not registered as lower case but when submitting them from the console they are converted, leading to a match fail.
    /// </summary>
    //[HarmonyPatch(typeof(Console), nameof(Console.RegisterConVarInternal))]
    //[HarmonyILManipulator]
    public static void FixConVarCaseSensitive(ILContext il)
    {
        var c = new ILCursor(il);
        // Technically we aren't checking if ToLowerInvariant or ToLower(CultureInfo.InvariantCulture)
        // is called after these instructions just like Console.Awake does for ConCommands, mostly
        // because it's a hassle to check for the existence of either, but an extra call wouldn't hurt.
        if (!c.TryGotoNext(
            MoveType.After,
            x => x.MatchLdarg(1),
            x => x.MatchLdfld<RoR2.ConVar.BaseConVar>(nameof(RoR2.ConVar.BaseConVar.name))))
        {
            Log.PatchFail(il);
            return;
        }
        c.Emit<string>(OpCodes.Callvirt, nameof(string.ToLowerInvariant));
    }


        /// <summary>
        /// Fix the Xi Construct not creating a blast at the end of the laser attack.
        /// SotS changed `outer.SetNextState(GetNextState())` to `outer.SetNextStateToMain()`
        /// </summary>
        //[HarmonyPatch(typeof(FireBeam), nameof(FireBeam.FixedUpdate))]
        //[HarmonyILManipulator]
        public static void FireBeam_FixedUpdate(ILContext il)
        {
            var c = new ILCursor(il);
            // Using ShouldFireLaser as a landmark juuuust in case there are ever multiple SetNextStateToMain calls
            if (c.TryGotoNext(x => x.MatchCallOrCallvirt<FireBeam>(nameof(FireBeam.ShouldFireLaser))) &&
                c.TryGotoNext(x => x.MatchCallOrCallvirt<EntityStateMachine>(nameof(EntityStateMachine.SetNextStateToMain))))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Callvirt, AccessTools.Method(typeof(FireBeam), nameof(FireBeam.GetNextState)));
                c.Next.Operand = AccessTools.Method(typeof(EntityStateMachine), nameof(EntityStateMachine.SetNextState));
            }
            else Log.PatchFail(il);
        }

        /// <summary>
        /// Fix CHEF's BurnMithrix achievement not unsubscribing a DamageDealt event once completed.
        /// </summary>
        //[HarmonyPatch(typeof(RoR2.Achievements.Chef.BurnMithrix.BurnMithrixServerAchievement), nameof(RoR2.Achievements.Chef.BurnMithrix.BurnMithrixServerAchievement.OnUninstall))]
        //[HarmonyILManipulator]
        public static void BurnMithrix_BurnMithrixServerAchievement_OnUninstall(ILContext il)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(x => x.MatchCallOrCallvirt<GlobalEventManager>("add_" + nameof(GlobalEventManager.onServerDamageDealt))))
            {
                Log.PatchFail(il);
                return;
            }
            c.Next.Operand = typeof(GlobalEventManager).GetMethod("remove_" + nameof(GlobalEventManager.onServerDamageDealt));
        }
        /// <summary>
        /// Filter allies from Merc's Eviscerate target search.
        /// </summary>
        //[HarmonyPatch(typeof(EntityStates.Merc.EvisDash), nameof(EntityStates.Merc.EvisDash.FixedUpdate))]
        //[HarmonyILManipulator]
        public static void EvisDash_FixedUpdate(ILContext il)
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
                    x => x.MatchCallOrCallvirt<UnityEngine.Object>("op_Inequality"),
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
        /// Fix False Son not using Tainted Offering in phase 2 due to a misconfigured AISkillDriver.
        /// </summary>
        public static void FixFalseSonBossP2NotUsingSpecial()
        {
            Addressables.LoadAssetAsync<GameObject>(RoR2_DLC2_FalseSonBoss.FalseSonBossLunarShardMaster_prefab).Completed += delegate (AsyncOperationHandle<GameObject> obj)
            {
                var skillDrivers = obj.Result.GetComponents<AISkillDriver>();
                foreach (var skillDriver in skillDrivers)
                {
                    if (skillDriver.customName == "Corrupted Paths (Step Brothers)")
                    {
                        skillDriver.requiredSkill = null;
                        Log.Debug("FalseSon Boss P2 Not Using Special done");
                        return;
                    }
                }

                Log.PatchFail("False Son Boss Phase 2 special skill");
            };
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
        //[HarmonyPatch(typeof(AffixAurelioniteBehavior), nameof(AffixAurelioniteBehavior.Update))]
        //[HarmonyPrefix]
        public static bool AffixAurelioniteBehavior_Update(AffixAurelioniteBehavior __instance) => __instance.body?.coreTransform;

        /// <summary>
        /// The method never null checks target, which can lead to multiple body.gameObject NREs.
        /// The dotDef can also be null, in which case we should continue to the next iteration.
        /// </summary>
        //[HarmonyPatch(typeof(VineOrb), nameof(VineOrb.OnArrival))]
        //[HarmonyILManipulator]
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
        //[HarmonyPatch(typeof(ProjectileController), nameof(ProjectileController.Start))]
        //[HarmonyILManipulator]
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
        //[HarmonyPatch(typeof(StatManager), nameof(StatManager.ProcessGoldEvents))]
        //[HarmonyILManipulator]
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
        /// Fixes an NRE when picking up an Elusive Antlers orb from a respawned/dead character.
        /// </summary>
        //[HarmonyPatch(typeof(ElusiveAntlersPickup), nameof(ElusiveAntlersPickup.OnShardDestroyed))]
        //[HarmonyILManipulator]
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
        //[HarmonyPatch(typeof(FogDamageController), nameof(FogDamageController.EvaluateTeam))]
        //[HarmonyILManipulator]
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
        /// The printer uses EffectManager for VFX without an EffectComponent, use Object.Instantiate instead.
        /// </summary>
        //[HarmonyPatch(typeof(EntityStates.Duplicator.Duplicating), nameof(EntityStates.Duplicator.Duplicating.DropDroplet))]
        //[HarmonyILManipulator]
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
        /// Iterating a list while modifying it raises an error, iterate on a copy of it instead. Occurs when Seeker respawns.
        /// </summary>
        //[HarmonyPatch(typeof(CrosshairUtils.CrosshairOverrideBehavior), nameof(CrosshairUtils.CrosshairOverrideBehavior.OnDestroy))]
        //[HarmonyILManipulator]
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
        /// Prevent RewiredIntegration from running if not initialised, occurs when exiting the game.
        /// </summary>
        //[HarmonyPatch(typeof(RewiredIntegrationManager), nameof(RewiredIntegrationManager.RefreshJoystickAssignment))]
        //[HarmonyPrefix]
        public static bool RewiredIntegrationManager_RefreshJoystickAssignment()
        {
            return ReInput.initialized && ReInput.controllers != null;
        }

        /// <summary>
        /// Prevent an IndexOutOfRange if the Child finds 0 or only 1 suitable node to teleport to.
        /// </summary>
        //[HarmonyPatch(typeof(EntityStates.ChildMonster.Frolic), nameof(EntityStates.ChildMonster.Frolic.TeleportAroundPlayer))]
        //[HarmonyILManipulator]
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
        //[HarmonyPatch(typeof(TeamManager), nameof(TeamManager.LongstandingSolitudesInParty))]
        //[HarmonyILManipulator]
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
        //[HarmonyPatch(typeof(Util), nameof(Util.HealthComponentToTransform))]
        //[HarmonyILManipulator]
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
        /// Fix Meridian's Will NRE for targets without a rigid body, e.g. Grandparent
        /// </summary>
        //[HarmonyPatch(typeof(EntityStates.FalseSon.MeridiansWillFire), nameof(EntityStates.FalseSon.MeridiansWillFire.InitializePullInfo))]
        //[HarmonyILManipulator]
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
        /// thanks to bubbet for the wonderful code
        /// 
        /// lobbby dies when the event system isn't the one that it's expecting.
        /// forces game restart to recover.
        /// </summary>
        /// <param name="il"></param>
        //[HarmonyPatch(typeof(MPEventSystem), nameof(MPEventSystem.Update))]
        //[HarmonyILManipulator]
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
            else Log.PatchFail(il);
        }

        /// <summary>
        /// Fix Halcyonite's Whirlwind NRE spam when its target is killed during the skill.
        /// </summary>
        //[HarmonyPatch(typeof(EntityStates.Halcyonite.WhirlWindPersuitCycle), nameof(EntityStates.Halcyonite.WhirlWindPersuitCycle.UpdateDecelerate))]
        //[HarmonyILManipulator]
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
 */
