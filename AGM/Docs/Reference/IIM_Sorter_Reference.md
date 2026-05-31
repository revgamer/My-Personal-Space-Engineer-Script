# IIM Sorter — Unminified Reference

> **Study reference only — not runnable code.**
> Original script by Isy (v2.9.5, 2023-12-10).
> This document extracts and annotates the core sorting pipeline from the obfuscated/minified IIM source.

---

## How IIM Sorting Works — Overview

Each `Main()` cycle IIM runs through 20 numbered steps (`Ȯ` = step counter). Steps run one per `Update10` tick unless a step signals "not done yet" by setting the `ȭ` flag (causes same step to re-run next tick).

| Step | Function | Purpose |
|---|---|---|
| 0 | `RescanInventories()` | Builds all block/inventory lists |
| 1 | `FindNewItems()` | Discovers new item types in inventories |
| 3 | `NameCorrection()` | Fixes capitalisation of keywords |
| 4 | `ContainerAssignment()` | Auto-assigns untyped containers |
| 5 | `FillSpecialContainers()` | Fills `[Special]` quota containers first |
| **6** | **`SortCargo()`** | **THE MAIN SORTER** |
| 7 | `BalanceContainers()` | Optional: balance amounts across same-type containers |

---

## Block List Declarations

All populated by `RescanInventories()` at step 0.

```csharp
// Typed destination container lists (blocks whose names contain the matching keyword)
List<IMyTerminalBlock> oreContainers       = new List<IMyTerminalBlock>();  // name has "Ores"
List<IMyTerminalBlock> ingotContainers     = new List<IMyTerminalBlock>();  // name has "Ingots"
List<IMyTerminalBlock> componentContainers = new List<IMyTerminalBlock>();  // name has "Components"
List<IMyTerminalBlock> toolContainers      = new List<IMyTerminalBlock>();  // name has "Tools"
List<IMyTerminalBlock> ammoContainers      = new List<IMyTerminalBlock>();  // name has "Ammo"
List<IMyTerminalBlock> bottleContainers    = new List<IMyTerminalBlock>();  // name has "Bottles"
List<IMyTerminalBlock> specialContainers   = new List<IMyTerminalBlock>();  // name has "Special"
List<IMyTerminalBlock> typeContainers      = new List<IMyTerminalBlock>();  // union of all above
List<IMyTerminalBlock> untaggedCargo       = new List<IMyTerminalBlock>();  // no type tag, not locked

// Source lists — inventories that may contain wrongly-placed items
List<IMyTerminalBlock> allInventories      = new List<IMyTerminalBlock>();  // every inventory
List<IMyTerminalBlock> containsItems       = new List<IMyTerminalBlock>();  // inventories with items
List<IMyAssembler>     assemblerOutput     = new List<IMyAssembler>();      // assemblers with output
List<IMyRefinery>      refineryOutput      = new List<IMyRefinery>();       // refineries with output

// Exclusion sets
HashSet<IMyCubeGrid>   noSortingGrids      = new HashSet<IMyCubeGrid>();   // grids tagged [No Sorting]
HashSet<IMyCubeGrid>   noIIMGrids          = new HashSet<IMyCubeGrid>();   // grids tagged [No IIM]
```

---

## Step 0 — RescanInventories()

Clears all lists and rebuilds from `GridTerminalSystem`. Also called when blocks change, at boot, and before each sort cycle.

```csharp
void RescanInventories()
{
    // Any grid connected via a connector named [No Sorting] or [No IIM]
    // is added to exclusion sets and skipped during sorting.
    BuildExclusionSets();

    // IIM keeps one reference inventory to check conveyor reachability.
    // Used only when connectionCheck = true.
    RebuildConnectionReference();

    // Clear all lists
    typeContainers.Clear();
    oreContainers.Clear();
    // ... (all lists)

    // Scan all blocks that have inventories, owned by correct faction
    List<IMyTerminalBlock> blocksWithInventory = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(
        blocksWithInventory,
        block => block.HasInventory && block.OwnerId != 0
    );

    // Classify in batches of 200 to avoid hitting instruction limit.
    // If 200 processed and more remain, set notFinished = true to continue next tick.
    for (int i = startIndex; i < blocksWithInventory.Count; i++)
    {
        ClassifyBlock(blocksWithInventory[i]);
        startIndex++;
        if (i % 200 == 0) { notFinished = true; return; }
    }

    // Sort typed container lists by priority tokens [P1], [P2], [PMax], [PMin]
    // Blocks without a priority token get a value derived from their EntityId.
    SortByPriority(oreContainers);
    SortByPriority(ingotContainers);
    // ...

    // Untagged cargo sorted largest-volume-first so big containers receive items first
    untaggedCargo.Sort((a, b) =>
        b.GetInventory().MaxVolume.ToIntSafe()
         .CompareTo(a.GetInventory().MaxVolume.ToIntSafe())
    );
}
```

