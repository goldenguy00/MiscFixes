using System.Security.Permissions;
using System.Security;
using BepInEx;
using HarmonyLib;

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
        public const string PluginVersion = "1.3.1";

        private Harmony harmonyPatcher;

        public void Awake()
        {
            Log.Init(Logger);
            
            harmonyPatcher = new Harmony(PluginGUID);
            harmonyPatcher.CreateClassProcessor(typeof(SimpleFixes)).Patch();
            harmonyPatcher.CreateClassProcessor(typeof(FixVanilla)).Patch();
            harmonyPatcher.CreateClassProcessor(typeof(ServerCommandsOnClient)).Patch();

            AssetFixes.Init();
        }
    }
}
