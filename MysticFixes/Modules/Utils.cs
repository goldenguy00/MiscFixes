using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Bootstrap;
using RoR2.ContentManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using SYS = System;

namespace MiscFixes.Modules
{
    internal static class Utils
    {
        #region Assets
        internal static AsyncOperationHandle<T> PreloadAsset<T>(AssetReferenceT<T> reference) where T : Object
        {
            return AssetAsyncReferenceManager<T>.LoadAsset(reference, AsyncReferenceHandleUnloadType.Preload);
        }

        internal static void UnloadAsset<T>(AssetReferenceT<T> reference) where T : Object
        {
            AssetAsyncReferenceManager<T>.UnloadAsset(reference);
        }
        #endregion

        #region Config
        private static readonly List<string> _invalidConfigChars = ["=", "[", "]", "\n", "\t", "\\", "\'", "\""];

        private static void CheckInvalidConfigChars(ref string item)
        {
            if (item == null)
                throw new SYS.ArgumentNullException("ConfigEntry Name and Section cannot be null");

            item = item.Trim();
            foreach (var invalidC in _invalidConfigChars)
            {
                item = item.Replace(invalidC, string.Empty);
            }
        }

        internal static void BuildValidConfigEntry(ref string section, ref string name, ref string description, string defaultValue, Extensions.ConfigFlags flags)
        {
            CheckInvalidConfigChars(ref section);
            CheckInvalidConfigChars(ref name);

            if (string.IsNullOrEmpty(description))
                description = name;

            description += $" (Default: {defaultValue})";

            if ((flags & Extensions.ConfigFlags.RestartRequired) != 0)
                description += " (Restart Required)";

            if ((flags & Extensions.ConfigFlags.SyncRequired) != 0)
            {
                description += " (Server-Client Sync Required)";
            }
            else
            {
                if ((flags & Extensions.ConfigFlags.ClientSided) != 0)
                    description += " (Client-Sided)";

                if ((flags & Extensions.ConfigFlags.ServerSided) != 0)
                    description += " (Server-Sided)";
            }
        }

        internal static void GetModMetaDataSafe(Assembly assembly, out string guid, out string name)
        {
            guid = "";
            name = "";

            var types = assembly.GetExportedTypes();
            for (int i = 0; i < types.Length; i++)
            {
                BepInPlugin customAttribute = types[i]?.GetCustomAttribute<BepInPlugin>();
                if (customAttribute != null)
                {
                    guid = customAttribute.GUID;
                    name = customAttribute.Name;

                    if (MiscFixesPlugin.RooInstalled)
                        InitRoO(guid, name);

                    return;
                }
            }

            Log.Error($"No BepInPlugin attribute could be found for {assembly.FullName}");
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static void InitRoO(string guid, string name)
        {
            if (RiskOfOptions.ModSettingsManager.OptionCollection.ContainsModGuid(guid))
                return;

            if (!Chainloader.PluginInfos.TryGetValue(guid, out var pluginInfo))
                return;

            string directory = Path.GetDirectoryName(pluginInfo.Location);

            LoadIcon(directory, guid, name);
            SetDescription(directory, guid, name);
        }

        internal static void LoadIcon(string directory, string guid, string name)
        {
            if (!FindPath(directory, "icon.png", out string path))
                return;

            var texture = new Texture2D(256, 256, TextureFormat.ARGB32, mipCount: 3, linear: false);

            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(path)))
                    throw new SYS.Exception();
            }
            catch (SYS.Exception e)
            {
                Log.Error("Unable to load '" + path + "'\n");
                Log.Error(e);
                return;
            }

            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            RiskOfOptions.ModSettingsManager.SetModIcon(sprite, guid, name);
        }

        internal static void SetDescription(string directory, string guid, string name)
        {
            if (!FindPath(directory, "manifest.json", out string path))
                return;

            string description = "description";

            try
            {
                description = SimpleJSON.JSON.Parse(File.ReadAllText(path))[description];
            }
            catch (SYS.Exception e)
            {
                Log.Error("Unable to load '" + path + "'\n");
                Log.Error(e);
                return;
            }

            RiskOfOptions.ModSettingsManager.SetModDescription(description, guid, name);
        }

        internal static bool FindPath(string directory, string filename, out string path)
        {
            path = Path.Combine(directory, filename);

            if (!File.Exists(path))
                path = Path.Combine(Directory.GetParent(directory).FullName, filename);

            return File.Exists(path);
        }
        #endregion
    }
}
