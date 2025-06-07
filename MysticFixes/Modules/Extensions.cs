using BepInEx.Configuration;
using System.Collections.Generic;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine.Networking;
using System.Reflection;
using System.Runtime.CompilerServices;
using System;
using System.Text;

namespace MiscFixes.Modules
{
    public static class Extensions
    {
        #region IL
        public static void EmitNetworkServerActive(this ILCursor cursor) => cursor.Emit<NetworkServer>(OpCodes.Call, "get_active");
        public static void EmitOpImplicit(this ILCursor c) => c.Emit<UnityEngine.Object>(OpCodes.Call, "op_Implicit");
        public static bool MatchOpImplicit(this Instruction instr) => instr.MatchCallOrCallvirt<UnityEngine.Object>("op_Implicit");
        public static bool MatchOpInequality(this Instruction instr) => instr.MatchCallOrCallvirt<UnityEngine.Object>("op_Inequality");

        public static bool MatchAny(this Instruction instr, out Instruction instruction)
        {
            instruction = instr;
            return true;
        }
        #endregion

        #region Config Binding

        [Flags]
        public enum ConfigFlags : byte
        {
            None = 0,
            RestartRequired = 1,
            ClientSided = 1 << 1,
            ServerSided = 1 << 2,
        }

        private static readonly StringBuilder _sb = new StringBuilder();
        private static string BuildDescription(string name, string description, string defaultValue, ConfigFlags flags)
        {
            if (string.IsNullOrEmpty(description))
                description = name;

            _sb.Append(description + $" (Default: {defaultValue})");

            if ((flags & ConfigFlags.RestartRequired) != 0)
                _sb.Append(" (Restart Required)");

            if ((flags & ConfigFlags.ClientSided) != 0)
                _sb.Append(" (Client-Sided)");

            if ((flags & ConfigFlags.ServerSided) != 0)
                _sb.Append(" (Server-Sided)");

            return _sb.Take();
        }

