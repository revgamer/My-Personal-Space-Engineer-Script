# AGM — AutoGrid Manager
## Project Reference & Developer Guide

**Author:** RevGamer  
**Build:** 0.4.0-test  
**Game:** Space Engineers (Programmable Block Script)  
**Script type:** Paste directly into PB editor — no `using`, namespace, or class wrapper needed.

---

## 1. Project Summary

AutoGrid Manager is a unified grid-management system for Space Engineers, combining:

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

## 2. Tags

| Tag | Used on | Purpose |
|---|---|---|
| `{AGM-Main}` | Programmable Block only | Enables PB branding screen, boot display, and status summary |
| `[AGM-S]` | External LCD panels | Current test-phase inventory / dashboard LCDs |
| `[AGM]`, `[LCD]` | Legacy | Avoid during testing — may cause flicker if old PBs are still running |

### PB naming
```
PB AutoGrid Manager {AGM-Main}
```

### LCD naming examples
```
LCD Components Left [AGM-S]
LCD Ore 1 [AGM-S]
LCD Ingots Status [AGM-S]
```

---

## 3. Run Arguments

| Argument | Effect |
|---|---|
| `reload` | Reload config + rescan + reindex + boot visual |
| `rescan` | Rescan blocks + boot visual |
| `reboot` | Show boot visual again without recompiling |
| `sort` | Trigger one sorting pass immediately |
| `reset` | Clear all state, rescan, reindex, boot visual |

---

## 4. Refresh Pacing

| Task | Interval |
|---|---|
| LCD draw | Every Update10 run (1 LCD per run ≈ 6 LCDs/sec) |
| Inventory index | Every 30 ticks ≈ 0.5 s |
| Block rescan | Every 300 ticks ≈ 5 s |
| Autocrafting check | Every 300 ticks ≈ 5 s |
| Sorting pass | Every 100 ticks ≈ 1.67 s (only if `enable_sorting=true`) |

---

## 5. PB Custom Data Config

Place in the **Programmable Block** Custom Data.

### Enable sorting
```ini
enable_sorting=true
```

### Power dashboard profile
```ini
[power:RAB Base]
batteries=G:[RAB] Batteries
reactors=G:[RAB] Nuclear Reactors
solar=
wind=
hydrogen=
include_ungrouped=false
```

### Fuel & life support profile
```ini
[LifeSupport:Base]
hydrogen=G:Hydrogen Tanks
oxygen=G:Oxygen Tanks
generators=G:O2 Generators
include_ungrouped=false
```

---

## 6. LCD Custom Data Commands

Place **one command** in each LCD's Custom Data (unless paging).

### Inventory screens

```
IndustrialInventory=Component
IndustrialInventory=Ore
IndustrialInventory=Ingot
IndustrialInventory=Ammo
```

With manual page number:
```
IndustrialInventory=Component:1
IndustrialInventory=Component:2
```

With manual page + rows per page:
```
IndustrialInventory=Component:1:14
IndustrialInventory=Component:2:14
```

### Wide (side-by-side) layout
```
IndustrialInventoryWide=Component:1:14:left
IndustrialInventoryWide=Component:2:14:right
```

Three LCDs:
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
Or without a named profile (uses all detected tanks/generators):
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

Short number notation accepted:
```
SteelPlate=20k
```

Multi-LCD autocrafting (page 1 holds the full quota list):
```
LCD Autocrafting [AGM-S] !LINK:A1    ← Custom Data has the full list
LCD Autocrafting [AGM-S] !LINK:A2    ← Custom Data can be empty (borrows from page 1)
```

### Sorter dashboard
```
SorterDashboard
```
Aliases: `Sorter`, `AutoSorter`

---

## 7. Cargo Container Tags (Sorting)

Add to cargo container **names**.

| Tag | Meaning |
|---|---|
| `[Ore]` / `[Ores]` | Ore destination |
| `[Ingot]` / `[Ingots]` | Ingot destination |
| `[Component]` / `[Components]` | Component destination |
| `[Ammo]` | Ammo destination |
| `[Tool]` / `[Tools]` | Tool destination |
| `[Bottle]` / `[Bottles]` | Bottle destination |
| `[Inventory]` | General fallback |
| `[GOAT]` | Compatibility fallback (treated same as `[Inventory]`) |
| `[Locked]` | Never move items to/from this container |
| `[Hidden]` | Exclude from quota/count logic |

