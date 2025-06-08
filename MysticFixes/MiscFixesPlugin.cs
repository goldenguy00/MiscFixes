using System.Security.Permissions;
using System.Security;
using BepInEx;
using HarmonyLib;
using MiscFixes.Modules;
using MiscFixes.Fixes;
using MiscFixes.Fixes.Compat;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System;
using BepInEx.Bootstrap;
using MiscFixes.Fixes.ErrorPolice;

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
        public const string PluginGUID = "_" + PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "score";
        public const string PluginName = "MiscFixes";
        public const string PluginVersion = "1.4.0";

        private Harmony harmonyPatcher;
        internal static bool RooInstalled => Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");

        private void Awake()
        {
            Log.Init(Logger);

            AssetFixes.Init();

            harmonyPatcher = new Harmony(PluginGUID);
            harmonyPatcher.CreateClassProcessor(typeof(FixVanilla)).Patch();
            harmonyPatcher.CreateClassProcessor(typeof(ServerCommandsOnClient)).Patch();
            harmonyPatcher.CreateClassProcessor(typeof(SimpleFixes)).Patch();
            harmonyPatcher.CreateClassProcessor(typeof(SkinFixes)).Patch();

            AddCompatPatches();
        }

        private void OnDestroy()
        {
            harmonyPatcher?.UnpatchSelf();
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private void AddCompatPatches()
        {
            try { PatchStarstorm("0.6.20"); } catch { }
            try { PatchLobbySkins("1.2.1"); } catch { }
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private void PatchStarstorm(string version)
        {
            var targetVersion = new Version(version);
            var bepinAttribute = typeof(SS2.SS2Main).GetCustomAttribute<BepInPlugin>();

            if (bepinAttribute.Version.Equals(targetVersion))
                harmonyPatcher.CreateClassProcessor(typeof(StarstormFix)).Patch();
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private void PatchLobbySkins(string version)
        {
            var targetVersion = new Version(version);
            var bepinAttribute = typeof(LobbySkinsFix.LobbySkinsFixPlugin).GetCustomAttribute<BepInPlugin>();

            if (bepinAttribute.Version.Equals(targetVersion))
                harmonyPatcher.CreateClassProcessor(typeof(UnfuckLobbySkins)).Patch();
        }
    }
}
