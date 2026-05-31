# AutoGrid Manager v1.3 Reference

AutoGrid Manager is a unified Space Engineers programmable block script by RevGamer.

Current script:

```text
Scripts/AGM.cs
```

Current layout:

```text
AGM/
  Scripts/AGM.cs
  Docs/Guide/
  Docs/reference/
  Backup/
```

## Current Build

Version: `1.3`

Main v1.3 work:

- Unified script moved to `Scripts/AGM.cs`.
- Old testing/project folders removed from active tree.
- Production dashboard added and revised.
- Production Custom Data merged into `[Production]`; no `[ProductionV2]`.
- Missing-resource warning spam disabled by default.
- Production page 2 is assembler details.
- Production page 3 is refinery details.
- Component stock page parsing fixed for `page=1`, `page=2`, `page=3`.
- Power v1.2 dashboards retained.
- Reactor refuel uses configured reactor groups to avoid other-grid reactors.
- PB front screen animation changed to bus-square plus simple reactor logo.

## Active Guides

```text
Docs/Guide/AGM_Setup_Step_By_Step.md
Docs/Guide/AGM_v1.3_Custom_Data.md
Docs/Guide/AGM_v1.3_LCD_Dashboards.md
```

## Kept References

```text
Docs/reference/AGM.md
Docs/reference/AutoGrid_Manager_Roadmap.md
Docs/reference/CLAUDE.md
Docs/reference/IIM_Sorter_Reference.md
```

Historical v1.0/generated references are backed up in:

```text
Backup/Docs/OldReference/
```

## LCD Tag

```text
[AGM-S]
```

Add this tag to an LCD/block name so AGM manages the display.

## Dashboard Commands

Core and alerts:

```text
CoreDashboard
AlertDashboard
WarningDashboard
```

Power:

```text
PowerDashboard page=1
ReactorRefuel
BatteryControl
```

Logistics:

```text
LogisticsDashboard
```

Production:

```text
ProductionDashboard page=1
ProductionDetails
ProductionWarnings
```

Stock:

```text
InventoryStock page=1
OreStock page=1
IngotStock page=1
ComponentStock page=1
AmmoStock page=1
ToolStock page=1
BottleStock page=1
```

Autocrafting and fuel:

```text
Autocrafting page=1
FuelLifeSupport
LifeSupport
```

## Important Safety Rules

- Do not add reactor/fuel balancing that fights Space Engineers conveyors.
- Do not sort from reactors, gas generators, or gas tanks.
- Use block groups for base-only scans.
- Keep generated `bin`, `obj`, and testing projects out of the clean GitHub folder.
- Keep `Scripts/AGM.cs` under the programmable block character limit.

## Theme

```text
Background #01080D
Panel #02121C
Rows #033A4E
Accent border/title #26EFFF
Accent text #70F7FF
Text #7EF6FF
Dim cyan #2CB1C3
Yellow progress #FFCC24
OK #61FFD6
Warning #FFCA22
Error #FF4F42
```
