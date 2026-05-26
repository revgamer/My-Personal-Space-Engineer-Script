# AGM.md — AutoGrid Manager Continuation Guide

Project: **AutoGrid Manager**
Short name: **AGM**
Author: **RevGamer**
Current build: **0.4.1**
Current test LCD tag: `[AGM-S]`
Current PB tag: `{AGM-Main}`
Game: **Space Engineers**
Script type: **Programmable Block script — paste directly, no wrapper needed**

---

## 1. Project Summary

AutoGrid Manager is a unified Space Engineers grid-management system combining:

- Automatic LCD display commands (sprite-based, industrial HUD style)
- Inventory totals per category
- Cargo fill status
- Power / battery / reactor dashboard
- Fuel & life support dashboard
- Autocrafting quota tracking and assembler queueing
- Item sorting across tagged cargo containers
- Multi-LCD stock boards (wide / vertical / stacked layouts)
- Boot / reboot screen animation
- Branded Programmable Block controller screen

---

## 2. Permission Context

The user has permission to use GOAT-style icons/methods for personal use.
For any public Workshop release: use clean-room AGM code, credit appropriately, keep private builds labelled as private.

---

## 3. Tag System

### PB tag — only on the main AGM Programmable Block
```
{AGM-Main}
```
Example: `PB AutoGrid Manager {AGM-Main}`

### External LCD tag
```
[AGM-S]
```
Example: `LCD Components Left [AGM-S]`

### Legacy tags — avoid during testing
```
[AGM]   [LCD]
```

---

## 4. PB Custom Data Config

Paste into the **Programmable Block** Custom Data.

```ini
[general]
lcd_tag=[LCD]
agm_tag=[AGM]
sorter_tag=[GOAT]
enable_lcd=true
enable_sorting=true
include_docked_grids=true
max_transfers_per_run=8
refresh_every_ticks=1

[quotas]
Component/SteelPlate=300000
Component/InteriorPlate=55000
Component/Construction=50000
Component/Motor=16000
Component/Computer=6500
Ingot/Iron=30000000
Ingot/Nickel=2000000
Ingot/Cobalt=1200000
Ore/Ice=5000000
AmmoMagazine/NATO_25x184mm=2500
GasContainerObject/HydrogenBottle=5
OxygenContainerObject/OxygenBottle=5

[power:RAB Base]
batteries=G:[RAB] Batteries
reactors=G:[RAB] Nuclear Reactors
solar=
wind=
hydrogen=
include_ungrouped=false

[LifeSupport:Base]
hydrogen=G:[RAB] Hydrogen Tanks
oxygen=G:[RAB] Oxygen Tanks
generators=G:[RAB] Ice Generators
include_ungrouped=false
```

AGM only reads these keys from the config. Everything else is ignored harmlessly:

| Key | Default | Purpose |
|---|---|---|
| `enable_sorting` | false | Turn sorter on/off |
| `include_docked_grids` | false | Pull items from docked ship cargo |

Section headers like `[general]`, `[quotas]` etc. are skipped safely.

---

## 5. Run Arguments

| Argument | Effect |
|---|---|
| `reload` | Reload config + rescan + boot visual |
| `rescan` | Rescan blocks + boot visual |
| `reboot` | Show boot visual again |
| `sort` | Trigger one immediate sort pass |
| `reset` | Clear all state and restart |

---

## 6. Refresh Pacing

| Task | Interval |
|---|---|
| LCD draw | Every Update10 (1 LCD per run ≈ 6 LCDs/sec) |
| Inventory index | Every 30 ticks ≈ 0.5s |
| Block rescan | Every 300 ticks ≈ 5s |
| Autocrafting check | Every 300 ticks ≈ 5s |
| Sort pass | Every 100 ticks ≈ 1.67s (only if `enable_sorting=true`) |

---

## 7. LCD Custom Data Commands

One command per LCD in Custom Data.

### Inventory screens
```
IndustrialInventory=Component
IndustrialInventory=Ore
IndustrialInventory=Ingot
IndustrialInventory=Ammo
```

With page number:
```
IndustrialInventory=Component:1
IndustrialInventory=Component:2
```

With page + rows per page:
```
IndustrialInventory=Component:1:14
IndustrialInventory=Component:2:14
```

### Wide (side by side) layout
```
IndustrialInventoryWide=Component:1:14:left
IndustrialInventoryWide=Component:2:14:right
```

