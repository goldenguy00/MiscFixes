using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;

namespace MiscFixes.Modules
{
    public static class PluginConfig
    {
        public static void Init(ConfigFile cfg)
        {
            WipeConfig(cfg);
        }

        private static void WipeConfig(ConfigFile cfg)
        {
            var orphanedEntriesProp = typeof(ConfigFile).GetProperty("OrphanedEntries", ~BindingFlags.Default);

            if (orphanedEntriesProp?.GetValue(cfg) is Dictionary<ConfigDefinition, string> orphanedEntries)
                orphanedEntries.Clear();

            cfg.Save();
        }

        #region Config Binding
        public static ConfigEntry<T> BindOption<T>(this ConfigFile myConfig, string section, string name, T defaultValue, string description = "")
        {
            if (defaultValue is int or float)
                return myConfig.BindOptionSlider(section, name, defaultValue, description);

            if (string.IsNullOrEmpty(description))
                description = name;

            description += $" (Default: {defaultValue})";

            var configEntry = myConfig.Bind(section, name, defaultValue, new ConfigDescription(description, null));

            return configEntry;
        }

        public static ConfigEntry<T> BindOptionSlider<T>(this ConfigFile myConfig, string section, string name, T defaultValue, string description = "", float min = 0, float max = 20)
        {
            if (defaultValue is not int and not float)
                return myConfig.BindOption(section, name, defaultValue, description);

            if (string.IsNullOrEmpty(description))
                description = name;

            description += $" (Default: {defaultValue})";

            AcceptableValueBase range = typeof(T) == typeof(int)
                ? new AcceptableValueRange<int>((int)min, (int)max)
                : new AcceptableValueRange<float>(min, max);

            var configEntry = myConfig.Bind(section, name, defaultValue, new ConfigDescription(description, range));

            return configEntry;
        }
        #endregion
    }
}
