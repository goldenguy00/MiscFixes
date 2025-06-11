using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace MiscFixes.ErrorPolice
{
    public class ReverseSkinAsync : MonoBehaviour
    {
        private CharacterModel characterModel;
        private ModelSkinController modelSkinController;

        private readonly List<CharacterModel.RendererInfo> baseRendererInfos = [];
        private readonly List<SkinDef.GameObjectActivationTemplate> gameObjectActivationTemplates = [];
        private readonly List<SkinDef.MeshReplacementTemplate> meshReplacementTemplates = [];

        private void Awake()
        {
            characterModel = GetComponent<CharacterModel>();
            modelSkinController = GetComponent<ModelSkinController>();
            modelSkinController.onSkinApplied += ModelSkinController_onSkinApplied;
        }

        private void ModelSkinController_onSkinApplied(int newSkinIndex)
        {
            var newSkin = HG.ArrayUtils.GetSafe(modelSkinController.skins, newSkinIndex);
            if (newSkin)
                Initialize(newSkin);
        }

        private void Initialize(SkinDef skinDef)
        {
            baseRendererInfos.AddRange(characterModel.baseRendererInfos);
            foreach (var objectActivation in skinDef.runtimeSkin.gameObjectActivationTemplates)
            {
                gameObjectActivationTemplates.Add(new SkinDef.GameObjectActivationTemplate
                {
                    transformPath = objectActivation.transformPath,
                    shouldActivate = !objectActivation.shouldActivate
                });
            }

            foreach (var meshReplacement in skinDef.runtimeSkin.meshReplacementTemplates)
            {
                var rendererTransform = transform.Find(meshReplacement.transformPath);
                if (!rendererTransform)
                    continue;

                Mesh mesh = null;

                var renderer = rendererTransform.GetComponent<Renderer>();
                if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
                    mesh = skinnedMeshRenderer.sharedMesh;
                else if (renderer.TryGetComponent<MeshFilter>(out var filter))
                    mesh = filter.sharedMesh;

                meshReplacementTemplates.Add(new SkinDef.MeshReplacementTemplate
                {
                    transformPath = meshReplacement.transformPath,
                    meshReference = new RoR2.ContentManagement.AssetOrDirectReference<Mesh>() { directRef = mesh }
                });
            }
        }

        public void Dispose()
        {
            characterModel.baseRendererInfos = [.. baseRendererInfos];

            foreach (var objectActivation in gameObjectActivationTemplates)
            {
                var gameActivationTransform = transform.Find(objectActivation.transformPath);
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
