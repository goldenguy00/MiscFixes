using System.Security.Permissions;
using System.Security;
using BepInEx;
using HarmonyLib;
using BepInEx.Logging;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace MiscFixes
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class MiscFixesPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "_" + PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "score";
        public const string PluginName = "MiscFixes";
        public const string PluginVersion = "1.3.0";

        private Harmony harm;

        internal static new ManualLogSource Logger;

        public void Awake()
        {
            Logger = base.Logger;
            harm = new Harmony(PluginGUID);
            harm.CreateClassProcessor(typeof(SimpleFixes)).Patch();
            harm.CreateClassProcessor(typeof(FixVanilla)).Patch();
            GameFixes.Init();
        }
    }
}
