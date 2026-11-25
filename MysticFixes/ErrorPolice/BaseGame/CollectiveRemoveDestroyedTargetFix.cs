using System.Collections.Generic;
using HarmonyLib;
using RoR2;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // Collective elites remove their share buff from bodies that leave their range, including bodies
    // that were just destroyed. Setting a buff on a destroyed body runs every mod's
    // OnClientBuffsChanged hook on it, and those throw (e.g. Classic Items Returns' Hit List). A
    // destroyed body has no buff to remove, so just drop it from the member list.
    [HarmonyPatch(typeof(AffixCollectiveAttachmentBehaviour), "RemoveTarget")]
    internal static class CollectiveRemoveDestroyedTargetFix
    {
        private static bool Prefix(CharacterBody body, List<CharacterBody> ____collectiveMembersIncludingSelf)
        {
            if (body)
                return true;

            ____collectiveMembersIncludingSelf?.Remove(body);
            return false;
        }
    }
}
