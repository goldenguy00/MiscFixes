
/*


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
