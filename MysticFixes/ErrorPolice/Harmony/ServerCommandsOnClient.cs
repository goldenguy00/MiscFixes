using EntityStates;
using HarmonyLib;
using MiscFixes.Modules;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;

namespace MiscFixes.ErrorPolice.Harmony
{
    /// <summary>
    /// A collection of patches that skip Server method calls on a client preventing log spam.
    /// </summary>
    [HarmonyPatch]
    public class ServerCommandsOnClient
    {
    }
}
