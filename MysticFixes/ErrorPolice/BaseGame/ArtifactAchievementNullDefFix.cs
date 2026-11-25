using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RoR2;
using RoR2.Achievements.Artifacts;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // OnInstall reads artifactDef.unlockableDef straight away. Mods register "obtain artifact"
    // achievements unconditionally, but when the artifact itself is disabled in their config its
    // def is null (or the getter throws, like Expedition's Destination). That breaks achievement
    // setup for the whole profile on load. Treat it as having no unlockable, which OnInstall
    // already handles; the achievement just can't be granted until the artifact exists.
    [HarmonyPatch(typeof(BaseObtainArtifactAchievement), nameof(BaseObtainArtifactAchievement.OnInstall))]
    internal static class ArtifactAchievementNullDefFix
    {
        private static readonly Func<BaseObtainArtifactAchievement, ArtifactDef> getArtifactDef =
            AccessTools.MethodDelegate<Func<BaseObtainArtifactAchievement, ArtifactDef>>(
                AccessTools.PropertyGetter(typeof(BaseObtainArtifactAchievement), "artifactDef"));

        private static readonly HashSet<Type> reportedAchievements = new HashSet<Type>();

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var artifactDefGetter = AccessTools.PropertyGetter(typeof(BaseObtainArtifactAchievement), "artifactDef");
            var unlockableField = AccessTools.Field(typeof(ArtifactDef), nameof(ArtifactDef.unlockableDef));

            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count - 1; i++)
            {
                if (!codes[i].Calls(artifactDefGetter) || !codes[i + 1].LoadsField(unlockableField))
                    continue;

                // Stack holds the achievement; GetUnlockableDef(achievement) replaces artifactDef.unlockableDef.
                codes[i] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ArtifactAchievementNullDefFix), nameof(GetUnlockableDef))).MoveLabelsFrom(codes[i]);
                codes.RemoveAt(i + 1);
                return codes;
            }

            Log.Warning("Could not find artifactDef.unlockableDef in BaseObtainArtifactAchievement.OnInstall");
            return codes;
        }

        private static UnlockableDef GetUnlockableDef(BaseObtainArtifactAchievement achievement)
        {
            ArtifactDef artifactDef = null;
            try
            {
                artifactDef = getArtifactDef(achievement);
            }
            catch (NullReferenceException)
            {
            }

            if (artifactDef)
                return artifactDef.unlockableDef;

            if (reportedAchievements.Add(achievement.GetType()))
                Log.Warning($"Achievement '{achievement.GetType().FullName}' has no ArtifactDef (artifact probably disabled), it can't be earned this session.");
            return null;
        }
    }
}