Three screens:
```
IndustrialInventoryWide=Component:1:14:left
IndustrialInventoryWide=Component:2:14:middle
IndustrialInventoryWide=Component:3:14:right
```

### Vertical (stacked) layout
```
IndustrialInventoryVertical=Component:1:8:top
IndustrialInventoryVertical=Component:2:8:bottom
```

### Power dashboard
```
PowerDashboard=RAB Base
```

### Fuel & life support
```
FuelLifeSupport=Base
```
Without profile name:
```
FuelLifeSupport
```

### Autocrafting
```
AutoCrafting=Component
SteelPlate=20000
InteriorPlate=5000
Construction=10000
Computer=2000
Motor=3000
```

Short notation: `SteelPlate=20k`

Multi-LCD link (page 1 holds quota list, page 2 borrows it):
```
LCD Autocrafting [AGM-S] !LINK:A1
LCD Autocrafting [AGM-S] !LINK:A2
```

### Sorter dashboard
```
SorterDashboard
```
Aliases: `Sorter`, `AutoSorter`

Shows: online status, cargo fill, type container counts, fallback/locked/hidden counts, last sort status, last moved item, source → destination route (full block names, no truncation).

---

## 8. Cargo Container Tags

Add tags to cargo container **names**.

| Tag | Meaning |
|---|---|
| `[Ore]` / `[Ores]` | Ore destination |
| `[Ingot]` / `[Ingots]` | Ingot destination |
| `[Component]` / `[Components]` | Component destination |
| `[Ammo]` | Ammo destination |
| `[Tool]` / `[Tools]` | Tool destination |
| `[Bottle]` / `[Bottles]` | Bottle destination |
| `[Inventory]` | General fallback — auto-promoted when typed container is full |
| `[Locked]` | Never moved to or from |
| `[Hidden]` | Excluded from quota counts |
| `[GOAT]` | Treated same as `[Inventory]` |

Recommended naming:
```
Large Cargo Container [Ore]
Large Cargo Container [Ingot]
Large Cargo Container [Component]
Large Cargo Container [Ammo]
Large Cargo Container [Tool]
Large Cargo Container [Bottle]
Large Cargo Container [Inventory]
Large Cargo Container [Locked]
```

---

## 9. Sorter Behaviour

### How it works

1. Every 100 ticks `ProcessSorting()` runs
2. `BuildSortCargos()` rebuilds source and destination lists
3. Visits every source in round-robin order
4. Iterates backwards through items per source
5. Skips an item only if source is a typed container and the item already matches that type
6. Calls `MoveSortedItem()` which scans typed destinations, skips full ones (≥98%), tries next
7. If all typed containers are full, promotes a fallback `[Inventory]` container
8. Up to 50 stacks moved per cycle using full-stack transfer (IIM method — no amount = entire stack in one call)

### Sort sources
- All cargo containers (every tag including `[Ore]`, `[Inventory]`, untagged)
- Assembler output inventories (slot 1)
- Refinery output inventories (slot 1)
- Single-inventory non-cargo blocks (connectors, welders, drills etc.)
- Docked ship cargo when `include_docked_grids=true`

### Never touched by sorter
- `[Locked]` containers
- `[Hidden]` containers
- Reactor inventories
- Gas generator inventories
- Gas tank inventories
- Assembler input inventories (slot 0)
- Refinery input inventories (slot 0)

### Docked ship behaviour
When `include_docked_grids=true` and a ship docks:
- Ship cargo is added as sort **sources only**
- AGM pulls items off the ship into base typed containers
- Base items are **never** sent to the ship
- `[Locked]` and `[Hidden]` on ship containers are respected
- After undocking, run `reload` or wait for next rescan

### Full container fallback
If `[Component] 1` is full → tries `[Component] 2` → tries `[Component] 3` etc.
If all typed containers for a category are full → promotes first `[Inventory]` container by renaming it.

---

## 10. Visual Style

| Element | Colour |
|---|---|
| Background | `new Color(13, 9, 5)` |
| Panel | `new Color(28, 19, 10)` |
| Panel 2 | `new Color(42, 29, 13)` |
| Accent (border/title) | `new Color(255, 174, 48)` |
| Accent 2 | `new Color(255, 213, 91)` |
| Text | `new Color(236, 218, 177)` |
| Dim text | `new Color(120, 94, 58)` |
| OK green | `new Color(75, 210, 120)` |
| Warning amber | `new Color(255, 142, 45)` |
| Error red | `new Color(226, 64, 45)` |

