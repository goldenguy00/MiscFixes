using SYS = System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using HG.GeneralSerializer;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using HG;

namespace MiscFixes.Modules
{
    public static class Extensions
    {
        [SYS.Flags]
        public enum ConfigFlags : byte
        {
            None = 0,
            RestartRequired = 1,
            ClientSided = 1 << 1,
            ServerSided = 1 << 2,
        }

        #region IL
        /// <summary> Emits call to UnityEngine's NetworkServer.active. Places bool on the stack. </summary>
        public static void EmitNetworkServerActive(this ILCursor cursor) => cursor.Emit<NetworkServer>(OpCodes.Call, "get_active");

        /// <summary> Emits call to UnityEngine's Object.op_Implicit, aka the Unity Nullcheck -- if (obj). Consumes Object reference then places bool on the stack.</summary>
        public static void EmitOpImplicit(this ILCursor c) => c.Emit<UnityEngine.Object>(OpCodes.Call, "op_Implicit");

        /// <summary> Matches with UnityEngine's NetworkSever.active </summary>
        public static bool MatchNetworkServerActive(this Instruction instr) => instr.MatchCallOrCallvirt<NetworkServer>("get_active");

        /// <summary> Matches with UnityEngine.Object.op_Implicit, aka the Unity Nullcheck -- if (obj) </summary>
        public static bool MatchOpImplicit(this Instruction instr) => instr.MatchCallOrCallvirt<UnityEngine.Object>("op_Implicit");

        /// <summary>
        /// Match to any arbitrary instruction. Useful for setting up new branches.
        /// </summary>
        /// <param name="instruction">Any instruction</param>
        /// <returns>Always true</returns>
        public static bool MatchAny(this Instruction instr, out Instruction instruction)
        {
            instruction = instr;
            return true;
        }

        public static void EmitNullConditional(this ILCursor c, ILLabel notNullBranch, ILLabel nullBranch)
        {
            c.Emit(OpCodes.Dup);
            c.EmitOpImplicit();
            c.Emit(OpCodes.Brtrue, notNullBranch);
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Br, nullBranch);
        }

        public static void EmitNullConditional(this ILCursor c, Instruction notNullBranch, Instruction nullBranch) => c.EmitNullConditional(c.Context.DefineLabel(notNullBranch), c.Context.DefineLabel(nullBranch));
        public static void EmitNullConditional(this ILCursor c, Instruction notNullBranch, ILLabel nullBranch) => c.EmitNullConditional(c.Context.DefineLabel(notNullBranch), nullBranch);
        #endregion

        #region Config Binding

        #region Internal
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
            
