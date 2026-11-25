using HarmonyLib;
using UnityEngine;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // EnableCustomRenderTextureOnLight switches its light's cookie texture between realtime and
    // on-demand updates on enable/disable/destroy. The cookie can be unloaded before the light is
    // (scene teardown), and setting updateMode on it then throws. Skip it when the texture is gone.
    [HarmonyPatch(typeof(EnableCustomRenderTextureOnLight), "SetUpdateMode")]
    internal static class CustomRenderTextureLightFix
    {
        private static bool Prefix(CustomRenderTexture ___targetLightCookie)
        {
            return ___targetLightCookie;
        }
    }
}
