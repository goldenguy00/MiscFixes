using System;
using System.Reflection;
using BepInEx;

namespace MiscFixes.Modules
{
    internal static class Utils
    {
        internal static void GetModMetaDataSafe(this Assembly assembly, out string guid, out string name)
        {
            guid = "";
            name = "";

            try
            {
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
            catch (Exception e)
            {
                Log.Warning(e);
            }
        }
    }
}
