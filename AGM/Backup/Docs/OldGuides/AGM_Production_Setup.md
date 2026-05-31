# AGM Production Setup

Script file:

```text
Scripts/AGM.cs
```

Suggested programmable block name:

```text
PB AutoGrid Manager
```

AGM Production runs inside the unified AGM script.

By default this module is monitor-only. Set `monitor_only=false` when you are ready for it to manage production queues.

Use `[AGM-S]` LCDs with `ProductionDashboard`, `ProductionDetails`, `ProductionWarnings`, or `Autocrafting` in Custom Data.

## Core PB Custom Data

Core must include:

```ini
[Core]
enabled=true
production=true
global_pause=false
include_docked_grids=false
no_sorting_tag=[No Sorting]
manual_tag={Manual}
hidden_tag={Hidden}

```

## Production Custom Data

```ini
[Production]
monitor_only=true
autocraft_components=true
sort_assembler_queue=true
sort_refinery_input=true
max_queue_per_run=2
max_queue_amount=500
assemblers=G:Base Assemblers
refineries=G:Base Refineries
enabled=true
show_machine_details=true
show_current_blueprint=true
show_refinery_input=true
show_missing_resources=false
show_blocked_assemblers=true
show_blocked_refineries=true
missing_warning_below_percent=90

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

`RefineryPriority` moves the highest priority ore already inside a refinery to the front.

`AssemblerPriority` moves important queued items to the front. If an assembler has a queue but is not producing, it may move the next priority item forward so the assembler has a better chance to stay busy.

`ComponentQuotas` watches component stock. If stock plus queued amount is below the quota, Production queues more, limited by `max_queue_per_run` and `max_queue_amount`.

## Protected Machines

Use either block name or Custom Data:

```text
{Manual}
{Hidden}
[No Sorting]
```

`{Manual}` keeps a machine out of Production monitoring/management.
`{Hidden}` keeps it out of monitoring.
`[No Sorting]` on a grid or block keeps it out of AGM module handling.

## Dashboard Data

LCD Custom Data:

```text
ProductionDashboard 1
ProductionDashboard 2
ProductionDashboard 3
ProductionDetails
AGM-Production2
```

`ProductionDashboard 1` is the main overview. `ProductionDashboard 2` / `ProductionDetails` shows assembler jobs. `ProductionDashboard 3` shows refinery input.

AGM Production publishes a `[ProductionState]` section in its own PB Custom Data.
AGM Core reads that section and renders `ProductionDashboard` on `[AGM-S]` LCDs.

Autocrafting wall LCD:

```text
Autocrafting
Autocrafting page=2
Autocrafting vertical page=3
```

## LCD-Style Quotas

AGM Production also accepts this format in its own PB Custom Data:

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

## Theme

Production uses the same AGM V1 neon cyan/teal HUD theme as Core, Power, and Logistics.



