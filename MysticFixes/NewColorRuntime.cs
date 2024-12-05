using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using TanksMod.Modules.Components;
using UnityEngine;

namespace MiscFixes
{
    public class NewColorRuntime : MonoBehaviour
    {
        public ColorRuntime colorRuntime;
        public ModelLocator modelLoc;
        public Light light;
        public ParticleSystem[] particleSystems;

        public void OnEnable()
        {
            this.colorRuntime = GetComponent<ColorRuntime>();
            modelLoc = GetComponent<ModelLocator>();
        }

        public void Update()
        {
            if (colorRuntime.previousGlowColor != colorRuntime.currentGlowColor)
            {
                ChangeGlowColor(colorRuntime.currentGlowColor);

                if (modelLoc && modelLoc.modelTransform)
                {
                    particleSystems ??= modelLoc.modelTransform.GetComponentsInChildren<ParticleSystem>(true);
                    foreach (ParticleSystem particleSystem in particleSystems)
                    {
                        if (particleSystem && particleSystem.main.startColor.color != colorRuntime.currentGlowColor &&
                            particleSystem.TryGetComponent<ParticleSystemRenderer>(out var renderer) && renderer.material.name.Contains("ThrusterParticleGradient"))
                        {
                            var main = particleSystem.main;
                            main.startColor = colorRuntime.currentGlowColor;
                        }
                    }

                    light ??= modelLoc.modelTransform.GetComponentInChildren<Light>(true);
                    if (light)
                        light.color = colorRuntime.currentGlowColor;
                }
            }

            if (colorRuntime.previousBodyColor != colorRuntime.currentBodyColor)
            {
                ChangeBodyColor(colorRuntime.currentBodyColor);
            }
        }

        public void ChangeBodyColor(Color color)
        {
            for (int i = 0; i < colorRuntime._matProperties.Length; i++)
            {
                foreach (Renderer bodyOnlyRenderer in colorRuntime.bodyOnlyRenderers)
                {
                    if (colorRuntime.renderers[i] == bodyOnlyRenderer && (colorRuntime.renderers[i].material.name.Contains("Base") || colorRuntime.renderers[i].material.name.Contains("Gummy")))
                    {
                        colorRuntime._matProperties[i].SetColor("_Color", color);
                        colorRuntime._matProperties[i].SetColor("_EmColor", Color.black);
                        colorRuntime._matProperties[i].SetFloat("_EmPower", 0f);
                        colorRuntime._matProperties[i].SetFloat("_SpecularStrength", 1f);
                        colorRuntime._matProperties[i].SetFloat("_SpecularExponent", 10f);
                        colorRuntime._matProperties[i].SetFloat("_RampInfo", 0f);
                        colorRuntime._matProperties[i].SetFloat("_DecalLayer", 2f);
                    }
                }

                colorRuntime.renderers[i].SetPropertyBlock(colorRuntime._matProperties[i]);
            }

            colorRuntime.previousBodyColor = color;
        }

        public void ChangeGlowColor(Color color)
        {
            for (int i = 0; i < colorRuntime._matProperties.Length; i++)
            {
                if (colorRuntime.renderers[i].material.name.Contains("Base") || colorRuntime.renderers[i].material.name.Contains("Gummy"))
                {
                    colorRuntime._matProperties[i].SetColor("_EmColor", color);
                    colorRuntime.renderers[i].SetPropertyBlock(colorRuntime._matProperties[i]);
                }
            }

            colorRuntime.previousGlowColor = color;
        }
    }
}
