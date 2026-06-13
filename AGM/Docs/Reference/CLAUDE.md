# CLAUDE.md -- AutoGrid Manager

AI agent instructions for AGM Core.
Author: RevGamer

---

## Project Layout

```
F:\Space Engineers Script\My-Personal-Space-Engineer-Script\
  AGM\
    Scripts\AGM.cs                        <- EDIT THIS -- source file
    Minified_Tool\AGM_Minified.sln        <- minifier solution
    Docs\Guide\
    Docs\reference\
    GitHub\

C:\Users\corra\AppData\Roaming\SpaceEngineers\IngameScripts\local\
  Autogrid Manager\Script.cs             <- minified output (SE paste target)

F:\Space Engineers Script\minifiedtool\IngameScriptMergeTool\
  IngameScriptMergeTool.exe              <- shared minifier
```

---

## Minifier Command

```powershell
& "F:\Space Engineers Script\minifiedtool\IngameScriptMergeTool\IngameScriptMergeTool.exe" -s "F:\Space Engineers Script\My-Personal-Space-Engineer-Script\AGM\Minified_Tool\AGM_Minified.sln" -m -d "Autogrid Manager"
```

Always run after every source change. Never paste raw AGM.cs into SE.
After minifying, sync GitHub: Copy AGM.cs to GitHub\AGM.cs

Current minified size: ~96.7KB (limit 100KB).

---

## Current Version: 1.5

---

## Key Rules

- Edit Scripts/AGM.cs only -- never the minified Script.cs
- No static fields -- static methods only
- Plain ASCII only in string literals -- no em dashes, no smart quotes
- C# 6 only -- no inline out var, no string interpolation, no tuples
- monitor_only=false required for autocrafting
- [AGM-LIGHT] blocks excluded from _screens entirely
- DrawAlertLcds() every tick -- alert LCDs never flicker
- Always re-fetch disk file before patching -- never patch stale copy
- Validate brace balance before writing: open == close

---

## Draw System Rules

- All borders drawn on VP (screen edge) at 6f thickness -- never on panel
- PrepSurf has null check and try/catch
- VP() has null and zero-size guard -- fallback 512x512
- DrawScreen has null block guard and safe GetSurface
- DrawAlertLcds has per-entry try/catch -- bad entries auto-removed
- Corner LCD is BOTH IMyLightingBlock AND IMyTextSurfaceProvider -- never gate LCD registration on if(light==null)
- All Inset(vp, Xf) values use 10f for normal screens, 8f for corner LCD

---

## Autocrafting Rules

- monitor_only=false required in [Production]
- FindBpFor validates blueprints with CanUseBlueprint against real assemblers, caches result
- QueueToAllMasters queues to first available master only -- coop assemblers share automatically
- Assembler mode check before queuing -- switches Disassembly->Assembly if not producing
- DisassembleExcess skips items with assembly queued -- cannot fight autocrafting
- max_queue_amount default 5000, cap 100000
- max_queue_per_run default 5, cap 20

---

## Docked Grid Exclusion

- _dockedGridIds is HashSet<IMyCubeGrid> (not EntityId)
- Both connector sides added to _dockedGridIds on [No Sorting] match
- _dockedGridIds.Remove(Me.CubeGrid) -- base never self-excludes
- Block filter: b.CubeGrid==dg || b.CubeGrid.IsSameConstructAs(dg)
- [No Sorting] checked in connector CustomData AND CustomName
- Connector scan uses c.CubeGrid.EntityId == _myGridId to only check base connectors

---

## Item Categories

Ore, Ingot, Component, Ammo, Tool, Bottle -- original
Food (_ConsumableItem, _Consumable) -- v1.5
Seed (_TreeObject) -- v1.5
Ingredient (IsFoodIngredient check) -- v1.5

---

## Dashboard Commands

CoreDashboard, AlertDashboard, WarningDashboard
PowerDashboard page=N, ReactorRefuel, BatteryControl
LogisticsDashboard
ProductionDashboard page=N, ProductionDetails, ProductionWarnings
InventoryStock, OreStock, IngotStock, ComponentStock, AmmoStock, ToolStock, BottleStock
FoodStock, SeedStock, IngredientStock (v1.5)
Autocrafting page=N
FuelLifeSupport, LifeSupport

---

## Colour Theme

```
Background   new Color(1,8,13)
Panel        new Color(2,18,28)
Panel2/Rows  new Color(3,58,78)
Accent       new Color(38,239,255)
Accent2      new Color(112,247,255)
Text         new Color(126,246,255)
Dim          new Color(44,177,195)
OK           new Color(97,255,214)
Warning      new Color(255,202,34)
Error        new Color(255,79,66)
ProgBg       new Color(18,48,32)
ProgFill     new Color(255,204,36)
```

---

## What Is Next

- v1.6: CoreDashboard AGM family PB scan
- AGM Defence Grid: future separate PB
