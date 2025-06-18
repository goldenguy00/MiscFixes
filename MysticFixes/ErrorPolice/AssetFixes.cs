using HG;
using MiscFixes.Modules;
using RoR2;
using RoR2.ContentManagement;
using RoR2.UI;
using RoR2BepInExPack.GameAssetPaths;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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
            FixGlassMithrixMaterials();
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

        /// <summary>
        /// Fixes Glass Mithrix missing material due to skin system not supporting more than 1 material per renderer
        /// </summary>
        private static void FixGlassMithrixMaterials()
        {
            // Intentionally not loading any assets async, since other mods might access or duplicate the model during load,
            // at which point the material fixes needs to already be in place.

            AssetReferenceGameObject brotherGlassBodyRef = new AssetReferenceGameObject(RoR2_Junk_BrotherGlass.BrotherGlassBody_prefab);
            GameObject brotherGlassBody = Utils.PreloadAsset(brotherGlassBodyRef).WaitForCompletion();

            Transform modelTransform = null;
            if (brotherGlassBody.TryGetComponent(out ModelLocator modelLocator))
            {
                modelTransform = modelLocator.modelTransform;
            }

            if (modelTransform)
            {
                AssetReferenceT<SkinDef> originalSkinRef = new AssetReferenceT<SkinDef>(RoR2_Base_Brother.skinBrotherBodyDefault_asset);
                SkinDef originalSkin = Utils.PreloadAsset(originalSkinRef).WaitForCompletion();

                ModelSkinController modelSkinController = modelTransform.gameObject.EnsureComponent<ModelSkinController>();
                int replacementSkinIndex = Array.IndexOf(modelSkinController.skins, originalSkin);

                SkinDef skinDef = GameObject.Instantiate(originalSkin);
                skinDef.name = "skinBrotherGlassBodyDefault";
                Transform originalSkinRoot = skinDef.rootObject.transform;
                skinDef.rootObject = modelTransform.gameObject;

                (AssetReferenceT<SkinDefParams> paramsAddress, SkinDefParams paramsDirect) = skinDef.GetSkinParams();
                AssetOrDirectReference<SkinDefParams> skinDefParamsReference = new AssetOrDirectReference<SkinDefParams>
                {
                    address = paramsAddress,
                    directRef = paramsDirect
                };

                SkinDefParams skinDefParams = GameObject.Instantiate(skinDefParamsReference.WaitForCompletion());
                skinDefParams.name = $"{skinDef.name}_params";
                skinDef.skinDefParams = skinDefParams;
                skinDef.skinDefParamsAddress = new AssetReferenceT<SkinDefParams>(string.Empty);
                skinDef.optimizedSkinDefParams = skinDefParams;
                skinDef.optimizedSkinDefParamsAddress = new AssetReferenceT<SkinDefParams>(string.Empty);

                List<CharacterModel.RendererInfo> rendererInfos = [.. skinDefParams.rendererInfos];
                List<SkinDefParams.GameObjectActivation> gameObjectActivations = [.. skinDefParams.gameObjectActivations];
                List<SkinDefParams.MeshReplacement> meshReplacements = [.. skinDefParams.meshReplacements];
                List<CharacterModel.LightInfo> lightReplacements = [.. skinDefParams.lightReplacements];

                for (int i = rendererInfos.Count - 1; i >= 0; i--)
                {
                    CharacterModel.RendererInfo rendererInfo = rendererInfos[i];

                    rendererInfo.renderer = rendererInfo.renderer.ResolveComponentInNewRoot(originalSkinRoot, modelTransform);

                    if (rendererInfo.renderer)
                    {
                        switch (rendererInfo.renderer.name)
                        {
                            case "BrotherHammerConcrete":
                            case "BrotherBodyMesh":
                                rendererInfo.defaultMaterial = null;
                                rendererInfo.defaultMaterialAddress = new AssetReferenceT<Material>(RoR2_Base_Brother.maBrotherGlassOverlay_mat);
                                break;
                        }

                        rendererInfos[i] = rendererInfo;
                    }
                    else
                    {
                        rendererInfos.RemoveAt(i);
                    }
                }

                for (int i = gameObjectActivations.Count - 1; i >= 0; i--)
                {
                    SkinDefParams.GameObjectActivation gameObjectActivation = gameObjectActivations[i];

                    gameObjectActivation.gameObject = gameObjectActivation.gameObject.ResolveObjectInNewRoot(originalSkinRoot, modelTransform);

                    if (gameObjectActivation.gameObject)
                    {
                        gameObjectActivations[i] = gameObjectActivation;
                    }
                    else
                    {
                        gameObjectActivations.RemoveAt(i);
                    }
                }

                for (int i = meshReplacements.Count - 1; i >= 0; i--)
                {
                    SkinDefParams.MeshReplacement meshReplacement = meshReplacements[i];

                    meshReplacement.renderer = meshReplacement.renderer.ResolveComponentInNewRoot(originalSkinRoot, modelTransform);

                    if (meshReplacement.renderer)
                    {
                        meshReplacements[i] = meshReplacement;
                    }
                    else
                    {
                        meshReplacements.RemoveAt(i);
                    }
                }

                for (int i = lightReplacements.Count - 1; i >= 0; i--)
                {
                    CharacterModel.LightInfo lightReplacement = lightReplacements[i];

                    lightReplacement.light = lightReplacement.light.ResolveComponentInNewRoot(originalSkinRoot, modelTransform);

                    if (lightReplacement.light)
                    {
                        lightReplacements[i] = lightReplacement;
                    }
                    else
                    {
                        lightReplacements.RemoveAt(i);
                    }
                }

                skinDefParams.rendererInfos = [.. rendererInfos];
                skinDefParams.gameObjectActivations = [.. gameObjectActivations];
                skinDefParams.meshReplacements = [.. meshReplacements];
                skinDefParams.lightReplacements = [.. lightReplacements];

                if (ArrayUtils.IsInBounds(modelSkinController.skins, replacementSkinIndex))
                {
                    modelSkinController.skins[replacementSkinIndex] = skinDef;
                }
                else
                {
                    ArrayUtils.ArrayAppend(ref modelSkinController.skins, skinDef);
                }

                PersistentOverlay persistentOverlay = modelTransform.gameObject.AddComponent<PersistentOverlay>();
                persistentOverlay.OverlayMaterialReference = new AssetReferenceT<Material>(RoR2_Base_Brother.matBrotherGlassDistortion_mat);

                Utils.UnloadAsset(originalSkinRef);
            }

            Utils.UnloadAsset(brotherGlassBodyRef);
        }
    }
}
