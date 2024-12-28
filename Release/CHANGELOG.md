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
- Antler NREs are all gone now, sheesh
    - ElusiveAntlersPickup.Start, CharacterBody.OnShardDestroyed, ElusiveAntlersPickup.FixedUpdate
- Mysterious RouletteChestController.Idle NRE
- Minionleash OnDisable NRE (todo: fix the teleport coroutine)
- Remove light flicker thing cuz ss2 is good now.
    - I still earn my keep by fixing the TetherVfxOrigin calling a null event.
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