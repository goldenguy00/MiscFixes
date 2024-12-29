using BelmontMod.Content.Components;
using HarmonyLib;
using MaterialHud;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using BelmontMod.Content.Survivors;
using RoR2.UI;

namespace MiscFixes
{
    [HarmonyPatch]
    public class FixBelmont
    {
        [HarmonyPatch(typeof(HoverGauge), nameof(HoverGauge.Awake))]
        [HarmonyFinalizer]
        public static System.Exception FixAwake(System.Exception __exception) => null;

        [HarmonyPatch(typeof(Belmont), nameof(Belmont.HUDSetup))]
        [HarmonyFinalizer]
        public static System.Exception Hud(System.Exception __exception, RoR2.UI.HUD hud)
        {
            if (__exception != null)
            {
                if (hud.targetBodyObject && hud.targetMaster.bodyPrefab == Belmont.characterPrefab && hud.targetMaster.hasAuthority)
                {
                    if (!hud.healthBar.transform.parent.Find("HoverGauge"))
                    {
                        GameObject hoverGaugeObject = GameObject.Instantiate(hud.expBar.transform.parent.gameObject, hud.healthBar.transform.parent);
                        GameObject.DestroyImmediate(hoverGaugeObject.transform.Find("LevelText").gameObject);
                        MonoBehaviour.DestroyImmediate(hoverGaugeObject.transform.Find("LevelBar").GetComponent<ExpBar>());
                        MonoBehaviour.DestroyImmediate(hoverGaugeObject.GetComponent<LevelText>());

                        hoverGaugeObject.name = "HoverGauge";
                        hoverGaugeObject.transform.Find("PrefixText").GetComponent<TextMeshProUGUI>().text = "Hover";
                        hoverGaugeObject.transform.Find("LevelBar").GetComponent<Image>().enabled = true;
                        hoverGaugeObject.GetComponent<HorizontalLayoutGroup>().spacing = -8;

                        HoverGauge hoverGauge = hoverGaugeObject.AddComponent<HoverGauge>();
                        hoverGauge.rectTransform = hoverGauge.GetComponent<RectTransform>();
                        hoverGauge.fillRectTransform = hoverGauge.transform.Find("LevelBar/SunkenRoot/FillPanel").GetComponent<RectTransform>();
                        hoverGauge.fillBar = hoverGauge.transform.Find("LevelBar/SunkenRoot/FillPanel").GetComponent<Image>();
                        hoverGauge.targetHUD = hud;

                        GameObject holyCrossObject = Object.Instantiate(hud.skillIcons[3].gameObject, hud.skillIcons[3].transform.parent, worldPositionStays: true);
                        GameObject.DestroyImmediate(holyCrossObject.transform.Find("BottomContainer").gameObject);
                        MonoBehaviour.DestroyImmediate(holyCrossObject.GetComponent<MaterialSkillIcon>());
                        MonoBehaviour.DestroyImmediate(holyCrossObject.GetComponent<SkillIcon>());

                        var holyCrossIcon = holyCrossObject.AddComponent<MaterialHolyCrossIcon>();
                        holyCrossIcon.mainTransform = hud.skillIcons[3].GetComponent<RectTransform>();
                        holyCrossIcon.mainPosition = holyCrossIcon.mainTransform.localPosition;
                        holyCrossIcon.targetHUD = hud;
                        hoverGaugeObject.SetActive(false);
                        holyCrossObject.SetActive(false);
                    }
                }
            }
            return null;
        }
    }
}
