# AutoGrid Manager v1.5
### A comprehensive inventory, power, production, and logistics management script for Space Engineers
**Author:** RevGamer

---

## Overview

AutoGrid Manager (AGM) is a programmable block script that manages your base automatically:

- **Sorting** -- moves items to designated cargo containers by category
- **Autocrafting** -- monitors component stock and queues assembler jobs to meet quotas
- **Power management** -- reactor fuel balancing, battery charge automation, solar/wind tracking
- **Production monitoring** -- assembler and refinery status, queue management
- **Stock dashboards** -- real-time LCD screens for every item category
- **Alert system** -- lights and corner LCDs that change colour based on system status
- **Docked grid exclusion** -- allied ships dock safely without AGM touching their inventory
- **Food / Seed / Ingredient support** -- full Apex Survival DLC integration

AGM is configuration-only. Everything is set via Custom Data and block names -- no scripting knowledge needed.

---

## Requirements

- Standard Programmable Block (large or small grid)
- Block groups for batteries, reactors, assemblers, refineries (recommended but optional)
- One or more cargo containers with type tags in their name or Custom Data

---

## Quick Start

1. Paste the script into a Programmable Block and compile
2. AGM writes default Custom Data on first run -- open the PB and edit it
3. Set your group names in the config
4. Add `[AGM-S]` to your LCD names and add a dashboard command to their Custom Data
5. Add cargo type tags to your containers
6. Done -- AGM starts sorting and monitoring automatically

---

# Part 1: Configuration

All AGM configuration lives in the Programmable Block Custom Data.

---

## [Core] Section

```ini
[Core]
enabled=true
power=true
logistics=true
production=true
global_pause=false
include_docked_grids=false
no_sorting_tag={No AGM}
locked_tag={Locked}
manual_tag={Manual}
hidden_tag={Hidden}
```

| Key | Default | Description |
|-----|---------|-------------|
| `enabled` | true | Master switch for AGM |
| `power` | true | Enable power management system |
| `logistics` | true | Enable sorting and logistics |
| `production` | true | Enable production management |
| `global_pause` | false | Pause all work without recompiling |
| `include_docked_grids` | false | If false, ALL docked grids are excluded. If true, only connectors tagged `{No AGM}` are excluded |
| `no_sorting_tag` | `{No AGM}` | Tag to put on a BASE connector to exclude its docked grid |
| `locked_tag` | `{Locked}` | Tag to exclude a container from sort destinations |
| `manual_tag` | `{Manual}` | Tag to exclude a block from production management |
| `hidden_tag` | `{Hidden}` | Tag to hide a block from all AGM scanning |

---

## [Alerts] Section

```ini
[Alerts]
enabled=true
warning_lights=true
battery_low_percent=25
hydrogen_low_percent=20
oxygen_low_percent=20
uranium_low_kg=5
ingot_low_percent=20
component_low_percent=20
ammo_low_percent=20
```

---

## [Power:Base] Section

You can have multiple power profiles by naming them `[Power:Name]`.

```ini
[Power:Base]
batteries=G:[BMS-II] Batteries
reactors=G:[BMS-II] Nuclear Reactors
solar=
wind=
hydrogen=G:[BMS-II] Backup Engines
include_ungrouped=false
```

Use `G:Group Name` to reference a block group. Leave blank to skip that power type.

---

## [ReactorRefuel] Section

```ini
[ReactorRefuel]
enabled=true
min_uranium_per_reactor=2
target_uranium_per_reactor=10
uranium_low_warning_kg=5
auto_refuel=false
```

When `auto_refuel=true`, AGM moves uranium from storage to reactors automatically to keep them topped up to `target_uranium_per_reactor` kg each.

---

## [PowerControl] Section

```ini
[PowerControl]
enabled=true
auto_reactor_charge=true
battery_low_percent=25
battery_full_percent=100
control_reactors=G:Base Reactors
control_batteries=G:Base Batteries
turn_reactors_off_when_full=true
amber_while_charging=true
minimum_reactors_online=0
never_turn_off_reactors_if_output_above_percent=80
```

When `auto_reactor_charge=true`, AGM turns reactors on when battery drops below `battery_low_percent` and off when batteries reach `battery_full_percent`. `minimum_reactors_online` ensures at least N reactors are always on regardless of charge.

