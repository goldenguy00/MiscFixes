using BelmontMod.Content.Components;
using BelmontMod.Content.Survivors;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiscFixes
{
    public class MaterialHolyCrossIcon : MonoBehaviour
    {
        public Vector3 mainPosition;

        public Vector3 secondaryPosition = new Vector3(290f, -90f, 500f);

        public HUD targetHUD;

        public BelmontController target;

        public RectTransform mainTransform;

        public RectTransform rectTransform;

        public PlayerCharacterMasterController pcmc;

        public GameObject onCooldownPanel;

        public TextMeshProUGUI cooldownText;

        public Image icon;

        public Image mask;

        public void Awake()
        {
            // equip icon can be there, so move to the side if it is
            if (!this.transform.parent.parent.Find("EquipmentSlotPos1").GetChild(0).gameObject.activeSelf)
                secondaryPosition.x = 390f;

            rectTransform = GetComponent<RectTransform>();

            cooldownText = base.transform.Find("CooldownText").GetComponent<TextMeshProUGUI>();
            cooldownText.gameObject.SetActive(true);

            icon = base.transform.Find("IconPanel").gameObject.GetComponent<Image>();
            icon.sprite = Belmont.holyCrossSkillDef.icon;

            onCooldownPanel = icon.transform.Find("OnCooldown").gameObject;

            mask = base.transform.Find("Mask").GetComponent<Image>();
        }

        public void Update()
        {
            if (!target)
            {
                if (!pcmc)
                {
                    pcmc = (targetHUD.targetMaster ? targetHUD.targetMaster.GetComponent<PlayerCharacterMasterController>() : null);
                }

                if ((bool)pcmc && pcmc.master.hasBody)
                {
                    BelmontController belmontCtrl = pcmc.master.GetBodyObject().GetComponent<BelmontController>();
                    if (belmontCtrl)
                    {
                        SetTarget(belmontCtrl);
                    }
                }
            }
            else
            {
                UpdateDisplay();
            }
        }

        public void SetTarget(BelmontController jhhhh)
        {
            target = jhhhh;
        }

        public void UpdateDisplay()
        {
            if (!target.hasHolyCross)
            {
                rectTransform.localPosition = secondaryPosition + new Vector3(2000f, 0f, 0f);
            }
            else if (target.holyCrossCharge >= 180f)
            {
                icon.color = Color.white;
                mask.fillAmount = 0f;

                cooldownText.text = "";

                onCooldownPanel.SetActive(false);

                rectTransform.localPosition = Vector3.Slerp(rectTransform.localPosition, mainPosition, 5f * Time.fixedDeltaTime);
                mainTransform.localPosition = Vector3.Slerp(mainTransform.localPosition, secondaryPosition, 5f * Time.fixedDeltaTime);
            }
            else
            {
                icon.color = new Color(0.5f, 0.5f, 0.5f);
                mask.fillAmount = 1f - Util.Remap(target.holyCrossCharge, 0f, 180f, 0f, 1f);

                cooldownText.text = Mathf.CeilToInt(180f - target.holyCrossCharge).ToString();
                onCooldownPanel.SetActive(true);

                rectTransform.localPosition = Vector3.Slerp(rectTransform.localPosition, secondaryPosition, 5f * Time.fixedDeltaTime);
                mainTransform.localPosition = Vector3.Slerp(mainTransform.localPosition, mainPosition, 5f * Time.fixedDeltaTime);
            }
        }
    }
}