IIM-style plain keywords (`Cargo Ores`, `Cargo Components`) also work without brackets.

### Auto-expansion behaviour
When all typed containers of a category are full, AGM will promote the first available `[Inventory]` or untagged cargo and assign it to that category. `[Locked]` and `[Hidden]` containers are never auto-assigned.

### Sorter sources
- Cargo containers (all inventories)
- Assembler output inventories (inventory index 1)
- Refinery output inventories (inventory index 1)

AGM **does not** drain assembler or refinery input inventories to avoid fighting active production.

### Recommended cargo names
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

## 8. IIM Sorter Comparison

Isy's Inventory Manager (IIM) is the most widely used SE inventory sorter. AGM's sorter follows similar concepts with some differences:

### What IIM does (from source analysis)

- Scans all inventories every cycle, building typed destination lists from keyword tags in block names (e.g. `"Ores"`, `"Ingots"`, `"Components"`).
- Uses a priority system via `[P1]`, `[P2]`, `[PMax]`, `[PMin]` tokens in names to control sort destination order.
- Moves items from any non-locked, non-hidden, non-special inventory to the correct type container using `TransferItemTo()`.
- Handles assembler and refinery output inventories (drains them if not actively producing).
- Supports `[Locked]` (never moved from), `[Hidden]` (excluded from counts), and `[Special]` (quota-managed stock container).
- Fills O2/H2 generators and reactors as a special case, independent of normal sorting.
- Balances items across multiple containers of the same type when `balanceTypeContainers = true`.
- Auto-assigns untagged containers when a category has no typed container yet.
- Protects type container names from being overwritten when another IIM PB is running on the same construct.

### AGM sorting differences

| Feature | IIM | AGM (current) |
|---|---|---|
| Transfers per pass | Unlimited per tick (instruction-limited) | `SORT_MOVES_PER_PASS = 2` max per cycle |
| Transfer batch size | Up to full stack | `SORT_MAX_TRANSFER_AMOUNT = 1000` per move |
| Source rotation | All sources every pass | Round-robin across sources (`sortSourceIndex`) |
| Assembler output drain | Yes (when queue empty) | Yes (inventory index 1) |
| Refinery output drain | Yes | Yes (inventory index 1) |
| Assembler input drain | No | No |
| Balance across same-type containers | Yes (optional) | Not yet implemented |
| Special/quota containers | Yes (full system) | Not yet implemented |
| Reactor/generator filling | Yes | Not yet implemented |
| Priority tokens | Yes (`[P1]` etc.) | Not yet implemented |

---

## 9. Item Categories

| Category string | TypeId match |
|---|---|
| `Ore` | ends with `_Ore` |
| `Ingot` | ends with `_Ingot` |
| `Component` | ends with `_Component` |
| `Ammo` | ends with `_AmmoMagazine` |
| `Tool` | ends with `_PhysicalGunObject` |
| `Bottle` | ends with `_GasContainerObject` or `_OxygenContainerObject` |

---

## 10. Item Sprite IDs

Used for icon drawing in stock boards.

Format: `TypeId/SubtypeId`

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

For modded / dynamic items use: `item.Type.TypeId + "/" + item.Type.SubtypeId`

### Drawing a sprite
```csharp
void TryDrawSprite(MySpriteDrawFrame frame, string spriteName, Vector2 center, Vector2 size, Color col)
{
    try
    {
        frame.Add(new MySprite(SpriteType.TEXTURE, spriteName, center, size, col));
    }
    catch
    {
        // sprite missing — draw fallback icon here if needed
    }
}
```

---

## 11. Visual Style

