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
using MiscFixes.ErrorPolice.Harmony;
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
        public const string PluginGUID = "_" + PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "score";
        public const string PluginName = "MiscFixes";
        public const string PluginVersion = "1.5.3";

        private Harmony harmonyPatcher;
        internal static bool RooInstalled => Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");

        private void Awake()
        {
            Log.Init(Logger);

            AssetFixes.Init();

            // dev note:
            // do not patch all! patch individual classes always!
            // PatchAll will trigger an assembly scan, which will throw when it reads the compat classes!
            harmonyPatcher = new Harmony(PluginGUID);
            harmonyPatcher.CreateClassProcessor(typeof(BackwardsCompat)).Patch();
            //harmonyPatcher.CreateClassProcessor(typeof(ServerCommandsOnClient)).Patch();
            harmonyPatcher.CreateClassProcessor(typeof(VanillaFixes)).Patch();

            AddCompatPatches();
        }


        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private void AddCompatPatches()
        {
            try { PatchStarstorm("0.6.28"); } catch { }
        }
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private void PatchStarstorm(string version)
        {
            var targetVersion = new Version(version);
            var bepinAttribute = typeof(SS2.SS2Main).GetCustomAttribute<BepInPlugin>();

            if (bepinAttribute.Version.Equals(targetVersion))
                harmonyPatcher.CreateClassProcessor(typeof(StarstormFix)).Patch();
        }
    }
}
