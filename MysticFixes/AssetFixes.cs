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
            FixSaleStarCollider();
            FixFalseSonBossP2NotUsingSpecial();
            FixVillageCscDrones();
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
            var iscDrone1 = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/Base/Drones/iscBrokenDrone1.asset").WaitForCompletion();
            var iscDrone2 = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/Base/Drones/iscBrokenDrone2.asset").WaitForCompletion();
            Addressables.LoadAssetAsync<DirectorCardCategorySelection>("RoR2/DLC2/village/dccsVillageInteractables_DLC2.asset").Completed += delegate (AsyncOperationHandle<DirectorCardCategorySelection> obj)
            {
                var dccs = obj.Result;
                for (int i = 0; i < dccs.categories.Length; i++)
                {
                    ref var category = ref dccs.categories[i];
                    if (category.name == "Drones")
                    {
                        for (int j = 0; j < category.cards.Length; j++)
                        {
                            var card = category.cards[j];
                            if (card?.spawnCard?.name == "cscDrone1")
                            {
                                card.spawnCard = iscDrone1;
                            }
                            else if (card?.spawnCard?.name == "cscDrone2")
                            {
                                card.spawnCard = iscDrone2;
                                return;
                            }
                        }
                    }
                }
                Log.PatchFail("Village Dccs drone fix");
            };
        }
    }
}