| Element | Value |
|---|---|
| Background | `new Color(13, 9, 5)` |
| Panel | `new Color(28, 19, 10)` |
| Panel 2 | `new Color(42, 29, 13)` |
| Accent (border/title) | `new Color(255, 174, 48)` |
| Accent 2 | `new Color(255, 213, 91)` |
| Text | `new Color(236, 218, 177)` |
| Dim text | `new Color(120, 94, 58)` |
| OK (green) | `new Color(75, 210, 120)` |
| Warning (amber) | `new Color(255, 142, 45)` |
| Low/error (red) | `new Color(226, 64, 45)` |

Stock board layout:
- Warm black/brown panel background
- Orange/yellow outer border
- Bold category title
- Item icon (sprite) on left
- Item name + current amount
- Optional quota text
- Progress bar on right
- `+ more` marker if rows overflow
- Clear page number indicator

Multi-LCD rule: all joined screens **must** use the same renderer and colour palette. No mismatched borders between screens.

---

## 12. PB Screen

### Boot / reboot display
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

### Viewport (required — do not omit)
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

---

## 13. Air Vent Monitoring

Only vents that opt in are monitored. Requirements:

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

A vent is considered OK when: working, can pressurize, oxygen level ≥ 95%.

---

## 14. Known Issues

| Issue | Cause | Fix |
|---|---|---|
| Flickering LCDs | Two scripts writing to same LCD | Use only `[AGM-S]`; disable old AGM/AutoLCD2 PBs |
| Screen not updating | Config change but refresh hasn't fired | Run `reload` or `rescan` |
| Page 2 not showing | Not enough items, or wrong row count | Force rows: `IndustrialInventory=Component:1:8` + `Component:2:8` |
| PB screen tiny/offset | Wrong viewport | Use `(TextureSize - SurfaceSize) * 0.5f` as origin |
| Blue HUD text over LCD | SE GPS/HUD overlay, not AGM | Not a script bug — turn off HUD |

---

## 15. Architecture Overview

```
Program()
  ├─ InitBlueprints()
  ├─ ReadProgramConfig()
  ├─ RescanBlocks()
  └─ StartBoot()

Main(argument, updateSource)
  ├─ Argument handling (reload / rescan / reboot / sort / reset)
  ├─ Tick counters
  │    ├─ RescanBlocks()       every RESCAN_TICKS (300)
  │    ├─ IndexInventory()     every INDEX_TICKS (30)
  │    ├─ ReadProgramConfig()  every INDEX_TICKS (30)
  │    ├─ ProcessAutocrafting() every CRAFT_TICKS (300)
  │    └─ ProcessSorting()     every SORT_TICKS (100) if enabled
  ├─ Boot animation (DrawBootAll / EchoBoot)
  └─ Normal run (DrawNextScreens / DrawPbStatus / EchoStatus)
```

---

## 16. Development Rules

1. Always generate a **full PB-ready `.cs` file** when changing code.
2. Keep `[AGM-S]` for test LCDs unless finalising tags.
3. Keep `{AGM-Main}` for PB screen only.
4. Do not reuse `[LCD]` during testing.
5. Include LCD names and Custom Data examples with every code change.
6. Keep code paste-ready for the in-game PB editor (no `using`, no `namespace`, no outer `Program` class).
7. Preserve `RevGamer` as author.
8. Note if a build has not been tested inside the in-game PB editor.

---

## 17. Roadmap (Near-Term)

### Display
- Finalise StockBoard style (row spacing, icon scale, text alignment, page number)
- True wide/vertical join borders
- Optional title only on first screen, footer only on last
- PB screen themes: Classic / Industrial / Minimal

### Inventory / quotas
- Quota parser from LCD Custom Data (`SteelPlate: 300K`)
- Global quota config in PB Custom Data `[quotas]` section
- Missing stock screens: `IndustrialMissing=Component`
- Search/filter screen: `FindItem=SteelPlate`

### Sorting
- Balance items across same-type containers
- Special/quota stock containers with Custom Data config
- Priority tokens `[P1]`, `[PMax]`, `[PMin]`
- Reactor uranium fill
- O2/H2 generator ice fill

### Production
- Blueprint learning for modded items
- Refinery manager (ore priority, uranium protection, ice handling)
- Production status LCD

### Power / system
- Connector / dock screen
- Cargo fill dashboard
- Reactor refuel command
