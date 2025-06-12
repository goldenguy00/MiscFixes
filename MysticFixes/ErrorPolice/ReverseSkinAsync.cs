using System.Collections.Generic;
using System.Linq;
using MiscFixes.Modules;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;

namespace MiscFixes.ErrorPolice
{
    public class ReverseSkinAsync : MonoBehaviour
    {
        private CharacterModel characterModel;
        private ModelSkinController modelSkinController;

        private IEnumerable<CharacterModel.RendererInfo> baseRendererInfos = [];
        private IEnumerable<SkinDef.GameObjectActivationTemplate> cachedGameObjectActivationTemplates = [];
        private IEnumerable<SkinDef.MeshReplacementTemplate> meshReplacementTemplates = [];

        private void Awake()
        {
            characterModel = GetComponent<CharacterModel>();
            modelSkinController = GetComponent<ModelSkinController>();
            modelSkinController.onSkinApplied += ModelSkinController_onSkinApplied;
        }

        private void ModelSkinController_onSkinApplied(int newSkinIndex)
        {
            Log.Error("ON APPLY SKIN FOR INDEX " + newSkinIndex);
            var newSkin = HG.ArrayUtils.GetSafe(modelSkinController.skins, newSkinIndex);
            if (newSkin)
                Initialize(newSkin);
        }

        private void Initialize(SkinDef skinDef)
        {
            Log.Error("INITIALIZE CALLED FOR " + (skinDef.name ?? skinDef.nameToken));

            baseRendererInfos = HG.ArrayUtils.Clone(characterModel.baseRendererInfos);

            cachedGameObjectActivationTemplates = skinDef.runtimeSkin.gameObjectActivationTemplates
                .Select(objectActivation => new SkinDef.GameObjectActivationTemplate
                {
                    transformPath = objectActivation.transformPath,
                    shouldActivate = !objectActivation.shouldActivate
                });

            meshReplacementTemplates = skinDef.runtimeSkin.meshReplacementTemplates
                .Where(meshReplacement => transform.Find(meshReplacement.transformPath))
                .Select(meshReplacement => new SkinDef.MeshReplacementTemplate
                {
                    transformPath = meshReplacement.transformPath,
                    meshReference = ConvertToReference(meshReplacement)
                });

            AssetOrDirectReference<Mesh> ConvertToReference(SkinDef.MeshReplacementTemplate meshReplacement)
            {
                Mesh mesh = null;
                var renderer = transform.Find(meshReplacement.transformPath).GetComponent<Renderer>();
                if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
                    mesh = skinnedMeshRenderer.sharedMesh;
                else if (renderer.TryGetComponent<MeshFilter>(out var filter))
                    mesh = filter.sharedMesh;
                return new AssetOrDirectReference<Mesh>() { directRef = mesh };
            }
        }

        public void Dispose()
        {
            Log.Error("DISPOSE CALLED!!!!!");
            characterModel.baseRendererInfos = [.. baseRendererInfos];

            foreach (var objectActivation in cachedGameObjectActivationTemplates)
            {
                var gameActivationTransform = transform.Find(objectActivation.transformPath);
                Log.Info("FUC");
                if (gameActivationTransform)
                {
                    gameActivationTransform.gameObject.SetActive(objectActivation.shouldActivate);
                }
            }

            foreach (var meshReplacement in meshReplacementTemplates)
            {
                var rendererTransform = transform.Find(meshReplacement.transformPath);
                if (!rendererTransform)
                    continue;

                var mesh = meshReplacement.meshReference?.Result;
                if (!mesh)
                    continue;

                var renderer = rendererTransform.GetComponent<Renderer>();
                if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
                    skinnedMeshRenderer.sharedMesh = mesh;
                else if (renderer.TryGetComponent<MeshFilter>(out var filter))
                    filter.sharedMesh = mesh;
            }
        }
    }
}
