using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using RoR2;
using UnityEngine;

namespace MysticFixes
{
    internal class Hooks
    {
        public static Hooks instance;
        public static void Init() => instance ??= new Hooks();

        private Hooks() 
        {
            On.RoR2.PickupCatalog.Init += PickupCatalog_Init;
        }

        private static IEnumerator PickupCatalog_Init(On.RoR2.PickupCatalog.orig_Init orig)
        {
            yield return orig();

            foreach (var equipIndex in EquipmentCatalog.equipmentList)
            {
                var equipDef = EquipmentCatalog.GetEquipmentDef(equipIndex);
                if (equipDef && equipDef.isLunar && equipDef != RoR2Content.Equipment.AffixLunar)
                {
                    var pickupDef = PickupCatalog.GetPickupDef(PickupCatalog.FindPickupIndex(equipDef.equipmentIndex));
                    if (pickupDef != null)
                    {
                        var trail = pickupDef.dropletDisplayPrefab.GetComponentInChildren<TrailRenderer>();
                        trail.startColor = new Color(0.3f, 0.45f, 0.9f, 0f);
                        trail.endColor = new Color(0.2f, 0.3f, 0.9f);
                        pickupDef.baseColor = new Color(0.45f, 0.6f, 0.9f);
                        pickupDef.darkColor = new Color(0.45f, 0.6f, 0.9f);
                    }
                }
            }
        }
    }
}
