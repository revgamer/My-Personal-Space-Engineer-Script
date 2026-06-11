# AGM v1.5 Custom Data Reference

Full PB Custom Data section reference for `Scripts/AGM.cs`.

---

## Group Syntax

```
G:Group Name
```

Leave blank for no group. Use `include_ungrouped=false` to stop AGM scanning blocks outside the named group.

---

## [Core]

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

| Key | Effect |
|-----|--------|
| `global_pause` | Pauses all active work without recompiling |
| `include_docked_grids` | If false, docked grids are always excluded |
| `no_sorting_tag` | Tag on a connector to exclude its docked grid |
| `locked_tag` | Tag on a container to exclude it from sort destinations |
| `manual_tag` | Tag on assembler/refinery to exclude from production management |
| `hidden_tag` | Tag on any block to hide it from all AGM scanning |

---

## [Alerts]

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

## [Power:Base]

```ini
[Power:Base]
batteries=G:Base Batteries
reactors=G:Base Reactors
solar=
wind=
hydrogen=
include_ungrouped=false
```

---

## [ReactorRefuel]

```ini
[ReactorRefuel]
enabled=true
min_uranium_per_reactor=2
target_uranium_per_reactor=10
uranium_low_warning_kg=5
auto_refuel=false
```

Start with `auto_refuel=false`.

---

## [PowerControl]

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

---

## [Logistics]

```ini
[Logistics]
auto_assign=true
max_moves_per_run=2
```

---

## [FuelLifeSupport]

```ini
[FuelLifeSupport]
o2h2_generators=G:Base Ice Generators
h2_tanks=G:Base Hydrogen Tanks
o2_tanks=G:Base Oxygen Tanks
include_ungrouped=false
```

---

## [Production]

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
show_machine_details=true
show_current_blueprint=true
show_refinery_input=true
show_missing_resources=false
show_blocked_assemblers=true
show_blocked_refineries=true
missing_warning_below_percent=90
```

| Key | Notes |
|-----|-------|
| `monitor_only=false` | **Must be false for autocrafting to run** |
| `auto_disassemble=false` | Default off -- enable manually. Disassembly will not fight autocrafting (v1.5) |
| `autocraft_components` | Queues components to meet quota targets |

---

## [RefineryPriority]

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

## [AssemblerPriority] and Quotas

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

---

## Cargo Container Type Tags

Put in the **block name** OR **Custom Data** (v1.5 supports both):

```
{Ore 1}
{Ingot 1}
{Component 1}
{Ammo 1}
{Tools 1}
{Bottle 1}
{Food 1}
{Seed 1}
{Ingredient 1}
```

Lower number fills first. Number up for multiple containers: `{Ore 1}`, `{Ore 2}`, `{Ore 3}`.

---

## Changelog

| Version | Custom Data Changes |
|---------|---------------------|
| 1.5 | `auto_disassemble` added to default config; Food/Seed/Ingredient cargo tags supported; cargo type tags now work in Custom Data as well as block name |
| 1.4 | `auto_disassemble` key added (was hidden) |
| 1.3 | Production section merged; no more [ProductionV2] |
