# AGM v1.3 Custom Data Reference

This file explains the main PB Custom Data sections for `Scripts/AGM.cs`.

## Group Syntax

Use block groups like this:

```text
G:[RAB] Batteries
```

Blank value means no explicit group.

```ini
solar=
```

For base-only scans, use groups and set:

```ini
include_ungrouped=false
```

## Core

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
```

- `enabled`: master AGM switch.
- `global_pause`: pauses active work.
- `include_docked_grids`: allows logistics source scanning from docked grids.
- tag keys protect cargo or blocks from sorting.

## Alerts

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

Alerts drive the alert dashboard and warning lights. Production missing-resource spam is controlled separately by `[Production] show_missing_resources`.

## Power Base

```ini
[Power:Base]
batteries=G:[RAB] Batteries
reactors=G:[RAB] Nuclear Reactors
solar=
wind=
hydrogen=
include_ungrouped=false
```

Power page 1 shows batteries, stored power, input/output, reactors, solar, wind turbine, and H2 engine rows.

## Reactor Refuel

```ini
[ReactorRefuel]
enabled=true
min_uranium_per_reactor=2
target_uranium_per_reactor=10
uranium_low_warning_kg=5
auto_refuel=false
```

`auto_refuel=false` is the safest starting value. Reactor refuel uses configured reactor groups before general scanning.

## Power Control

```ini
[PowerControl]
enabled=true
auto_reactor_charge=true
battery_low_percent=25
battery_full_percent=100
control_reactors=G:[RAB] Nuclear Reactors
control_batteries=G:[RAB] Batteries
turn_reactors_off_when_full=true
amber_while_charging=true
minimum_reactors_online=0
never_turn_off_reactors_if_output_above_percent=80
```

This controls reactor charging behavior from the configured battery/reactor groups.

## Logistics

```ini
[Logistics]
auto_assign=true
max_moves_per_run=2
```

Use low values first. Raise only after dashboards show correct source/destination behavior.

## Fuel Life Support

```ini
[FuelLifeSupport]
o2h2_generators=G:[RAB] Ice Generators
h2_tanks=G:[RAB] Hydrogen Tanks
o2_tanks=G:[RAB] Oxygen Tanks
include_ungrouped=false
```

This controls the fuel/life-support dashboard and avoids off-grid tanks if grouped.

## Production

```ini
[Production]
monitor_only=true
autocraft_components=true
sort_assembler_queue=true
sort_refinery_input=true
max_queue_per_run=2
max_queue_amount=500
assemblers=G:[RAB] Assemblers
refineries=G:[RAB] Refineries
enabled=true
show_machine_details=true
show_current_blueprint=true
show_refinery_input=true
show_missing_resources=false
show_blocked_assemblers=false
show_blocked_refineries=false
missing_warning_below_percent=90
```

`show_missing_resources=false` keeps the production warning page quiet. `ProductionWarnings` now shows refinery details in v1.3.

## Refinery Priority

```ini
[RefineryPriority]
Stone
Gold
Platinum
Uranium
Iron
Nickel
Cobalt
Silicon
Magnesium
Silver
```

AGM sorts refinery input based on this order. It checks available ores and moves the best available priority ore first.

## Assembler Priority And Quotas

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