---

## ClassifyBlock(block)

Decides which lists a block goes into based on its name and type.

```csharp
void ClassifyBlock(IMyTerminalBlock block)
{
    // Skip excluded grids ([No IIM])
    foreach (var excludedGrid in noIIMGrids)
        if (block.CubeGrid.IsSameConstructAs(excludedGrid)) return;

    // Skip excluded block types (Parachute, VendingMachine, etc.)
    foreach (var excludedType in excludedBlocks)
        if (block.BlockDefinition.SubtypeId.Contains(excludedType)) return;

    // Skip blocks with no conveyor access (optional)
    if (!HasConveyorAccess(block))
    {
        if (showNoConveyorTag) TagBlock(block, "[No Conveyor]");
        return;
    }

    // Skip welders / grinders / drills if excluded
    if (block is IMyShipWelder  && excludeWelders)  return;
    if (block is IMyShipGrinder && excludeGrinders) return;
    if (block is IMyShipDrill   && excludeDrills)   return;

    // Skip [PROTECTED] containers (multi-PB protection)
    if (block.CustomName.Contains("[PROTECTED] "))
    {
        protectedTypeContainers.Add(block);
        return;
    }

    string name = block.CustomName;

    bool isSpecial = name.Contains(specialContainerKeyword);
    bool isLocked  = false;
    bool isManual  = false;
    bool isHidden  = false;
    bool isTyped   = false;

    foreach (var kw in lockedContainerKeywords)   if (name.Contains(kw)) { isLocked = true; break; }
    foreach (var kw in manualMachineKeywords)      if (name.Contains(kw)) { isManual = true; break; }
    foreach (var kw in hiddenContainerKeywords)    if (name.Contains(kw)) { isHidden = true; break; }

    if (!block.ShowInInventory && treatNotShownAsHidden) isHidden = true;

    if (!isHidden) allInventories.Add(block);

    // Assign to typed destination lists
    if (name.Contains(oreContainerKeyword))       { oreContainers.Add(block);       isTyped = true; }
    if (name.Contains(ingotContainerKeyword))     { ingotContainers.Add(block);     isTyped = true; }
    if (name.Contains(componentContainerKeyword)) { componentContainers.Add(block); isTyped = true; }
    if (name.Contains(toolContainerKeyword))      { toolContainers.Add(block);      isTyped = true; }
    if (name.Contains(ammoContainerKeyword))      { ammoContainers.Add(block);      isTyped = true; }
    if (name.Contains(bottleContainerKeyword))    { bottleContainers.Add(block);    isTyped = true; }

    if (isTyped) typeContainers.Add(block);

    // Special machines handled separately
    if (block is IMyRefinery)        HandleRefineryClassification(...);
    else if (block is IMyAssembler)  HandleAssemblerClassification(...);
    else if (block is IMyReactor)    HandleReactorClassification(...);
    else if (block is IMyCargoContainer)
    {
        // Untagged, unlocked, non-special → candidate for auto-assignment
        if (block.IsSameConstructAs(Me) && !isTyped && !isLocked && !isSpecial)
            untaggedCargo.Add(block);
    }

    // Any single-inventory block (not special, not locked, not reactor)
    // with items becomes a sort source
    if (block.InventoryCount == 1 && !isSpecial && !isLocked && !(block is IMyReactor))
        if (block.GetInventory(0).ItemCount > 0)
            containsItems.Add(block);
}
```

