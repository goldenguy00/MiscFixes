using System.Security.Permissions;
using System.Security;
using BepInEx;
using HarmonyLib;
using UnityEngine.AddressableAssets;
using RoR2;
using System;
using BepInEx.Configuration;
using UnityEngine.SceneManagement;
using UnityEngine;
using RoR2.UI.MainMenu;

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
        public const string PluginVersion = "1.2.5";

        public ConfigEntry<bool> fixTank;
        public ConfigEntry<bool> extraFixTank;

        private Harmony harm;

        public void Awake()
        {
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

            if (fixTank.Value)
                Tank(harm);

            MainMenuController.OnPreMainMenuInitialized += SS2;
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

        public void SS2()
        {
            bool hasChristmasMenuEffect = false;
            var sceneObj = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = sceneObj.Length - 1; i >= 0; i--)
            {
                var obj = sceneObj[i];
                if (obj && obj.name.StartsWith("ChristmasMenuEffect"))
                {
                    if (!hasChristmasMenuEffect)
                        hasChristmasMenuEffect = true;
                    else
                        GameObject.Destroy(obj);
                }
            }
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
    }
}
