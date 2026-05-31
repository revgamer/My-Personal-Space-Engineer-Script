# AGM Core Setup

Script file:

```text
Scripts/AGM.cs
```

Suggested programmable block name:

```text
PB AutoGrid Manager
```

AGM Core is built into the unified AGM script.

Core coordinates:

- `AGM-Power`
- `AGM-Logistics`
- `AGM-Production`
- future AGM modules

The unified AGM script holds shared config, runs enabled systems, and renders all `[AGM-S]` wall LCD dashboards.

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

```

## System Rule

The unified script reads Core settings before running each enabled system.

Expected behavior:

- If `global_pause=true`, AGM pauses active work.
- If `logistics=false`, AGM Logistics will not sort or rename cargo.
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



