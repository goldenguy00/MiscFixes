using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using MiscFixes.ErrorPolice;
using MiscFixes.Modules;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

[assembly: SuppressMessage("CodeQuality", "IDE0051:Remove unused private members")]

namespace MiscFixes
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class MiscFixesPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = $"_{PluginAuthor}.{PluginName}";
        public const string PluginAuthor = "score";
        public const string PluginName = "MiscFixes";
        public const string PluginVersion = "1.5.6";

        internal static bool RooInstalled => Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");

        private Harmony harmonyPatcher;

        private void Awake()
        {
            Log.Init(Logger);

            FixAssets.Init();

            // dev note:
            // do not patch all! patch individual classes always!
            // PatchAll will trigger an assembly scan, which will throw when it reads the compat classes!
            harmonyPatcher = new Harmony(PluginGUID);
            TryHarmonyPatch<FixGameplay>();
            TryHarmonyPatch<FixEventSystem>();
            TryHarmonyPatch<FixNullRefs>();
            TryHarmonyPatch<FixParticleScale>();
            TryHarmonyPatch<FixTempOverlay>();
        }

        private void TryHarmonyPatch<T>()
        {
            try { harmonyPatcher.CreateClassProcessor(typeof(T)).Patch(); } catch (Exception e) { Log.Error(e); }
        }

    }
}