        /// <summary>
        /// Erases all unbound config extries from the config file. Call this after all your ConfigEntries are bound!
        /// </summary>
        public static void WipeConfig(this ConfigFile cfg)
        {
            Log.Debug("Clearing config " + System.IO.Path.GetFileName(cfg.ConfigFilePath));
            var orphanedEntriesProp = typeof(ConfigFile).GetProperty("OrphanedEntries", ~BindingFlags.Default);

            if (orphanedEntriesProp?.GetValue(cfg) is Dictionary<ConfigDefinition, string> orphanedEntries)
                orphanedEntries.Clear();

            cfg.Save();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static ConfigEntry<T> BindOption<T>(this ConfigFile myConfig, string section, string name, string description, T defaultValue, ConfigFlags flags = ConfigFlags.None)
        {
            description = BuildDescription(name, description, defaultValue?.ToString(), flags);
            var configEntry = myConfig.Bind(section, name, defaultValue, new ConfigDescription(description));

            try
            {
                if (MiscFixesPlugin.RooInstalled)
                    configEntry.TryRegisterOption((flags & ConfigFlags.RestartRequired) != 0);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

            return configEntry;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static ConfigEntry<T> BindOption<T>(this ConfigFile myConfig, string section, string name, string description, T defaultValue, T[] acceptableValues, ConfigFlags flags = ConfigFlags.None) where T : IEquatable<T>
        {
            description = BuildDescription(name, description, defaultValue?.ToString(), flags);

            AcceptableValueBase valuesList = null;
            if (acceptableValues?.Length > 0)
                valuesList = new AcceptableValueList<T>();

            var configEntry = myConfig.Bind(section, name, defaultValue, new ConfigDescription(description, valuesList));

            try
            {
                if (MiscFixesPlugin.RooInstalled)
                    configEntry.TryRegisterOption((flags & ConfigFlags.RestartRequired) != 0);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

            return configEntry;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static ConfigEntry<T> BindOptionSlider<T>(this ConfigFile myConfig, string section, string name, string description, T defaultValue, ConfigFlags flags = ConfigFlags.None)
        {
            description = BuildDescription(name, description, defaultValue.ToString(), flags);
            var configEntry = myConfig.Bind(section, name, defaultValue, new ConfigDescription(description));

            try
            {
                if (MiscFixesPlugin.RooInstalled)
                    configEntry.TryRegisterOptionSlider((flags & ConfigFlags.RestartRequired) != 0);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

            return configEntry;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static ConfigEntry<T> BindOptionSlider<T>(this ConfigFile myConfig, string section, string name, string description, T defaultValue, T min, T max, ConfigFlags flags = ConfigFlags.None) where T : IComparable
        {
            description = BuildDescription(name, description, defaultValue.ToString(), flags);
            var configEntry = myConfig.Bind(section, name, defaultValue, new ConfigDescription(description, new AcceptableValueRange<T>(min, max)));

            try
            {
                if (MiscFixesPlugin.RooInstalled)
                    configEntry.TryRegisterOptionSlider((flags & ConfigFlags.RestartRequired) != 0);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

            return configEntry;
        }

        /// <summary>
        /// For use with RiskOfOptions. <see cref="BindOptionSlider{T}(ConfigFile, string, string, string, T, T, T, ConfigFlags)"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static ConfigEntry<float> BindOptionSteppedSlider(this ConfigFile myConfig, string section, string name, string description, float defaultValue, float increment, float min = 0, float max = 100, ConfigFlags flags = ConfigFlags.None)
        {
            description = BuildDescription(name, description, defaultValue.ToString(), flags);
            var configEntry = myConfig.Bind(section, name, defaultValue, new ConfigDescription(description, new AcceptableValueRange<float>(min, max)));

            try
            {
                if (MiscFixesPlugin.RooInstalled)
                    configEntry.TryRegisterOptionSteppedSlider(increment, min, max, (flags & ConfigFlags.RestartRequired) != 0);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

            return configEntry;
        }
        #endregion

        #region RoO
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void TryRegisterOption<T>(this ConfigEntry<T> entry, bool restartRequired = false)
        {
            if (entry is ConfigEntry<string> stringEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.StringInputFieldOption(stringEntry, new RiskOfOptions.OptionConfigs.InputFieldConfig
                {
                    lineType = TMPro.TMP_InputField.LineType.SingleLine,
                    submitOn = RiskOfOptions.OptionConfigs.InputFieldConfig.SubmitEnum.OnExitOrSubmit,
                    restartRequired = restartRequired
                }));
            }
            else if (entry is ConfigEntry<bool> boolEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.CheckBoxOption(boolEntry, restartRequired));
            }
            else if (entry is ConfigEntry<KeyboardShortcut> shortCutEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(shortCutEntry, restartRequired));
            }
            else if (typeof(T).IsEnum)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.ChoiceOption(entry, restartRequired));
            }
            else
            {
                Log.Error($"Config entry {entry.Definition.Key} in section {entry.Definition.Section} with type {typeof(T).Name} " +
                    $"could not be registered in Risk Of Options using {nameof(TryRegisterOption)}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void TryRegisterOptionSlider<T>(this ConfigEntry<T> entry, bool restartRequired = false)
        {
            if (entry is ConfigEntry<int> intEntry)
            {
                var config = new RiskOfOptions.OptionConfigs.IntSliderConfig
                {
                    formatString = "{0:0.00}",
                    restartRequired = restartRequired,
                };

                if (entry.Description.AcceptableValues is AcceptableValueRange<int> range)
                {
                    config.min = range.MinValue;
                    config.max = range.MaxValue;
                }

                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.IntSliderOption(intEntry, config));
            }
            else if (entry is ConfigEntry<float> floatEntry)
            {
                var config = new RiskOfOptions.OptionConfigs.SliderConfig
                {
                    FormatString = "{0:0.00}",
                    restartRequired = restartRequired,
                };

                if (entry.Description.AcceptableValues is AcceptableValueRange<float> range)
                {
                    config.min = range.MinValue;
                    config.max = range.MaxValue;
                }

                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.SliderOption(floatEntry, config));
            }
            else
            {
                Log.Error($"Config entry {entry.Definition.Key} in section {entry.Definition.Section} with type {typeof(T).Name} " +
                    $"could not be registered in Risk Of Options using {nameof(TryRegisterOptionSlider)}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void TryRegisterOptionSteppedSlider(this ConfigEntry<float> entry, float increment, float min, float max, bool restartRequired = false)
        {
            if (entry is ConfigEntry<float> floatEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.StepSliderOption(floatEntry, new RiskOfOptions.OptionConfigs.StepSliderConfig
                {
                    increment = increment,
                    min = min,
                    max = max,
                    FormatString = "{0:0.00}",
                    restartRequired = restartRequired,
                }));
            }
            else
            {
                Log.Error($"Config entry {entry.Definition.Key} in section {entry.Definition.Section} with type {typeof(float).Name} " +
                    $"could not be registered in Risk Of Options using {nameof(TryRegisterOptionSteppedSlider)}.");
            }
        }
        #endregion
    }
}
