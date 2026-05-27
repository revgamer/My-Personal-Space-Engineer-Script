# AGM Step-By-Step Setup

This is the clean V1 setup with four programmable blocks.

## 1. Create The PBs

Create four programmable blocks and paste these scripts:

```text
PB AutoGrid Manager Core {AGM-Core}
  Scripts/AGM_Core.cs

PB AutoGrid Manager Power {AGM-Power}
  Scripts/AGM_Power.cs

PB AutoGrid Manager Logistics {AGM-Logistics}
  Scripts/AGM_Logistics.cs

PB AutoGrid Manager Production {AGM-Production}
  Scripts/AGM_Production.cs
```

Recompile all four. Each PB should show its own AGM boot screen.

When Core is rebooted with `reboot` or recompiled, it also sends a reboot command to the enabled AGM modules and draws AGM boot/loading screens on `[AGM-S]` wall LCDs.

## 2. Core Custom Data

If Core Custom Data is empty, Core creates this automatically. If it already has data, make sure these lines exist:

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

Run Core with:

```text
reload
```

Core should show Power, Logistics, and Production as online or missing.

Core owns all wall LCD rendering. The other modules publish state only:

```text
AGM Power      -> [PowerState]
AGM Logistics  -> [LogisticsState]
AGM Production -> [ProductionState]
```

## 3. Power Setup

Create terminal groups for your base power blocks, then edit `{AGM-Power}` Custom Data:

```ini
[Power:Base]
batteries=G:Base Batteries
reactors=G:Base Reactors
solar=G:Base Solar
wind=G:Base Wind
hydrogen=G:Base Hydrogen Engines
include_ungrouped=false
```

Run Power with:

```text
reload
```

It writes `[PowerState]` to its PB Custom Data.

The Power PB front screen uses the shared AGM navy/amber/mint theme. Wall `PowerDashboard` LCDs are still drawn by Core.

## 4. Logistics Setup

Start safe:

```ini
[Logistics]
auto_assign=true
max_moves_per_run=2
```

Name one or more cargo containers:

```text
{Ore 1}
{Ingot 1}
{Component 1}
```

Small cargo containers are for:

```text
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

Run Logistics with:

```text
reload
```

It writes `[LogisticsState]` to its PB Custom Data.

The Logistics PB front screen uses the shared AGM navy/amber/mint theme. Wall `LogisticsDashboard` and `SorterDashboard` LCDs are drawn by Core.

## 5. Production Setup

Keep monitor mode first:

```ini
[Production]
monitor_only=true
autocraft_components=true
sort_assembler_queue=true
sort_refinery_input=true
max_queue_per_run=2
max_queue_amount=500
```

Add priorities and quotas:

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

[ComponentQuotas]
SteelPlate=50000
InteriorPlate=50000
Construction=50000
Computer=5000
Motor=10000
Display=1000
MetalGrid=5000
SmallTube=5000
LargeTube=5000
GravityGenerator=100
```

AGM Production also accepts the LCD-style autocrafting quota block:

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

Run Production with:

```text
reload
```

When the dashboards look correct, switch active mode on:

```ini
monitor_only=false
```

The Production PB front screen uses the shared AGM navy/amber/mint theme. Wall `ProductionDashboard` and `Autocrafting` LCDs are drawn by Core.

## 6. LCD Setup

Add `[AGM-S]` to the LCD/block name, then put one command in Custom Data.

Core dashboard:

```text
CoreDashboard
```

Power dashboard:

```text
PowerDashboard
```

Logistics dashboard:

```text
LogisticsDashboard
```

Sorter/logistics action dashboard:

```text
SorterDashboard
```

Production dashboard:

```text
ProductionDashboard
```

Stock dashboards:

```text
InventoryStock
OreStock
IngotStock
ComponentStock
AmmoStock
ToolStock
BottleStock
```

Autocrafting dashboard:

```text
Autocrafting
```

Fuel and life support dashboard:

```text
FuelLifeSupport
```

Fuel and Life Support scans hydrogen tanks, oxygen tanks, O2/H2 generators, ice stock, bottles, and opted-in interior vents. It also tags monitored vents as `[Pressurized]` or `[Leaking]`.

Interior vent setup:

```text
Block name:   Base Air Vent [AGM-S]
Custom Data:  InteriorVent
```

For another page, put the page number on the same line:

```text
ComponentStock 2
IngotStock page=2
Autocrafting 2
```

For stacked vertical LCDs, use one page per LCD:

```text
ComponentStock vertical page=1
ComponentStock vertical page=2
ComponentStock vertical page=3
```

For side-by-side horizontal LCDs, use the same idea:

```text
InventoryStock horizontal page=1
InventoryStock horizontal page=2
InventoryStock horizontal page=3
```

The LCD talks to Core. Core reads module state from the module PB Custom Data and draws the dashboard.
Stock dashboards are also drawn by Core. They scan readable grid inventories and ignore blocks tagged `{Hidden}` or `[No Sorting]`.
Autocrafting is owned by AGM Production. Core reads `[ProductionState]` and only draws the wall LCD.

Core automatically repaints managed LCDs after server load/reconnect. A screen is managed when its block name contains `[AGM-S]` or its Custom Data contains a known AGM dashboard command. If an `[AGM-S]` screen has no valid command, Core draws an AGM "waiting for command" screen instead of leaving it blank.

If a screen says `OFFLINE`, that is a Space Engineers power/terminal state, not an AGM drawing failure. Power the LCD/block and Core will repaint it on the next cycle.

## 7. First Test Order

1. Recompile Core.
2. Recompile Power.
3. Recompile Logistics.
4. Recompile Production.
5. Run `reload` on Core.
6. Check Core PB screen: Power, Logistics, Production should be online.
7. Check each module PB Custom Data has its state section:

```text
[PowerState]
[LogisticsState]
[ProductionState]
```

8. Add LCDs and verify Core draws the dashboards.

## 8. Safe Defaults

Use these while testing:

```ini
global_pause=false
include_docked_grids=false
max_moves_per_run=2
monitor_only=true
```

Use `[No Sorting]` on dock connectors or ship grids you do not want AGM to pull from.

## 9. Visual Theme

AGM V1 dashboards share one palette:

```text
Background: dark navy
Rows: amber-brown
Border/title: bright yellow
Normal text: cream
Online/OK: mint green
Warnings/errors: red-orange
```

If a screen still shows the old brown-only palette, recompile the matching script from `Scripts/`.
