# GOAT Sorter v1.2.1 — Unminified Reference

> **Study reference only — not runnable code.**
> Original script by Khodrin, Documentation by Katarina_Valenxia.
> Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=3581915365

---

## Overview

GOAT Sorter is a full inventory manager with auto-sorting, autocrafting, refinery handling, and multi-LCD display support. It uses a coroutine pattern — two `IEnumerator<bool>` loops (`mainLoop` and `displayLoop`) that each yield at fixed points so the script never hits the instruction limit in a single tick.

---

## Update Frequency

```csharp
Runtime.UpdateFrequency = UpdateFrequency.Update1 | UpdateFrequency.Update10;
```

- `Update1` — runs `mainLoop.MoveNext()` every tick (throttled by `VTEC` and dynamic speed)
- `Update10` — runs `displayLoop.MoveNext()` every 10 ticks, but only when main loop is not in certain heavy stages

---

## Main Loop Stages

The main loop (`e()`) runs through stages numbered by the `Ò` variable. Each stage yields frequently to spread CPU load.

| Stage | Purpose |
|---|---|
| 1 | Update grid — scan all blocks, connectors, mechanical connections |
| 2 | Manage conveyor disable on refineries (UseConveyorSystem = false) |
| 3 | Read item index from PB Custom Data |
| 4 | Update managed containers — read [Stock] container quotas |
| 5 | Learn blueprints from assemblers tagged `is Learning` |
| 6 | Learn ore yields from refineries |
| 7 | Update item database from external sources |
| 8 | Write item index back to PB Custom Data |
| 9 | Sort items — main sorting pass |
| 10 | Update item counts across all inventories |
| 11 | Update assembler queue counts |
| 12 | Autocrafting / autodisassembling |
| 13 | Assembler cleanup — pull excess from input inventories |
| 14 | Refinery handling — ore routing |
| 15 | Refill gas bottles |
| 16 | Wait for MinCycle to elapse |
| 17 | Remove blueprints that no assembler can use |

---

## Block Discovery

Stage 1 scans all terminal blocks and classifies them:

```csharp
// Blocks that have inventory and an owner
GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(allBlocks, b => b.HasInventory && b.OwnerId != 0);

foreach (var block in allBlocks)
{
    // Skip [No GOAT] grids completely
    // Skip [No Sorting] connector grids (except special/reactor/generator)

    if (block is IMyTextSurfaceProvider && name has [GOAT] tag)
        multiLCDProviders.Add(block);

    if (block is IMyProductionBlock && (main grid or not SGP))
        managedProduction.Add(block);    // assemblers + refineries

    if (block is IMyRefinery && (main grid or not SGP))
        refineries.Add(block);

    if (block is IMyReactor)    reactors.Add(block);
    if (block is IMyGasGenerator) gasGenerators.Add(block);
    if (block is IMyBatteryBlock && main grid) batteries.Add(block);
    if (block is IMyGasTank && main grid) gasTanks.Add(block);

    // Inventory sources (for sorting)
    if (block has inventory && not in ITS exclusion tags && not ship tool if IBT=true)
        inventorySources.Add(block);
}
```

---

## Container System (Managed Containers)

GOAT uses a class called `Į` (Container Group) to represent typed containers defined by `[Stock]` tagged blocks or special setups. Each container group has:

- A priority `i`
- A list of inventories `ʵ`
- A quota dictionary `Ƭ` — maps `MyItemType` to wanted amount + mode

### Container Modes

Set in the [Stock] container's Custom Data:

