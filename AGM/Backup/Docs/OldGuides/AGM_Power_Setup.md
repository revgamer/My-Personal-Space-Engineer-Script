# AGM Power Setup

Script file:

```text
Scripts/AGM.cs
```

Suggested programmable block name:

```text
PB AutoGrid Manager
```

AGM Power runs inside the unified AGM script. It scans configured power groups and draws power dashboards from the main PB.

Use `[AGM-S]` LCDs with `PowerDashboard`, `ReactorRefuel`, or `BatteryControl` in Custom Data.

The unified AGM script supports the v1.2 power pages directly from `Scripts/AGM.cs`.

## Handles

```text
PowerDashboard data
Battery groups
Reactor groups
Solar groups
Wind groups
Hydrogen engine groups
```

## Core Custom Data

Core should include:

```ini
[Core]
power=true

```

## Power Custom Data

Example:

```ini
[Power:RAB Base]
batteries=G:[RAB] Batteries
reactors=G:[RAB] Reactors
solar=G:[RAB] Solar
wind=G:[RAB] Wind
hydrogen=G:[RAB] Hydrogen Engines
include_ungrouped=false
```

## Published State

AGM Power writes this into its own PB Custom Data:

```ini
[PowerState]
state=ONLINE
profile=RAB Base
batteries=8
battery_percent=73.4
stored_mwh=32.500
capacity_mwh=44.000
input_mw=1.250
output_mw=3.100
max_output_mw=22.000
reactors=1
solar=10
wind=4
hydrogen=2
```

AGM Core should read `[PowerState]` and render wall LCDs using the shared style/layout engine.

## LCD Command

On an LCD with `[AGM-S]` in the name:

```text
PowerDashboard 1
PowerDashboard 2
PowerDashboard 3
ReactorRefuel
BatteryControl
AGM-Reactor
AGM-Battery
```

`PowerDashboard 1` is the main overview, `PowerDashboard 2` / `ReactorRefuel` is the monitor-only reactor uranium page, and `PowerDashboard 3` / `BatteryControl` is the battery/reactor control page.

## AGM v1.2 Config

The unified script adds these sections:

```ini
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
```

`auto_refuel` remains off by default. Reactor uranium movement is monitor-only in v1.2; automatic behaviour is limited to turning configured reactors on for low battery charge and, if safety rules allow it, back off when batteries are full.

## Theme

Power uses the same AGM V1 neon cyan/teal HUD theme as Core, Logistics, and Production.