---

## Step 6 — SortCargo()

The main sorter. Runs in 10 sub-stages spread over multiple ticks to avoid instruction limits.

```csharp
bool SortCargo()
{
    // Each sub-stage handles one item TypeId string
    if (subStage == 0) MoveItemsToTypedContainer("Ore",                   oreContainers,       oreContainerKeyword);
    if (subStage == 1) MoveItemsToTypedContainer("Ingot",                 ingotContainers,     ingotContainerKeyword);
    if (subStage == 2) MoveItemsToTypedContainer("Component",             componentContainers, componentContainerKeyword);
    if (subStage == 3) MoveItemsToTypedContainer("PhysicalGunObject",     toolContainers,      toolContainerKeyword);
    if (subStage == 4) MoveItemsToTypedContainer("AmmoMagazine",          ammoContainers,      ammoContainerKeyword);
    if (subStage == 5) MoveItemsToTypedContainer("OxygenContainerObject", bottleContainers,    bottleContainerKeyword);
    if (subStage == 6) MoveItemsToTypedContainer("GasContainerObject",    bottleContainers,    bottleContainerKeyword);
    if (subStage == 7) MoveItemsToTypedContainer("PhysicalObject",        toolContainers,      toolContainerKeyword);
    if (subStage == 8) MoveItemsToTypedContainer("ConsumableItem",        toolContainers,      toolContainerKeyword);
    if (subStage == 9) MoveItemsToTypedContainer("Datapad",               toolContainers,      toolContainerKeyword);

    subStage++;
    if (subStage > 9) { subStage = 0; return true; }
    return false;
}
```

---

## MoveItemsToTypedContainer()

Finds the best destination for a given TypeId, then sweeps all sources.

```csharp
void MoveItemsToTypedContainer(string typeString, List<IMyTerminalBlock> destinations, string destinationKeyword)
{
    if (destinations.Count == 0)
    {
        LogWarning("No containers for type '" + destinationKeyword + "'!");
        missingTypeSets.Add(typeString);
        return;
    }

    // Find first non-full destination (list already sorted by priority)
    IMyTerminalBlock bestDest = null;
    for (int i = 0; i < destinations.Count; i++)
    {
        if (IsInventoryNotFull(destinations[i].GetInventory(0)))
        {
            bestDest = destinations[i];
            break;
        }
    }

    if (bestDest == null)
    {
        LogWarning("All containers for type '" + destinationKeyword + "' are full!");
        missingTypeSets.Add(typeString);
        return;
    }

    // Sweep containsItems (all inventories that currently hold items)
    for (int i = 0; i < containsItems.Count; i++)
    {
        IMyTerminalBlock source = containsItems[i];
        if (source == bestDest) continue;
        // Skip if already in correct container with higher/equal priority
        if (source.CustomName.Contains(destinationKeyword) && GetPriority(source) <= GetPriority(bestDest)) continue;
        // Skip if balancing same-type containers this pass
        if (source.CustomName.Contains(destinationKeyword) && balanceTypeContainers) continue;

        TransferMatchingItems(typeString, source, 0, bestDest, 0);
    }

    // Sweep refinery output inventories (slot 1)
    for (int i = 0; i < refineryOutput.Count; i++)
        TransferMatchingItems(typeString, refineryOutput[i], 1, bestDest, 0);

    // Sweep assembler output inventories (slot 1) — skip if actively disassembling
    for (int i = 0; i < assemblerOutput.Count; i++)
    {
        IMyAssembler asm = assemblerOutput[i];
        if (asm.Mode == MyAssemblerMode.Disassembly && asm.IsProducing) continue;
        TransferMatchingItems(typeString, asm, 1, bestDest, 0);
    }
}
```

---

## TransferMatchingItems()

The core transfer function. Iterates source backwards, finds items matching typeFilter, calls `TransferItemTo()`.

**Key IIM insight: uses `TransferItemTo(dest, i, null, true)` with NO amount parameter — transfers entire stack in one call. Much faster than batching.**

