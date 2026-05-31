# AutoGrid Manager

AutoGrid Manager is a unified Space Engineers programmable block script for base automation, LCD dashboards, stock monitoring, power management, production monitoring, logistics, alerts, and fuel/life support.

## Script

Use the unified script:

```text
Scripts/AGM.cs
```

Copy `Scripts/AGM.cs` into one programmable block, then configure that PB Custom Data from the guide files.

## Docs

Setup and usage guides:

```text
Docs/Guide/README.md
Docs/Guide/AGM_Setup_Step_By_Step.md
Docs/Guide/AGM_v1.3_Custom_Data.md
Docs/Guide/AGM_v1.3_LCD_Dashboards.md
```

Reference, roadmap, and older planning notes:

```text
Docs/reference/AGM.md
Docs/reference/AutoGrid_Manager_Roadmap.md
Docs/reference/CLAUDE.md
Docs/reference/IIM_Sorter_Reference.md
```

Legacy and older modular script sources are kept as backup only:

```text
Backup/
```

## Wall LCD Tag

Add `[AGM-S]` to any LCD or cockpit/block surface that AGM should draw.

```text
LCD Power [AGM-S]
LCD Component Stock [AGM-S]
LCD Fuel Life Support [AGM-S]
```

Put one dashboard command in the LCD Custom Data:

```text
CoreDashboard
PowerDashboard
ReactorRefuel
BatteryControl
LogisticsDashboard
ProductionDashboard
ProductionDetails
ProductionWarnings
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

## Theme

AGM screens use the current neon cyan/teal HUD style:

```text
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
