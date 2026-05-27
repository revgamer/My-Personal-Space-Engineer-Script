# CLAUDE.md — AGM AutoGrid Manager

## Project context

- **Author:** RevGamer
- **Build:** 0.4.1
- **Game:** Space Engineers — Programmable Block script
- **Paste directly** into the in-game PB editor. No `using`, no `namespace`, no outer `Program` class wrapper.

---

## Files

| File | Purpose |
|---|---|
| `AGM.cs` | Full PB-ready script (current working build) |
| `AGM.md` | Full project reference — read this first in every new session |
| `CLAUDE.md` | This file — session change log and rules |
| `AGM_Reference.md` | Extended reference doc (generated) |
| `IIM_Sorter_Reference.md` | IIM sorter study reference (not runnable) |

---

## All fixes applied across sessions

### Session 1 — Sorter not working

**Bug 1 — SORT_MOVES_PER_PASS = 2**
Only 2 stacks moved per 1.67s. Changed to 50.

**Bug 2 — Loop stopped after 2 moves total**
while condition bailed out immediately. Replaced with do/while that visits every source.

**Bug 3 — break after first item per source**
Only 1 item sorted per container per pass. Removed. Now iterates backwards through all items.

**Bug 4 — DrawSorterDashboard called BuildSortCargos every frame**
Cleared sortSources/sortCargos on every LCD refresh. Removed. Dashboard does lazy init only.

**Bug 5 — Single-inventory non-cargo blocks skipped**
Connectors, welders, drills invisible to sorter. Fixed — added as sources with category = "".

**Bug 6 — source.Category skip condition too broad**
Skipped items even in general [Inventory] containers. Fixed — only skip when source is typed AND type matches.

**Bug 7 — Full containers not skipped**
Now checks HasInventorySpace (98% threshold) before transfer. Tries next container if full.

**Bug 8 — Transfer method slow (4 calls per item)**
Fixed to IIM method: TransferItemTo(dest, i, null, true) — entire stack in one call. Fall back to batches only if it fails.

**Bug 9 — Route text truncated with ..**
ShortBlockName truncated to 24 chars. Fixed — full block names shown.

---

### Session 2 — Docked ships

**Feature: include_docked_grids**
Added include_docked_grids=true config key. When enabled:
- dockedSourceBlocks populated with all blocks on non-same-construct grids
- Added as sort sources only — never as destinations
- Ship cargo pulled to base typed containers
- [Locked]/[Hidden] respected on ship containers
- Reactors/generators/tanks excluded from docked sources too

---

### Session 3 — Reactors cycling on/off

**Root cause — sorter pulling uranium out of reactors**
Sorter treated reactors as single-inventory non-cargo sources, found uranium ingots, moved them to [Ingot] containers. Reactors pulled uranium back via conveyor. Infinite loop every 1.67s.

**Fix — exclude reactors from sort sources**
Added IMyReactor / IMyGasGenerator / IMyGasTank exclusion in BuildSortCargos for both base grid and docked grid sweeps.

---

### Session 3b — Uranium balancing (added then removed)

Attempted IIM-style uranium balancing. Caused reactors to cycle because:
1. UseConveyorSystem = false set before checking if uranium existed
2. Docked ship ingot containers interfering
3. Fill and drain passes in same tick fighting each other

**Decision: removed uranium balancing entirely.**
SE native conveyor system handles reactor uranium reliably. Leave UseConveyorSystem = true on all reactors.

---

## Rules for future sessions

1. Always generate a full PB-ready .cs file when changing code
2. Keep [AGM-S] for test LCDs unless finalising tags
3. Use the four live PB tags: {AGM-Core}, {AGM-Power}, {AGM-Logistics}, {AGM-Production}
4. Keep wall LCD rendering in AGM_Core.cs unless explicitly asked otherwise
5. Paste-ready for SE PB editor — no using, no namespace, no outer class
6. Preserve RevGamer as author
7. Note if build has not been tested in-game
8. After every code change verify: wc -c AGM.cs must be under 100,000
9. Never add uranium/reactor/fuel balancing — SE handles it natively
10. Never remove IMyReactor/IMyGasGenerator/IMyGasTank from sort-source exclusion list

---

## Current config (RevGamer RAB base)

```
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

---

## Known working state (0.4.1)

- Sorter routes items to correct typed container
- Skips full containers, tries next of same type
- Promotes [Inventory] fallback when all typed containers full
- Docked ship cargo pulled to base (include_docked_grids=true)
- Reactor/generator/tank inventories never touched by sorter
- Dashboard shows live fill, type counts, last move route (full names, no truncation)
- Character count: ~94,700 (well under 100,000 limit)
