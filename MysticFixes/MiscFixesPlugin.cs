using System;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using MiscFixes.ErrorPolice;
using MiscFixes.ErrorPolice.BaseGame;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

[assembly: SuppressMessage("CodeQuality", "IDE0051:Remove unused private members")]

namespace MiscFixes
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(RoR2BepInExPack.RoR2BepInExPack.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    public class MiscFixesPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = $"_{PluginAuthor}.{PluginName}";
        public const string PluginAuthor = "score";
        public const string PluginName = "MiscFixes";
        public const string PluginVersion = "1.6.2";

        internal static bool RooInstalled => Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");

        private Harmony harmonyPatcher;

        private void Awake()
        {
            Log.Init(Logger);

            FixAssets.Init();

            // dev note:
            // do not patch all! patch individual classes always!
            // PatchAll will trigger an assembly scan, which will throw when it reads the compat classes!
            harmonyPatcher = new Harmony(PluginGUID);
            TryHarmonyPatch<FixGameplay>();
            TryHarmonyPatch<FixEventSystem>();
            TryHarmonyPatch<FixNullRefs>();
            TryHarmonyPatch<FixParticleScale>();
            TryHarmonyPatch<FixTempOverlay>();

            TryHarmonyPatch(typeof(AddTimedBuffUnawakenedBodyFix));
            TryHarmonyPatch(typeof(AffixBeadDestroyedModelFix));
            TryHarmonyPatch(typeof(AnimationEventsNullEffectFix));
            TryHarmonyPatch(typeof(ArtifactAchievementNullDefFix));
            TryHarmonyPatch(typeof(AurelioniteBlessingNoAttackerFix));
            TryHarmonyPatch(typeof(BarrageOnBossNoMasterFix));
            TryHarmonyPatch(typeof(BellTrioBombNoEnemyBodyFix));
            TryHarmonyPatch(typeof(BrotherUltTargetDirectionFix));
            TryHarmonyPatch(typeof(CausticPheromonesNoTeamFix));
            TryHarmonyPatch(typeof(CharacterDeathRecursionFix));
            TryHarmonyPatch(typeof(CharacterMainMissingAnimatorFix));
            TryHarmonyPatch(typeof(CharacterMainMissingAnimatorFixedUpdateFix));
            TryHarmonyPatch(typeof(ChipAnimatorDestroyFix));
            TryHarmonyPatch(typeof(ClassicStageInfoNoMonsterCategoryFix));
            TryHarmonyPatch(typeof(CollectiveRemoveDestroyedTargetFix));
            TryHarmonyPatch(typeof(ColliderBlockFilterNoHurtBoxFix));
            TryHarmonyPatch(typeof(CombatDirectorNoMonsterCardsFix));
            TryHarmonyPatch(typeof(CombatShrineNoMonsterCardsFix));
            TryHarmonyPatch(typeof(ComputePathNodeIndexFix));
            TryHarmonyPatch(typeof(CustomRenderTextureLightFix));
            TryHarmonyPatch(typeof(DccsPoolEmptyCategoryFix));
            TryHarmonyPatch(typeof(DefectiveUnitDeathStateFix));
            TryHarmonyPatch(typeof(DirectorCardNullFix));
            TryHarmonyPatch(typeof(DreamDemonProjectileFix));
            TryHarmonyPatch(typeof(DrifterReclaimBagNoBodyFix));
            TryHarmonyPatch(typeof(DroneCommandReceiverChestTargetFix));
            TryHarmonyPatch(typeof(EnemyDetectionHurtBoxFix));
            TryHarmonyPatch(typeof(EntitlementLockNullDefFix));
            TryHarmonyPatch(typeof(EquipmentSlotNoInventoryFix));
            TryHarmonyPatch(typeof(EventLockableDestroyedFix));
            TryHarmonyPatch(typeof(FootstepDustPrefabFix));
            TryHarmonyPatch(typeof(FriendlyFireNoBodyFix));
            TryHarmonyPatch(typeof(GiveMoneyNoInventoryFix));
            TryHarmonyPatch(typeof(HallowedCauldronClientActivatorFix));
            TryHarmonyPatch(typeof(InspectPanelEventSystemFix));
            TryHarmonyPatch(typeof(JellyBarrageChildLocatorFix));
            TryHarmonyPatch(typeof(MiniGeodeTrackerNoRunFix));
            TryHarmonyPatch(typeof(MinionOwnershipRemovedFix));
            TryHarmonyPatch(typeof(ModelPanelNoEventSystemFix));
            TryHarmonyPatch(typeof(NetworkSceneObjectDestroyedFix));
            TryHarmonyPatch(typeof(NullWardMissingZoneFix));
            TryHarmonyPatch(typeof(NullWardNoRunFix));
            TryHarmonyPatch(typeof(OrbEffectNoPoolFix));
            TryHarmonyPatch(typeof(PointSoundManagerDeadEmitterFix));
            TryHarmonyPatch(typeof(RigidbodySoundOnImpactFix));
            TryHarmonyPatch(typeof(SceneDirectorZeroWeightCardFix));
            TryHarmonyPatch(typeof(MinionLeashNoSceneInfoFix));
            TryHarmonyPatch(typeof(AIWanderNoSceneInfoFix));
            TryHarmonyPatch(typeof(SetStateOnHurtDurationFix));
            TryHarmonyPatch(typeof(SimpleMuzzleFlashNullPrefabFix));
            TryHarmonyPatch(typeof(SolusFlamethrowerDeadTargetFix));
            TryHarmonyPatch(typeof(SolusHeartFallOfferingMasterFix));
            TryHarmonyPatch(typeof(SolusHeartSilhouetteAnimatorFix));
            TryHarmonyPatch(typeof(SolusWingBeamTransformsFix));
            TryHarmonyPatch(typeof(SpawnLynxNoOwnerMasterFix));
            TryHarmonyPatch(typeof(SpecialObjectAttributesSetVisibleFix));
            TryHarmonyPatch(typeof(SpecialObjectAttributesStartFix));
            TryHarmonyPatch(typeof(TakeDamageNoTeamComponentFix));
            TryHarmonyPatch(typeof(TempItemDecayDestroyedInventoryFix));
            TryHarmonyPatch(typeof(TracerNoPoolFix));
            TryHarmonyPatch(typeof(TrialGongNoHurtboxFix));
            TryHarmonyPatch(typeof(UnityLogFilter));
            TryHarmonyPatch(typeof(VoidBarnacleSpawnFXFix));
            TryHarmonyPatch(typeof(VoidFieldsCellSpawnFix));
            TryHarmonyPatch(typeof(VultureFightOverrideClientRngFix));
            TryHarmonyPatch(typeof(VultureHunterMurderNoMasterFix));
            TryHarmonyPatch(typeof(WaitingForCursedBookExitFix));
        }

        private void TryHarmonyPatch<T>() => TryHarmonyPatch(typeof(T));

        private void TryHarmonyPatch(Type type)
        {
            try { harmonyPatcher.CreateClassProcessor(type).Patch(); } catch (Exception e) { Log.Error(e); }
        }

    }
}
