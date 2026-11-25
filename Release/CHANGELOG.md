## Unreleased
- Merged base game fixes from BaseGameFixes:
    - **Defective Unit** - death state no longer throws when its model is already gone (e.g. Void Fields wave ending).
    - **Void Barnacle** - spawn no longer tries to spawn a null muzzle flash effect.
    - **Dream Demon** - projectile no longer throws on destroyed growth renderers.
    - **RigidbodySoundOnImpact** - no longer throws when a collision happens before `Start`.
    - **SetStateOnHurt** - freeze/stun no longer throws when applied before `Start` (duration modifiers are still respected).
    - **Drone Tech** - drone command targeting no longer throws when aiming at a chest.
    - **SpecialObjectAttributes** - no longer throws in `Start` when a prefab has missing objects in its collision/detach lists.
    - **Scene loading (multiplayer)** - a scene object destroyed during spawning no longer stops the rest of the scene from being spawned for clients.
    - **Wandering Vagrant** - Jelly Barrage no longer throws every tick when the Vagrant has no model.
    - **Wake mission** - leaving the cursed book state no longer throws when the scene is unloading.
    - **Footsteps** - bodies with footstep dust enabled but no dust prefab no longer throw on spawn.
    - **Void Fields spawns** - monsters no longer spawn inside the bubble of the cell you're charging.
    - **Void Fields cells** - objects running the null ward states without a `SphereZone` no longer throw on start.
    - **EnemyDetection** - no longer throws when touching a hurtbox that has no health component/body.
    - **Interactable locks** - unlocking no longer stops partway when one of the locked interactables was destroyed.
    - **Aurelionite's Blessing** - taking damage with no attacker (e.g. Shared Suffering) no longer throws.
    - **Director cards** - null director cards are dropped instead of breaking the stage's spawn pool (and with it the teleporter boss).
    - **Teleporter boss** - no longer throws when the stage has no monster cards at all.
    - **Lynx totem** - summoned Lynx no longer throw when the totem owner has no master.
    - **Inspect panel** - no longer throws when shown from a panel with no event system, or for items with no name/description/lore token (e.g. Starstorm 2's trader and shard panel).
    - **Animation effects** - animation events with no effect assigned no longer log `Unable to SpawnEffect`.
    - **Artifact achievements** - a mod's "obtain artifact" achievement for a disabled artifact no longer breaks profile loading (e.g. Expedition).
    - **Void Fields cells** - an active cell no longer throws every frame (or on exit) when the run ends while it's charging.
    - **Collective elites** - removing the share buff from destroyed bodies no longer throws in other mods' buff hooks (e.g. Classic Items Returns' Hit List).
    - **Stage spawn pool** - monster pool categories with nothing available (e.g. a family event with no available families) are no longer picked, which aborted building the stage's monsters and interactables.
    - **Interactable spawning** - cards with 0 weight are no longer picked, which threw and stopped the rest of the stage's interactables from spawning.
    - **Chained kills** - on-kill effects that kill more enemies (Gasoline and modded equivalents) no longer overflow the stack and crash the game in big modpacks; procs past a few levels deep run right after instead of nested.
    - **Solus Heart** - attack and teleport states no longer throw when the heart has no target silhouette.
    - **Solus Heart** - the death fall no longer throws when its offering master is gone.
    - **Light cookies** - lights with a custom render texture cookie no longer throw when disabled/destroyed after the texture was unloaded.
    - **Stage spawn pool** - stages with no available monster category at all no longer throw while building cards; they get no monster cards but still spawn their interactables.
    - **Shrine of Combat** - no longer throws on stages without monster cards.
    - **Alloy Hunter** - its spawn no longer throws when the enemy it kills has no master.
    - **Vehicle seats** - entering/leaving a seat (e.g. Rising Tides' Aquamarine bubble) no longer throws on destroyed lights/renderers, which left ejected passengers without collisions and hurtboxes.
    - **Point sounds** - sounds played while a scene unloads no longer throw on destroyed pooled emitters.
    - **Character animations** - bodies whose animator was destroyed no longer throw every frame.
    - **Duplicator** - dying bodies whose master is already gone no longer throw when updating temp item decay.
    - **Model panels** - no longer throw every frame without an event system.
    - **Hallowed Cauldron** - no longer throws every frame on clients while draining items, and finishes properly if the player who started it leaves.
    - **Geode portal tracker** - no longer throws every frame after the run ends.
    - **Equipment** - bodies whose master is gone (e.g. on the game over screen) no longer throw every frame when trying to use equipment.
    - **Drifter vault** - opening a vault bag chest while any player is dead no longer throws, which also cut the chest's opening short.
    - **Solus Amalgamator** - its flamethrower turret no longer throws when its target dies mid-attack.
    - **Vulture fight** - no longer throws on clients when it starts.
    - **Tracers and orb effects** - effects spawned outside the effect pool no longer log `Tracer: No owning pool set` / `OrbEffect: No owning pool set`.
    - **Friendly fire checks** - attacks no longer throw when hitting a health component without a body or team.
    - **Caustic Pheromones** - the aura no longer throws when destroyed before it found its owner's team.
    - **Shield blocking** - bullets no longer throw against blocking shields when the hit has no hurtbox.
    - **Mithrix** - phase 2 death pizzas no longer throw (and fail to fire) when the add was killed by a drone or anything else without a character direction.
    - **Scene transitions** - minions with Spare Drone Parts and wandering AI no longer throw when they start before the new stage is set up.
    - **Viewables** - mod content with no required entitlement no longer throws when checking for new logbook/character entries.
    - **Log noise** - silences `Could not find target object ... RpcOnHitGroundSFX` warnings, `Could not register ... no NetworkIdentity` and `has multiple NetworkIdentity components` messages, `Attempting to fix...` item/tier messages, `GetItemDef returned null`, `PopulateTypeFields issues` for vanilla content, `Animator.GotoState: State could not be found`, `Invalid Layer Index '-1'`, `DeductCooldown`, `GenericPool: ReleasePools called on non-empty pool`, `Particle System is trying to spawn on a mesh with zero surface area`, `Sub-emitters may not use stop actions`.

score is a hero

## 1.6.2
- Fixed persistent sound bug
- Fixed NRE with SetStateOnHurt

## 1.6.1
- Removed temp DLC4 item drop fix

## 1.6.0
- Temp DLC4 item drop fix

## 1.5.9
- Oops

## 1.5.8
- Added fix for luck being reset for a frame on inventory changed (thanks Gorakh)
- Add null checks for InitializeProjectile and CalculatePassengerFinalPosition
- Re-added Meridian log spam fix
- Re-added elder lemurian log spam fix
- Re-added virmin spawn vfx position fix

## 1.5.7
- Restored salestar fix
- Null checks
    - BuffWard_BuffTeam
    - AttackSpeedPerNearbyCollider_ReconcileBuffCount
    - CharacterModel_HighlightEquipentDisplay

## 1.5.6
- Null checks
    - CharacterDeathBehavior_OnDeath
    - ModelSkinController_ApplySkinAsync
    - CharacterBody_OnSkillActivated
    - JumpDamageStrikeBodyBehavior_UpdateDisplayInstances
    - TemporaryVisualEffect_RebuildVisualComponents

## 1.5.5
- Removed SS2 fix

## 1.5.4
- Fixed PsuedoCharacterMotor.velocity*Authority* setter
- Reorg hook management so that broken hooks are self contained
- Removed more unnecessary hooks to minimize this mod's presence

## 1.5.3

- Re-added flicker light fix for SS2
- Added PsuedoCharacterMotor.velocity setter

## 1.5.2

- Removed a lot of changes that were implemented in vanilla
- Fixed issues with vehicle seats
- Disabled SS2 santa hats

## 1.5.1

- Added fix for mithrix clone materials

## 1.5.0

- Moved some skin related validation to R2API.Skins
- Changed some extensions to use Component instead of MonoBehaviour

## 1.4.9

- Removed lobby skins fix o7

## 1.4.8

- Added lobby skins fix!
- Changed to proper usage of the new loading methods (maybe???)
- Fixed NRE with PlayerStatsComponent receiving a null body
- Added entityStateConfiguration extensions
- Added Component removal and deep cloning extensions
- Fixed typo in config extensions

## 1.4.7

- Using new asset loading methods
- Added fix for vermin spawn effect
- Made void seed hooks better, it should behave more akin to vanilla now
- Fixed chef achievement RolyPolyHitFiveAirEnemies
- Fixed AffixAurelioniteBehavior NRE hook

## 1.4.6

- Fixed the OnStageLoad hook (thanks Gorakh)

## 1.4.5

- Fixed identity crisis with config extensions
- Added scrapper sounds back

## 1.4.4

- Fixed edgecase with lobby skin handling

## 1.4.3

- Affix Aurelionite targeting NRE
- Removed False Son Boss's AI fix for missing attacks
- Removed merc evis targetting fix
- Removed Xi Construct blast fix
  - Thanks to Cap_ for bringing these up!
  - These fixes push the boundary of gameplay changes in a way I'm really not comfortable with.
  - Go download a QoL mod or something. 

## 1.4.2

- Added fix for OnStageLoad invalid key exceptions
- Config extensions sanitize inputs

## 1.4.1

- Replace Skin validation with better runtime AsyncOperationHandle validation
- Removed Starstorm fix

## 1.4.0

- Added extended validation checking to skins on startup to prevent common NREs
  - This will remove broken skins and log errors that should help the developers know whats going wrong.
- Added ModelSkinController to Survivor display prefabs if they are missing. 
- Added a good version of LobbySkinsFix
  - This includes a patch that disables LobbySkinsFix v1.2.1 
- Removed VAPI and MSU fixes (Starstorm2 v0.6.20 fix is still needed for now)

## 1.3.9

- Added version dependent fixes for VAPI, MSU and SS2
- Added RiskOfOptions compatible config binding extensions
- Added some IL matching extensions
- Changed asset loading to GUIDs
- Internal restructure
- Readme update
- Fixed previous patch's changelog

## 1.3.8

- Temporarily fixed ss2
- (NOT IMPLEMENTED) Added redirect to the new SkinDef.BakeAsync

## 1.3.7

- Fixed common henry creation template error
- Fixed null itemDisplayRuleset prefab load error
- Included the dotParticleFix hooks

## 1.3.6

- Fix for UnityExplorer's all commando bug
- Fix for Item display rule instantiation
- Fix for SurvivorMannequinSlotController ApplyLoadoutToMannequinInstance

## 1.3.5

- Removed CharacterBody HandleDisableAllSkillsDebuff fix
- Removed fix for CHEF's "It's Getting Hot Here" achievement not unregistering an event upon completion

## 1.3.4

- Added fix for Xi Construct's laser attack not exploding at the end
- Updated HUD childloc to include springCanvas
- Removed convar fix for now.

## 1.3.3

- Added more childLocator entries and renamed some
- Added fix for Halcyonite Shrine being able to drain 0 gold and getting stuck

## 1.3.2

- Didn't happen

## 1.3.1

- Fixed 9 seperate possible NREs in CharacterBody_HandleDisableAllSkillsDebuff
- Added additional child locator entries to the Hud. These are difficult to find normally because they have no identifiable component attached.
> - UpperLeftCluster
> - NotificationArea
> - SkillIconContainer
> - BuffDisplayRoot
> - InventoryContainer

## 1.3.0

- Removed fixes that have been addressed by vanilla
- Rewrote the VineOrb.OnArrival patch and added an additional fix for null DotDefs
- Added fix for null HurtBoxes in CharacterModel.Awake
- Added fix for the multiple Lemurian Egg lock VFX
- Added fix for False Son not using Tainted Offering in phase 2
- Added fix for tether errors with the twisted elite
- Added fix for Longstanding Solitude NRE when spawning near a chest
- Added fix for the Elder Lemurian footstep sounds
- Added fix for ConVars with uppercase letters working
- Added fix for CHEF's "It's Getting Hot Here" achievement not unregistering an event upon completion
- Reworked a bunch of patches to use Harmony so there is no MMHOOK dependency
- Removed log spam for the Facepunch fix

## 1.2.8

- Changed false to true in OnBossGroupDefeated

## ugh

- ugh

## 1.2.6

- Goodbye tank, no longer need fixing
- Thats the last of the mod compats yay!

## 1.2.5

- Goodbye tyranitar, no longer need fixing

## 1.2.4

- Goodbye hunk, no longer need fixing

## 1.2.3

- Goodbye rifter, no longer need fixing

## 1.2.2

- Skip call to DotController.GetDotDef from new Noxious Thorns that existed for the sole purpose of throwing index out of bounds exceptions...
- This was vanilla, probably some partially commented out code or something idk

## 1.2.1

- Fixed FreeItemOnLevelup hook
- Updated changelog for previous patch

## 1.2.0

### **Added a big group of changes, thanks to Chinchi**
    * EvisDash.FixedUpdate
      - Merc eviserate no longer targets allies
    * Duplicating.DropDroplet
      - Printers use vfx again
    * DetachParticleOnDestroyAndEndEmission.OnDisable
      - Particle systems dont unparent inactive children
    * PositionIndicator.UpdatePositions
      - Position indicator NRE when hud is disabled
    * Indicator.SetVisibleInternal
      - IL only rewrite
    * CrosshairOverrideBehavior.OnDestroy
      - Collection modified error
    * RuleChoiceController.FindNetworkUser 
      - Quit to menu event system nre
    * RewiredIntegrationManager.RefreshJoystickAssignment
      - No rewired input on quitout
    * Frolic.TeleportAroundPlayer
      - Handles teleport with no available nodes
    * BurnEffectController.HandleDestroy
      - Prevents engine call on destroyed object
    * MeridianEventTriggerInteraction.Awake
      - Fixes test state spam
    * DamageIndicator.Awake
      - Loads asset into the main menu camera's damageindicator
    - Additionally added a ton of safeguards against calling server methods on client, instead of just letting those calls go through.
    - Fixes sale star collider being incorrectly configured
---
### Now onto my own fixes...
- Mods using the old TemporaryOverlay component will now properly initialize the new TempOverlayInstance
- Antler NREs are all gone now, sheesh
    - ElusiveAntlersPickup.Start, CharacterBody.OnShardDestroyed, ElusiveAntlersPickup.FixedUpdate
- Mysterious RouletteChestController.Idle NRE
- Minionleash OnDisable NRE
- ProjectileController.Start nre without ghost
- ProcessGoldEvent nre
- FreeItemOnLevelUp called on obj with null inventory
- Vineorb onArrival
- BossGroup Ondefeated calling event with no run instance

- Remove light flicker thing cuz ss2 is good now.
    - I still earn my keep by fixing the TetherVfxOrigin calling a null event.
    - Also fixed the flashbang menu scene
    - Thanks for continuing to boost my download count ily ss2 devs <3
- Made the Rifter fix actually fix Rifter

- **Todo:** update readme but euuuugh

## 1.1.3

- Forgot to make a hook actually do stuff

## 1.1.2

- Welcome Rifter to the club of mod fixes!
- Fixes a couple common errors
- Adjusts primary proc co-efficient to 1.0

- Fixed tether nre when invoking an event

- Tank: Fixed particles on genesis loop
- Tank: Enable overlays on renderers only if theyre enabled 

- Updated readme to reflect all changes
- Added mod specific config options

## 1.1.1

- Tank utility gives fuel again
- Added new optimization config option for tank
    - Tank no longer tanks fps hooray!
    - Default is disabled in case of bugs.

## 1.1.0

- Fixed tank save bug hooray!
- Removed damage indicator "fix" because it broke stuff when it didnt break lol

## 1.0.9

- Fixed the stupid SceneDirector.PopulateScene exception that the new stage 1 has
- Why are they using the wrong spawn cards
- Why is the nullchecking so inconsistent
- Why

## 1.0.8

- Fixed the fix for Hunk TVirus
- Moved stuff around

## 1.0.7

- Added git repo
- Fixed damage indicator startup exception

## 1.0.6

- Actually fixed the previously mentioned CheesePlayerHandler error

## 1.0.5

- Fixed incompletable void seed bug
- Fixed GoldenCoast chest interaction error
- Fixed more CheesePlayerHandler errors
- Fixed Hunk Urostep

## 1.0.4

- Fixed Celestial War Tank duplicating crosshair bug
- Fixed Celestial War Tank CheesePlayerHandler spam on stage start
- Adjusted some metadata

## 1.0.3

- Corrected mistake made with the tyranitar rock fix

## 1.0.2

- Fixed Steamworks loading error
- Prevented (harmless) exception when loading without Hunk

## 1.0.1

- Fixed some more vanilla bugs
- Added mod specific fixes for Hunk and Tyranitar

## 1.0.0

- Initial release