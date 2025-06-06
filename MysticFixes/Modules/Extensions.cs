using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine.Networking;

namespace MiscFixes.Modules
{
    public static class Extensions
    {
        public static void EmitNetworkServerActive(this ILCursor cursor) => cursor.Emit<NetworkServer>(OpCodes.Call, "get_active");
        public static void EmitOpImplicit(this ILCursor c) => c.Emit<UnityEngine.Object>(OpCodes.Call, "op_Implicit");
        public static bool MatchOpImplicit(this Instruction instr) => instr.MatchCallOrCallvirt<UnityEngine.Object>("op_Implicit");
        public static bool MatchOpInequality(this Instruction instr) => instr.MatchCallOrCallvirt<UnityEngine.Object>("op_Inequality");

        public static bool MatchAny(this Instruction instr, out Instruction instruction)
        {
            instruction = instr;
            return true;
        }
    }
}
