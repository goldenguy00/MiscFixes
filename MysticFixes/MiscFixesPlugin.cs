using System.Security.Permissions;
using System.Security;
using BepInEx;
using HarmonyLib;
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
        public const string PluginVersion = "1.2.0";

        public ConfigEntry<bool> extraFixTank;
        public ConfigEntry<bool> fixTank;
        public ConfigEntry<bool> fixRift;
        public ConfigEntry<bool> fixHunk;
        public ConfigEntry<bool> fixTyr;
        public Harmony harm;

        public void Awake()
        {
            fixHunk = Config.Bind("Main",
                "Fix Hunk",
                true,
                "Enables fixes for Hunk");

            fixRift = Config.Bind("Main",
                "Fix Rifter",
                true,
                "Enables fixes for Rifter");

            fixTyr = Config.Bind("Main",
                "Fix Tyranitar",
                true,
                "Enables fixes for Tyranitar");

            fixTank = Config.Bind("Main",
                "Fix CelestialWarTank",
                true,
                "Enables fixes for CelestialWarTank");

            extraFixTank = Config.Bind("Experimental",
                "Optimize Tank",
                true,
                "Enables extremely effective optimization with no (known) drawbacks for Celestial War Tank.");

            ReplaceDCCS();

            harm = new Harmony(PluginGUID);
            harm.CreateClassProcessor(typeof(FixVanilla)).Patch();
            GameFixes.Init();

            if (fixHunk.Value)
                Hunk(harm);

            if (fixTyr.Value)
                Tyr(harm);

            if (fixTank.Value)
                Tank(harm);

            if (fixRift.Value)
                Rift(harm);
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

        public void Hunk(Harmony harm)
        {
            try
            {
                harm.CreateClassProcessor(typeof(FixHunk)).Patch();
            }
            catch (Exception) { }
        }

        public void Tyr(Harmony harm)
        {
            try
            {
                harm.CreateClassProcessor(typeof(FixRocks)).Patch();
            } 
            catch (Exception) { }
        }

        public void Tank(Harmony harm)
        {
            try
            {
                harm.CreateClassProcessor(typeof(FixTank)).Patch();
                On.RoR2.BurnEffectController.AddFireParticles += FixTank.BurnEffectController_AddFireParticles;
                On.EntityStates.VagrantNovaItem.BaseVagrantNovaItemState.OnEnter += FixTank.BaseVagrantNovaItemState_OnEnter;
                if (extraFixTank.Value)
                {
                    harm.CreateClassProcessor(typeof(ReplaceRuntime)).Patch();
                    harm.CreateClassProcessor(typeof(ReplaceColorRuntime)).Patch();
                    harm.CreateClassProcessor(typeof(ReplaceVisualRuntime)).Patch();
                }
            }
            catch (Exception) { }
        }


        public void Rift(Harmony harm)
        {
            try
            {
                harm.CreateClassProcessor(typeof(FixRiftNames)).Patch();
                RoR2Application.onLoad += () =>
                {
                    try
                    {
                        harm.CreateClassProcessor(typeof(FixRift)).Patch();
                    }
                    catch (Exception) { }
                };
            }
            catch (Exception) { }
        }
    }
}
