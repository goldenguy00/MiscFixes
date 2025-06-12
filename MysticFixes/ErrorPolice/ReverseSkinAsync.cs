using System.Collections.Generic;
using System.Linq;
using HG;
using RoR2;
using UnityEngine;

namespace MiscFixes.ErrorPolice
{
    public class ReverseSkinAsync : MonoBehaviour
    {
        private CharacterModel characterModel;
        private ModelSkinController modelSkinController;

        private List<SkinDefParams.GameObjectActivation> cachedGos = [];
        private List<SkinDefParams.MeshReplacement> cachedMeshs = [];
        private List<CharacterModel.RendererInfo> cachedInfos = [];
        private List<CharacterModel.LightInfo> cachedLights = [];

        private static Mesh GetMeshFromRenderer(Renderer rend)
        {
            if (rend is SkinnedMeshRenderer skinned)
                return skinned.sharedMesh;
            var filter = rend.GetComponent<MeshFilter>();
            return filter ? filter.sharedMesh : null;
        }

        private void Awake()
        {
            characterModel = GetComponent<CharacterModel>();
            modelSkinController = GetComponent<ModelSkinController>();
        }

        public void ApplyDelta(int newSkinIndex)
        {
            RevertToOriginals();

            var newSkin = ArrayUtils.GetSafe(modelSkinController.skins, newSkinIndex);
            if (!newSkin)
                return;

            var enumerable2 = newSkin.BakeAsync();
            while (enumerable2.MoveNext()) ;

            var newRuntime = newSkin.runtimeSkin;
            if (newRuntime is null)
                return;

            cachedInfos = 
            [.. 
                from temp in newRuntime.rendererInfoTemplates
                let trans = transform.Find(temp.transformPath)
                where trans != null
                let rend = trans.GetComponent<Renderer>()
                where rend != null
                select temp.rendererInfoData with { renderer = rend }
            ];

            cachedLights = 
            [.. 
                from temp in newRuntime.lightReplacementTemplates
                let trans = transform.Find(temp.transformPath)
                where trans != null
                let light = trans.GetComponent<Light>()
                where light != null
                select temp.data with { light = light}
            ];

            cachedGos = 
            [.. 
                from temp in newRuntime.gameObjectActivationTemplates
                let trans = transform.Find(temp.transformPath)
                where trans != null
                select new SkinDefParams.GameObjectActivation
                {
                    gameObject = trans.gameObject,
                    shouldActivate = trans.gameObject.activeSelf
                }
            ];

            cachedMeshs = 
            [.. 
                from temp in newRuntime.meshReplacementTemplates
                let trans = transform.Find(temp.transformPath)
                where trans != null
                let rend = trans.GetComponent<Renderer>()
                where rend != null
                select new SkinDefParams.MeshReplacement
                {
                    renderer = rend,
                    mesh = GetMeshFromRenderer(rend)
                }
            ];

        }

        public void RevertToOriginals()
        {
            if (cachedInfos.Any())
                characterModel.baseRendererInfos = [.. cachedInfos];

            if (cachedLights.Any())
                characterModel.baseLightInfos = [.. cachedLights];

            foreach (var info in cachedInfos)
            {
                if (info.renderer)
                    info.renderer.material = info.defaultMaterial;
            }

            foreach (var light in cachedLights)
            {
                if (light.light)
                    light.light.color = light.defaultColor;
            }

            foreach (var go in cachedGos)
            {
                if (go.gameObject)
                    go.gameObject.SetActive(go.shouldActivate);
            }

            foreach (var rend in cachedMeshs)
            {
                if (rend.renderer)
                {
                    if (rend.renderer is SkinnedMeshRenderer skinned)
                        skinned.sharedMesh = rend.mesh;
                    else if (rend.renderer.TryGetComponent<MeshFilter>(out var filter))
                        filter.sharedMesh = rend.mesh;
                }
            }

            cachedInfos.Clear();
            cachedLights.Clear();
            cachedMeshs.Clear();
            cachedGos.Clear();
        }
    }
}
