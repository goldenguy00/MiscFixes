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

        private void Awake()
        {
            characterModel = GetComponent<CharacterModel>();
            modelSkinController = GetComponent<ModelSkinController>();
        }

        public void ApplyDelta(int currentSkinIndex, int newSkinIndex)
        {
            if (currentSkinIndex == newSkinIndex)
                return;

            var currentSkin = ArrayUtils.GetSafe(modelSkinController.skins, currentSkinIndex);
            var newSkin = ArrayUtils.GetSafe(modelSkinController.skins, newSkinIndex);

            if (!(currentSkin && newSkin))
                return;
            var enumerable = currentSkin.BakeAsync();
            while (enumerable.MoveNext()) ;

            var enumerable2 = newSkin.BakeAsync();
            while (enumerable2.MoveNext()) ;

            var currentRuntime = currentSkin.runtimeSkin;
            var newRuntime = newSkin.runtimeSkin;

            var deltaRendererInfos = newRuntime.rendererInfoTemplates.ToArray();
            var deltaGoActivations = newRuntime.gameObjectActivationTemplates.ToArray();
            var deltaMeshReplacements = newRuntime.meshReplacementTemplates.ToArray();
        }
    }
}
