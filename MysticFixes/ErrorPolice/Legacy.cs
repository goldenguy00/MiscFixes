
/*


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

 */
