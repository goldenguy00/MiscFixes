using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine;
using RoR2.CharacterAI;
using RoR2;

namespace MiscFixes
{
    public static class AssetFixes
    {
        internal static void Init()
        {
            FixElderLemurianFootstepEvents();
            FixSaleStarCollider();
            FixFalseSonBossP2NotUsingSpecial();
            FixVillageCscDrones();
        }

        /// <summary>
        /// Fix two Elder Lemurian footstep events to play sound and not spam Layer Index -1.
        /// </summary>
        public static void FixElderLemurianFootstepEvents()
        {
            Addressables.LoadAssetAsync<RuntimeAnimatorController>("RoR2/Base/Lemurian/animLemurianBruiser.controller").Completed += delegate (AsyncOperationHandle<RuntimeAnimatorController> obj)
            {
                PatchClip(obj.Result, 4, "LemurianBruiserArmature|RunRight", 1, "", "FootR");
                PatchClip(obj.Result, 12, "LemurianBruiserArmature|Death", 2, "MouthMuzzle", "MuzzleMouth");

                static void PatchClip(RuntimeAnimatorController anim, int clipIndex, string clipName, int eventIndex, string oldEventString, string newEventString)
                {
                    if (anim.animationClips.Length > clipIndex && anim.animationClips[clipIndex].name == clipName)
                    {
                        var clip = anim.animationClips[clipIndex];
                        if (clip.events.Length > eventIndex && clip.events[eventIndex].stringParameter == oldEventString)
                        {
                            var events = clip.events;
                            events[eventIndex].stringParameter = newEventString;
                            clip.events = events;
                            return;
                        }
                    }
                    Log.PatchFail(anim.name + " - " + clipName);
                }
            };
        }

        /// <summary>
        /// Prevent Sale Star's pickup from complaining about its collider's settings.
        /// </summary>
        public static void FixSaleStarCollider()
        {
            Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Items/LowerPricedChests/PickupSaleStar.prefab").Completed += delegate (AsyncOperationHandle<GameObject> obj)
            {
                var collider = obj.Result.transform.Find("SaleStar")?.GetComponent<MeshCollider>();
                if (collider == null || collider.convex)
                {
                    Log.PatchFail("collider of SaleStar");
                    return;
                }
                collider.convex = true;
            };
        }

        /// <summary>
        /// Fix False Son not using Tainted Offering in phase 2 due to a misconfigured AISkillDriver.
        /// </summary>
        public static void FixFalseSonBossP2NotUsingSpecial()
        {
            Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/FalseSonBoss/FalseSonBossLunarShardMaster.prefab").Completed += delegate (AsyncOperationHandle<GameObject> obj)
            {
                var skillDrivers = obj.Result.GetComponents<AISkillDriver>();
                foreach (var skillDriver in skillDrivers)
                {
                    if (skillDriver.customName == "Corrupted Paths (Step Brothers)")
                    {
                        skillDriver.requiredSkill = null;
                        return;
                    }
                }
                Log.PatchFail("False Son Boss Phase 2 special skill");
            };
        }

        /// <summary>
        /// Fix village using csc instead of isc for drones
        /// </summary>
        public static void FixVillageCscDrones()
        {
            Addressables.LoadAssetAsync<DirectorCardCategorySelection>("RoR2/DLC2/village/dccsVillageInteractables_DLC2.asset").Completed += delegate (AsyncOperationHandle<DirectorCardCategorySelection> obj)
            {
                var dccs = obj.Result;
                bool success = false;

                var categoryIndex = dccs.FindCategoryIndexByName("Drones");
                if (categoryIndex != -1)
                {
                    ref var category = ref dccs.categories[categoryIndex];
                    for (int i = 0; i < category.cards.Length; i++)
                    {
                        var card = category.cards[i];
                        if (card?.spawnCard == null)
                            continue;

                        if (card.spawnCard.name == "cscDrone1")
                        {
                            Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/Base/Drones/iscBrokenDrone1.asset").Completed += delegate (AsyncOperationHandle<InteractableSpawnCard> obj1)
                            {
                                card.spawnCard = obj1.Result;
                            };
                            success = true;
                        }
                        else if (card.spawnCard.name == "cscDrone2")
                        {
                            Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/Base/Drones/iscBrokenDrone2.asset").Completed += delegate (AsyncOperationHandle<InteractableSpawnCard> obj2)
                            {
                                card.spawnCard = obj2.Result;
                            };
                            success = true;
                        }
                    }
                }

                if (!success)
                    Log.PatchFail("Village Dccs drone fix");
            };
        }
    }
}