            return _sb.Take().Replace("'", string.Empty);
        }

        private static bool GetModMetaDataSafe(this Assembly assembly, out string guid, out string name)
        {
            guid = "";
            name = "";

            foreach (var type in assembly.GetLoadableTypes())
            {
                BepInPlugin customAttribute = type.GetCustomAttribute<BepInPlugin>();
                if (customAttribute != null)
                {
                    guid = customAttribute.GUID;
                    name = customAttribute.Name;
                    return true;
                }
            }

            Log.Error("Unable to associate config assembly " + assembly.FullName);
            return false;
        }

        // https://haacked.com/archive/2012/07/23/get-all-types-in-an-assembly.aspx/
        public static IEnumerable<SYS.Type> GetLoadableTypes(this Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }
        #endregion

        /// <summary> Use DeleteOrphanedEntries instead </summary>
        [SYS.Obsolete]
        public static void WipeConfig(this ConfigFile cfg) => cfg.DeleteOrphanedEntries();

        /// <summary>
        /// Erases all unbound config extries from the config file. Call this after all your ConfigEntries are bound!
        /// </summary>
        public static void DeleteOrphanedEntries(this ConfigFile cfg)
        {
            var orphanedEntriesProp = typeof(ConfigFile).GetProperty("OrphanedEntries", ~BindingFlags.Default);
            if (orphanedEntriesProp?.GetValue(cfg) is Dictionary<ConfigDefinition, string> orphanedEntries)
            {
                orphanedEntries.Clear();
            }
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
                {
                    Assembly.GetCallingAssembly().GetModMetaDataSafe(out var guid, out var modName);
                    configEntry.TryRegisterOption((flags & ConfigFlags.RestartRequired) != 0, guid, modName);
                }
            }
            catch (SYS.Exception e) { Log.Error(e); }

            return configEntry;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static ConfigEntry<T> BindOption<T>(this ConfigFile myConfig, string section, string name, string description, T defaultValue, T[] acceptableValues, ConfigFlags flags = ConfigFlags.None) where T : SYS.IEquatable<T>
        {
            description = BuildDescription(name, description, defaultValue?.ToString(), flags);

            AcceptableValueBase valuesList = null;
            if (acceptableValues?.Length > 0)
                valuesList = new AcceptableValueList<T>();

            var configEntry = myConfig.Bind(section, name, defaultValue, new ConfigDescription(description, valuesList));

            try
            {
                if (MiscFixesPlugin.RooInstalled)
                {
                    Assembly.GetCallingAssembly().GetModMetaDataSafe(out var guid, out var modName);
                    configEntry.TryRegisterOption((flags & ConfigFlags.RestartRequired) != 0, guid, modName);
                }
            }
            catch (SYS.Exception e) { Log.Error(e); }

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
                {
                    Assembly.GetCallingAssembly().GetModMetaDataSafe(out var guid, out var modName);
                    configEntry.TryRegisterOptionSlider((flags & ConfigFlags.RestartRequired) != 0, guid, modName);
                }
            }
            catch (SYS.Exception e) { Log.Error(e); }

            return configEntry;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static ConfigEntry<T> BindOptionSlider<T>(this ConfigFile myConfig, string section, string name, string description, T defaultValue, T min, T max, ConfigFlags flags = ConfigFlags.None) where T : SYS.IComparable
        {
            description = BuildDescription(name, description, defaultValue.ToString(), flags);
            var configEntry = myConfig.Bind(section, name, defaultValue, new ConfigDescription(description, new AcceptableValueRange<T>(min, max)));

            try
            {
                if (MiscFixesPlugin.RooInstalled)
                {
                    Assembly.GetCallingAssembly().GetModMetaDataSafe(out var guid, out var modName);
                    configEntry.TryRegisterOptionSlider((flags & ConfigFlags.RestartRequired) != 0, guid, modName);
                }
            }
            catch (SYS.Exception e) { Log.Error(e); }

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
                {
                    Assembly.GetCallingAssembly().GetModMetaDataSafe(out var guid, out var modName);
                    configEntry.TryRegisterOptionSteppedSlider(increment, min, max, (flags & ConfigFlags.RestartRequired) != 0, guid, modName);
                }
            }
            catch (SYS.Exception e) { Log.Error(e); }

            return configEntry;
        }
        #endregion

        #region RoO
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void TryRegisterOption<T>(this ConfigEntry<T> entry, bool restartRequired = false, string modGuid = null, string modName = null)
        {
            if (modGuid is null && modName is null)
                Assembly.GetCallingAssembly().GetModMetaDataSafe(out modGuid, out modName);

            if (entry is ConfigEntry<string> stringEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.StringInputFieldOption(stringEntry, new RiskOfOptions.OptionConfigs.InputFieldConfig
                {
                    lineType = TMPro.TMP_InputField.LineType.MultiLineNewline,
                    submitOn = RiskOfOptions.OptionConfigs.InputFieldConfig.SubmitEnum.OnExitOrSubmit,
                    restartRequired = restartRequired
                }), modGuid, modName);
            }
            else if (entry is ConfigEntry<bool> boolEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.CheckBoxOption(boolEntry, restartRequired), modGuid, modName);
            }
            else if (entry is ConfigEntry<KeyboardShortcut> shortCutEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(shortCutEntry, restartRequired), modGuid, modName);
            }
            else if (typeof(T).IsEnum)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.ChoiceOption(entry, restartRequired), modGuid, modName);
            }
            else
            {
                Log.Error($"Config entry {entry.Definition.Key} in section {entry.Definition.Section} with type {typeof(T).Name} " +
                    $"could not be registered in Risk Of Options using {nameof(TryRegisterOption)}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void TryRegisterOptionSlider<T>(this ConfigEntry<T> entry, bool restartRequired = false, string modGuid = null, string modName = null)
        {
            if (modGuid is null && modName is null)
                Assembly.GetCallingAssembly().GetModMetaDataSafe(out modGuid, out modName);

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

                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.IntSliderOption(intEntry, config), modGuid, modName);
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

                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.SliderOption(floatEntry, config), modGuid, modName);
            }
            else
            {
                Log.Error($"Config entry {entry.Definition.Key} in section {entry.Definition.Section} with type {typeof(T).Name} " +
                    $"could not be registered in Risk Of Options using {nameof(TryRegisterOptionSlider)}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void TryRegisterOptionSteppedSlider(this ConfigEntry<float> entry, float increment, float min, float max, bool restartRequired = false, string modGuid = null, string modName = null)
        {
            if (entry is ConfigEntry<float> floatEntry)
            {
                if (modGuid is null && modName is null)
                    Assembly.GetCallingAssembly().GetModMetaDataSafe(out modGuid, out modName);

                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.StepSliderOption(floatEntry, new RiskOfOptions.OptionConfigs.StepSliderConfig
                {
                    increment = increment,
                    min = min,
                    max = max,
                    FormatString = "{0:0.00}",
                    restartRequired = restartRequired,
                }), modGuid, modName);
            }
            else
            {
                Log.Error($"Config entry {entry.Definition.Key} in section {entry.Definition.Section} with type {typeof(float).Name} " +
                    $"could not be registered in Risk Of Options using {nameof(TryRegisterOptionSteppedSlider)}.");
            }
        }
        #endregion

        #region Unity Objects

        /// <summary> Use HG.EnsureComponent instead</summary>
        public static T GetOrAddComponent<T>(this GameObject obj) where T : Component => obj.EnsureComponent<T>();

        /// <summary> Use HG.EnsureComponent instead</summary>
        public static T GetOrAddComponent<T>(this Component obj) where T : Component => obj.EnsureComponent<T>();

        public static void TryDestroyComponent<T>(this GameObject obj) where T : Component
        {
            if (obj.TryGetComponent<T>(out var component))
                Object.Destroy(component);
        }

        public static void TryDestroyComponent<T>(this Component obj) where T : Component
        {
            if (obj.TryGetComponent<T>(out var component))
                Object.Destroy(component);
        }

        public static void TryDestroyAllComponents<T>(this GameObject obj) where T : Component
        {
            var components = obj.GetComponents<T>();
            for (var i = components.Length - 1; i >= 0; i--)
                Object.Destroy(components[i]);
        }

        public static void TryDestroyAllComponents<T>(this Component obj) where T : Component
        {
            var components = obj.GetComponents<T>();
            for (var i = components.Length - 1; i >= 0; i--)
                Object.Destroy(components[i]);
        }

        public static T CloneComponent<T>(this GameObject go, T src) where T : Component => go.AddComponent<T>().CloneFrom(src);

        public static T CloneFrom<T>(this T obj, T other) where T : Object
        {
            var type = obj.GetType();
            if (type != other.GetType())
                throw new SYS.TypeAccessException($"Type mismatch of {obj?.GetType()} and {other?.GetType()}");

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            var finfos = type.GetFields(flags);
            foreach (var info in finfos)
            {
                // ignore stuff like name, transform, etc
                if (info.DeclaringType == typeof(Object) || info.DeclaringType == typeof(Component))
                    continue;

                try
                {
                    info.SetValue(obj, info.GetValue(other));
                    Log.Info($"Set field {info.Name} to value of {info.GetValue(obj)}");
                }
                catch (SYS.Exception e) { Log.Warning(e); }
            }

            var pinfos = type.GetProperties(flags);
            foreach (var info in pinfos)
            {
                // ignore stuff like name, transform, etc
                if (info.DeclaringType == typeof(Object) || info.DeclaringType == typeof(Component))
                    continue;

                if (!info.CanWrite || info.DeclaringType == typeof(Object))
                    continue;

                try
                {
                    info.SetValue(obj, info.GetValue(other));
                    Log.Info($"Set property {info.Name} to value of {info.GetValue(obj)}");
                }
                catch (SYS.Exception e) { Log.Warning(e); }   // In case of NotImplementedException being thrown.
                                                               // For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
            }

            return obj;
        }

        public static GameObject ResolveObjectInNewRoot(this GameObject obj, Transform originalRoot, Transform newRoot)
        {
            if (!obj)
                return null;

            string objectPath = Util.BuildPrefabTransformPath(originalRoot, obj.transform);
            if (string.IsNullOrEmpty(objectPath))
                return null;

            Transform newObjectTransform = newRoot.Find(objectPath);
            if (!newObjectTransform)
                return null;

            return newObjectTransform.gameObject;
        }

        public static T ResolveComponentInNewRoot<T>(this T component, Transform originalRoot, Transform newRoot) where T : Component
        {
            if (!component)
                return null;

            GameObject newComponentObject = component.gameObject.ResolveObjectInNewRoot(originalRoot, newRoot);
            if (!newComponentObject)
                return null;

            // Intentionally not using GetComponent<T> since it's not guaranteed to be the full type of the component.
            // eg. if this is called with T=Renderer but component is a SkinnedMeshRenderer,
            // we need to make sure we still return the right component type
            return newComponentObject.GetComponent(component.GetType()) as T;
        }
        #endregion

        #region ESConfigs
        public static bool TryModifyFieldValue<T>(this EntityStateConfiguration entityStateConfiguration, string fieldName, T value)
        {
            ref var serializedField = ref entityStateConfiguration.serializedFieldsCollection.GetOrCreateField(fieldName);
            if (serializedField.fieldValue.objectValue && typeof(Object).IsAssignableFrom(typeof(T)))
            {
                serializedField.fieldValue.objectValue = value as Object;
                return true;
            }
            else if (serializedField.fieldValue.stringValue != null && StringSerializer.CanSerializeType(typeof(T)))
            {
                serializedField.fieldValue.stringValue = StringSerializer.Serialize(typeof(T), value);
                return true;
            }

            Log.Error("Failed to modify field " + fieldName);
            return false;
        }

        public static bool TryGetFieldValue<T>(this EntityStateConfiguration entityStateConfiguration, string fieldName, out T value) where T : Object
        {
            ref var serializedField = ref entityStateConfiguration.serializedFieldsCollection.GetOrCreateField(fieldName);
            if (serializedField.fieldValue.objectValue && typeof(Object).IsAssignableFrom(typeof(T)))
            {
                value = serializedField.fieldValue.objectValue as T;
                return true;
            }

            if (!string.IsNullOrEmpty(serializedField.fieldValue.stringValue))
                Log.Error($"Failed to return {fieldName} as an Object, try getting the string value instead.");
            else
                Log.Error("Field is null " + fieldName);

            value = default;
            return false;
        }

        public static bool TryGetFieldValueString<T>(this EntityStateConfiguration entityStateConfiguration, string fieldName, out T value) where T : SYS.IEquatable<T>
        {
            ref var serializedField = ref entityStateConfiguration.serializedFieldsCollection.GetOrCreateField(fieldName);
            if (serializedField.fieldValue.stringValue != null && StringSerializer.CanSerializeType(typeof(T)))
            {
                value = (T)StringSerializer.Deserialize(typeof(T), serializedField.fieldValue.stringValue);
                return true;
            }

            if (serializedField.fieldValue.objectValue)
                Log.Error($"Failed to return {fieldName} as a string, try getting the Object value instead.");
            else
                Log.Error("Field is null " + fieldName);

            value = default;
            return false;
        }

        public static void PrintAllSerializedValues(this EntityStateConfiguration esc)
        {
            Log.Info("Printing: " + esc.targetType.assemblyQualifiedName);
            for (var i = 0; i < esc.serializedFieldsCollection.serializedFields.Length; i++)
            {
                ref var serializedField = ref esc.serializedFieldsCollection.serializedFields[i];
                
                var fieldName = serializedField.fieldName;
                var fieldType = "Unknown";
                var fieldValue = "<Null>";

                if (serializedField.fieldValue.objectValue)
                {
                    fieldType = nameof(Object);
                    fieldValue = serializedField.fieldValue.objectValue.name;
                }
                else if (serializedField.fieldValue.stringValue is not null)
                {
                    fieldType = nameof(SYS.String);
                    fieldValue = serializedField.fieldValue.stringValue;
                }

                Log.Info(string.Format("\tName: {0}\t|\t Field Type: {1}" + SYS.Environment.NewLine
                    + "{2}", fieldName, fieldType, fieldValue));
            }
        }
        #endregion
    }
}
