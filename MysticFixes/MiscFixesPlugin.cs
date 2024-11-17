using System.Security.Permissions;
using System.Security;
using BepInEx;
using HarmonyLib;
using System.Runtime.CompilerServices;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace MiscFixes
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class MiscFixesPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com." + PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "score";
        public const string PluginName = "MiscFixes";
        public const string PluginVersion = "1.0.6";

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void Awake()
        {
            var harm = new Harmony(PluginGUID);
            harm.CreateClassProcessor(typeof(FixVanilla)).Patch();

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rob.Hunk"))
                Hunk(harm);
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rob.Tyranitar"))
                Tyr(harm);
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.cheesewithholes.TanksMod"))
                Tank(harm);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void Hunk(Harmony harm)
        {
            harm.CreateClassProcessor(typeof(FixHunk)).Patch();
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void Tyr(Harmony harm)
        {
            harm.CreateClassProcessor(typeof(FixRocks)).Patch();
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void Tank(Harmony harm)
        {
            harm.CreateClassProcessor(typeof(FixCheese)).Patch();
        }
    }
}
