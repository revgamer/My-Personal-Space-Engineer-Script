# IIM — Isy's Inventory Manager v2.9.5 — Unminified Reference

> **Study reference only — not runnable code.**
> Original script by Isy (v2.9.5, 2023-12-10).
> Guide: http://steamcommunity.com/sharedfiles/filedetails/?id=1226261795

---

## Overview

IIM is a full inventory manager that runs through 20 numbered steps per cycle. One step runs per `Update10` tick. Each step may set a "not done yet" flag to re-run on the next tick if the instruction limit is approaching.

---

## Sorting — How It Works

IIM sorts items by TypeId into named typed containers. The container name must contain the configured keyword.

### Container Keywords (configurable)

| Keyword (default) | Items sorted into it |
|---|---|
| `Ores` | `MyObjectBuilder_Ore` |
| `Ingots` | `MyObjectBuilder_Ingot` |
| `Components` | `MyObjectBuilder_Component` |
| `Tools` | `PhysicalGunObject`, `PhysicalObject`, `ConsumableItem`, `Datapad` |
| `Ammo` | `MyObjectBuilder_AmmoMagazine` |
| `Bottles` | `OxygenContainerObject`, `GasContainerObject` |

### How Items Are Moved

Step 6 (`SortCargo`) sweeps through all source inventories. For each item:

1. Find the first non-full destination container of the matching type
2. Call `TransferItemTo()` — one API call per item stack
3. If destination is full, log a warning and add type to missing set

```csharp
// Simplified sort pass
void SortCargo()
{
    if (subStage == 0) MoveType("Ore",                   oreContainers);
    if (subStage == 1) MoveType("Ingot",                 ingotContainers);
    if (subStage == 2) MoveType("Component",             componentContainers);
    if (subStage == 3) MoveType("PhysicalGunObject",     toolContainers);
    if (subStage == 4) MoveType("AmmoMagazine",          ammoContainers);
    if (subStage == 5) MoveType("OxygenContainerObject", bottleContainers);
    if (subStage == 6) MoveType("GasContainerObject",    bottleContainers);
    if (subStage == 7) MoveType("PhysicalObject",        toolContainers);
    if (subStage == 8) MoveType("ConsumableItem",        toolContainers);
    if (subStage == 9) MoveType("Datapad",               toolContainers);
    subStage++;
    if (subStage > 9) { subStage = 0; return true; }
    return false;
}
```

### What Blocks Are Skipped

- Blocks on `[No IIM]` grids — completely excluded
- Blocks on `[No Sorting]` grids — excluded from sorting (but reactors/generators still filled)
- Blocks with any `lockedContainerKeyword` in name (default: `Locked`, `Control Station`, etc.)
- Blocks with `[PROTECTED]` prefix (multi-PB protection)
- Blocks without a valid conveyor path (if `connectionCheck = true`)
- Welders/grinders/drills if `excludeWelders/Grinders/Drills = true`
- Parachutes and VendingMachines (hardcoded exclusion list)
- Different owner/faction blocks (shows warning if `showOwnerWarnings = true`)

---

## Internal Sorting

Step 8. Optional — `enableInternalSorting = false` by default.

Sorts items inside each inventory by a 2-character pattern:

| Quantifier | Direction | Result |
|---|---|---|
| `A` = amount | `a` = ascending | Least first |
| `A` | `d` = descending | Most first |
| `N` = name | `a` | A-Z |
| `N` | `d` | Z-A |
| `T` = type (alphabetical) | `a`/`d` | By TypeId string |
| `X` = type (count) | `a`/`d` | By type then amount |

Per-container override: add `(sort:Na)` to the container name. Works even if global internal sorting is off.

> **Warning:** Internal sorting can cause inventory desync in multiplayer. Use at your own risk.

---

## Priorities — Container Priority System

Containers are sorted into priority order. IIM uses a `[P<N>]` tag in the block name:

| Tag | Meaning |
|---|---|
| `[P1]` | Priority 1 (highest — filled first) |
| `[P2]` | Priority 2 |
| `[PMax]` | Highest possible priority |
| `[PMin]` | Lowest possible priority |
| *(none)* | Priority derived from EntityId (stable but arbitrary) |

The typed container lists are sorted by priority before sorting runs. Items always go to the highest-priority non-full container of the correct type.

---

## Automation — Auto Container Assignment

