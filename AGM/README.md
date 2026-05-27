# AutoGrid Manager

Current live PB scripts are in `Scripts/`. AGM V1 uses four programmable blocks:

```text
Scripts/AGM_Core.cs
Scripts/AGM_Power.cs
Scripts/AGM_Logistics.cs
Scripts/AGM_Production.cs
```

Core is the only script that renders wall LCD dashboards. Power, Logistics, and Production publish state into their own PB Custom Data and draw only their own PB front screen.

Setup notes are in `Docs/Setup/`.

Full install guide:

```text
Docs/AGM_Setup_Step_By_Step.md
```

Reference and planning notes are in `Docs/Reference/`.

Old combined code is kept in `Legacy/`.

## In-Game PB Names

```text
PB AutoGrid Manager Core {AGM-Core}
PB AutoGrid Manager Power {AGM-Power}
PB AutoGrid Manager Logistics {AGM-Logistics}
PB AutoGrid Manager Production {AGM-Production}
```

## Wall LCD Tag

Add `[AGM-S]` to any LCD or cockpit/block surface that Core should draw.

```text
LCD Power [AGM-S]
LCD Component Stock [AGM-S]
LCD Fuel Life Support [AGM-S]
```

Put one dashboard command in the LCD Custom Data:

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

## Current Theme

All AGM screens use the same V1 palette:

```text
Dark navy background
Amber-brown data rows
Bright yellow border/title
Cream text
Mint green online/OK state
Red-orange warning/error state
```
