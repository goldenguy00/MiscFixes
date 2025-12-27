using BepInEx.Logging;
using MonoMod.Cil;

namespace MiscFixes
{
    internal static class Log
    {
        private static ManualLogSource _logSource;

        internal static void Init(ManualLogSource logSource)
        {
            _logSource = logSource;
        }

        internal static void Debug(object data)
        {
#if DEBUG
            _logSource.LogDebug(data);
#endif
        }
        internal static void Error(object data) => _logSource.LogError(data);
        internal static void Fatal(object data) => _logSource.LogFatal(data);
        internal static void Info(object data) => _logSource.LogInfo(data);
        internal static void Message(object data) => _logSource.LogMessage(data);
        internal static void Warning(object data) => _logSource.LogWarning(data);

        internal static void PatchFail(string method) => _logSource.LogWarning("Failed to patch " + method);
        internal static void PatchFail(ILContext il) => PatchFail(il.Method.Name);
    }
}