using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine;
using RoR2.CharacterAI;
using RoR2.UI;
using System.Linq;
using RoR2;
using MiscFixes.Modules;
using HG;
using RoR2.UI.MainMenu;
using RoR2BepInExPack.GameAssetPaths;

namespace MiscFixes.ErrorPolice
{
    internal static class AssetFixes
    {
        internal static void Init()
        {
            FixElderLemurianFootstepEvents();
            FixSaleStarCollider();
            FixFalseSonBossP2NotUsingSpecial();
            MoreHudChildLocEntries();
            FixHenry();

            MainMenuController.OnMainMenuInitialised += OnLoad;
        }

        private static void OnLoad()
        {
            MainMenuController.OnMainMenuInitialised -= OnLoad;

            try
            {
                foreach (var survivor in SurvivorCatalog.survivorDefs)
                {
                    var display = survivor?.displayPrefab;
                    var body = survivor?.bodyPrefab;

                    if (!(display && body))
                        continue;

                    var bodySkins = body.GetComponentInChildren<ModelSkinController>();
                    if (bodySkins?.skins?.Length == 0)
                        continue;

                    var displayModel = display.GetComponentInChildren<CharacterModel>();
                    if (!displayModel)
                    {
                        Log.Warning($"Display prefab {display.name} is missing the CharacterModel component! Skipping...");
                        continue;
                    }

                    var displaySkins = displayModel.GetComponent<ModelSkinController>();
                    if (!displaySkins)
                    {
                        Log.Warning($"Display prefab {displayModel.name} is missing a ModelSkinController component! Adding...");
                        displaySkins = displayModel.gameObject.AddComponent<ModelSkinController>();
                    }

                    if (displaySkins.skins?.Length != bodySkins.skins.Length)
                    {
                        Log.Warning($"Display prefab {displayModel.name} ModelSkinController.skins array is incorrect! Cloning from body prefab...");
                        displaySkins.skins = ArrayUtils.Clone(bodySkins.skins);
                    }
                }
            }
            catch (System.Exception e)
            {
                Log.Error(e);
            }
        }

        private static void FixHenry()
        {
            Addressables.LoadAssetAsync<Material>(RoR2_Base_Commando.matCommandoDualies_mat).Completed += delegate (AsyncOperationHandle<Material> obj0)
            {
                Addressables.LoadAssetAsync<GameObject>(RoR2_Base_Commando.CommandoBody_prefab).Completed += delegate (AsyncOperationHandle<GameObject> obj)
                {
                    var mdlLoc = obj.Result.GetComponent<ModelLocator>();
                    var childLoc = mdlLoc.modelTransform.GetComponent<ChildLocator>();
                    var matCmd = obj0.Result;

                    mdlLoc.modelTransform.GetComponent<CharacterModel>().baseRendererInfos =
                    [
                        new CharacterModel.RendererInfo
                        {
                            renderer = childLoc.FindChildComponent<MeshRenderer>("GunMeshL"),
                            defaultMaterial = matCmd,
                            defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On
                        },
                        new CharacterModel.RendererInfo
                        {
                            renderer = childLoc.FindChildComponent<MeshRenderer>("GunMeshR"),
                            defaultMaterial = matCmd,
                            defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On
                        },
                        new CharacterModel.RendererInfo
                        {
                            renderer = mdlLoc.modelTransform.Find("CommandoMesh").GetComponent<SkinnedMeshRenderer>(),
                            defaultMaterial = matCmd,
                            defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On
                        }
                    ];

                    Log.Debug("Commando baseRendererInfos done");
                };
            };
        }

        /// <summary>
        /// Fix two Elder Lemurian footstep events to play sound and not spam Layer Index -1.
        /// </summary>
        public static void FixElderLemurianFootstepEvents()
        {
            Addressables.LoadAssetAsync<RuntimeAnimatorController>(RoR2_Base_Lemurian.animLemurianBruiser_controller).Completed += delegate (AsyncOperationHandle<RuntimeAnimatorController> obj)
            {
                var anim = obj.Result;
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
            };
        }

