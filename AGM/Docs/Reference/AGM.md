# AGM - AutoGrid Manager Continuation Guide

Project: **AutoGrid Manager**
Short name: **AGM**
Author: **RevGamer**
Current build: **1.0 module split**
LCD tag: `[AGM-S]`
Game: **Space Engineers**
Script type: **Programmable Block scripts - paste directly into the in-game PB editor**

---

## 1. Current Architecture

AGM V1 uses four programmable blocks:

```text
PB AutoGrid Manager Core {AGM-Core}
PB AutoGrid Manager Power {AGM-Power}
PB AutoGrid Manager Logistics {AGM-Logistics}
PB AutoGrid Manager Production {AGM-Production}
```

Script files:

```text
Scripts/AGM_Core.cs
Scripts/AGM_Power.cs
Scripts/AGM_Logistics.cs
Scripts/AGM_Production.cs
```

Core owns all `[AGM-S]` wall LCD rendering. Modules draw only their own PB front screen and publish state sections:

```text
AGM Power      -> [PowerState]
AGM Logistics  -> [LogisticsState]
AGM Production -> [ProductionState]
```

---

## 2. Core Custom Data

```ini
[Core]
enabled=true
power=true
logistics=true
production=true
global_pause=false
include_docked_grids=false
no_sorting_tag=[No Sorting]
locked_tag={Locked}
manual_tag={Manual}
hidden_tag={Hidden}

[Modules]
power=PB AutoGrid Manager Power {AGM-Power}
logistics=PB AutoGrid Manager Logistics {AGM-Logistics}
production=PB AutoGrid Manager Production {AGM-Production}
```

Run Core with `reload` after changing module names.

Run Core with `reboot` to show wall LCD boot screens and send reboot to enabled modules.

---

## 3. Wall LCD Commands

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

---

## 4. Logistics Tags

Cargo type tags:

```text
{Ore 1}
{Ingot 1}
{Component 1}
{Ammo 1}
{Tool 1}
{Bottle 1}
```

Protection tags:

```text
{Locked}
{Manual}
{Hidden}
[No Sorting]
```

Rules:

- Ore, Ingot, and Component should use Large Grid Cargo Container or Bulk Cargo Container.
- Ammo, Tool, and Bottle should use Small Cargo Container.
- If a typed cargo is full, Logistics can assign the next empty eligible cargo, e.g. `{Ore 2}`.
- `[No Sorting]` on a dock/ship/grid prevents pulling from it.

---

## 5. Production Autocrafting

Autocrafting quotas live in AGM Production PB Custom Data.

Accepted quota format:

```ini
AutoCrafting=Component
SteelPlate=70000
InteriorPlate=70000
Construction=70000
Computer=10000
Motor=15000
MetalGrid=10000
Girder=10000
SmallTube=10000
LargeTube=10000
Display=5000
BulletproofGlass=5000
PowerCell=5000
SolarCell=1000
Detector=1000
RadioCommunication=1000
Medical=200
Reactor=10000
Thrust=12000
GravityGenerator=500
Superconductor=10000
Explosives=500
Canvas=200
ShieldComponent=2000
```

Core reads `[ProductionState]` and draws the wall `Autocrafting` dashboard.

---

## 6. Fuel And Life Support

LCD command:

```text
FuelLifeSupport
```

Core scans:

- Hydrogen tanks
- Oxygen tanks
- O2/H2 generators
- Ice in generators
- Ice stock
- Oxygen and hydrogen bottles
- Opted-in interior vents

Interior vent opt-in:

```text
Block name: Base Air Vent [AGM-S]
Custom Data: InteriorVent
```

Core tags monitored vents:

```text
[Pressurized]
[Leaking]
```

Only vents with both `[AGM-S]` and `InteriorVent` are monitored, so exterior/depressurization vents are ignored.

---

## 7. Visual Style

Current AGM V1 palette:

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

Style summary:

- Dark navy background
- Amber-brown rows
- Bright yellow border/title
- Cream text
- Mint green online/OK state
- Red-orange warning/error state

---

## 8. Known Fixes

| Issue | Fix |
|---|---|
| Flickering LCDs | Only Core writes `[AGM-S]` wall LCDs |
| Status module confusion | Status is legacy; active V1 uses Core/Power/Logistics/Production |
| Truncated Logistics route | Logistics publishes full names; Core scales route rows |
| Fuel tank text overlap | Tank count is drawn above the bar, not on top of it |
| Missing icons | Stock dashboards draw item sprite IDs from item type/subtype |

---

## 9. Development Rules

1. Keep wall LCD rendering in `AGM_Core.cs`.
2. Keep module PB front screens in their own scripts.
3. Do not revive AGM Status unless the user explicitly asks.
4. Keep all four live scripts paste-ready for the in-game PB editor.
5. Preserve `[AGM-S]` as the wall LCD tag.
6. Preserve `RevGamer` as author.
7. Update setup markdown when dashboard commands, tags, or PB names change.
