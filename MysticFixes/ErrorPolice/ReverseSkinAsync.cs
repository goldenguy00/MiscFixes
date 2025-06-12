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

        private IEnumerable<SkinDef.GameObjectActivationTemplate> cachedGos = [];
        private IEnumerable<SkinDef.MeshReplacementTemplate> cachedMeshs = [];
        private IEnumerable<SkinDef.RendererInfoTemplate> cachedInfos = [];

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

            cachedInfos = from ninfo in newRuntime.rendererInfoTemplates
                        let trans = transform.Find(ninfo.transformPath)
                        where trans != null
                        let rend = trans.GetComponent<Renderer>()
                        select new SkinDef.RendererInfoTemplate
                        {
                            transformPath = ninfo.transformPath,
                            data = ninfo.data,
                            materialReference = new AssetOrDirectReference<Material> { directRef = rend.material }
                        };

            cachedGos = from ngo in newRuntime.gameObjectActivationTemplates
                        let trans = transform.Find(ngo.transformPath)
                        where trans != null
                        select new SkinDef.GameObjectActivationTemplate
                        {
                            transformPath = ngo.transformPath,
                            shouldActivate = trans.gameObject.activeSelf
                        };


            cachedMeshs = from nm in newRuntime.meshReplacementTemplates
                        let trans = transform.Find(nm.transformPath)
                        where trans != null
                        let mesh = GetMeshFromRenderer(transform)
                        select new SkinDef.MeshReplacementTemplate
                        {
                            transformPath = nm.transformPath,
                            meshReference = new AssetOrDirectReference<Mesh> { directRef = mesh }
                        };
        }

        private void RevertToOriginals()
        {
            if (cachedInfos.Any())
            {
                characterModel.baseRendererInfos = [.. from info in cachedInfos
                                                       select info.data];

                foreach (var info in from ci in cachedInfos
                                     let trans = transform.Find(ci.transformPath)
                                     where trans != null
                                     let rend = trans.GetComponent<Renderer>()
                                     where rend != null
                                     select new { renderer = rend, mat = ci.materialReference.Result })
                {
                    Log.Warning($"setting rend {info.renderer.name} to mat {info.mat.name}");
                    info.renderer.material = info.mat;
                }
            }

            if (cachedGos.Any())
            {
                foreach (var cgt in from cg in cachedGos
                                    let trans = transform.Find(cg.transformPath)
                                    where trans != null
                                    select new { trans.gameObject, active = cg.shouldActivate })
                {
                    Log.Warning($"setting go {cgt.gameObject.name} to {cgt.active}");
                    cgt.gameObject.SetActive(cgt.active);
                }
            }

            if  (cachedMeshs.Any())
            {
                foreach (var rend in from cm in cachedMeshs
                                     let trans = transform.Find(cm.transformPath)
                                     where trans != null
                                     select new { trans, mesh = cm.meshReference.Result })
                {
                    Log.Warning($"setting rend {rend.trans.name} to mesh {rend.mesh?.name ?? "null"}");
                    if (rend.trans.TryGetComponent<SkinnedMeshRenderer>(out var skinned))
                        skinned.sharedMesh = rend.mesh;
                    else if (rend.trans.TryGetComponent<MeshFilter>(out var filter))
                        filter.sharedMesh = rend.mesh;
                }
            }
        }

        private Mesh GetMeshFromRenderer(Transform trans)
        {
            if (trans.TryGetComponent<SkinnedMeshRenderer>(out var skinned))
                return skinned.sharedMesh;
            else if (trans.TryGetComponent<MeshFilter>(out var filter))
                return filter.sharedMesh;
            return null;
        }
    }
}
