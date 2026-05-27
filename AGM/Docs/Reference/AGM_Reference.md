# AGM Reference

This file is now a compact current-state reference. The older combined-script notes were moved out of the active setup path because AGM V1 is a four-PB module split.

## Live Scripts

```text
Scripts/AGM_Core.cs
Scripts/AGM_Power.cs
Scripts/AGM_Logistics.cs
Scripts/AGM_Production.cs
```

## PB Names

```text
PB AutoGrid Manager Core {AGM-Core}
PB AutoGrid Manager Power {AGM-Power}
PB AutoGrid Manager Logistics {AGM-Logistics}
PB AutoGrid Manager Production {AGM-Production}
```

## Responsibility Split

```text
AGM Core        = shared config, module health, all [AGM-S] wall LCD rendering
AGM Power       = power scanning, [PowerState], PB front screen
AGM Logistics   = cargo naming/sorting, [LogisticsState], PB front screen
AGM Production  = autocrafting/refinery/assembler priority, [ProductionState], PB front screen
```

Status is legacy. Do not add `AGM_Status.cs` back into the V1 live setup unless explicitly requested.

## Wall LCD Commands

Add `[AGM-S]` to the LCD/block name, then put one command in Custom Data.

```text
CoreDashboard
PowerDashboard
LogisticsDashboard
SorterDashboard
ProductionDashboard
InventoryStock
OreStock
IngotStock
ComponentStock
AmmoStock
ToolStock
BottleStock
Autocrafting
FuelLifeSupport
```

Paging examples:

```text
ComponentStock page=1
ComponentStock page=2
ComponentStock vertical page=3
InventoryStock horizontal page=2
Autocrafting page=2
```

## Visual Style

| Element | Colour |
|---|---|
| Background | `new Color(5, 16, 28)` |
| Panel | `new Color(9, 24, 40)` |
| Rows | `new Color(105, 73, 29)` |
| Accent border/title | `new Color(255, 231, 38)` |
| Accent 2 | `new Color(255, 225, 94)` |
| Text | `new Color(244, 227, 184)` |
| Dim text | `new Color(191, 160, 100)` |
| OK mint | `new Color(91, 242, 159)` |
| Warning/error | `new Color(255, 100, 78)` |

## Current Setup Guide

Use this as the main install guide:

```text
Docs/AGM_Setup_Step_By_Step.md
```