```csharp
double TransferMatchingItems(string typeFilter, IMyTerminalBlock sourceBlock, int sourceSlot,
                              IMyTerminalBlock destBlock, int destSlot, double maxAmount = -1)
{
    var sourceInv = sourceBlock.GetInventory(sourceSlot);
    var destInv   = destBlock.GetInventory(destSlot);

    if (destInv.IsFull || maxAmount == 0) return 0.0;

    var items = new List<MyInventoryItem>();
    sourceInv.GetItems(items);

    double totalTransferred = 0.0;
    double remaining = maxAmount;

    // Iterate backwards — index stays valid after transfer removes/shrinks items
    for (int i = items.Count - 1; i >= 0; i--)
    {
        if (!items[i].Type.ToString().Contains(typeFilter)) continue;

        if (!sourceInv.CanTransferItemTo(destInv, items[i].Type))
        {
            LogWarning("No conveyor path!");
            return 0.0;
        }

        if (maxAmount == -1)
        {
            // KEY IIM METHOD: transfer entire stack, no amount = single API call
            sourceInv.TransferItemTo(destInv, i, null, true);
        }
        else if (maxAmount == -0.5)
        {
            double half = Math.Ceiling((double)items[i].Amount / 2.0);
            sourceInv.TransferItemTo(destInv, i, null, true, (VRage.MyFixedPoint)half);
        }
        else
        {
            sourceInv.TransferItemTo(destInv, i, null, true, (VRage.MyFixedPoint)remaining);
        }

        // Refresh list and read actual moved amount
        double before = (double)items[i].Amount;
        items.Clear();
        sourceInv.GetItems(items);
        double after = (i < items.Count) ? (double)items[i].Amount : 0.0;
        double moved = before - after;

        totalTransferred += moved;
        remaining -= moved;
        if (remaining <= 0 && maxAmount >= 0) break;
        if (!IsInventoryNotFull(destInv)) break;
    }

    return totalTransferred;
}
```

---

## Step 4 — Auto Container Assignment

When a category has no typed destination, IIM renames the next untagged cargo container.

```csharp
void AutoAssignContainers()
{
    // missingTypeSets populated during SortCargo when destinations.Count == 0
    for (int i = 0; i < untaggedCargo.Count; i++)
    {
        IMyTerminalBlock cargo = untaggedCargo[i];

        if (assignOres && (oreContainers.Count == 0 || missingType == "Ore"))
        {
            if (oresIngotsInOne)
            {
                cargo.CustomName += " " + oreContainerKeyword;
                cargo.CustomName += " " + ingotContainerKeyword;
            }
            else
            {
                cargo.CustomName += " " + oreContainerKeyword;
            }
        }
        // ... same pattern for each category
    }
    missingTypeSets.Clear();
}
```

---

## Step 7 — BalanceTypeContainers() (optional)

Redistributes items evenly across all containers of the same type.

```csharp
void BalanceTypeContainers()
{
    // One category per sub-stage
    if (balanceSubStage == 0) BalanceCategory(oreContainers,       "Ore");
    if (balanceSubStage == 1) BalanceCategory(ingotContainers,     "Ingot");
    // ...
}

void BalanceCategory(List<IMyTerminalBlock> containers, string typeFilter)
{
    // Remove multi-inventory blocks and sub-grid containers
    var singleCargo = new List<IMyTerminalBlock>(containers);
    singleCargo.RemoveAll(b => b.InventoryCount == 2);
    singleCargo.RemoveAll(b => !b.CubeGrid.IsSameConstructAs(Me.CubeGrid));
    if (singleCargo.Count < 2) return;

    // Calculate total of each item type across all containers
    var totals  = new Dictionary<MyItemType, double>();
    // ... (GetItems loop)

    // Target = total / containerCount
    var targets = new Dictionary<MyItemType, double>();
    foreach (var pair in totals)
        targets[pair.Key] = (int)(pair.Value / singleCargo.Count);

    // For each container: if over target, push surplus to an under-target container
    for (int srcIdx = 0; srcIdx < singleCargo.Count; srcIdx++)
    {
        foreach (var pair in totals)
        {
            double currentHere = GetItemAmount(pair.Key, singleCargo[srcIdx]);
            double target      = targets[pair.Key];
            if (currentHere <= target + 1.0) continue;

            for (int dstIdx = 0; dstIdx < singleCargo.Count; dstIdx++)
            {
                if (srcIdx == dstIdx) continue;
                double currentThere = GetItemAmount(pair.Key, singleCargo[dstIdx]);
                if (currentThere >= target - 1.0) continue;

                double amount = Math.Min(target - currentThere, currentHere - target);
                currentHere -= TransferMatchingItems(pair.Key.ToString(),
                    singleCargo[srcIdx], 0, singleCargo[dstIdx], 0, amount, exactMatch: true);
            }
        }
    }
}
```

