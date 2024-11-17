using System.IO;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;

namespace MiscFixes
{
    internal static class PluginConfig
    {
        public static ConfigFile myConfig;

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void Init()
        {
            myConfig = new(Path.Combine(Paths.ConfigPath, MiscFixesPlugin.PluginGUID + ".cfg"), true);

            if (MiscFixesPlugin.RooInstalled)
                InitRoO();
        }

        #region Config Binding
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static ConfigEntry<T> BindOption<T>(string section, string name, T defaultValue, string description = "", bool restartRequired = false)
        {
            if (string.IsNullOrEmpty(description))
                description = name;

            if (restartRequired)
                description += " (restart required)";

            var configEntry = myConfig.Bind(section, name, defaultValue, description);

            if (MiscFixesPlugin.RooInstalled)
                TryRegisterOption(configEntry, restartRequired);

            return configEntry;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static ConfigEntry<T> BindOptionSlider<T>(string section, string name, T defaultValue, string description = "", float min = 0, float max = 20, bool restartRequired = false)
        {
            if (string.IsNullOrEmpty(description))
                description = name;

            description += " (Default: " + defaultValue + ")";

            if (restartRequired)
                description += " (restart required)";

            var configEntry = myConfig.Bind(section, name, defaultValue, description);

            if (MiscFixesPlugin.RooInstalled)
                TryRegisterOptionSlider(configEntry, min, max, restartRequired);

            return configEntry;
        }
        #endregion

        #region RoO
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void InitRoO()
        {
            RiskOfOptions.ModSettingsManager.SetModDescription("Mutes annoying logs.");
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void TryRegisterOption<T>(ConfigEntry<T> entry, bool restartRequired)
        {
            if (entry is ConfigEntry<string> stringEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.StringInputFieldOption(stringEntry, restartRequired));
                return;
            }
            if (entry is ConfigEntry<float> floatEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.SliderOption(floatEntry, new RiskOfOptions.OptionConfigs.SliderConfig()
                {
                    min = 0,
                    max = 20,
                    FormatString = "{0:0.00}",
                    restartRequired = restartRequired
                }));
                return;
            }
            if (entry is ConfigEntry<int> intEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.IntSliderOption(intEntry, restartRequired));
                return;
            }
            if (entry is ConfigEntry<bool> boolEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.CheckBoxOption(boolEntry, restartRequired));
                return;
            }
            if (entry is ConfigEntry<KeyboardShortcut> shortCutEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(shortCutEntry, restartRequired));
                return;
            }
            if (typeof(T).IsEnum)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.ChoiceOption(entry, restartRequired));
                return;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void TryRegisterOptionSlider<T>(ConfigEntry<T> entry, float min, float max, bool restartRequired)
        {
            if (entry is ConfigEntry<int> intEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.IntSliderOption(intEntry, new RiskOfOptions.OptionConfigs.IntSliderConfig()
                {
                    min = (int)min,
                    max = (int)max,
                    formatString = "{0:0.00}",
                    restartRequired = restartRequired
                }));
                return;
            }

            if (entry is ConfigEntry<float> floatEntry)
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.SliderOption(floatEntry, new RiskOfOptions.OptionConfigs.SliderConfig()
                {
                    min = min,
                    max = max,
                    FormatString = "{0:0.00}",
                    restartRequired = restartRequired
                }));
        }
        #endregion
    }
}
