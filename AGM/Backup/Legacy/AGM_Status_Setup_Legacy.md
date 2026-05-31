# AGM Status Setup

Script file:

```text
Scripts/AGM_Status.cs
```

Programmable Block name:

```text
PB AutoGrid Manager Status {AGM-Status}
```

LCD/screen tag:

```text
[AGM-S]
```

AGM Status is the read-only dashboard module under AGM Core. It should not sort cargo, rename cargo, or queue assembler production.

Recommended Core PB name:

```text
PB AutoGrid Manager Core {AGM-Core}
```

AGM Status reads Core settings and runs in safe standalone mode if Core is missing, but the intended setup is through AGM Core.

It currently keeps:

- inventory dashboards,
- power dashboard,
- fuel & life support dashboard,
- pressurization checks,
- sorter/logistics dashboard display,
- production dashboard display,
- autocrafting quota display.

It does not actively:

- move items,
- assign cargo,
- rename cargo containers,
- queue assembler jobs.

The only block-name change it may still do is air vent status tagging for opted-in interior vents:

```text
[Pressurized]
[Leaking]
```

## LCD Commands

```text
IndustrialInventory=Ore
IndustrialInventory=Ingot
IndustrialInventory=Component
PowerDashboard=Base
FuelLifeSupport=Base
SorterDashboard
ProductionDashboard
AutoCrafting=Component
```

`SorterDashboard` is display-only. It reads the `[LogisticsState]` section written by `PB AutoGrid Manager Logistics {AGM-Logistics}` and does not run its own sorter.

## Fuel & Life Support Profile

PB Custom Data:

```ini
[LifeSupport:Base]
hydrogen=G:Hydrogen Tanks
oxygen=G:Oxygen Tanks
generators=G:O2 Generators
include_ungrouped=false
```

LCD Custom Data:

```text
FuelLifeSupport=Base
```

## Power Profile

PB Custom Data:

```ini
[power:Base]
batteries=G:Base Batteries
reactors=G:Base Reactors
solar=G:Base Solar
wind=
hydrogen=G:Base Hydrogen Engines
other=
include_ungrouped=false
```

LCD Custom Data:

```text
PowerDashboard=Base
```