Step 4. Enabled by `autoContainerAssignment = true` and `assignNewContainers = true`.

When the sort step finds no containers for a type, it adds the type to `missingTypeSets`. Auto-assignment then renames the next available untagged cargo container by appending the matching keyword.

```csharp
// If no ore container exists and assignOres = true:
untaggedContainer.CustomName += " " + oreContainerKeyword;  // e.g. "Large Cargo Container Ores"
oreContainers.Add(untaggedContainer);
LogAction("Assigned '" + oldName + "' as new container for type 'Ores'.");
```

**Combined assignment options:**
- `oresIngotsInOne = true` — one container gets both `Ores` AND `Ingots` keywords
- `toolsAmmoBottlesInOne = true` — one container gets `Tools`, `Ammo`, AND `Bottles`

**Auto-unassignment** (`unassignEmptyContainers = true`): if a type has more than one container and one has been empty for a full cycle, it is marked for removal on the next cycle. Containers with `[P...]` tags are protected.

---

## Special Containers

Tag a container with `Special` in its name. IIM fills it with a user-configured loadout.

After renaming, IIM writes config to the container's Custom Data:

```
Special Container modes:
- Normal: stores wanted amount, removes excess. Usage: item=100
- Minimum: stores wanted amount, ignores excess. Usage: item=100M
- Limiter: doesn't store items, only removes excess. Usage: item=100L
- All: stores all items it can get until it's full. Usage: item=All

~~~~~~~~ Components ~~~~~~~~
SteelPlate=5000
Computer=1000
Motor=500
```

Step 5 (`FillSpecialContainers`) runs through each special container and moves items in/out to match quotas. `allowSpecialSteal = true` allows a higher-priority special container to pull from a lower-priority one.

---

## Bottle Refilling

Step 15. `fillBottles = true` by default.

IIM automatically refills gas bottles (O2 and H2) before storing them:

```csharp
// For each bottle found in any inventory:
// 1. Find a gas tank that is filled and has space in its inventory
// 2. Transfer bottle to tank inventory
// 3. Call tank.RefillBottles()
// 4. Transfer refilled bottle back to source
```

Bottles are refilled in-place before being sorted into the bottle container.

---

## Autocrafting

Step 11-13. `enableAutocrafting = true` by default.

### LCD Setup

Tag an LCD with the `autocraftingKeyword` (default: `Autocrafting`). IIM reads AND writes to it each cycle. Multi-LCD: name them `Autocrafting 1`, `Autocrafting 2`, etc.

### LCD Text Format

IIM writes the current amounts and reads the wanted amounts from the same LCD text:

```
Isy's Inventory Manager Autocrafting
=====================================

Component              Current | Wanted
SteelPlate             15237 = 70000
Computer                 891 = 10000A
Motor                   4521 = 15000
Display                   77 = 5000H
```

The text after `=` is the wanted amount + optional modifiers.

### Modifiers

Appended directly after the wanted number:

| Modifier | Meaning |
|---|---|
| `A` | Assemble only |
| `D` | Disassemble only |
| `P` | Priority — always queue first |
| `H` | Hide from display, manage via Custom Data |
| `I` | Ignore completely — hide and do not manage |
| `Y<N>` | Yield modifier — 1 craft yields N items |

Examples:
- `70000` — craft until 70,000 in stock
- `70000A` — craft only (never disassemble)
- `5000D` — disassemble if above 5,000
- `10000P` — craft, and always queue this first
- `0H` — manage at 0 quota but hide from display

### How IIM Queues Items

```csharp
// For each item needing crafting:
double needed = wantedAmount - currentStock;

// Apply margin: only craft if below (wanted - wanted * assembleMargin)
if (currentStock >= wantedAmount - wantedAmount * assembleMargin) continue;

// Find all capable assemblers (not tagged !disassemble-only, mode = Assembly)
var capable = assemblers.Where(a => a.CanUseBlueprint(blueprint));

// Split equally across all capable assemblers
double perAssembler = Math.Ceiling(needed / capable.Count());
foreach (var asm in capable)
    asm.InsertQueueItem(0, blueprint, perAssembler);  // InsertQueueItem(0) = insert at front if priority
```

### Blueprint Learning

IIM auto-guesses blueprints by trying common patterns:

