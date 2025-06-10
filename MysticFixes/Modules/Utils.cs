using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using HG.GeneralSerializer;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;

namespace MiscFixes.Modules
{
    internal static class Utils
    {
        internal static void GetModMetaDataSafe(this Assembly assembly, out string guid, out string name)
        {
            guid = "";
            name = "";

            // safer cuz ror2bepinex hook
            Type[] types = assembly.GetTypes();
            for (int i = 0; i < types.Length; i++)
            {
                BepInPlugin customAttribute = types[i].GetCustomAttribute<BepInPlugin>();
                if (customAttribute != null)
                {
                    guid = customAttribute.GUID;
                    guid = customAttribute.Name;
                }
            }
        }

        public static void ReorderSkillDrivers(GameObject master, int targetIdx)
        {
            var c = master.GetComponents<AISkillDriver>();
            ReorderSkillDrivers(master, c, c.Length - 1, targetIdx);
        }

        public static void ReorderSkillDrivers(GameObject master, AISkillDriver targetSkill, int targetIdx)
        {
            var c = master.GetComponents<AISkillDriver>();
            ReorderSkillDrivers(master, c, Array.IndexOf(c, targetSkill), targetIdx);
        }

        public static void ReorderSkillDrivers(GameObject master, AISkillDriver[] skills, int currentIdx, int targetIdx)
        {
            if (currentIdx < 0 || currentIdx >= skills.Length)
            {
                Log.Error($"{currentIdx} index not found or out of range. Must be less than {skills.Length}");
                return;
            }
            var targetName = skills[currentIdx].customName;

            if (targetIdx < 0 || targetIdx >= skills.Length)
            {
                Log.Error($"Unable to reorder skilldriver {targetName} into position {targetIdx}. target must be less than {skills.Length}");
                return;
            }

            if (targetIdx == currentIdx)
            {
                Log.Warning($"Skilldriver {targetName} already has the target index of {targetIdx}");
                return;
            }

            // reference to original might get nulled so they need to be re-added later
            var overrides = skills.Where(s => s.nextHighPriorityOverride != null)
                .ToDictionary(
                s => s.customName,
                s => s.nextHighPriorityOverride.customName);

            // move down. this modifies the order.
            if (targetIdx > currentIdx)
            {
                master.CloneComponent(skills[currentIdx]);
                Component.DestroyImmediate(skills[currentIdx]);
            }

            // anything before the target idx can be ignored.
            // move all elements after the target target skilldriver without modifying order
            for (var i = targetIdx; i < skills.Length; i++)
            {
                if (i != currentIdx)
                {
                    // start with skill that currently occupies target idx
                    master.CloneComponent(skills[i]);
                    Component.DestroyImmediate(skills[i]);
                }
            }

            // sanity check
            skills = master.GetComponents<AISkillDriver>();
            var newTarget = skills.FirstOrDefault(s => s.customName == targetName);
            if (newTarget != null && Array.IndexOf(skills, newTarget) == targetIdx)
                Log.Debug($"Successfully set {targetName} to {targetIdx}");
            else
                Log.Error($"Done fucked it up on {targetName} with {targetIdx}");

            // restore overrides
            if (overrides.Any())
            {
                for (var i = 0; i < skills.Length; i++)
                {
                    var skill = skills[i];
                    if (skill && overrides.TryGetValue(skill.customName, out var target))
                    {
                        var skillComponent = skills.FirstOrDefault(s => s.customName == target);
                        if (skillComponent == null)
                        {
                            Log.Error($"Unable to reset skill override for {skill.customName} targeting {target}");
                        }
                        else
                        {
                            skill.nextHighPriorityOverride = skillComponent;
                            Log.Debug($"successfully reset override for {skill.customName} targeting {target}");
                        }
                    }
                }
            }
        }

        public static void TryRemoveComponent<T>(this GameObject go) where T : MonoBehaviour
        {
            if (go.TryGetComponent<T>(out var component))
            {
                Component.Destroy(component);
            }
        }

        public static void RemoveComponentsOfType<T>(this GameObject go) where T : MonoBehaviour
        {
            var coms = go.GetComponents<T>();
            for (var i = 0; i < coms.Length; i++)
            {
                Component.Destroy(coms[i]);
            }
        }

        public static T CloneFrom<T>(this T obj, T other) where T : UnityEngine.Object
        {
            var type = obj.GetType();
            if (type != other.GetType())
                throw new TypeAccessException($"Type mismatch of {obj?.GetType()} and {other?.GetType()}");

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            var finfos = type.GetFields(flags);
            foreach (var info in finfos)
            {
                if (info.DeclaringType == typeof(UnityEngine.Object) || info.DeclaringType == typeof(UnityEngine.Component))
                    continue;

                try
                {
                    info.SetValue(obj, info.GetValue(other));
                    Log.Debug($"Set field {info.Name} to value of {info.GetValue(obj)}");
                }
                catch (Exception e) { Log.Debug(e); }
            }

            var pinfos = type.GetProperties(flags);
            foreach (var info in pinfos)
            {
                if (info.DeclaringType == typeof(UnityEngine.Object) || info.DeclaringType == typeof(UnityEngine.Component))
                    continue;

                if (!info.CanWrite || info.DeclaringType == typeof(UnityEngine.Object))
                    continue;

                try
                {
                    info.SetValue(obj, info.GetValue(other));
                    Log.Debug($"Set property {info.Name} to value of {info.GetValue(obj)}");
                }
                catch (Exception e) { Log.Debug(e); }   // In case of NotImplementedException being thrown.
                                                        // For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
            }

            return obj;
        }

        public static T CloneComponent<T>(this GameObject go, T src) where T : MonoBehaviour => go.AddComponent<T>().CloneFrom(src);

        public static bool TryModifyFieldValue<T>(this EntityStateConfiguration entityStateConfiguration, string fieldName, T value)
        {
            ref var serializedField = ref entityStateConfiguration.serializedFieldsCollection.GetOrCreateField(fieldName);
            if (serializedField.fieldValue.objectValue && typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)))
            {
                serializedField.fieldValue.objectValue = value as UnityEngine.Object;
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

        public static bool TryGetFieldValue<T>(this EntityStateConfiguration entityStateConfiguration, string fieldName, out T value) where T : UnityEngine.Object
        {
            ref var serializedField = ref entityStateConfiguration.serializedFieldsCollection.GetOrCreateField(fieldName);
            if (serializedField.fieldValue.objectValue && typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)))
            {
                value = (T)serializedField.fieldValue.objectValue;
                return true;
            }
            if (!string.IsNullOrEmpty(serializedField.fieldValue.stringValue))
                Log.Error($"Failed to return {fieldName} as an Object, try getting the string value instead.");
            else
                Log.Error("Field is null " + fieldName);
            value = default;
            return false;
        }

        public static bool TryGetFieldValueString<T>(this EntityStateConfiguration entityStateConfiguration, string fieldName, out T value) where T : IEquatable<T>
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
    }
}
