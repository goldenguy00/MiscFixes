# Misc Fixes

Designed to comprehensively address the bugs and exceptions that game updates introduce, and attempts to soften the impact that breaking changes have on the existing mod ecosystem, primarily through Harmony based IL hooks. It also applies a small amount of game integrity focused asset updates. 

Note that these changes are NOT intended to modify the vanilla gameplay experience. If it's even debatable on whether or not it's in line with the "intended" vanilla experience, it doesn't belong here.

---

# FOR DEVS:

- HUD ChildLocator Entries
  - These are difficult to find normally because they have no identifiable component attached.
- Extension methods for RiskOfOptions compatible config binding
- Extension methods for some common ILCursor functions

## New entries:

> - "SpringCanvas"
> - "UpperRightCluster"
> - "BottomRightCluster"
> - "UpperLeftCluster"
> - "BottomCenterCluster"
> - "LeftCluster"
> - "RightCluster"

> - "NotificationArea"
> - "ScoreboardPanel"
> - "SkillDisplayRoot"
> - "BuffDisplayRoot"
> - "InventoryDisplayRoot"

---

## Existing entries:

> - "BottomLeftCluster"
> - "TopCenterCluster"


> - "RightUtilityArea"
> - "ScopeContainer"
> - "CrosshairExtras"
> - "BossHealthBar"
> - "RightInfoBar" -Always null, kept in for compat

---

# Important Changes
### Check the changelog for more info. This list may not include everything.

- Fixes many of the issues modded characters and skins have in the recent update.
- Fixes AssetLoading issues introduced in the recent memory optimization update.
- Fixes NRE with Aurelionite affix targeting
- Fixes common Henry prefab creation error
- Fixes NRE when masterless bodies level up (TryGiveFreeUnlockWhenLevelUp)
- Fixes various NREs with VineOrb (dead target on arrival, null dotDef)
- Prevents an error when spawning some projectiles, probably because they lack a model
- Some temporary overlay bug
- Some ProcessGoldEvents error
- Fixes an NRE when leaving the stage with drones (MinionLeashBodyBehavior.OnDisable)
- Fixes any Antler NREs
- FogDamageController NRE
- Lunar exploder killed by void death error
- Fixes the roulette check NRE
- Fixes Gilded Coast chest interaction error
- Fixes the printer not using a VFX when printing the item
- Prevents pointless error spam when destroying some objects, e.g., killing a Stone Titan
- Fixes an error for objects that have null HurtBoxes (only seen it for the hanging mushrooms in Golden Dieback)
- Fixes Lemurian Eggs constantly creating new lock VFX when charging the teleporter which don't get cleared upon completion
- Fixes NRE spam for arrow indicators when playing with the HUD disabled (teleporter boss, void seed, etc)
- Equipment indicator error
- Fixes a crosshair error when Seeker respawns
- Fixes a Rewired error when quiting the game
- Fixes the TestState1 - TestState2 log spam on Prime Meridian
- Fixes an error when the Child fails to teleport near your location
- Fixes an NRE when spawning close to a chest
- Fixes NREs related to TetherVfxOrigin.AddTether and TetherVfxOrigin.RemoveTetherAt for the twisted elite
- Fixes Halcyonite's Whirlwind NRE spam when its target is killed during the skill
- Fixes Meridian's Will failing to pull monsters in when a stationary target is hit

- Restores some failing Elder Lemurian footstep sounds
- Fixes an error message for Sale's Star pickup

- The dreadful Facepunch exception that can occur, which randomly prevented loading (3% bug)
- Flicker light error
- Fixes an unrecoverable error caused by having multiple event systems (thanks Bubbet)
- Incompletable void seed bug
- (temporarily removed) Fixes ConVars with uppercase letters not working, e.g, egsToggle

- Prevents spam error for various server methods called on clients


---

# _SPECIAL THANKS TO:_

- Chinchi, wouldn't have been possible without ya
