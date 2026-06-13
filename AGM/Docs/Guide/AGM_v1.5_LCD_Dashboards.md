# AGM v1.5 LCD Dashboard Guide

Put `[AGM-S]` in the LCD block name. Put one command in LCD Custom Data. Recompile PB.

---

## LCD Setup

1. Rename LCD to include `[AGM-S]`
2. Open LCD Custom Data
3. Add one dashboard command
4. Recompile or run the PB

Example:

```
Name:        LCD Power [AGM-S]
Custom Data: PowerDashboard page=1
```

---

## All Dashboard Commands

### Core and Alerts

```
CoreDashboard
AlertDashboard
WarningDashboard
```

### Power

```
PowerDashboard page=1
ReactorRefuel
BatteryControl
```

Page map:
- `page=1` -- Power overview (batteries, stored, input/output, reactors, solar, wind, H2 engine)
- `page=2` -- Reactor refuel
- `page=3` -- Battery control

### Logistics

```
LogisticsDashboard
```

### Production

```
ProductionDashboard page=1
ProductionDetails
ProductionWarnings
```

- `page=1` -- Production overview
- `ProductionDetails` -- Assembler details, scroll, sorted Adv then Basic A-Z
- `ProductionWarnings` -- Refinery details sorted A-Z

### Stock -- Standard

```
InventoryStock page=1
OreStock page=1
IngotStock page=1
ComponentStock page=1
AmmoStock page=1
ToolStock page=1
BottleStock page=1
```

Ore/Ingot/Component/Ammo/Tool/Bottle stock always shows known items even at 0 qty.

### Stock -- New in v1.5

```
FoodStock page=1
SeedStock page=1
IngredientStock page=1
```

### Autocrafting

```
Autocrafting page=1
```

Shows component stock vs configured quotas with progress bars.

### Fuel and Life Support

```
FuelLifeSupport
LifeSupport
```

---

## Multi-Page Setup

Use separate LCDs per page:

```
LCD Component Stock 1 [AGM-S]   ->  ComponentStock page=1
LCD Component Stock 2 [AGM-S]   ->  ComponentStock page=2
LCD Component Stock 3 [AGM-S]   ->  ComponentStock page=3
```

## Wide and Narrow LCDs

AGM automatically uses a compact summary layout on wide, narrow, and short LCD
surfaces so full dashboard rows are not clipped.

Optional layout controls:

```
ProductionDetails layout=compact
ProductionDetails layout=full
```

- `layout=compact` or `layout=bar` forces the compact summary.
- `layout=full` forces the detailed dashboard even on a narrow surface.

---

## Troubleshooting

| Problem | Fix |
|---------|-----|
| LCD blank | Confirm `[AGM-S]` in block name and valid command in Custom Data |
| Wrong page | Use `page=1`, `page=2` etc. -- one command per LCD |
| Text cut off | Remove `layout=full` or use `layout=compact` |
| Alert LCD flickering | Remove `[AGM-S]` -- use `[AGM-LIGHT]` in Custom Data only |
