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
| Background | `new Color(1, 8, 13)` / `#01080D` |
| Panel | `new Color(2, 18, 28)` / `#02121C` |
| Rows | `new Color(3, 58, 78)` / `#033A4E` |
| Accent border/title | `new Color(38, 239, 255)` / `#26EFFF` |
| Accent text | `new Color(112, 247, 255)` / `#70F7FF` |
| Text | `new Color(126, 246, 255)` / `#7EF6FF` |
| Dim cyan | `new Color(44, 177, 195)` / `#2CB1C3` |
| Row dim cyan | `new Color(63, 207, 222)` / `#3FCFDE` |
| OK mint | `new Color(97, 255, 214)` / `#61FFD6` |
| Warning | `new Color(255, 202, 34)` / `#FFCA22` |
| Error | `new Color(255, 79, 66)` / `#FF4F42` |
| Progress background | `new Color(18, 48, 32)` / `#123020` |
| Progress fill | `new Color(255, 204, 36)` / `#FFCC24` |

## Current Setup Guide

Use this as the main install guide:

```text
Docs/AGM_Setup_Step_By_Step.md
```