---

## [Logistics] Section

```ini
[Logistics]
auto_assign=true
max_moves_per_run=2
```

`max_moves_per_run` controls how many item moves happen per tick. Lower this if you hit instruction limits (default 2, max 10).

---

## [FuelLifeSupport] Section

```ini
[FuelLifeSupport]
o2h2_generators=G:Base Ice Generators
h2_tanks=G:Base Hydrogen Tanks
o2_tanks=G:Base Oxygen Tanks
include_ungrouped=false
```

---

## [Production] Section

```ini
[Production]
enabled=true
monitor_only=false
autocraft_components=true
auto_disassemble=false
sort_assembler_queue=true
sort_refinery_input=true
max_queue_per_run=2
max_queue_amount=500
assemblers=G:Base Assemblers
refineries=G:Base Refineries
```

| Key | Notes |
|-----|-------|
| `monitor_only=false` | **MUST be false for autocrafting to queue jobs** |
| `autocraft_components=true` | Queues assembler jobs to meet component quotas |
| `auto_disassemble=false` | Disassembles excess components. Never fights autocrafting in v1.5 |
| `max_queue_per_run` | Jobs queued per assembler per run. Default 2, max 20 |
| `max_queue_amount` | Max stack to queue per blueprint. Default 500, max 100000 |

---

## [RefineryPriority] Section

Lists ore types in priority order -- Stone/Iron first to empty hoppers, Uranium last to preserve furnace time.

```ini
[RefineryPriority]
Stone
Iron
Nickel
Cobalt
Silicon
Magnesium
Silver
Gold
Platinum
Uranium
```

---

## [AssemblerPriority] and Component Quotas

List components in priority order above the quota lines.

```ini
[AssemblerPriority]
SteelPlate
InteriorPlate
Construction
Computer
Motor
Display
MetalGrid
SmallTube
LargeTube
GravityGenerator
Superconductor

AutoCrafting=Component
SteelPlate=70000
InteriorPlate=70000
Construction=70000
Computer=10000
Motor=15000
MetalGrid=50000
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
```

`AutoCrafting=Component` sets autocrafting to manage only components. Use `AutoCrafting=All` to autocraft everything detected in stock.

---

# Part 2: Block Tags

All tags go in the block name or Custom Data. No coding required.

---

## Cargo Container Tags

Put these in the block **name** OR **Custom Data** (both work):

| Tag | Items sorted into this container |
|-----|----------------------------------|
| `{Ore 1}` | All ores |
| `{Ingot 1}` | All ingots |
| `{Component 1}` | All components |
| `{Ammo 1}` | All ammo |
| `{Tools 1}` | All tools and weapons |
| `{Bottle 1}` | Hydrogen and oxygen bottles |
| `{Food 1}` | Food items (Apex Survival) |
| `{Seed 1}` | Seeds (Apex Survival) |
| `{Ingredient 1}` | Ingredients (Apex Survival) |

**Priority:** lower number fills first. When a container reaches 98% full, AGM spills to the next number.

**Multiple containers:**
```
Large Cargo Container {Ore 1}
Large Cargo Container {Ore 2}
Large Cargo Container {Ore 3}
```

---

## Protection Tags

| Tag | Where to put it | Effect |
|-----|-----------------|--------|
| `{No AGM}` | Base connector Custom Data or name | Completely excludes docked grid |
| `{Locked}` | Container name or Custom Data | Never used as a sort destination |
| `{Manual}` | Assembler/refinery name or Custom Data | Excluded from queue management |
| `{Hidden}` | Any block name or Custom Data | Excluded from all AGM scanning |
| `[Air Vent]` | Air vent Custom Data or name | Includes vent in FuelLifeSupport monitoring. Do NOT tag airlock vents |

---

## LCD / Screen Tag: `[AGM-S]`

Put `[AGM-S]` in the **block name** of any LCD, text panel, wide LCD, button panel, or cockpit screen.

Then put one dashboard command in the block **Custom Data**.

**Example:**
```
Block name:  LCD Power Status [AGM-S]
Custom Data: PowerDashboard page=1
```

---

## Alert Light / Corner LCD Tag: `[AGM-LIGHT]`