Stock board layout:
- Warm black/brown panel background
- Orange/yellow outer border
- Bold category title
- Item icon (sprite) on left
- Item name + current amount
- Optional quota text
- Progress bar on right
- `+ more` marker if rows overflow
- Clear page number

Multi-LCD rule: all joined screens must use the same renderer and colour palette. Never mix yellow border on one screen and blue/teal on another.

---

## 11. Item Sprite IDs

### Ores
```
MyObjectBuilder_Ore/Iron
MyObjectBuilder_Ore/Nickel
MyObjectBuilder_Ore/Cobalt
MyObjectBuilder_Ore/Silicon
MyObjectBuilder_Ore/Magnesium
MyObjectBuilder_Ore/Silver
MyObjectBuilder_Ore/Gold
MyObjectBuilder_Ore/Platinum
MyObjectBuilder_Ore/Uranium
MyObjectBuilder_Ore/Stone
MyObjectBuilder_Ore/Scrap
MyObjectBuilder_Ore/Ice
```

### Ingots
```
MyObjectBuilder_Ingot/Iron
MyObjectBuilder_Ingot/Nickel
MyObjectBuilder_Ingot/Cobalt
MyObjectBuilder_Ingot/Silicon
MyObjectBuilder_Ingot/Magnesium
MyObjectBuilder_Ingot/Silver
MyObjectBuilder_Ingot/Gold
MyObjectBuilder_Ingot/Platinum
MyObjectBuilder_Ingot/Uranium
MyObjectBuilder_Ingot/Stone
```

### Components
```
MyObjectBuilder_Component/SteelPlate
MyObjectBuilder_Component/InteriorPlate
MyObjectBuilder_Component/Construction
MyObjectBuilder_Component/MetalGrid
MyObjectBuilder_Component/SmallTube
MyObjectBuilder_Component/LargeTube
MyObjectBuilder_Component/Motor
MyObjectBuilder_Component/Display
MyObjectBuilder_Component/Computer
MyObjectBuilder_Component/PowerCell
```

For modded / dynamic items:
```csharp
item.Type.TypeId + "/" + item.Type.SubtypeId
```

### Drawing sprites
```csharp
void TryDrawSprite(MySpriteDrawFrame frame, string spriteName, Vector2 center, Vector2 size, Color col)
{
    try { frame.Add(new MySprite(SpriteType.TEXTURE, spriteName, center, size, col)); }
    catch { }
}
```

---

## 12. PB Screen

### Viewport (required — always use this)
```csharp
RectangleF viewport = new RectangleF(
    (surface.TextureSize - surface.SurfaceSize) * 0.5f,
    surface.SurfaceSize
);
```

### LCD sprite mode (required for all sprite surfaces)
```csharp
surface.ContentType = VRage.Game.GUI.TextPanel.ContentType.SCRIPT;
surface.ScriptBackgroundColor = COLOR_BG;
surface.BackgroundColor = COLOR_BG;
```

Always call `frame.Dispose()` — frame does not render without it.

### Boot display
```
AGM
AutoGrid Manager
by RevGamer
REBOOT %
[||||||||..............]
```

### Normal running display
```
AGM
AutoGrid Manager
ONLINE
LCDs  : 4
Cargo : 12
Items : 53
Sort  : ON
```

---

## 13. Code Architecture

```
Program()
  ├─ InitBlueprints()
  ├─ ReadProgramConfig()
  ├─ RescanBlocks()
  └─ StartBoot()

Main(argument, updateSource)
  ├─ Argument handling (reload / rescan / reboot / sort / reset)
  ├─ Tick counters
  │    ├─ RescanBlocks()        every RESCAN_TICKS (300)
  │    ├─ IndexInventory()      every INDEX_TICKS (30)
  │    ├─ ReadProgramConfig()   every INDEX_TICKS (30)
  │    ├─ ProcessAutocrafting() every CRAFT_TICKS (300)
  │    └─ ProcessSorting()      every SORT_TICKS (100) if enabled
  ├─ Boot animation (DrawBootAll / EchoBoot)
  └─ Normal run (DrawNextScreens / DrawPbStatus / EchoStatus)
```

Key constants:
```csharp
private const int RESCAN_TICKS      = 300;
private const int INDEX_TICKS       = 30;
private const int CRAFT_TICKS       = 300;
private const int SORT_TICKS        = 100;
private const int SORT_MOVES_PER_PASS = 50;
private const double SORT_MAX_TRANSFER_AMOUNT = 1000.0;
private static readonly StringComparison SC = StringComparison.OrdinalIgnoreCase;
```

