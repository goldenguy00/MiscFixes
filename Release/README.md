# Misc Fixes

Designed to comprehensively address the bugs and exceptions that game updates introduce, and attempts to soften the impact that breaking changes have on the existing mod ecosystem, primarily through Harmony based IL hooks. It also applies a small amount of game integrity focused asset updates, and provides tools to assist with safely developing around error prone aspects of the game code.

Note that these changes are NOT intended to modify the vanilla gameplay experience. If it's even debatable on whether or not it's in line with the "intended" vanilla experience, it doesn't belong here.

---

# Important Changes

- Fixes common Henry based character creation errors
- Fixes overscaled burn particles - **thanks to Nuxlar**
- Restores backwards compatibilty for pre-SotS temporary overlays
- <>
- Prevents Unity Explorer from nuking the EventSystem
- Prevents Void Seeds from being tricked by Schrodinger's cat
- Prevents Halcyonite Shrine drain value from being < 1 gold per tick
- Prevents error on Lunar Exploder void death
- <>
- Filters out null HurtBoxes on CharacterModels
- Filters out null vehicleIdleStateMachines on CharacterBodies

---

# FOR DEVS:

- ## Extension Methods 
  - RiskOfOptions compatible config binding
  - Common ILCursor functions
  - Component removal and cloning
  - EntityStateConfiguration reading and modification

- To gain access, add the MiscFixes.dll as an assembly reference (Nuget package coming soon)
- Methods can be found in the MiscFixes.Modules.Extensions class

---

- ## HUD ChildLocator Entries
  - These are difficult to find normally because they have no identifiable component attached.

### New entries:

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

### Existing entries:

> - "BottomLeftCluster"
> - "TopCenterCluster"


> - "RightUtilityArea"
> - "ScopeContainer"
> - "CrosshairExtras"
> - "BossHealthBar"
> - "RightInfoBar" -Always null, kept in for compat

---

- # SPECIAL THANKS TO CHINCHI
wouldn't have been possible without ya