Put `[AGM-LIGHT]` in the **Custom Data** of any light block or corner LCD.

```ini
[AGM-LIGHT]
watch=Battery
```

Do NOT add `[AGM-S]` to these blocks.

---

# Part 3: LCD Dashboard Commands

---

## Setting Up an LCD

1. Add `[AGM-S]` to the block name
2. Open the block Custom Data
3. Add one command from the list below
4. Recompile or rescan the PB

---

## Dashboard Command Reference

### Core Status
```
CoreDashboard
```
Shows: AGM version, Power / Logistics / Production / Alerts status, total screens, instruction count.

---

### Alert Dashboard
```
AlertDashboard
```
Shows: Battery %, Cargo fill, Hydrogen %, Oxygen %, Uranium stock, Production status with details.

---

### Warning Dashboard
```
WarningDashboard
```
Shows: Active warnings and critical alerts with descriptions.

---

### Power Dashboards
```
PowerDashboard page=1
ReactorRefuel
BatteryControl
```

- `PowerDashboard page=1` -- Battery stored/input/output, reactor count, solar, wind, H2 engine, state
- `ReactorRefuel` -- Reactor count, uranium in reactors, uranium in stock, lowest reactor, auto-refuel status
- `BatteryControl` -- Battery %, low/full triggers, reactor count, load safety, light state, status

---

### Logistics Dashboard
```
LogisticsDashboard
```
Shows: Sorting state, cargo count, source count, items per category, moves per run, last item moved, from/to blocks, warnings.

---

### Production Dashboards
```
ProductionDashboard page=1
ProductionDetails
ProductionWarnings
```

- `ProductionDashboard page=1` -- Overview: state, mode, assembler count, queue depth, refinery count, autocrafting status, last queued
- `ProductionDetails` -- Per-assembler status and current blueprint
- `ProductionWarnings` -- Per-refinery status and input

---

### Stock Dashboards

Each shows items with quantity and a coloured progress bar.

```
OreStock page=1
IngotStock page=1
ComponentStock page=1
AmmoStock page=1
ToolStock page=1
BottleStock page=1
FoodStock page=1
SeedStock page=1
IngredientStock page=1
InventoryStock page=1
```

Items always show even at 0 quantity (pre-populated list). Unknown items appear automatically when detected.

Multi-page setup -- one LCD per page:
```
LCD Ore Stock 1 [AGM-S]   ->  OreStock page=1
LCD Ore Stock 2 [AGM-S]   ->  OreStock page=2
```

---

### Autocrafting Dashboard
```
Autocrafting page=1
```
Shows each tracked component: current stock vs target quota with a coloured progress bar.
Green = at quota. Yellow = below quota, queuing. Red = critically low.

---

### Fuel and Life Support
```
FuelLifeSupport
LifeSupport
```

- `FuelLifeSupport` -- H2 tank level, O2 tank level, generator count, ice stock, ice to generators, air vent status
- `LifeSupport` -- O2/H2 only

### Air Vent Status

Air vents are monitored by AGM when tagged. Put `[Air Vent]` in the vent **Custom Data** or **block name** to include it in monitoring.

Airlock vents should NOT have this tag -- airlocks are intentionally depressurised and would show as warnings constantly.

```
Air Vent Custom Data:
[Air Vent]
```

Tagged vents show on the FuelLifeSupport screen automatically. Status: pressurised OK or leaking with the vent name listed.

---

# Part 4: Alert Lights and Corner LCDs

---

## Setup

Open the block Custom Data and add:

```ini
[AGM-LIGHT]
watch=Battery
```

That is all. No block renaming needed. AGM rescans every ~10 seconds and picks up new alert blocks automatically.

---

## Watch Values

| watch= | Monitors |
|--------|----------|
| `Battery` | Battery charge alert level |
| `Cargo` | Cargo stock alert level |
| `Hydrogen` | Hydrogen tank level |
| `Oxygen` | Oxygen tank level |
| `Uranium` | Uranium stock level |
| `Production` | Production alert level |
| `Charging` | Reactor charging state |
| `Power OK` | Power stable indicator |
| *(blank)* | Overall AGM alert level |

---

## Alert States

