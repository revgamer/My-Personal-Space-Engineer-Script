# AutoGrid Manager v1.5 Reference

AutoGrid Manager is a unified Space Engineers programmable block script by RevGamer.

Current version: **1.5**

---

## What Changed in v1.5

1. **Docked grid exclusion fixed** -- `[No Sorting]` in connector Custom Data OR block name now blocks that docked grid completely.
2. **Autocrafting mode fix** -- Assemblers left in Disassembly mode are switched back to Assembly before queuing. Root cause of autocrafting silently stopping.
3. **Disassembly logic fixed** -- Disassembly skips any component with active assembly queued. Cannot fight autocrafting.
4. **Food / Seed / Ingredient stock** -- Full support: sorting, cargo assignment, stock screens (`FoodStock`, `SeedStock`, `IngredientStock`).
5. **Cargo type from Custom Data** -- Type tags (e.g. `{Ore 1}`) now work in block Custom Data as well as block names.
6. `auto_disassemble=false` added to default config.

---

## Script File

```
Scripts/AGM.cs
```

## LCD Tag

```
[AGM-S]
```

Put in the LCD **block name**. Put a dashboard command in LCD **Custom Data**.

---

## Dashboard Commands

### Core
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

### Stock
```
InventoryStock page=1
OreStock page=1
IngotStock page=1
ComponentStock page=1
AmmoStock page=1
ToolStock page=1
BottleStock page=1
FoodStock page=1
SeedStock page=1
IngredientStock page=1
```

### Autocrafting and Fuel
```
Autocrafting page=1
FuelLifeSupport
LifeSupport
```

---

## Theme

```
Background   #01080D
Panel        #02121C
Row bg       #033A4E
Accent       #26EFFF
Accent text  #70F7FF
Text         #7EF6FF
Dim cyan     #2CB1C3
Progress bar #FFCC24
OK           #61FFD6
Warning      #FFCA22
Error        #FF4F42
```

---

## Build Steps

1. Edit `Scripts/AGM.cs`
2. Run minifier: `IngameScriptMergeTool.exe -s AGM_Minified.sln -m -d "Autogrid Manager"`
3. Deployed to: `IngameScripts\local\Autogrid Manager\Script.cs`

---

## Changelog

| Version | Notes |
|---------|-------|
| 1.5 | Docked grid fix; autocrafting mode fix; disassembly vs autocraft fix; Food/Seed/Ingredient; cargo type from Custom Data |
| 1.4 | Assembler details display; autocrafting regression (fixed in 1.5) |
| 1.3 | Production dashboard v2; unified script |
| 1.2 | Power dashboard v2; reactor refuel; battery automation |
| 1.1 | Alert dashboard; warning lights |
| 1.0 | Initial release |