---

## Helper: GetPriority(block)

```csharp
int GetPriority(IMyTerminalBlock block)
{
    string token = Regex.Match(block.CustomName, @"\[p(\d+|max|min)\]",
        RegexOptions.IgnoreCase).Groups[1].Value.ToLower();

    if (token == "max") return int.MinValue;
    if (token == "min") return int.MaxValue;

    int priority;
    if (!Int32.TryParse(token, out priority))
    {
        // No explicit token — derive from EntityId
        // Same construct gets smaller prefix → higher priority
        string prefix = block.IsSameConstructAs(Me) ? "" : "1";
        Int32.TryParse(prefix + block.EntityId.ToString().Substring(0, 4), out priority);
    }
    return priority;
}
```

---

## Helper: IsInventoryNotFull(inventory)

IIM uses a 98% fill threshold. For very large containers, uses an absolute 500L buffer.

```csharp
bool IsInventoryNotFull(IMyInventory inventory)
{
    float current = (float)inventory.CurrentVolume;
    float max     = (float)inventory.MaxVolume;

    // Large container: use absolute buffer (inventoryFullBuffer in kL)
    if (max * 0.02 < inventoryFullBuffer)
        return current < max * 0.98;
    else
        return current < max - (float)inventoryFullBuffer;
}
```

---

## Step 4b — UnassignEmptyContainers()

When a typed container has been empty for a while, removes its type keyword so it can be reused.

```csharp
void TryUnassign(List<IMyTerminalBlock> containers, string keyword)
{
    // Need at least 2 containers before unassigning one
    if (containers.Count <= 1) return;

    // Two-tick process: flag candidate, then unassign next tick
    IMyTerminalBlock scheduled;
    if (pendingUnassignment.TryGetValue(keyword, out scheduled))
    {
        pendingUnassignment.Remove(keyword);
        if (scheduled.GetInventory(0).ItemCount == 0)
        {
            // Remove keyword and fill % from name using regex
            string newName = Regex.Replace(scheduled.CustomName, @"(" + keyword + @")", "");
            newName = Regex.Replace(newName, @"\(\d+\.?\d*\%\)", "");
            scheduled.CustomName = newName.TrimEnd(' ');
            typeContainers.Remove(scheduled);
        }
        return;
    }

    // Find empty containers without explicit [P...] priority
    int emptyCandidates = 0;
    IMyTerminalBlock candidate = null;
    foreach (var container in containers)
    {
        if (container.CustomName.Contains("[P")) continue;
        if (container.GetInventory(0).ItemCount == 0) { candidate = container; emptyCandidates++; }
    }

    // Only schedule if 2+ empty so at least one remains after unassign
    if (emptyCandidates > 1)
        pendingUnassignment[keyword] = candidate;
}
```

---

## Key Differences vs AGM Sorter

| Feature | IIM | AGM (0.4.1) |
|---|---|---|
| Transfers per pass | Unlimited (instruction-limited) | 50 cap per cycle |
| Transfer method | Full stack, no amount param | Full stack first, batch fallback |
| Sub-stages | 10 stages across 10 ticks | Single pass all categories |
| Balance across containers | Yes (optional) | Not implemented |
| Special/quota containers | Yes (full system) | Not implemented |
| Reactor/generator protection | Implicit (not added as sources) | Explicit `IMyReactor` exclusion |
| Priority tokens | Yes (`[P1]`, `[PMax]`) | Not implemented |
| Multi-PB conflict detection | Yes (`pauseThisPB`) | Not implemented |
| Auto-unassign empty containers | Yes | Not implemented |