| State | Light colour | Corner LCD border and text |
|-------|-------------|---------------------------|
| OK | Solid green | Green border, ONLINE |
| Warning | Solid amber | Amber border, WARNING |
| Critical | Red blinking | Red border, blinking CRITICAL |

---

## Corner LCD Display

Corner LCDs show:
- Watch topic large and centred (e.g. BATTERY)
- Status below (ONLINE / WARNING / CRITICAL)
- Coloured border matching alert state
- AGM version at bottom

Redrawn every tick -- never flickers even if the area is unloaded and reloaded.

---

## Multiple Alert Blocks

Each block has its own Custom Data:

```
Interior Light A     ->  [AGM-LIGHT]  watch=Battery
Interior Light B     ->  [AGM-LIGHT]  watch=Cargo
Corner LCD Left      ->  [AGM-LIGHT]  watch=Hydrogen
Corner LCD Right     ->  [AGM-LIGHT]  watch=Production
Spotlight            ->  [AGM-LIGHT]  (blank = overall)
```

---

# Part 5: Docked Grids

---

## include_docked_grids=false (Default)

All docked ships are excluded. AGM will not touch any cargo, turrets, or blocks on any docked grid. No tags needed on any connectors.

Use this if you want complete separation between your base and docked ships.

---

## include_docked_grids=true

AGM sorts docked ships as if they are part of the base, EXCEPT for connectors tagged `{No AGM}`.

Use this when you want AGM to resupply your own ships automatically, but exclude allied or visitor ships.

**Setup:**
1. Set `include_docked_grids=true` in [Core]
2. On your BASE connector (the one physically on your base), open Custom Data
3. Add `{No AGM}` on its own line
4. Recompile PB

AGM immediately excludes the docked ship on that connector. All other docked ships are sorted normally.

**Important:**
- The tag goes on the BASE connector -- never on the ship connector
- AGM only reads base connectors. Ship connector tags are ignored
- Works instantly when a ship docks -- no delay

---

## Turrets

AGM never pulls ammo from turrets (base or ship). Turrets manage their own ammo via SE's conveyor system. This is by design.

---

# Part 6: Apex Survival DLC Integration

---

## New Item Categories

| Category | Cargo tag | Examples |
|----------|-----------|---------|
| Food | `{Food 1}` | Clang Cola, Cosmic Coffee, Meal Packs |
| Seed | `{Seed 1}` | Fruit Seeds, Grain Seeds, Mushroom Seeds, Vegetable Seeds |
| Ingredient | `{Ingredient 1}` | Algae, Grain, Fruit, Mushrooms, Vegetables, Mammal Meat, Medkit |

---

## Stock Screens

Add these commands to LCD Custom Data:

```
FoodStock page=1
SeedStock page=1
IngredientStock page=1
```

Items pre-populated with all known Apex Survival items. Unknown items appear automatically when detected in inventory.

---

## Item Classification

| Item | Category |
|------|----------|
| Clang Cola, Cosmic Coffee | Food |
| All MealPack_ items | Food |
| Mammal Meat Cooked, Insect Meat Cooked | Food |
| Fruit Seeds, Grain Seeds, Mushroom Seeds, Vegetable Seeds | Seed |
| Algae, Grain | Ingredient |
| Fruit, Mushrooms, Vegetables | Ingredient |
| Mammal Meat Raw, Insect Meat Raw | Ingredient |
| Medkit, Powerkit | Ingredient |
| Drill Inhibitor Blocker, Player Inhibitor Blocker | Ingredient |

---

# Part 7: Troubleshooting

---

## LCD shows blank

- Confirm `[AGM-S]` is in the **block name** (not Custom Data)
- Confirm there is a valid dashboard command in the **Custom Data**
- Recompile the PB after changes
- Do NOT put `[AGM-S]` on a block that also has `[AGM-LIGHT]` in its Custom Data

## Corner LCD shows light only, no text

- Put `[AGM-LIGHT]` in the **Custom Data** only
- Do NOT add `[AGM-S]` to the same block
- Recompile PB after adding the tag

## Autocrafting not running

- `monitor_only=false` in [Production] is required
- Confirm assemblers are in the group named in `assemblers=G:...`
- Confirm component quotas are set under `[AssemblerPriority]`

