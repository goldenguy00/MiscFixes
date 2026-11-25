using System.Collections.Generic;
using HarmonyLib;
using RoR2;
using RoR2.UI;
using TMPro;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // Show assumes a fully set up panel and a fully filled InspectInfo. Panels mods put on their own UI
    // (Starstorm 2's trader and shard panel) break both assumptions:
    // - It reads the user profile through the panel's MPEventSystem before it even looks at
    //   incomingUserProfile, and these panels can have a locator with no event system. Fall back to
    //   the first local user's event system.
    // - It reads .Length on the title/description/flavor tokens, which are null for items that never
    //   set them. Treat those as empty, which Show already replaces with the fallback text.
    // If the panel itself is missing the parts Show writes to, skip showing and report it once.
    [HarmonyPatch(typeof(InspectPanelController), nameof(InspectPanelController.Show), new[] { typeof(InspectInfo), typeof(bool), typeof(UserProfile) })]
    internal static class InspectPanelEventSystemFix
    {
        private static readonly HashSet<int> reportedPanels = new HashSet<int>();

        private static bool Prefix(InspectPanelController __instance, InspectInfo ToShow, ref MPEventSystem ___eventSystem,
            LanguageTextMeshController ___InspectTitle, TMP_Text ___InspectTitleText)
        {
            if (!___eventSystem)
            {
                var locator = __instance.GetComponent<MPEventSystemLocator>();
                ___eventSystem = locator ? locator.eventSystem : null;
                if (!___eventSystem)
                    ___eventSystem = LocalUserManager.GetFirstLocalUser()?.eventSystem;
            }

            if (!___eventSystem || ___eventSystem.localUser == null)
                return false;

            if (!__instance.asyncIconImage || !___InspectTitle || !___InspectTitleText || __instance.MissingInspectDefFallback?.Info == null)
            {
                if (reportedPanels.Add(__instance.GetInstanceID()))
                    Log.Warning($"Inspect panel '{__instance.name}' is missing its icon/title/fallback setup, not showing it.");
                return false;
            }

            if (ToShow != null)
            {
                ToShow.TitleToken ??= "";
                ToShow.DescriptionToken ??= "";
                ToShow.FlavorToken ??= "";
            }
            return true;
        }
    }
}
