# Misc Fixes

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
- Prevents Eviscerate from targetting allies
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
- Fixes CHEF's "It's Getting Hot Here" achievement not unregistering an event upon completion
- Fixes Halcyonite's Whirlwind NRE spam when its target is killed during the skill
- Fixes Meridian's Will failing to pull monsters in when a stationary target is hit

- Restores some failing Elder Lemurian footstep sounds
- Fixes an error message for Sale's Star pickup
- Fixes False Son not using Tainted Offering during phase 2

- The dreadful Facepunch exception that can occur, which randomly prevented loading (3% bug)
- Flicker light error
- Fixes an unrecoverable error caused by having multiple event systems (thanks Bubbet)
- Incompletable void seed bug
- Fixes ConVars with uppercase letters not working, e.g, egsToggle

- Prevents spam error for various server methods called on clients