## Docked ship still being sorted

- Change `include_docked_grids=false` in [Core] to exclude ALL ships with no tags
- Or with `include_docked_grids=true`: put `{No AGM}` in the BASE connector Custom Data and recompile

## Script execution terminated -- instruction limit

- Lower `max_moves_per_run` in [Logistics] (default 2)
- Lower `max_queue_per_run` in [Production] (default 2)
- Check Instructions row on the PB screen -- green is healthy, red is over 40000/50000
- AGM skips heavy work on the same tick as block scanning to help with this

## Screen shows stale data / frozen

- Recompile the PB -- the instruction limit may have been hit and terminated the script
- If it keeps happening, lower `max_moves_per_run`

## Items going to wrong container

- Check the number suffix: `{Ore 1}` fills before `{Ore 2}`
- If a container has no type tag, leftover items may go there
- Items in turrets will not be moved -- turrets are excluded by design

## Food / Seed items not sorting

- Check the tag is `{Food 1}` `{Seed 1}` `{Ingredient 1}` -- note the exact spelling
- Make sure the container tag is in the block name or Custom Data
- Recompile PB after adding tags

---

# Part 8: Full Custom Data Template

Complete working template. Replace group names with your own.

```ini
[Core]
enabled=true
power=true
logistics=true
production=true
global_pause=false
include_docked_grids=false

[Alerts]
enabled=true
warning_lights=true
battery_low_percent=25
hydrogen_low_percent=20
oxygen_low_percent=20
uranium_low_kg=5
ingot_low_percent=20
component_low_percent=20
ammo_low_percent=20

[Power:Base]
batteries=G:Base Batteries
reactors=G:Base Reactors
solar=
wind=
hydrogen=
include_ungrouped=false

[ReactorRefuel]
enabled=true
min_uranium_per_reactor=2
target_uranium_per_reactor=10
uranium_low_warning_kg=5
auto_refuel=false

[PowerControl]
enabled=true
auto_reactor_charge=true
battery_low_percent=25
battery_full_percent=100
control_reactors=G:Base Reactors
control_batteries=G:Base Batteries
turn_reactors_off_when_full=true
amber_while_charging=true
minimum_reactors_online=0
never_turn_off_reactors_if_output_above_percent=80

[Logistics]
auto_assign=true
max_moves_per_run=2

[FuelLifeSupport]
o2h2_generators=G:Base Ice Generators
h2_tanks=G:Base Hydrogen Tanks
o2_tanks=G:Base Oxygen Tanks
include_ungrouped=false

[Production]
enabled=true
monitor_only=false
autocraft_components=true
auto_disassemble=false
sort_assembler_queue=true
sort_refinery_input=true
max_queue_per_run=2
max_queue_amount=500
assemblers=G:Base Assemblers
refineries=G:Base Refineries

[RefineryPriority]
Stone
Iron
Nickel
Cobalt
Silicon
Magnesium
Silver
Gold
Platinum
Uranium

[AssemblerPriority]
SteelPlate
InteriorPlate
Construction
Computer
Motor
Display
MetalGrid
SmallTube
LargeTube
GravityGenerator
Superconductor

AutoCrafting=Component
SteelPlate=70000
InteriorPlate=70000
Construction=70000
Computer=10000
Motor=15000
MetalGrid=50000
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
```

---

# Changelog

## v1.5
- Food, Seed, Ingredient categories with full Apex Survival DLC support
- Cargo type tags now work in Custom Data (not just block name)
- Docked grid exclusion completely rewritten -- `{No AGM}` tag on base connector only
- Instant rescan when a ship docks or undocks
- Turrets excluded from sorting on base and docked ships
- Corner LCD fix -- now correctly detected and drawn
- All LCD borders drawn on screen edge at 6px -- visible on any screen size
- Bulletproof draw system -- crash protection on every surface access
- DrawPowerDash border fixed
- Autocrafting: blueprints validated with CanUseBlueprint against real assemblers
- Autocrafting: assembler mode checked before queuing
- Disassembly never fights autocrafting -- skips components with assembly queued
- 50k instruction limit mitigation -- heavy work spread across ticks
- Instructions counter on PB screen

---

*AutoGrid Manager v1.5 by RevGamer*
