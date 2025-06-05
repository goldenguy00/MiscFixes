using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils;
using RoR2;

namespace MiscPatches
{
    public static class MiscPatches
    {
        public static IEnumerable<string> TargetDLLs
        {
            get
            {
                yield return "RoR2.dll";
            }
        }

        public static void Patch(AssemblyDefinition assembly)
        {
            TypeDefinition skinDef = assembly.MainModule.GetType("RoR2", nameof(SkinDef));
            var bakeAsyncMethod = skinDef.Methods.First(m => m.Name is nameof(SkinDef.BakeAsync));

            var enumeratorTypeRef = assembly.MainModule.ImportReference(typeof(IEnumerator));
            var moveNextMethod = enumeratorTypeRef.Module.GetMemberReferences().First(m => m.Name is nameof(IEnumerator.MoveNext));

            var bakeMethod = new MethodDefinition("Bake", MethodAttributes.Public, assembly.MainModule.ImportReference(typeof(void)));
            var ilProcessor = bakeMethod.Body.GetILProcessor();
            bakeMethod.Body.Variables.Add(new VariableDefinition(enumeratorTypeRef));
            	
             /*	IL_0000: ldarg.0
	            IL_0001: callvirt instance class [netstandard]System.Collections.IEnumerator [RoR2]RoR2.SkinDef::BakeAsync()
	            IL_0006: stloc.0
		        IL_0007: ldloc.0
		        IL_0008: callvirt instance bool [netstandard]System.Collections.IEnumerator::MoveNext()
		        IL_000d: brtrue.s IL_0007
	            IL_000f: ret
            */
            ilProcessor.Emit(OpCodes.Ldarg_0);
            ilProcessor.Emit(OpCodes.Callvirt, bakeAsyncMethod);
            ilProcessor.Emit(OpCodes.Stloc_0);

            var instr = ilProcessor.Create(OpCodes.Ldloc_0);
            ilProcessor.Append(instr);
            ilProcessor.Emit(OpCodes.Callvirt, moveNextMethod);
            ilProcessor.Emit(OpCodes.Brtrue, instr);
            ilProcessor.Emit(OpCodes.Ret);

            skinDef.Methods.Add(bakeMethod);
        }
    }
}
