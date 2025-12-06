using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using SEC = System.Security;

//Allows you to access private methods/fields/etc from the stubbed Assembly-CSharp that is included.

[module: SEC.UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SEC.Permissions.SecurityPermission(SEC.Permissions.SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace MiscPatcher
{
    public static class MiscPatcher
    {
        public static IEnumerable<string> TargetDLLs
        {
            get
            {
                yield return "RoR2.dll";
            }
        }

        private const string RoR2 = "RoR2", RoR2UI = "RoR2.UI", RoR2Projectile = "RoR2.Projectile", RoR2Skills = "RoR2.Skills";
        private static ModuleDefinition ror2;

        public static void Patch(AssemblyDefinition assemblyDef)
        {
            if (assemblyDef?.Name?.Name != "RoR2")
                return;

            ror2 = assemblyDef.MainModule;

            PatchMissingFields();

            ror2 = null;
        }

        private static void PatchMissingFields()
        {

            void AddField(TypeDefinition typeDef, TypeReference fieldTypeRef, string fieldName, FieldAttributes attr)
            {
                if (typeDef.Fields.Any(f => f.Name == fieldName && f.FieldType.Name == fieldTypeRef.Name))
                {
                    Logger.Warn($"{fieldName} already exists in {typeDef.FullName}");
                    return;
                }

                typeDef.Fields.Add(new FieldDefinition(fieldName, attr, fieldTypeRef));
                Logger.Debug("Added " + typeDef.Fields.Last().FullName);
            }
        }
        internal static class Logger
        {
            private static readonly ManualLogSource logSource = BepInEx.Logging.Logger.CreateLogSource("SeekersPatcher");

            public static void Info(object data) => logSource.LogInfo(data);

            public static void Error(object data) => logSource.LogError(data);

            public static void Warn(object data) => logSource.LogWarning(data);

            public static void Fatal(object data) => logSource.LogFatal(data);

            public static void Message(object data) => logSource.LogMessage(data);

            public static void Debug(object data) => logSource.LogDebug(data);
        }
    }
}