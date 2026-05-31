# CLAUDE.md - AGM AutoGrid Manager

## Project Context

- Author: RevGamer
- Current build: AGM v1.3 unified script
- Game: Space Engineers
- Script type: Programmable Block script
- Active script file: `Scripts/AGM.cs`
- LCD tag: `[AGM-S]`

## Current Repository Layout

```text
AGM/
  Scripts/
    AGM.cs
  Docs/
    Guide/
      AGM_Setup_Step_By_Step.md
      AGM_v1.3_Custom_Data.md
      AGM_v1.3_LCD_Dashboards.md
    reference/
      AGM.md
      AutoGrid_Manager_Roadmap.md
      CLAUDE.md
      IIM_Sorter_Reference.md
  Backup/
    Docs/
    Legacy/
    ModularScripts/
```

## Current State

AGM v1.3 is a one-PB unified script. Old four-module docs and v1.0 generated references are kept in `Backup/Docs`.

Active behavior:

- Core dashboard and alerts.
- Logistics sorting with protected tags.
- Power status pages.
- Reactor refuel page.
- Battery control page.
- Fuel/life support page.
- Production overview.
- Assembler details.
- Refinery details.
- Stock dashboards.
- Autocrafting dashboards.
- PB front-screen bus/reactor animation.

## Rules For Future Sessions

1. Edit `Scripts/AGM.cs` as the active script.
2. Keep the script paste-ready for Space Engineers PB use.
3. Preserve RevGamer as author.
4. Keep `[AGM-S]` as the LCD tag unless explicitly changed.
5. Do not add uranium/reactor/fuel balancing that fights SE native conveyor behavior.
6. Do not sort from `IMyReactor`, `IMyGasGenerator`, or `IMyGasTank` inventories.
7. Use block groups and `include_ungrouped=false` for base-only scans.
8. After code changes, verify `Scripts/AGM.cs` stays under the PB size limit.
9. Keep generated `bin`, `obj`, `.vs`, and testing project folders out of the clean repo.
10. Keep active user docs in `Docs/Guide`; keep historical material in `Backup/Docs`.

## Current Guide Files

Start here:

```text
Docs/Guide/AGM_Setup_Step_By_Step.md
```

Detailed config:

```text
Docs/Guide/AGM_v1.3_Custom_Data.md
Docs/Guide/AGM_v1.3_LCD_Dashboards.md
```

Current reference:

```text
Docs/reference/AGM.md
Docs/reference/AutoGrid_Manager_Roadmap.md
Docs/reference/IIM_Sorter_Reference.md
```

## Notes

- `ProductionWarnings` is still accepted as a dashboard command, but in v1.3 it displays refinery details.
- `show_missing_resources=false` is the preferred default.
- `ComponentStock page=1/2/3` page parsing has been fixed.
- Reactor refuel should use configured reactor groups before any broad scan.
