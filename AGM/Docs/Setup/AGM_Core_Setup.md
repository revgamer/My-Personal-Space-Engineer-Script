# AGM Core Setup

Script file:

```text
Scripts/AGM_Core.cs
```

Programmable Block name:

```text
PB AutoGrid Manager Core {AGM-Core}
```

AGM Core is the central controller for the V1.0 module system.

Core coordinates:

- `AGM-Power`
- `AGM-Logistics`
- `AGM-Production`
- future AGM modules

Core holds shared config, shows module health, lets other AGM modules know whether they are enabled or paused, and renders all `[AGM-S]` wall LCD dashboards.

Power, Logistics, and Production draw only their own PB front screens and publish state sections for Core to read.

## Core PB Custom Data

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

## Module Rule

Modules should read Core settings, but they should fail safely if Core is missing.

Expected behavior:

- If Core is online and `global_pause=true`, modules pause active work.
- If Core says `logistics=false`, AGM Logistics should not sort or rename cargo.
- If Core says `production=false`, AGM Production should not queue assemblers.
- AGM Core owns shared dashboard rendering and module health.

## Wall LCD Ownership

Add `[AGM-S]` to LCD names and put one command in Custom Data.

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

Core reads:

```text
[PowerState] from AGM Power
[LogisticsState] from AGM Logistics
[ProductionState] from AGM Production
```

Core then draws the dashboard with the shared AGM theme.

## Core Dashboard

Suggested LCD command:

```text
CoreDashboard
```

Suggested display:

```text
AGM CORE

Power        ONLINE
Logistics    ONLINE
Production   PAUSED

Global Pause OFF
Docked Grids OFF

Last Warning none
```

## Shared Theme

Current AGM V1 style:

```text
Neon cyan/teal HUD
Background #01080D
Panel #02121C
Teal rows #033A4E
Cyan border/title #26EFFF
Bright cyan text #7EF6FF
Dim cyan #2CB1C3
Yellow progress fill #FFCC24
Mint online/OK #61FFD6
Warning #FFCA22
Error #FF4F42
```
