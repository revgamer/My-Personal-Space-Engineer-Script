# AGM Power Setup

Script file:

```text
Scripts/AGM_Power.cs
```

Programmable Block name:

```text
PB AutoGrid Manager Power {AGM-Power}
```

AGM Power requires AGM Core for active module state. It scans configured power groups, publishes `[PowerState]` to its own PB Custom Data, and shows a matching AGM boot/status screen on the PB front display.

AGM Power does not draw wall LCDs. Core reads `[PowerState]` and draws `PowerDashboard`.

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

[Modules]
power=PB AutoGrid Manager Power {AGM-Power}
```

## Power PB Custom Data

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
PowerDashboard
```

## Theme

Power uses the same AGM V1 navy/amber/mint theme as Core, Logistics, and Production.