        /// <summary>
        /// Prevent Sale Star's pickup from complaining about its collider's settings.
        /// </summary>
        public static void FixSaleStarCollider()
        {
            Addressables.LoadAssetAsync<GameObject>(RoR2_DLC2_Items_LowerPricedChests.PickupSaleStar_prefab).Completed += delegate (AsyncOperationHandle<GameObject> obj)
            {
                var collider = obj.Result.transform.Find("SaleStar")?.GetComponent<MeshCollider>();
                if (collider == null || collider.convex)
                {
                    Log.PatchFail("collider of SaleStar");
                    return;
                }

                collider.convex = true;
                Log.Debug("SaleStar Collider done");
            };
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

        public static void MoreHudChildLocEntries()
        {
            Addressables.LoadAssetAsync<GameObject>(RoR2_Base_UI.HUDSimple_prefab).Completed += delegate (AsyncOperationHandle<GameObject> obj)
            {
                var hud = obj.Result.GetComponent<HUD>();
                var childLoc = hud.GetComponent<ChildLocator>();
                var springCanvas = hud.mainUIPanel.transform.Find("SpringCanvas");

                var newChildLoc = childLoc.transformPairs.ToList();
                newChildLoc.AddRange(
                [
                    // main clusters
                    // exists:
                    // BottomLeftCluster
                    // TopCenterCluster
                    new ChildLocator.NameTransformPair
                    {
                        name = "SpringCanvas",
                        transform = springCanvas
                    },
                    new ChildLocator.NameTransformPair
                    {
                        name = "UpperRightCluster",
                        transform = springCanvas.Find("UpperRightCluster")
                    },
                    new ChildLocator.NameTransformPair
                    {
                        name = "BottomRightCluster",
                        transform = springCanvas.Find("BottomRightCluster")
                    },
                    new ChildLocator.NameTransformPair
                    {
                        name = "UpperLeftCluster",
                        transform = springCanvas.Find("UpperLeftCluster")
                    },
                    new ChildLocator.NameTransformPair
                    {
                        name = "BottomCenterCluster",
                        transform = springCanvas.Find("BottomCenterCluster")
                    },
                    new ChildLocator.NameTransformPair
                    {
                        name = "LeftCluster",
                        transform = springCanvas.Find("LeftCluster")
                    },
                    new ChildLocator.NameTransformPair
                    {
                        name = "RightCluster",
                        transform = springCanvas.Find("RightCluster")
                    },

                    // extra stuff
                    // exists:
                    // RightUtilityArea
                    // RightInfoBar
                    // ScopeContainer
                    // CrosshairExtras
                    // BossHealthBar
                    new ChildLocator.NameTransformPair
                    {
                        name = "NotificationArea",
                        transform = hud.mainContainer.transform.Find("NotificationArea")
                    },
                    new ChildLocator.NameTransformPair
                    {
                        name = "ScoreboardPanel",
                        transform = springCanvas.Find("ScoreboardPanel")
                    },
                    new ChildLocator.NameTransformPair
                    {
                        name = "SkillDisplayRoot",
                        transform = springCanvas.Find("BottomRightCluster/Scaler")
                    },
                    new ChildLocator.NameTransformPair
                    {
                        name = "BuffDisplayRoot",
                        transform = springCanvas.Find("BottomLeftCluster/BarRoots/LevelDisplayCluster/BuffDisplayRoot")
                    },
                    new ChildLocator.NameTransformPair
                    {
                        name = "InventoryDisplayRoot",
                        transform = springCanvas.Find("TopCenterCluster/ItemInventoryDisplayRoot")
                    }
                    // riskUI is the only REAL full UI overhaul so this should alleviate some of the differences
                ]);

                // shouldnt happen, but any duplicates can get removed
                childLoc.transformPairs =
                [
                    ..newChildLoc
                    .GroupBy(pair => pair.name)
                    .Select(group => group.First())
                ];

                Log.Debug("HUD Childlocator updated");
            };
        }
    }
}