---

## 14. Air Vent Monitoring

Requirements:
1. Block name contains `[AGM-S]`
2. Block Custom Data contains `InteriorVent`

Example:
```
Base Air Vent [AGM-S]
```
Custom Data:
```
InteriorVent
```

OK = working + can pressurize + oxygen ≥ 95%.

---

## 15. Known Issues & Fixes

| Issue | Cause | Fix |
|---|---|---|
| Flickering LCDs | Two scripts writing to same LCD | Only use `[AGM-S]`, disable old PBs |
| Screen not updating | Config change not picked up | Run `reload` |
| Page 2 not showing | Not enough items or wrong row count | Force rows: `Component:1:8` + `Component:2:8` |
| PB screen tiny/offset | Wrong viewport | Use `(TextureSize - SurfaceSize) * 0.5f` |
| Blue HUD text over LCD | SE GPS overlay, not AGM | Not a bug, turn off HUD |
| Reactors cycling on/off | Sorter pulling uranium out | Fixed — reactors excluded from sort sources |
| Items bouncing in/out of reactors | Sorter treating reactor as source | Fixed — IMyReactor excluded in BuildSortCargos |
| Dashboard frozen | BuildSortCargos missing from dashboard init | Fixed — lazy init when sortCargos empty |
| Route text truncated with `..` | ShortBlockName truncating to 24 chars | Fixed — full name shown |
| Items not moving to correct container | SORT_MOVES_PER_PASS was 2, loop broke early | Fixed — 50 moves, full sweep per pass |
| Full container not skipped | No space check before transfer | Fixed — HasInventorySpace (98% threshold) checked first |

---

## 16. Reactor Rules

**Do not add uranium balancing to AGM.** Tried and removed — causes reactors to cycle on/off due to:
- Script fighting SE native conveyor system
- Docked ship ingot containers interfering
- Fill/drain passes running in same tick

**Solution:** leave `UseConveyorSystem = true` on all reactors. SE handles uranium distribution natively and reliably.

Reactors, gas generators, and gas tanks are permanently excluded from sort sources so the sorter never touches their inventories.

---

## 17. Multi-PB / Docked Ship Rules

- **One PB, one AGM script.** Never run two AGMs with `enable_sorting=true` at the same time.
- Other PBs can run AGM for LCD display only with `enable_sorting=false`.
- When a ship docks with no PB — set `include_docked_grids=true` on base AGM. It will pull items off the ship automatically.
- When ship undocks — run `reload` to clear the docked grid from source lists.
- AGM has no multi-PB conflict detection (IIM-style `pauseThisPB` not implemented — not needed for current setup).

---

## 18. Development Rules

1. Always generate a **full PB-ready `.cs` file** when changing code
2. Keep `[AGM-S]` for test LCDs unless finalising tags
3. Keep `{AGM-Main}` for PB screen only
4. Never touch LCD drawing code unless explicitly asked
5. Paste-ready for SE in-game PB editor — no `using`, no `namespace`, no outer class
6. Preserve `RevGamer` as author
7. Note if build has not been tested in-game
8. When continuing in new session: read `AGM.md` first, then `CLAUDE.md`
9. After every code change: verify `wc -c AGM.cs` is under 100,000 characters

---

## 19. Recommended Test Setup

After pasting new script:
```
reload
```
Then:
```
sort
```
Check Sorter Dashboard LCD for `moved (N)` status.

Verify conveyors are ON for all reactors after any script change.

---

## 20. Roadmap

### Display
- Finalise StockBoard row spacing, icon scale, text alignment, page number
- True wide/vertical join borders
- Optional title on first screen only, footer on last only
- PB screen themes: Classic / Industrial / Minimal

### Inventory / Quotas
- Quota parser from LCD Custom Data (`SteelPlate: 300K`)
- Global quota config in PB Custom Data `[quotas]` section
- Missing stock screen: `IndustrialMissing=Component`
- Search/filter screen: `FindItem=SteelPlate`

### Sorting
- Balance items across same-type containers
- Special/quota stock containers with Custom Data config
- Priority tokens `[P1]`, `[PMax]`, `[PMin]`

### Production
- Blueprint learning for modded items
- Refinery manager (ore priority, ice handling)
- Production status LCD

### Power / System
- Connector / dock screen
- Cargo fill dashboard
- Reactor status warning screen (low uranium alert, not balancing)
