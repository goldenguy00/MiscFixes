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
        public const string PluginVersion = "1.2.8";

        private Harmony harm;

        public void Awake()
        {
            ReplaceDCCS();

            harm = new Harmony(PluginGUID);
            harm.CreateClassProcessor(typeof(FixVanilla)).Patch();
            GameFixes.Init();
        }

        private void ReplaceDCCS()
        {
            Addressables.LoadAssetAsync<DirectorCardCategorySelection>("RoR2/DLC2/village/dccsVillageInteractablesDLC1.asset").Completed += o =>
            {
                var dccs = o.Result;
                if (!dccs)
                    return;

                var pathFormat = "RoR2/Base/Drones/iscBroken{0}.asset";
                for (int i = 0; i < dccs.categories.Length; i++)
                {
                    if (dccs.categories[i].name == "Drones")
                    {
                        for (int j = 0; j < dccs.categories[i].cards.Length; j++)
                        {
                            var card = dccs.categories[i].cards[j];

                            if (card?.spawnCard && card.spawnCard.name.StartsWith("csc"))
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
            };
        }
    }
}
