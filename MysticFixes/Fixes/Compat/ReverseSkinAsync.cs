using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace MiscFixes.Fixes.Compat
{
    public class ReverseSkinAsync : MonoBehaviour
    {
        private readonly List<CharacterModel.RendererInfo> baseRendererInfos = new List<CharacterModel.RendererInfo>();

        private readonly List<SkinDef.GameObjectActivationTemplate> gameObjectActivationTemplates = new List<SkinDef.GameObjectActivationTemplate>();

        private readonly List<SkinDef.MeshReplacementTemplate> meshReplacementTemplates = new List<SkinDef.MeshReplacementTemplate>();

        public void Initialize(GameObject modelObject, SkinDef skinDef)
        {
            var en = skinDef.BakeAsync();
            while (en.MoveNext()) { }

            var runtimeSkin = skinDef.runtimeSkin;
            baseRendererInfos.AddRange(modelObject.GetComponent<CharacterModel>().baseRendererInfos);

            var array = runtimeSkin.gameObjectActivationTemplates;
            for (int i = 0; i < array.Length; i++)
            {
                SkinDef.GameObjectActivationTemplate gameObjectActivationTemplate = array[i];
                gameObjectActivationTemplates.Add(new SkinDef.GameObjectActivationTemplate
                {
                    transformPath = gameObjectActivationTemplate.transformPath,
                    shouldActivate = !gameObjectActivationTemplate.shouldActivate
                });
            }

            var array2 = runtimeSkin.meshReplacementTemplates;
            for (int i = 0; i < array2.Length; i++)
            {
                SkinDef.MeshReplacementTemplate meshReplacementTemplate = array2[i];
                Transform mdlTransform = modelObject.transform.Find(meshReplacementTemplate.transformPath);
                if (!mdlTransform)
                {
                    continue;
                }

                Renderer component = mdlTransform.GetComponent<Renderer>();
                Mesh sharedMesh = null;
                if (component is SkinnedMeshRenderer skinnedMeshRenderer)
                {
                    sharedMesh = skinnedMeshRenderer.sharedMesh;
                }
                else
                {
                    var filter = mdlTransform.GetComponent<MeshFilter>();
                    if (filter)
                        sharedMesh = filter.sharedMesh;
                }

                if (!sharedMesh)
                    continue;

                meshReplacementTemplates.Add(new SkinDef.MeshReplacementTemplate
                {
                    transformPath = meshReplacementTemplate.transformPath,
                    meshReference = new RoR2.ContentManagement.AssetOrDirectReference<Mesh> { directRef = sharedMesh }
                });
            }
        }

        public void ApplySkin(GameObject modelObject)
        {
            Transform transform = modelObject.transform;
            modelObject.GetComponent<CharacterModel>().baseRendererInfos = baseRendererInfos.ToArray();

            foreach (SkinDef.GameObjectActivationTemplate gameObjectActivationTemplate in gameObjectActivationTemplates)
            {
                Transform transform2 = transform.Find(gameObjectActivationTemplate.transformPath);
                if (transform2)
                {
                    transform2.gameObject.SetActive(gameObjectActivationTemplate.shouldActivate);
                }
            }

            foreach (SkinDef.MeshReplacementTemplate meshReplacementTemplate in meshReplacementTemplates)
            {
                Transform transform3 = transform.Find(meshReplacementTemplate.transformPath);
                if (!transform3)
                    continue;

                Renderer component = transform3.GetComponent<Renderer>();
                if (component is SkinnedMeshRenderer skinnedMeshRenderer)
                {
                    skinnedMeshRenderer.sharedMesh = meshReplacementTemplate.meshReference.Result;
                }
                else
                {
                    var filter = transform3.GetComponent<MeshFilter>();
                    if (filter)
                        filter.sharedMesh = meshReplacementTemplate.meshReference.Result;
                }
            }
        }
    }
}