| Suffix | Mode | Behaviour |
|---|---|---|
| `=100` (default) | Normal | Store up to 100, remove excess |
| `=100M` | Minimum | Store up to 100, ignore excess (don't pull out) |
| `=100L` | Limiter | Don't add items, remove anything above 100 |
| `=All` | All | Store everything until full |
| Append `P` | Pinned | Show at top of list in Custom Data |

### How Sorting Uses Container Groups

During stage 9 (sorting), for each item in each inventory:

```csharp
// Check if any managed container wants this item
foreach (var container in managedContainers)
{
    if (!container.WantsItem(sourceInv, item.Type)) continue;
    double wantedAmount = container.GetWantedAmount(item.Type, item.Amount, ref shouldForceMove);
    if (wantedAmount <= 0) continue;

    // If source is also a managed container, check if source should release it
    if (sourceIsManaged)
    {
        double releaseAmount = sourceContainer.GetExcess(item.Type, item.Amount, destContainer.Priority);
        if (releaseAmount == 0) continue;
        wantedAmount = Math.Min(wantedAmount, releaseAmount);
    }

    destContainer.TransferItem(sourceInv, itemIndex, wantedAmount);
}

// Then fall through to named type containers (Ores, Ingots, etc.)
if (!typeContainers.ContainsKey(item.Category)) continue;
foreach (var dest in typeContainers[item.Category])
{
    // Skip if source is same category and same or higher priority
    if (sourceIsTyped && sourceCategory == item.Category && sourcePriority >= dest.Priority) continue;
    sourceInv.TransferItemTo(dest.Inventory, itemIndex, null, true);
}
```

---

## Autocrafting

### How It Works

GOAT autocrafting compares current stock to wanted quotas set in the `[AutoCrafting]` LCD panel text. It then queues or disassembles items across all available assemblers.

### Autocrafting LCD Setup

Tag an LCD with `[AutoCrafting]` (configurable via `ACT`). The LCD text acts as both display AND config:

```
~ Header comments (lines starting with ~ are ignored)
~ ItemName~~~~<current>/<wanted><modifiers>

SteelPlate~~~~15000/70000A
Computer~~~~891/10000A
Motor~~~~4521/15000A
```

The script reads the `/<wanted>` value and modifiers from each line. It writes back the current amounts every cycle.

### Autocrafting Modifiers

Appended after the wanted amount:

| Modifier | Meaning |
|---|---|
| `A` | Assemble only (default behaviour — craft if below quota) |
| `D` | Disassemble only (disassemble if above quota) |
| `H` | Hide from display but still manage in Custom Data |
| `[Y:N]` | Yield modifier — 1 craft produces N items |
| `[P:N]` | Manual priority — lower N = queued first |

Modifiers can be combined: `70000AD` = assemble AND disassemble to maintain exact quota.

### Autocrafting Logic (Stage 12)

```csharp
// Build list of items that need crafting or disassembling
List<CraftingTask> tasks = new List<CraftingTask>();

foreach (var item in itemDatabase)
{
    if (!item.HasBlueprint) continue;

    double needed = item.WantedAmount - item.CurrentStock - item.AlreadyQueued;
    if (needed == 0) continue;

    float priority = item.ManualPriority != 0
        ? item.ManualPriority * -1f
        : (item.CurrentStock / item.AlreadyQueued);

    tasks.Add(new CraftingTask(item.Blueprint, needed, priority, item));
}

// Sort by priority
tasks = tasks.OrderBy(t => t.Priority).ToList();
if (RCP) tasks.Reverse();  // RCP = Reverse Crafting Priority setting

// Clear all assembler queues
foreach (var asm in managedAssemblers)
    asm.ClearQueue();

// Distribute tasks across assemblers that can handle them
foreach (var task in tasks)
{
    if (task.Amount > 0)  // need to craft
    {
        var capable = assemblers.Where(a => a.CanUseBlueprint(task.Blueprint) && !a.IsDisassembling);
        int count = capable.Count();
        foreach (var asm in capable)
        {
            int share = Math.Min(Math.Min((int)Math.Ceiling(task.Amount / count), task.Amount), task.Remaining);
            if (share <= 0) continue;
            asm.AddQueueItem(task.Blueprint, (MyFixedPoint)share);
            task.Remaining -= share;
        }
    }
    else if (task.Amount < 0)  // need to disassemble
    {
        var capable = assemblers.Where(a => a.CanUseBlueprint(task.Blueprint) && a.Mode == Disassembly);
        // same distribution logic
    }
}
```

### Blueprint Learning

GOAT can learn new blueprints from modded items via two assembler tags:

| Tag | Behaviour |
|---|---|
| `is Learning` | Queues 1000 of the item, learns blueprint, removes tag |
| `!learnMany` (GSIM) | Learns everything queued, never removed from learning mode |

The script also integrates with the **GSIM mod** (`GSIMIntegration_filteredBlueprintLib` property) to auto-import modded blueprints without manual learning.

---

## Refinery Handling (Stage 14)

GOAT refinery handling does three things:

1. **Clear refineries** — empty input inventory if wrong ore is loaded
2. **Fill refineries** — push the most-needed ore into each refinery
3. **Balance between refineries** — if one refinery is empty, pull ore from a full one

### Ore Priority

GOAT calculates which ore is most needed by:

```csharp
// For each ore, calculate: (current ingot stock) / (ore-to-ingot yield ratio)
// Ore with lowest ratio = most needed = refine first
var sortedOres = ores
    .OrderBy(ore => currentIngotStock[ore] / yieldRatio[ore])
    .ToList();

// Stone and Scrap always go last (or first if RSF=true for Scrap)
```

### Ore Yield Learning

GOAT measures actual ore yields by watching a refinery's input/output:

- Pushes 100 units of ore into a learning refinery
- Waits for refinery to finish
- Reads output amounts
- Stores yield per 100 units in PB Custom Data under `#### Ore Yields ####`

---

## Reactor and Gas Generator Balancing

### Reactor Uranium (MRs = true)

GOAT creates a managed container group for reactors with priority 100. Each reactor gets a quota of:

```csharp
int uraniumQuota = (int)Math.Round((float)reactor.GetInventory().MaxVolume * UA);
// UA = amount of uranium per 1000L of reactor volume (default: 25)
```

Reactors have `UseConveyorSystem = false` set to prevent them pulling more uranium.

### Gas Generator Ice (MGG = true)

Same pattern — ice quota per generator = `generator.MaxVolume * GGI` (default 2500 per 1000L).

---

## Container Level LCDs

Tag an LCD with `[ContainerLevel]` (or via multi-LCD Custom Data `@N GOAT-Level`). The script draws a container fill bar on it showing a nearby cargo container's fill level. Config is stored in the LCD's text (written by GOAT):

```
ContainerID=12345678
Name=My Container
Icon=SteelPlate
Icon2=
Style=1
Font=White
```

Styles 1-3 are compact (32px icon), styles 4-6 are large (64px icon).

---

## Display Loop

The display loop (`g()`) runs separately from the main loop and handles all LCD rendering:

| Stage | Output |
|---|---|
| 1 | Status screens (`[StatusScreen]`) — inventory bar overview |
| 2 | Build autocrafting panel text |
| 3 | Write autocrafting text to `[AutoCrafting]` LCD |
| 4-8 | Write autocrafting pages to extension LCDs |
| 10 | Container Level LCDs |
| 11 | Inventory LCDs (`[Inventory]`) |
| 12 | Custom inventory screen rendering from per-LCD config |

---

## Inventory LCD Format

Tag an LCD with `[Inventory]` and configure via its text (written and read by GOAT):

```
Margin=16
Size=0.7
StockStats
StockLevel,Color=2
StockVolume,Color=1
```

Supported commands per line:

| Command | Output |
|---|---|
| `StockStats` | "Managed Containers: N \| LastUpdate: HH:mm:ss" |
| `StockLevel,Color=N` | Progress bar showing fill level (quota-based) |
| `StockVolume,Color=N` | Progress bar showing volume fill |
| `StockLevelFull,Color=N` | Full-height bar for fill level |
| `StockVolumeFull,Color=N` | Full-height bar for volume |
| `StockList=Name` | Scrolling list of items vs quotas |

---

## Why Autocrafting May Not Work in AGM Context

GOAT autocrafting requires:

1. **An `[AutoCrafting]` LCD** — the text file IS the config. Without it, no quotas are set.
2. **`AC = true`** (default) — master switch.
3. **Items must appear in item database** — GOAT only knows items it has seen in an inventory.
4. **Assemblers must not have `!GSIM-Manual` or `[Manual]` tag** — otherwise excluded.
5. **`RH = true` for refinery handling** — refineries need `UseConveyorSystem = false`.
6. **Blueprint must be known** — either from the internal library (vanilla items) or learned via `is Learning`.

The wanted amount is set IN the `[AutoCrafting]` LCD text — not in the PB Custom Data. If the LCD shows `SteelPlate~~~~0/0`, the quota is 0 and nothing will be crafted.
