using System;
using System.Collections.Generic;
using System.Linq;
using HG;
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

        private IEnumerable<SkinDefParams.GameObjectActivation> cachedGos = [];
        private IEnumerable<SkinDefParams.MeshReplacement> cachedMeshs = [];
        private CharacterModel.RendererInfo[] cachedInfos = [];

        private void Awake()
        {
            characterModel = GetComponent<CharacterModel>();
            modelSkinController = GetComponent<ModelSkinController>();
        }

        // UN FUCKING REAdable
        public void ApplyDelta(int currentSkinIndex, int newSkinIndex)
        {
            if (currentSkinIndex == newSkinIndex)
                return;

            var newSkin = ArrayUtils.GetSafe(modelSkinController.skins, newSkinIndex);
            if (!newSkin)
                return;

            var enumerable2 = newSkin.BakeAsync();
            while (enumerable2.MoveNext()) ;

            var newRuntime = newSkin.runtimeSkin;
            if (newRuntime is null)
                return;

            RevertToOriginals();

            cachedInfos =  ArrayUtils.Clone(characterModel.baseRendererInfos);

            cachedGos = from ngo in newRuntime.gameObjectActivationTemplates
                        let trans = transform.Find(ngo.transformPath)
                        where trans != null
                        select new SkinDefParams.GameObjectActivation
                        {
                            gameObject = trans.gameObject,
                            shouldActivate = trans.gameObject.activeSelf
                        };


            cachedMeshs = from nm in newRuntime.meshReplacementTemplates
                        let trans = transform.Find(nm.transformPath)
                        where trans != null
                        let rend = trans.GetComponent<Renderer>()
                        where rend != null
                        let mesh = GetMeshFromRenderer(rend)
                        select new SkinDefParams.MeshReplacement
                        {
                            renderer = trans.GetComponent<Renderer>(),
                            mesh = mesh
                        };
        }

        private void RevertToOriginals()
        {
            if (cachedInfos.Any())
            {
                characterModel.baseRendererInfos = ArrayUtils.Clone(cachedInfos);

                foreach (var info in cachedInfos)
                {
                    Log.Warning($"setting rend {info.renderer.name} to mat {info.defaultMaterial.name}");
                    info.renderer.material = info.defaultMaterial;
                }
            }

            if (cachedGos.Any())
            {
                foreach (var go in cachedGos)
                {
                    Log.Warning($"setting go {go.gameObject.name} to {go.shouldActivate}");
                    go.gameObject.SetActive(go.shouldActivate);
                }
            }

            if  (cachedMeshs.Any())
            {
                foreach (var rend in cachedMeshs)
                {
                    Log.Warning($"setting rend {rend.renderer.name} to mesh {rend.mesh?.name ?? "null"}");
                    if (rend.renderer is SkinnedMeshRenderer skinned)
                        skinned.sharedMesh = rend.mesh;
                    else if (rend.renderer.TryGetComponent<MeshFilter>(out var filter))
                        filter.sharedMesh = rend.mesh;
                }
            }
        }

        private Mesh GetMeshFromRenderer(Renderer trans)
        {
            if (trans is SkinnedMeshRenderer skinned)
                return skinned.sharedMesh;
            if (trans.TryGetComponent<MeshFilter>(out var filter))
                return filter.sharedMesh;
            return null;
        }
    }
}
