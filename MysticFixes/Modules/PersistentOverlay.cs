using System;
using HG;
using RoR2;
using RoR2.ContentManagement;
using RoR2BepInExPack.GameAssetPathsBetter;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MiscFixes.Modules
{
    public class PersistentOverlay : MonoBehaviour
    {
        private CharacterModel _characterModel;
        private TemporaryOverlayInstance _temporaryOverlay;
        private AssetOrDirectReference<Material> _overlayReference;

        public static void Init(GameObject modelObject, SkinDef originalSkinDef, SkinDefParams originalSkinDefParams)
        {
            if (!modelObject.TryGetComponent(out ModelLocator modelLocator))
            {
                Log.Error("modelLocator is null");
                return;
            }

            var modelTransform = modelLocator.modelTransform;
            if (!modelTransform)
            {
                Log.Error("modelTransform is null");
                return;
            }

            if (!originalSkinDef?.rootObject)
            {
                Log.Error("originalSkinDef?.rootObject is null");
                return;
            }

            if (!originalSkinDefParams)
            {
                Log.Error("originalSkinDefParams is null");
                return;
            }

            var originalSkinRoot = originalSkinDef.rootObject.transform;
            var newSkinDef = SkinDef.Instantiate(originalSkinDef);
            newSkinDef.name = "skinBrotherGlassBodyDefault";
            newSkinDef.rootObject = modelTransform.gameObject;

            var skinDefParams = SkinDefParams.Instantiate(originalSkinDefParams);
            skinDefParams.name = $"{newSkinDef.name}_params";

            newSkinDef.skinDefParams = skinDefParams;
            newSkinDef.skinDefParamsAddress = new AssetReferenceT<SkinDefParams>(string.Empty);
            newSkinDef.optimizedSkinDefParams = skinDefParams;
            newSkinDef.optimizedSkinDefParamsAddress = new AssetReferenceT<SkinDefParams>(string.Empty);

            for (var i = skinDefParams.rendererInfos.Length - 1; i >= 0; i--)
            {
                ref var rendererInfo = ref skinDefParams.rendererInfos[i];

                rendererInfo.renderer = rendererInfo.renderer.ResolveComponentInNewRoot(originalSkinRoot, modelTransform);

                if (!rendererInfo.renderer)
                {
                    ArrayUtils.ArrayRemoveAtAndResize(ref skinDefParams.rendererInfos, i);
                    continue;
                }

                switch (rendererInfo.renderer.name)
                {
                    case "BrotherHammerConcrete":
                    case "BrotherBodyMesh":
                        rendererInfo.defaultMaterial = null;
                        rendererInfo.defaultMaterialAddress = new AssetReferenceT<Material>(RoR2_Base_Brother.maBrotherGlassOverlay_mat);
                        break;
                }
            }

            for (var i = skinDefParams.gameObjectActivations.Length - 1; i >= 0; i--)
            {
                ref var gameObjectActivation = ref skinDefParams.gameObjectActivations[i];

                gameObjectActivation.gameObject = gameObjectActivation.gameObject.ResolveObjectInNewRoot(originalSkinRoot, modelTransform);

                if (!gameObjectActivation.gameObject)
                    ArrayUtils.ArrayRemoveAtAndResize(ref skinDefParams.gameObjectActivations, i);
            }

            for (var i = skinDefParams.meshReplacements.Length - 1; i >= 0; i--)
            {
                ref var meshReplacement = ref skinDefParams.meshReplacements[i];

                meshReplacement.renderer = meshReplacement.renderer.ResolveComponentInNewRoot(originalSkinRoot, modelTransform);

                if (!meshReplacement.renderer)
                    ArrayUtils.ArrayRemoveAtAndResize(ref skinDefParams.meshReplacements, i);
            }

            for (var i = skinDefParams.lightReplacements.Length - 1; i >= 0; i--)
            {
                ref var lightReplacement = ref skinDefParams.lightReplacements[i];

                lightReplacement.light = lightReplacement.light.ResolveComponentInNewRoot(originalSkinRoot, modelTransform);

                if (!lightReplacement.light)
                    ArrayUtils.ArrayRemoveAtAndResize(ref skinDefParams.lightReplacements, i);
            }

            var modelSkinController = modelTransform.gameObject.EnsureComponent<ModelSkinController>();

            var replacementSkinIndex = Array.IndexOf(modelSkinController.skins, originalSkinDef);
            if (ArrayUtils.IsInBounds(modelSkinController.skins, replacementSkinIndex))
                modelSkinController.skins[replacementSkinIndex] = newSkinDef;
            else
                ArrayUtils.ArrayAppend(ref modelSkinController.skins, newSkinDef);

            modelTransform.gameObject.AddComponent<PersistentOverlay>();
        }

        private void Awake()
        {
            _characterModel = GetComponent<CharacterModel>();

            _overlayReference = new AssetOrDirectReference<Material>
            {
                unloadType = AsyncReferenceHandleUnloadType.AtWill,
                address = new AssetReferenceT<Material>(RoR2_Base_Brother.matBrotherGlassDistortion_mat)
            };
        }

        private void OnEnable()
        {
            if (_overlayReference.IsLoaded())
                SetOverlayMaterial(_overlayReference.Result);

            _overlayReference.onValidReferenceDiscovered += SetOverlayMaterial;
        }

        private void OnDisable()
        {
            if (_overlayReference is not null)
                _overlayReference.onValidReferenceDiscovered -= SetOverlayMaterial;

            SetOverlayMaterial(null);
        }

        private void OnDestroy()
        {
            _overlayReference?.Reset();
            _overlayReference = null;
        }

        public void SetOverlayMaterial(Material newMaterial)
        {
            var newHasOverlay = newMaterial != null;
            var hasOverlay = _temporaryOverlay is not null;

            if (newHasOverlay != hasOverlay || _temporaryOverlay?.originalMaterial != newMaterial)
            {
                _temporaryOverlay?.CleanupEffect();
                _temporaryOverlay = null;

                if (newHasOverlay)
                {
                    _temporaryOverlay = TemporaryOverlayManager.AddOverlay(base.gameObject);
                    _temporaryOverlay.duration = float.PositiveInfinity;
                    _temporaryOverlay.originalMaterial = newMaterial;
                    _temporaryOverlay.AddToCharacterModel(_characterModel);
                }
            }
        }
    }
}