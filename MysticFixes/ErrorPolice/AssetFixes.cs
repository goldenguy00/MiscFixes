using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine;
using RoR2.UI;
using System.Linq;
using RoR2;
using MiscFixes.Modules;
using RoR2BepInExPack.GameAssetPaths;

namespace MiscFixes.ErrorPolice
{
    internal static class AssetFixes
    {
        internal static void Init()
        {
            FixElderLemurianFootstepEvents();
            FixSaleStarCollider();
            MoreHudChildLocEntries();
            FixHenry();
            FixScrapper();
            FixVermin();
        }

        private static void FixVermin()
        {
            var ref1 = new AssetReferenceGameObject(RoR2_DLC1_Vermin.VerminSpawn_prefab);

            Utils.PreloadAsset(ref1).Completed += delegate (AsyncOperationHandle<GameObject> obj)
            {
                obj.Result.GetComponent<EffectComponent>().positionAtReferencedTransform = true;
                Utils.UnloadAsset(ref1);
            };
        }

        private static void FixScrapper()
        {
            var bodyRef = new AssetReferenceGameObject(RoR2_Base_Toolbot.ToolbotBody_prefab);
            var scrapRef = new AssetReferenceGameObject(RoR2_Base_Scrapper.Scrapper_prefab);

            Utils.PreloadAsset(bodyRef).Completed += delegate (AsyncOperationHandle<GameObject> toolbotBody)
            {
                Utils.PreloadAsset(scrapRef).Completed += delegate (AsyncOperationHandle<GameObject> scrapper)
                {
                    scrapper.Result.CloneComponent(toolbotBody.Result.GetComponent<AkBank>());
                    Log.Debug("Added AkBank to scrapper");

                    Utils.UnloadAsset(bodyRef);
                    Utils.UnloadAsset(scrapRef);
                };
            };
        }

        private static void FixHenry()
        {
            var bodyRef = new AssetReferenceGameObject(RoR2_Base_Commando.CommandoBody_prefab);
            var matRef = new AssetReferenceT<Material>(RoR2_Base_Commando.matCommandoDualies_mat);

            Utils.PreloadAsset(bodyRef).Completed += delegate (AsyncOperationHandle<GameObject> bodyHandle)
            {
                Utils.PreloadAsset(matRef).Completed += delegate (AsyncOperationHandle<Material> matHandle)
                {
                    var mdlLoc = bodyHandle.Result.GetComponent<ModelLocator>();
                    var childLoc = mdlLoc.modelTransform.GetComponent<ChildLocator>();
                    var matCmd = matHandle.Result;

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

                    Utils.UnloadAsset(bodyRef);
                    Utils.UnloadAsset(matRef);
                };
            };
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
                    return;
                }

                collider.convex = true;
                Log.Debug("SaleStar Collider done");
                Utils.UnloadAsset(objRef);
            };
        }

        public static void MoreHudChildLocEntries()
        {
            var objRef = new AssetReferenceGameObject(RoR2_Base_UI.HUDSimple_prefab);
            Utils.PreloadAsset(objRef).Completed += delegate (AsyncOperationHandle<GameObject> objHandle)
            {
                var hud = objHandle.Result.GetComponent<HUD>();
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
                Utils.UnloadAsset(objRef);
            };
        }
    }
}
