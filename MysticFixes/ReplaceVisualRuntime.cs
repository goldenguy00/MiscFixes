using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using R2API.Utils;
using RoR2;
using TanksMod.Modules.Components;
using TanksMod.Modules.Components.BasicTank;
using UnityEngine;

namespace MiscFixes
{
    [HarmonyPatch]
    public class ReplaceVisualRuntime
    {
        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (var method in AccessTools.GetDeclaredMethods(typeof(VisualRuntime)))
            {
                if (method.Name is not "Awake" and not "Update" and not "Start")
                    yield return method;
            }
        }

        [HarmonyPrefix]
        public static bool Prefix(VisualRuntime __instance, MethodBase __originalMethod, object[] __args)
        {
            if (__instance.TryGetComponent<NewVisualRuntime>(out var vr))
            {
                vr.InvokeMethod(__originalMethod.Name, __args);
                return false;
            }

            Debug.LogError("NO VISUAL RUNTIME WHAT THE FUUUUCK");
            return true;
        }
    }
    [HarmonyPatch]
    public class ReplaceColorRuntime
    {
        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (var method in AccessTools.GetDeclaredMethods(typeof(ColorRuntime)))
            {
                if (method.Name is not "Awake" and not "Update" and not "Start")
                    yield return method;
            }
        }

        [HarmonyPrefix]
        public static bool Prefix(ColorRuntime __instance, MethodBase __originalMethod, object[] __args)
        {
            if (__instance.TryGetComponent<NewColorRuntime>(out var vr))
            {
                vr.InvokeMethod(__originalMethod.Name, __args);
                return false;
            }

            Debug.LogError("NO COLOR RUNTIME WHAT THE FUUUUCK");
            return true;
        }
    }

    [HarmonyPatch]
    public class ReplaceRuntime
    {
        [HarmonyPatch(typeof(VisualRuntime), nameof(VisualRuntime.Update))]
        [HarmonyPrefix]
        public static bool NoUpdate() => false;

        [HarmonyPatch(typeof(VisualRuntime), nameof(VisualRuntime.Awake))]
        [HarmonyPostfix]
        public static void AddNewRuntime(VisualRuntime __instance)
        {
            __instance.gameObject.AddComponent<NewVisualRuntime>();
        }

        [HarmonyPatch(typeof(ColorRuntime), nameof(ColorRuntime.Update))]
        [HarmonyPrefix]
        public static bool NoUpdate2() => false;

        [HarmonyPatch(typeof(ColorRuntime), nameof(ColorRuntime.Awake))]
        [HarmonyPostfix]
        public static void AddNewRuntime2(ColorRuntime __instance)
        {
            __instance.gameObject.AddComponent<NewColorRuntime>();
        }
    }
}
