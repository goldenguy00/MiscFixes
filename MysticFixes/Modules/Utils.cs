using System.Reflection;
using BepInEx;
using RoR2.ContentManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MiscFixes.Modules
{
    internal static class Utils
    {
        internal static AsyncOperationHandle<T> PreloadAsset<T>(AssetReferenceT<T> reference) where T : Object
        {
            return AssetAsyncReferenceManager<T>.LoadAsset(reference, AsyncReferenceHandleUnloadType.Preload);
        }

        internal static void UnloadAsset<T>(AssetReferenceT<T> reference) where T : Object
        {
            AssetAsyncReferenceManager<T>.UnloadAsset(reference);
        }

        internal static void GetModMetaDataSafe(this Assembly assembly, out string guid, out string name)
        {
            guid = "";
            name = "";

            // safer cuz ror2bepinex hook
            var types = assembly.GetTypes();
            for (int i = 0; i < types.Length; i++)
            {
                BepInPlugin customAttribute = types[i].GetCustomAttribute<BepInPlugin>();
                if (customAttribute != null)
                {
                    guid = customAttribute.GUID;
                    name = customAttribute.Name;
                }
            }
        }
    }
}
