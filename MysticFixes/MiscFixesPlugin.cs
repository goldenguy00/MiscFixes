using System.Security.Permissions;
using System.Security;
using BepInEx;
using HarmonyLib;
using System.Runtime.CompilerServices;
using UnityEngine.AddressableAssets;
using RoR2;
using System;
using BepInEx.Configuration;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace MiscFixes
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class MiscFixesPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "_" + PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "score";
        public const string PluginName = "MiscFixes";
        public const string PluginVersion = "1.1.1";

        public ConfigEntry<bool> extraFixTank;

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void Awake()
        {
            extraFixTank = Config.Bind("Experimental", "Optimize Tank", false, "Enables extremely effective optimization with no (known) drawbacks for Celestial War Tank. Report bugs to mod's github page. Does nothing if the mod is not installed.");
            ReplaceDCCS();

            var harm = new Harmony(PluginGUID);
            harm.CreateClassProcessor(typeof(FixVanilla)).Patch();

            Hunk(harm);
            Tyr(harm);
            Tank(harm);
            //SS2(harm);
        }

        private void ReplaceDCCS()
        {
            var dccs = Addressables.LoadAssetAsync<DirectorCardCategorySelection>("RoR2/DLC2/village/dccsVillageInteractablesDLC1.asset").WaitForCompletion();
            var pathFormat = "RoR2/Base/Drones/iscBroken{0}.asset";
            foreach (var cat in dccs.categories)
            {
                if (cat.name == "Drones")
                {
                    foreach (var card in cat.cards)
                    {
                        if (card.spawnCard && card.spawnCard.name.StartsWith("csc"))
                        {
                            var newName = string.Format(pathFormat, card.spawnCard.name.Replace("csc", string.Empty));
                            var newCard = Addressables.LoadAssetAsync<InteractableSpawnCard>(newName).WaitForCompletion();
                            if (newCard)
                            {
                                Logger.LogDebug($"Replacing {card.spawnCard.name} with {newCard.name}");

                                card.spawnCard = newCard;
                            }
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void Hunk(Harmony harm)
        {
            try
            {
                harm.CreateClassProcessor(typeof(FixHunk)).Patch();
            }
            catch (Exception e)
            {
                Logger.LogDebug(e);
            }
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void Tyr(Harmony harm)
        {
            try
            {
                harm.CreateClassProcessor(typeof(FixRocks)).Patch();
            }
            catch (Exception e)
            {
                Logger.LogDebug(e);
            }
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void Tank(Harmony harm)
        {
            try
            {
                harm.CreateClassProcessor(typeof(FixTank)).Patch();
                if (extraFixTank.Value)
                {
                    harm.CreateClassProcessor(typeof(ReplaceRuntime)).Patch();
                    harm.CreateClassProcessor(typeof(ReplaceColorRuntime)).Patch();
                    harm.CreateClassProcessor(typeof(ReplaceVisualRuntime)).Patch();
                }
            }
            catch (Exception e)
            {
                Logger.LogDebug(e);
            }
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void SS2(Harmony harm)
        {
            try
            {
                //harm.CreateClassProcessor(typeof(FixSS2)).Patch();
            }
            catch (Exception e)
            {
                Logger.LogDebug(e);
            }
        }
    }
}
