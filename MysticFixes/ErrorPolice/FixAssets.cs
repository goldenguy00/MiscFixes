using System.Linq;
using MiscFixes.Modules;
using RoR2;
using RoR2.UI;
using RoR2BepInExPack.GameAssetPaths.Version_1_39_0;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MiscFixes.ErrorPolice
{
    internal static class FixAssets
    {
        internal static void Init()
        {
            MoreHudChildLocEntries();
            FixHenry();
            FixGlassMithrixMaterials();

            BodyCatalog.availability.CallWhenAvailable(FixBodies);
        }

        private static void FixBodies()
        {
            for (int i = 0; i < BodyCatalog.bodyPrefabBodyComponents.Length; ++i)
            {
                var body = BodyCatalog.bodyPrefabBodyComponents[i];
                if (ReferenceEquals(body.vehicleIdleStateMachine, null))
                {
                    Log.Error(BodyCatalog.bodyNames[i] + " | Null vehicleIdleStateMachine array!");
                    body.vehicleIdleStateMachine = [];
                }

                for (int j = body.vehicleIdleStateMachine.Length - 1; j >= 0; --j)
                {
                    if (body.vehicleIdleStateMachine[j] == null)
                    {
                        Log.Error(BodyCatalog.bodyNames[i] + " | Null vehicleIdleStateMachine at index " + j);
                        HG.ArrayUtils.ArrayRemoveAtAndResize(ref body.vehicleIdleStateMachine, j);
                    }
                }
            }
        }

        private static void FixHenry()
        {
            var bodyRef = Addressables.LoadAssetAsync<GameObject>(RoR2_Base_Commando.CommandoBody_prefab).WaitForCompletion();
            var matRef = Addressables.LoadAssetAsync<Material>(RoR2_Base_Commando.matCommandoDualies_mat).WaitForCompletion();

            var mdlLoc = bodyRef.GetComponent<ModelLocator>();
            var childLoc = mdlLoc.modelTransform.GetComponent<ChildLocator>();

            mdlLoc.modelTransform.GetComponent<CharacterModel>().baseRendererInfos =
            [
                new CharacterModel.RendererInfo
                {
                    renderer = childLoc.FindChildComponent<MeshRenderer>("GunMeshL"),
                    defaultMaterial = matRef,
                    defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On
                },
                new CharacterModel.RendererInfo
                {
                    renderer = childLoc.FindChildComponent<MeshRenderer>("GunMeshR"),
                    defaultMaterial = matRef,
                    defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On
                },
                new CharacterModel.RendererInfo
                {
                    renderer = mdlLoc.modelTransform.Find("CommandoMesh").GetComponent<SkinnedMeshRenderer>(),
                    defaultMaterial = matRef,
                    defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On
                }
            ];
        }

        public static void MoreHudChildLocEntries()
        {
            var objRef = Addressables.LoadAssetAsync<GameObject>(RoR2_Base_UI.HUDSimple_prefab).WaitForCompletion();

            var hud = objRef.GetComponent<HUD>();
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
        }

        /// <summary>
        /// Fixes Glass Mithrix missing material due to skin system not supporting more than 1 material per renderer
        /// </summary>
        private static void FixGlassMithrixMaterials()
        {
            var brotherGlassBodyRef = Addressables.LoadAssetAsync<GameObject>(RoR2_Junk_BrotherGlass.BrotherGlassBody_prefab).WaitForCompletion();
            var originalSkinRef = Addressables.LoadAssetAsync<SkinDef>(RoR2_Base_Brother.skinBrotherBodyDefault_asset).WaitForCompletion();
            var originalSkinParamsRef = Addressables.LoadAssetAsync<SkinDefParams>(RoR2_Base_Brother.skinBrotherBodyDefault_params_asset).WaitForCompletion();
            PersistentOverlay.Init(brotherGlassBodyRef, originalSkinRef, originalSkinParamsRef);
        }

        /// <summary>
        /// Prevent Sale Star's pickup from complaining about its collider's settings.
        /// </summary>
        public static void FixSaleStarCollider()
        {
            var objRef = Addressables.LoadAssetAsync<GameObject>(RoR2_DLC2_Items_LowerPricedChests.PickupSaleStar_prefab).WaitForCompletion();
            var collider = objRef?.transform.Find("SaleStar")?.GetComponent<MeshCollider>();
            if (collider == null || collider.convex)
            {
                Log.PatchFail("collider of SaleStar");
            }
            else
            {
                collider.convex = true;
            }
        }


    }
}