```
MyObjectBuilder_BlueprintDefinition/{SubtypeId}
MyObjectBuilder_BlueprintDefinition/{SubtypeId}Component
MyObjectBuilder_BlueprintDefinition/{SubtypeId}Magazine
MyObjectBuilder_BlueprintDefinition/Position0010_{SubtypeId}
... up to Position0200_...
```

Manual learning via assembler tags:

| Tag | Behaviour |
|---|---|
| `!learn` | Learn next item in queue, then remove tag |
| `!learnMany` | Always in learning mode, never removed |

Blueprints are saved to PB Custom Data as `itemTypeId;blueprintId` pairs.

---

## Assembler Cleanup

Step 14. `enableAssemblerCleanup = true` by default.

If an assembler has no queue, or its input inventory is nearly full (> 95%), IIM moves the input inventory contents back to an ingot container.

```csharp
foreach (var assembler in allAssemblers)
{
    if (assembler.IsQueueEmpty || assembler.InputInventory.VolumeFillFactor > 0.95f)
    {
        IMyTerminalBlock dest = FindFreeContainer(assembler, ingotContainers);
        if (dest != null)
            MoveAllItems(assembler.InputInventory, dest.GetInventory(0));
    }
}
```

---

## Ore Balancing

Step 17. `enableOreBalancing = true` by default.

Balances ores evenly across all refineries by volume ratio. Then optionally sorts refinery queues so the most-needed ore is refined first.

### Queue Sorting (sortRefiningQueue)

```csharp
// For each refinery, check if the most-needed ore is at position 0
// Most needed = lowest (currentIngotStock / defaultYieldRatio)
// If not at position 0, move it there via TransferItemTo(self, fromIndex, 0)
```

### Script-Assisted Refinery Filling (enableScriptRefineryFilling)

IIM actively pushes ore into refineries instead of relying on conveyors:

1. Find which ore is most needed
2. Pull from ore containers into each refinery's input
3. If a refinery is full of wrong ore, empty it first
4. If one refinery is empty but another has ore, split between them

---

## Ice Balancing

Step 18. `enableIceBalancing = true` by default.

Balances ice evenly across all O2/H2 generators by volume. Leaves space for `spaceForBottles` bottles (default: 1 bottle = 0.12 kL).

Generators have `UseConveyorSystem = false` to prevent auto-pulling. IIM manually pushes/pulls ice to maintain balance.

---

## Uranium Balancing

Step 19. `enableUraniumBalancing = true` by default.

Balances uranium ingots across all reactors:

- Large grid reactors: `uraniumAmountLargeGrid` ingots each (default: 100)
- Small grid reactors: `uraniumAmountSmallGrid` ingots each (default: 25)

Reactors have `UseConveyorSystem = false`. IIM manually fills/drains each reactor to its target amount, then balances evenly if totals are uneven.

---

## Multi-PB Protection

If two grids are connected and both run IIM, one defers to the other. Priority is determined by:

1. `@GSIM-GridPriority=N` in PB Custom Data (explicit override)
2. Static grid = 10, large grid = 5, small grid = 1

The lower-priority PB pauses and outputs `pauseThisPB;name;grid` as its argument. Type containers on the paused PB get `[PROTECTED] ` prefix to prevent the other PB from draining them.

---

## LCD Keywords

| Keyword (configurable) | What it shows |
|---|---|
| `IIM-main` | Main status — container stats, managed blocks, last action |
| `IIM-warnings` | Current warnings and problems |
| `IIM-actions` | Action log with timestamps |
| `IIM-performance` | Instruction count, runtime, per-method breakdown |
| `IIM-inventory` | Custom inventory display (configurable per LCD) |
| `Autocrafting` | Autocrafting panel (also config input) |

---

## Key Differences vs AGM

| Feature | IIM | AGM |
|---|---|---|
| Container keywords | In block name | In block name (`{Ore 1}` etc.) |
| Priority system | `[P1]`, `[PMax]`, `[PMin]` | Number in tag `{Ore 1}` < `{Ore 2}` |
| Autocrafting config | In LCD text | In PB Custom Data quotas |
| Blueprint learning | Auto-guess + manual tag | Manual blueprint resolution |
| Refinery handling | Full ore balance + script fill | Priority list + queue sort |
| Uranium balance | Per-reactor target amount | Refinery priority only |
| Internal sorting | Optional, per-container override | Not implemented |
| Special containers | `Special` keyword + Custom Data quotas | Not implemented |
| Multi-PB protection | Priority-based pause system | Not implemented |
