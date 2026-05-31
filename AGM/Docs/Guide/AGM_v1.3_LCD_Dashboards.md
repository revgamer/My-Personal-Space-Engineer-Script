# AGM v1.3 LCD Dashboard Guide

AGM draws LCDs that have `[AGM-S]` in the block name.

Each LCD should have one command in Custom Data.

## LCD Setup

1. Rename the LCD so it includes `[AGM-S]`.
2. Open LCD Custom Data.
3. Add one command.
4. Recompile or run the AGM PB.

Example:

```text
Name: LCD Power Main [AGM-S]
Custom Data: PowerDashboard page=1
```

## Core And Alerts

```text
CoreDashboard
AlertDashboard
WarningDashboard
```

`AlertDashboard` and `WarningDashboard` both show AGM alerts.

## Power

```text
PowerDashboard page=1
ReactorRefuel
BatteryControl
```

Power page notes:

- `PowerDashboard page=1`: battery, stored power, input/output, reactors, solar, wind turbine, H2 engine.
- `ReactorRefuel`: reactor uranium and refuel status.
- `BatteryControl`: auto reactor charge status.

`PowerDashboard page=2` also maps to reactor refuel behavior, and `PowerDashboard page=3` maps to battery control.

## Logistics

```text
LogisticsDashboard
```

Shows logistics state, cargo/source counts, moved items, last item, from, and to.

## Production

```text
ProductionDashboard page=1
ProductionDetails
ProductionWarnings
```

v1.3 page meanings:

- `ProductionDashboard page=1`: production overview.
- `ProductionDetails`: assembler details.
- `ProductionWarnings`: refinery details.

The old warning page name is kept as a command alias, but missing-resource warning spam is disabled by default with `show_missing_resources=false`.

## Stock Pages

```text
InventoryStock page=1
OreStock page=1
IngotStock page=1
ComponentStock page=1
AmmoStock page=1
ToolStock page=1
BottleStock page=1
```

Use separate LCDs for pages:

```text
ComponentStock page=1
ComponentStock page=2
ComponentStock page=3
```

AGM reads the number after `page=`, so `ComponentStock page=3` correctly shows page `3/3` instead of duplicating earlier pages.

## Autocrafting

```text
Autocrafting page=1
Autocrafting page=2
Autocrafting page=3
```

Shows component stock against configured quotas.

## Fuel And Life Support

```text
FuelLifeSupport
LifeSupport
```

Both commands draw the fuel/life-support dashboard.

## Common Problems

If an LCD shows the wrong page:

- Use `page=1`, `page=2`, etc.
- Put only one dashboard command on each LCD.

If an LCD is blank:

- Confirm `[AGM-S]` is in the block name.
- Confirm the block has a text surface.
- Confirm Custom Data contains a valid command.

If text looks cut off:

- Use a larger LCD or fewer rows.
- AGM v1.3 scales long values down, but very small LCDs still have limits.
