# Misc Fixes

Designed to comprehensively address the bugs and exceptions that game updates introduce, and attempts to soften the impact that breaking changes have on the existing mod ecosystem, primarily through Harmony based IL hooks. It also applies a small amount of game integrity focused asset updates, and provides tools to assist with safely developing around error prone aspects of the game code.

Note that these changes are NOT intended to modify the vanilla gameplay experience. If it's even debatable on whether or not it's in line with the "intended" vanilla experience, it doesn't belong here.

---

# FOR DEVS:

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

- ## Extension Methods 
  - RiskOfOptions compatible config binding
  - Common ILCursor functions
  - Component removal and cloning
  - EntityStateConfiguration reading and modification

- To gain access, add the MiscFixes.dll as an assembly reference (Nuget package coming soon)
- Methods can be found in the MiscFixes.Modules.Extensions class

---

# Important Changes
### Check the changelog for more info. This list may not include everything.

- Fixes many common Henry prefab creation errors
- Fixes vanilla Asset loading issues introduced in the recent memory optimization update.
- Restores backwards compatibilty for temporary overlays
- Overscaled burn particles fix - thanks to Nuxlar
- Misc Unity Explorer fixes
- Facepunch exception that can occur randomly, preventing loading
- Incompletable void seed bug
- Incompletable Halcyonite Shrine due to drain value being calculated at <1 gold
- Lunar exploder killed by void death error
- an error for objects that have null HurtBoxes (only seen it for the hanging mushrooms in Golden Dieback)

---

# _SPECIAL THANKS TO:_

- Chinchi, wouldn't have been possible without ya
