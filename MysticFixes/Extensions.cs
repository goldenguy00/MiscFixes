using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace MiscFixes
{
    public static class Extensions
    {
        internal static void EmitOpImplicit(this ILCursor c) => c.Emit<UnityEngine.Object>(OpCodes.Call, "op_Implicit");
        internal static bool MatchOpImplicit(this Instruction instr) => instr.MatchCallOrCallvirt<UnityEngine.Object>("op_Implicit");
        internal static bool MatchOpInequality(this Instruction instr) => instr.MatchCallOrCallvirt<UnityEngine.Object>("op_Inequality");

        internal static bool MatchAny(this Instruction instr, out Instruction instruction)
        {
            instruction = instr;
            return true;
        }
    }
}
