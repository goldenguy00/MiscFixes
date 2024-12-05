using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using SS2.Components;
using UnityEngine;

namespace MiscFixes
{
    [HarmonyPatch]
    public class FixSS2
    {
        [HarmonyPatch(typeof(NemCaptainController), "OnStressOverlayInstanceAdded")]
        [HarmonyPostfix]
        public static void ChildLocFix(GameObject instance)
        {
            var loc = instance.GetComponent<ChildLocator>();
            loc.transformPairs =
            [
                new()
                {
                    name = "StressThreshold",
                    transform = instance.transform.GetChild(0).GetChild(1).Find("StressThreshold")
                }
            ];
        }
    }
}
